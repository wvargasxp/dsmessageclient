using System;
using System.Diagnostics;
using AudioToolbox;
using AVFoundation;
using em;
using Foundation;
using UIKit;

namespace iOS {
	public class iOSSoundRecordingRecorder : SoundRecordingRecorder {

		public const string RECORDING_STARTED = "SOUND_RECORDING_RECORDER_STARTED";
		public const string RECORDING_STOPPED = "SOUND_RECORDING_RECORDER_STOPPED";

		AVAudioRecorder recorder;

		NSUrl url;

		string audioFilePath;

		private Action<string> delegateDidSavedRecording =  (string s) => {};
		private Action delegateDidBeginFileExport =  () => {};
		private Action delegateDidFailFileExport =  () => {};

		private WeakDelegateProxy didSavedRecordingProxy;

		SoundRecordingRecorderState State { get; set; }

		public Action<string> DelegateDidSavedRecording {
			get {
				return this.delegateDidSavedRecording;
			}
			set {
				this.delegateDidSavedRecording = value;
			}
		}

		public Action DelegateDidBeginFileExport {
			get {
				return this.delegateDidBeginFileExport;
			}
			set {
				this.delegateDidBeginFileExport = value;
			}
		}

		public Action DelegateDidFailFileExport {
			get {
				return this.delegateDidFailFileExport;
			}
			set {
				this.delegateDidFailFileExport = value;
			}
		}

		public iOSSoundRecordingRecorder () {
			didSavedRecordingProxy = WeakDelegateProxy.CreateProxy<object, AVStatusEventArgs> (DidSaveRecordingEvent);
		}

		protected void DidSaveRecordingEvent(object sender , AVStatusEventArgs e) {
			try {
				if (!ApplicationModel.SharedPlatform.GetFileSystemManager ().FileExistsAtPath (this.audioFilePath)) {
					this.DelegateDidFailFileExport ();
					return;
				}

				if (!e.Status) {
					Debug.WriteLine ("iOSSoundRecordingRecorder: unable to save recording {0}", e);
					ApplicationModel.SharedPlatform.GetFileSystemManager ().RemoveFileAtPath (this.audioFilePath);
					this.DelegateDidFailFileExport ();
					return;
				}

				if (this.State == SoundRecordingRecorderState.Canceled) {
					Debug.WriteLine ("iOSSoundRecordingRecorder: canceling recording");
					ApplicationModel.SharedPlatform.GetFileSystemManager ().RemoveFileAtPath (this.audioFilePath);
					this.DelegateDidFailFileExport ();
					return;
				}

				Debug.WriteLine ("iOSSoundRecordingRecorder: successfully finished recording");
				this.DelegateDidSavedRecording (this.audioFilePath);
			} finally {
				this.recorder.FinishedRecording -= this.didSavedRecordingProxy.HandleEvent<object, AVStatusEventArgs>;
				this.recorder.Dispose ();
				this.audioFilePath = null;
				this.State = SoundRecordingRecorderState.Stopped;
			}
		}

		public void Record (string savePath) {
			RecordIfPermitted (savePath);
		}

		void RecordIfPermitted (string savePath) {
			AVAudioSession audioSession = AVAudioSession.SharedInstance ();
			audioSession.RequestRecordPermission ((bool granted) => EMTask.DispatchMain (() => {
				if (granted) {
					DoRecord (savePath);
					em.NotificationCenter.DefaultCenter.PostNotification (RECORDING_STARTED);
				} else {
					ShowPermissionDeniedAlert ();
				}
			}));
		}

		void DoRecord (string savePath) {
			if (this.State != SoundRecordingRecorderState.Stopped) {
				return;
			}

			this.State = SoundRecordingRecorderState.Playing;

			try {
				PrepareAndRecord (savePath);
			} catch (Exception e) {
				Debug.WriteLine ("iOSSoundRecordingRecorder: cannot begin recording: {0}", e);
				this.State = SoundRecordingRecorderState.Stopped;
			}
		}

		void PrepareAndRecord (string savePath) {
			AVAudioSession audioSession = AVAudioSession.SharedInstance ();
			NSError err = null;
			err = audioSession.SetCategory (AVAudioSessionCategory.PlayAndRecord);
			if (err != null) {
				throw new Exception (String.Format ("iOSSoundRecordingRecorder: {0}", err));
			}
			err = audioSession.SetActive (true);
			if(err != null ){
				throw new Exception (String.Format ("iOSSoundRecordingRecorder: {0}", err));
			}

			//Declare string for application temp path and tack on the file extension
			this.audioFilePath = savePath + ".aac";
			string systemPath = ApplicationModel.SharedPlatform.GetFileSystemManager ().ResolveSystemPathForUri (audioFilePath);

			Debug.WriteLine(String.Format ("iOSSoundRecordingRecorder : Audio File Path: {0}",systemPath));
			ApplicationModel.SharedPlatform.GetFileSystemManager ().CreateParentDirectories (systemPath);
			url = NSUrl.FromFilename (systemPath);

			var audioSettings = new AudioSettings();
			audioSettings.Format = AudioFormatType.MPEG4AAC;
			//Set recorder parameters

			NSError error;
			this.recorder = AVAudioRecorder.Create(url, audioSettings, out error);

			//Set Recorder to Prepare To Record
			this.recorder.FinishedRecording += this.didSavedRecordingProxy.HandleEvent<object, AVStatusEventArgs>;
			this.recorder.PrepareToRecord();
			this.recorder.Record();
		}

		static void ShowPermissionDeniedAlert () {
			var alert = new UIAlertView ("RECORDING_PERMISSIONS_TITLE".t (), "RECORDING_PERMISSIONS_EXPLAINATION".t (), null, "OK_BUTTON".t (), null);
			alert.Show ();
		}

		public void Stop () {
			if (this.recorder != null) {
				double audioSessionDuration = this.recorder.currentTime;
				if (audioSessionDuration < Constants.SOUND_RECORDING_MIN_RECORDING_DURATION_SECONDS) {
					Cancel ();
				} else {
					this.DelegateDidBeginFileExport ();
					this.recorder.Stop();
					em.NotificationCenter.DefaultCenter.PostNotification (RECORDING_STOPPED);
					AppDelegate.Instance.SetAudioSessionToRespectSilence ();
				}
			}
		}

		public void Cancel () {
			if (this.State != SoundRecordingRecorderState.Playing) {
				return;
			}

			if (this.recorder != null) {
				this.State = SoundRecordingRecorderState.Canceled;
				this.recorder.Stop ();
				em.NotificationCenter.DefaultCenter.PostNotification (RECORDING_STOPPED);
			}
		}

		public void Dispose () {
			if (this.recorder != null) {
				this.recorder.Dispose ();
				this.recorder = null;
			}
		}
	}
}