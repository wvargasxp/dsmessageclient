using System;
using System.Diagnostics;

namespace em {
	public class SoundRecordingInlineController : IDisposable {

		private SoundRecordingPlayer player;

		private Media currentMedia;

		public WeakDelegateProxy didFinishedPlayingProxy;

		public SoundRecordingInlineController () {
			player = ApplicationModel.SharedPlatform.GetSoundRecordingPlayer ();
			didFinishedPlayingProxy =  WeakDelegateProxy.CreateProxy (DidFinishedPlaying);
			player.DelegateDidFinishedPlaying += didFinishedPlayingProxy.HandleEvent;

			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ENTERING_BACKGROUND, WillEnterBackgroundEvent);
		}

		public void DidFinishedPlaying () {
			if ( this.currentMedia != null) {
				this.currentMedia.SoundState = MediaSoundState.Stopped;
			}
		}

		public void DidTapMediaButton (Media mediaTapped) {
			switch (this.player.State) {
			default:
			case SoundRecordingPlayerState.Inactive:
				DidTapPlayerStopped (mediaTapped);
				break;
			case SoundRecordingPlayerState.Active:
				DidTapPlayerPlaying (mediaTapped);
				break;
			}
		}

		public void DidTapPlayerStopped (Media mediaTapped) {
			if (!ApplicationModel.SharedPlatform.GetFileSystemManager ().FileExistsAtPath (mediaTapped.GetPathForUri (ApplicationModel.SharedPlatform))) {
				return;
			}

			Play (mediaTapped);
		}

		public void DidTapPlayerPlaying (Media mediaTapped) {
			Stop ();

			bool shouldPlayMedia = this.SelectedNewSoundMedia (mediaTapped);
			if (shouldPlayMedia) {
				Play (mediaTapped);
			}
		}

		public void Stop () {
			player.Stop ();

			if (this.currentMedia != null) {
				this.currentMedia.SoundState = MediaSoundState.Stopped;
			}
		}

		private void Play (Media mediaToPlay) {
			if (mediaToPlay == null) {
				throw new ArgumentException ("media to play shold not be null");
			}

			this.currentMedia = mediaToPlay;
			player.Play (this.currentMedia.GetPathForUri (ApplicationModel.SharedPlatform));
			if (player.State == SoundRecordingPlayerState.Active) {
				this.currentMedia.SoundState = MediaSoundState.Playing;
			}
		}

		private bool SelectedNewSoundMedia (Media mediaToPlay) {
			if (mediaToPlay == null) {
				throw new ArgumentException ("media to play shold not be null");
			}

			if (this.currentMedia == null) {
				return true;
			}

			string currentFilePath = this.currentMedia.GetPathForUri (ApplicationModel.SharedPlatform);
			string newFilePath = mediaToPlay.GetPathForUri (ApplicationModel.SharedPlatform);

			return currentFilePath != newFilePath;
		}

		private void WillEnterBackgroundEvent (Notification notif) {
			Stop ();
		}

		~SoundRecordingInlineController () {
			Dispose (false);
		}

		public void Dispose () {
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		bool hasDisposed = false;
		protected virtual void Dispose (bool disposing) {
			lock (this) {
				if (!hasDisposed) {

					if (disposing) {
						// Free other state (managed objects).
					}

					// Free your own state (unmanaged objects).
					// Set large fields to null.
					NotificationCenter.DefaultCenter.RemoveObserverAction (WillEnterBackgroundEvent);
					player.DelegateDidFinishedPlaying -= didFinishedPlayingProxy.HandleEvent;
					player.Dispose ();
					player = null;

					hasDisposed = true;
				}
			}
		}

		public bool HasNotDisposed() {
			lock (this) {
				return !hasDisposed;
			}
		}
	}
}

