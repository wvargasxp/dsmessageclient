using System;
using System.Diagnostics;
using em;
using EMXamarin;
using Android.Media;
using System.Threading;
using System.Collections.Generic;

namespace Emdroid {
	public class AndroidSoundRecordingRecorder : SoundRecordingRecorder {

		private Action<string> delegateDidSavedRecording =  (string s) => {};
		private Action delegateDidBeginFileExport =  () => {};
		private Action delegateDidFailFileExport =  () => {};

		string audioFilePath;

		Android.Media.MediaRecorder mr;

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

		public AndroidSoundRecordingRecorder () {
		}

		private void DidSaveRecordingEvent () {
			try {
				if (!ApplicationModel.SharedPlatform.GetFileSystemManager ().FileExistsAtPath (this.audioFilePath)) {
					return;
				}

				if (this.State == SoundRecordingRecorderState.Canceled) {
					Debug.WriteLine ("AndroidSoundRecordingRecorder: canceling recording");
					DelegateDidFailFileExport ();
					ApplicationModel.SharedPlatform.GetFileSystemManager ().RemoveFileAtPath (this.audioFilePath);
					return;
				}

				double audioSessionDuration = AndroidSoundRecordingRecorder.GetAudioDuration (this.audioFilePath);
				if (audioSessionDuration < Constants.SOUND_RECORDING_MIN_RECORDING_DURATION_SECONDS) {
					DelegateDidFailFileExport ();
					ApplicationModel.SharedPlatform.GetFileSystemManager ().RemoveFileAtPath (this.audioFilePath);
					return;
				}

				Debug.WriteLine ("AndroidSoundRecordingRecorder: successfully finished recording");
				DelegateDidSavedRecording (this.audioFilePath);
			} finally {
				this.mr.Release ();
				this.audioFilePath = null;
				this.State = SoundRecordingRecorderState.Stopped;
			}

		}

		public void Record (string saveFile) {
			if (this.State != SoundRecordingRecorderState.Stopped) {
				return;
			}

			this.State = SoundRecordingRecorderState.Playing;

			try {
				PrepareAndRecord (saveFile);
			} catch (Exception e) {
				Debug.WriteLine ("AndroidSoundRecordingRecorder: cannot begin recording: {0}", e);
				this.State = SoundRecordingRecorderState.Stopped;
			}
		}

		private void PrepareAndRecord (string saveFile) {
			mr = new Android.Media.MediaRecorder ();

			this.audioFilePath = saveFile + ".3gpp";
			ApplicationModel.SharedPlatform.GetFileSystemManager ().CreateParentDirectories (this.audioFilePath);
			mr.SetOutputFile (this.audioFilePath);
			mr.SetAudioSource (Android.Media.AudioSource.Mic);
			mr.SetOutputFormat (Android.Media.OutputFormat.ThreeGpp);
			mr.SetAudioEncoder (Android.Media.AudioEncoder.Aac);
			mr.Prepare ();
			mr.Start();
			this.ShouldPollForUpdates = true;
			PollAndPostUpdatedAmplitudeChanges ();
		}

		private void PollAndPostUpdatedAmplitudeChanges () {
			EMTask.DispatchBackground (() => {
				while (true) {
					Thread.Sleep (5);
					lock (this.PollingLock) {
						if (this.ShouldPollForUpdates && mr != null && this.State == SoundRecordingRecorderState.Playing) {
							Dictionary<string, int> extra = new Dictionary<string, int> ();
							int lastMaxAmplitude = mr.MaxAmplitude;
							extra [Android_Constants.AndroidSoundRecordingRecorder_LastAmplitudeKey] = lastMaxAmplitude;
							NotificationCenter.DefaultCenter.PostNotification (null, Android_Constants.AndroidSoundRecordingRecorder_HasUpdatedAmplitude, extra);
						} else {
							break;
						}
					}
				}
			});
		}

        private object _pollingLock = new object ();
		private object PollingLock { get { return this._pollingLock; } set { this._pollingLock = value; } }

        private bool _shouldPollForUpdates = false;
        private bool ShouldPollForUpdates { get { return this._shouldPollForUpdates; } set { this._shouldPollForUpdates = value; } }

		public void Stop () {
			try {
				lock (this.PollingLock) {
					this.ShouldPollForUpdates = false;
				}
				mr.Stop ();
				DelegateDidBeginFileExport ();
			} catch (Exception e) {
				Debug.WriteLine ("AndroidSoundRecordingRecorder : error stopping MediaRecorder", e);
			} finally {
				DidSaveRecordingEvent ();
			}
		}

		public void Cancel () {
			if (this.State != SoundRecordingRecorderState.Playing) {
				return;
			}

			if (this.mr != null) {
				this.State = SoundRecordingRecorderState.Canceled;
				Stop ();
			}
		}

		public static double GetAudioDuration (string audioFile) {
			try {
				if (audioFile == null) {
					return 0;
				}

				MediaMetadataRetriever mmr = new MediaMetadataRetriever();
				mmr.SetDataSource (audioFile);
				String duration = mmr.ExtractMetadata(MediaMetadataRetriever.MetadataKeyDuration);
				return Convert.ToDouble (duration) / 1000d;
			} catch (Exception e) {
				Debug.WriteLine ("AndroidSoundRecordingRecorder: problem getting audio duration", e);
				return 0;
			}
		}

		public void Dispose () {
			if (this.mr != null) {
				this.mr.Dispose ();
				this.mr = null;
			}
		}
	}
}
 
