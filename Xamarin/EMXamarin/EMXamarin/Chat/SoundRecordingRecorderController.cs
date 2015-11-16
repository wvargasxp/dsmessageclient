using System;
using em;
using EMXamarin;
using System.IO;

namespace em {
	public class SoundRecordingRecorderController {
		
		private SoundRecordingRecorder recorder;

		private WeakDelegateProxy didSavedRecordingProxy;
		private WeakDelegateProxy didBeginFileExportProxy;
		private WeakDelegateProxy didFailFileExportProxy;

		public Action<string> OnFinishRecordingSuccess { set; get; }

		public SoundRecordingRecorderController () {
			recorder = ApplicationModel.SharedPlatform.GetSoundRecordingRecorder ();
			didSavedRecordingProxy =  WeakDelegateProxy.CreateProxy<string> (DidSavedRecording);
			didBeginFileExportProxy =  WeakDelegateProxy.CreateProxy (DidFileExportBegin);
			didFailFileExportProxy =  WeakDelegateProxy.CreateProxy (DidFileExportFail);

			recorder.DelegateDidSavedRecording += didSavedRecordingProxy.HandleEvent<string>;
			recorder.DelegateDidBeginFileExport += didBeginFileExportProxy.HandleEvent;
			recorder.DelegateDidFailFileExport += didFailFileExportProxy.HandleEvent;
		}

		private void DidSavedRecording (string saveFile) {
			OnFinishRecordingSuccess (saveFile);
		}

		public void Record () {
			string path = ApplicationModel.SharedPlatform.GetUriGenerator ().GetNewMediaFileNameForStagingContents ();
			recorder.Record (path);
		}

		public void Finish () {
			recorder.Stop ();
		}

		public void Cancel () {
			recorder.Cancel ();
		}

		public void StageFromFile (string dest) {
			DidSavedRecording (dest);
		}

		private void DidFileExportBegin () {
			NotificationCenter.DefaultCenter.PostNotification (Constants.STAGE_MEDIA_BEGIN);
		}

		private void DidFileExportFail () {
			NotificationCenter.DefaultCenter.PostNotification (Constants.STAGE_MEDIA_DONE);
		}

		~SoundRecordingRecorderController () {
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
					recorder.DelegateDidSavedRecording -= didSavedRecordingProxy.HandleEvent<string>;
					recorder.DelegateDidBeginFileExport -= didBeginFileExportProxy.HandleEvent;
					recorder.DelegateDidFailFileExport -= didFailFileExportProxy.HandleEvent;
					this.recorder.Dispose ();
					this.recorder = null;

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

