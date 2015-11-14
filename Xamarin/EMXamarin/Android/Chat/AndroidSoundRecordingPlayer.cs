using System;
using System.Diagnostics;
using em;
using EMXamarin;
using Android.Media;

namespace Emdroid {
	public class AndroidSoundRecordingPlayer : SoundRecordingPlayer {

		private MediaPlayerFactory factory;

		private MediaPlayer mediaPlayer;

		private Action delegateDidFinishedPlaying =  () => {};

		private WeakDelegateProxy didFinishPlayingProxy;

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

		public AndroidSoundRecordingPlayer () {
			didFinishPlayingProxy = WeakDelegateProxy.CreateProxy<object, EventArgs> (DidFinishPlaying);
			factory = new MediaPlayerFactory ();
		}

		public void CreateIfNotExistAVAudioPlayer (string pathToSoundFile) {
			if (this.mediaPlayer != null) {
				this.mediaPlayer.Completion -= didFinishPlayingProxy.HandleEvent<object, EventArgs>;
				this.mediaPlayer.Release ();
			}

			factory.PathToSoundFile = pathToSoundFile;
			this.mediaPlayer = factory.build ();
			this.mediaPlayer.Completion += didFinishPlayingProxy.HandleEvent;
		}

		public void Play (String pathToSoundFile) {
			try {
				this.CreateIfNotExistAVAudioPlayer (pathToSoundFile);
				this.mediaPlayer.Prepare ();
				this.mediaPlayer.Start ();
				this.State = SoundRecordingPlayerState.Active;
			} catch (Exception e) {
				Debug.WriteLine ("AndroidSoundRecordingPlayer: unable to play recording " + pathToSoundFile + ". Caused by " + e);
				this.State = SoundRecordingPlayerState.Inactive;
				this.DelegateDidFinishedPlaying ();
			}
		}

		public void Stop () {
			if (mediaPlayer == null) {
				return;
			}

			mediaPlayer.Stop ();
			this.State = SoundRecordingPlayerState.Inactive;
		}

		public void DidFinishPlaying(object sender, EventArgs e) {
			this.State = SoundRecordingPlayerState.Inactive;
			this.DelegateDidFinishedPlaying ();
		}

		private class MediaPlayerFactory {
			public String PathToSoundFile {
				get;
				set;
			}

			public Android.Media.MediaPlayer build () {
				MediaPlayer mediaPlayer = new MediaPlayer();
				mediaPlayer.SetDataSource(this.PathToSoundFile);

				return mediaPlayer;
			}
		}

		public void Dispose () {
			if (this.mediaPlayer != null) {
				this.mediaPlayer.Completion -= didFinishPlayingProxy.HandleEvent;
				this.mediaPlayer.Release ();
			}
		}
	}
}

