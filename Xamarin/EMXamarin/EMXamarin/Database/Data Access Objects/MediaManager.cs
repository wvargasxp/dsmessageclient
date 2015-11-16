namespace em {
	public class MediaManager {

		PlatformFactory platformFactory;

		public MediaManager (PlatformFactory platformFactory) {
			this.platformFactory = platformFactory;
		}

		public void ResolveState (Media media) {
			if (media.MediaState != MediaState.Unknown)
				return;
				
			string localPath = media.GetPathForUri (platformFactory);
			bool fileExists = platformFactory.GetFileSystemManager ().FileExistsAtPath (localPath);
			MediaState mediaState = fileExists ? MediaState.Present : MediaState.Absent;

			media.MediaState = mediaState;
		}

		public bool MediaOnFileSystem (Media media) {
			if (media == null)
				return false;

			ResolveState (media);
			switch (media.MediaState) {
			case MediaState.FailedUpload:
				return true;
			case MediaState.Present:
				return true;
			case MediaState.Uploading:
				return true;
			case MediaState.Encoding:
				return true;
			default:
				return false;
			}
		}

		public bool ShouldDownloadMedia (Media media) {
			if (media == null) {
				return false;
			}

			switch (media.MediaState) {
			case MediaState.Absent:
				return true;
			case MediaState.Unknown:
				return true;
			case MediaState.Uploading:
			case MediaState.Present:
			case MediaState.Downloading:
			case MediaState.FailedDownload:
			case MediaState.FailedUpload:
			case MediaState.Encoding:
			default:
				return false;
			}
		}
	}
}

