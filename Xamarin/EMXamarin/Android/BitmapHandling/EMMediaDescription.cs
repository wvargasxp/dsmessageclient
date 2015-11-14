using Com.EM.Android;
using em;

namespace Emdroid {
	public class EMMediaDescription : Java.Lang.Object, IMediaDescription {
		private Media media;
		private string mediaKey;
		private ApplicationModel appModel;
		private Media priorMedia;
		private string priorMediaKey;

		public EMMediaDescription () {}

		public EMMediaDescription (Media cMedia) {
			Initialize (cMedia, null);
		}

		public EMMediaDescription (Media cMedia, Media pMedia) {
			Initialize (cMedia, pMedia);
		}

		private void Initialize (Media currentMedia, Media priorMedia) {
			this.media = currentMedia;
			this.appModel = EMApplication.Instance.appModel;
			this.priorMedia = priorMedia;

			if (media != null) {
				string uriString = media.uri.ToString ();
				int indexOfLastSlash = uriString.LastIndexOf ('/');
				if (indexOfLastSlash == -1) {
					mediaKey = uriString;
				} else {
					mediaKey = uriString.Substring (indexOfLastSlash + 1);
				}
			}

			if (priorMedia != null) {
				string uriString = priorMedia.uri.ToString ();
				int indexOfLastSlash = uriString.LastIndexOf ('/');
				if (indexOfLastSlash == -1) {
					priorMediaKey = uriString;
				} else {
					priorMediaKey = uriString.Substring (indexOfLastSlash + 1);
				}
			}
		}

		public string LocalPathForTempThumbnail {
			get {
				string temp = appModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (media.tempPath);
				return temp;
			}
		}

		#region current media
		public string LocalPathForThumbnail {
			get {
				return media.GetPathForUri (appModel.platformFactory);
			}
		}

		public string LocalPathForScaledThumbnail (int height) {
			return media.GetPathForUriScaled (appModel, height);
		}

		public bool IsPresent {
			get {
				return appModel.mediaManager.MediaOnFileSystem (media);
			}
		}

		public bool IsDownloading {
			get {
				return media.MediaState == MediaState.Downloading;
			}
		}

		public void DownloadMediaIfPossible () {
			MediaManager manager = appModel.mediaManager;
			if (manager.ShouldDownloadMedia (media)) {
				media.DownloadMedia (appModel);
			}
		}

		public void FinishLoad() {
			media.DelegateDidFinisLoadMedia ();
		}

		public string MediaKey () {
			return mediaKey;
		}
		#endregion


		#region prior media
		public string LocalPathForPriorThumbnail {
			get {
				// these should only be called in a code path where prior media is present
				return priorMedia.GetPathForUri (appModel.platformFactory);
			}
		}

		public string LocalPathForPriorScaledThumbnail (int height) {
			// these should only be called in a code path where prior media is present
			return priorMedia.GetPathForUriScaled (appModel, height);
		}

		public bool IsPriorPresent {
			get {
				if (priorMedia == null)
					return false;
				return appModel.mediaManager.MediaOnFileSystem (priorMedia);
			}
		}

		public void PriorMediaFinishLoad () {
			// these should only be called in a code path where prior media is present
			priorMedia.DelegateDidFinisLoadMedia ();
		}

		public string PriorMediaKey () {
			return priorMediaKey;
		}
		#endregion

	}
}
