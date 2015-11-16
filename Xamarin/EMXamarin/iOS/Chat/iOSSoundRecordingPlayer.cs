using System;
using System.Diagnostics;
using CoreMedia;
using AVFoundation;
using Foundation;
using em;
using EMXamarin;

namespace iOS {
	public class iOSSoundRecordingPlayer : NSObject, SoundRecordingPlayer, IDisposable {

		private const int SOUND_RECORDING_PLAYER_AUDIO_SESSION_ID = 1;

		NSObject audioPlayerObserver;

		AVPlayer avPlayer;

		private Action delegateDidFinishedPlaying =  () => {};

		public Action DelegateDidFinishedPlaying {
			get {
				return this.delegateDidFinishedPlaying;
			}
			set {
				this.delegateDidFinishedPlaying = value;
			}
		}

		public SoundRecordingPlayerState State {
			get;
			set;
		}

		public iOSSoundRecordingPlayer () {
		}

		public void Play (String pathToSoundFile) {
			try {
				CreateIfNotExistAVPlayer ();
				PrepareAudioSessionForUse ();
				PrepareAudioFileForUse (pathToSoundFile);
				AttemptPlayAudio ();

				this.State = SoundRecordingPlayerState.Active;
			} catch (Exception e) {
				Debug.WriteLine ("iOSSoundRecordingPlayer: unable to play recording " + pathToSoundFile + ". Caused by " + e);
				this.State = SoundRecordingPlayerState.Inactive;
				BroadcastPlayerFinished ();
			}
		}

		private void PrepareAudioSessionForUse () {
			AudioSessionHelper.SharedInstance ().PrepareAudioSessionForPlayback (SOUND_RECORDING_PLAYER_AUDIO_SESSION_ID);
		}

		private void PrepareAudioFileForUse (String pathToSoundFile) {
			var soundurl = NSUrl.FromFilename(pathToSoundFile);
			AVAsset asset = AVAsset.FromUrl (soundurl);
			AVPlayerItem item = new AVPlayerItem (asset);
			this.avPlayer.ReplaceCurrentItemWithPlayerItem (item);
		}

		/**
		 * The AVPlayer will play immediately if it's ready, else play when ready.
		 */
		private void AttemptPlayAudio () {
			if (this.avPlayer.Status == AVPlayerStatus.ReadyToPlay) {
				this.avPlayer.Play ();
			}
		}

		public void Stop () {
			try {
				if (this.avPlayer == null) {
					return;
				}

				this.avPlayer.Pause ();
			} finally {
				this.State = SoundRecordingPlayerState.Inactive;
				BroadcastPlayerFinished ();
			}
		}

		private void CreateIfNotExistAVPlayer () {
			if (this.avPlayer != null) {
				return;
			}

			this.avPlayer = new AVPlayer ();
			RegisterObservers ();
		}

		private void RegisterObservers () {
			this.avPlayer.AddObserver (this, new NSString ("status"), NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New, IntPtr.Zero);
			this.audioPlayerObserver = NSNotificationCenter.DefaultCenter.AddObserver (AVPlayerItem.DidPlayToEndTimeNotification, HandleAVPlayerStop);
		}

		private void DeregisterObservers () {
			this.avPlayer.RemoveObserver (this, new NSString ("status"));
			NSNotificationCenter.DefaultCenter.RemoveObserver (this.audioPlayerObserver);
		}

		public override void ObserveValue (NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context) {
			switch (this.avPlayer.Status) {
			case AVPlayerStatus.ReadyToPlay:
				this.avPlayer.Play ();
				break;
			case AVPlayerStatus.Failed:
				BroadcastPlayerFinished ();
				break;
			}
		}

		private void HandleAVPlayerStop (NSNotification notification) {
			if (this.State == SoundRecordingPlayerState.Active) {
				BroadcastPlayerFinished ();
			}
		}

		private void BroadcastPlayerFinished () {
			this.State = SoundRecordingPlayerState.Inactive;

			AudioSessionHelper.SharedInstance ().ReturnAudioSessionToDefault (SOUND_RECORDING_PLAYER_AUDIO_SESSION_ID);

			this.DelegateDidFinishedPlaying ();
		}

		public new void Dispose () {
			if (this.avPlayer == null) {
				return;
			}

			DeregisterObservers ();
			this.avPlayer = null;
		}
	}
}

