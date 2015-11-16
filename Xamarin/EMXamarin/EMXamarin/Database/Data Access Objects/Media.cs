using System;
using System.Collections.Generic;

namespace em {
	public class Media {
		public static readonly String MEDIA_WILL_START_DOWNLOAD = "MEDIA_WILL_START_DOWNLOAD";
		public static readonly string MEDIA_DID_DOWNLOAD_PERCENT = "MEDIA_DID_DOWNLOAD_PERCENT";
		public static readonly string MEDIA_DID_DOWNLOAD_PERCENT_KEY = "MEDIA_DID_DOWNLOAD_PERCENT_KEY";
		public static readonly string MEDIA_DID_COMPLETE_DOWNLOAD = "MEDIA_DID_COMPLETE_DOWNLOAD";
		public static readonly string MEDIA_DID_COMPLETE_DOWNLOAD_LOCAL_PATH = "MEDIA_DID_COMPLETE_DOWNLOAD_LOCAL_PATH";
		public static readonly string MEDIA_DID_UPDATE_IMAGE_RESOURCE = "MEDIA_DID_UPDATE_IMAGE_RESOURCE";

		// The max number of media downloads that we allow amongst the 'throttled' media objects
		public static readonly int MAX_CONCONCURRENT_THROTTLED_MEDIA_DOWNLOADS = 7;

		// Most Media should partake in download throttling so we don't overwhelm the CPU
		// this flag allows some media (such as counterparty thumbnails) to skip throttling
		// so they don't get enqued behind large downloads like a video.
		public bool ThrottleDownload { get; set; }

		WeakReference RefToThumbnail;
		WeakReference RefToFullSizeImage;
		public string tempPath = null;

		Uri theUri;
		public Uri uri {
			get {
				return theUri;
			}

			set {
				bool clearRefs = false;
				if (value == null) {
					if (theUri != null) {
						clearRefs = true;
						theUri = value;
					}
				}
				else {
					if (theUri == null || !value.Equals (theUri)) {
						clearRefs = true;
						theUri = value;
					}
				}

				if (clearRefs) {
					RefToThumbnail = new WeakReference(null);
					RefToFullSizeImage = new WeakReference(null);
				}
			}
		}

		ContentType ct = ContentType.Unknown;
		public ContentType contentType { 
			get { return ct; } 
			set { ct = value; }
		}

		public Func<Uri,string> LocalPathFromUriFunc { get; set; }
		public Func<string> UploadPathFromUriFunc { get; set; }

		public delegate void DidFinisLoadMedia ();
		public DidFinisLoadMedia DelegateDidFinisLoadMedia = () => {};

		public delegate void WillStartDownload ();
		public delegate void DidDownloadPercentage(double perc);
		public delegate void DidCompleteDownload(String localpath);

		public delegate void DidChangeSoundState ();

		public WillStartDownload BackgroundDelegateWillStartDownload = () => { };
		public DidDownloadPercentage BackgroundDelegateDidDownloadPercentage = (double perc) => {};
		public DidCompleteDownload BackgroundDelegateDidCompleteDownload = (string localPath) => {};
		public DidChangeSoundState BackgroundDelegateDidChangeSoundState = () => { };
			
		double uploadingOrDownloadingPercentage = 0;
		public double Percentage {
			get { return uploadingOrDownloadingPercentage; }
			set { uploadingOrDownloadingPercentage = value; }
		}

		object nativeThumbnail = null;
		public object NativeThumbnail {
			get { return nativeThumbnail; }
			set { nativeThumbnail = value; }
		}

		MediaState mediaState = MediaState.Unknown;
		public MediaState MediaState {
			get {
				return mediaState;
			}
			set {
				mediaState = value;
			}
		}

		public T GetNativeThumbnail<T> () {
			return (T)nativeThumbnail;
		}

		public void SetNativeThumbnail<T> (T thumb) {
			nativeThumbnail = thumb;
		}

		private static Dictionary<string, Media> _mediaCache = null;
		private static Dictionary<string, Media> MediaCache {
			get {
				if (_mediaCache == null) {
					_mediaCache = new Dictionary<string, Media> ();
				}

				return _mediaCache;
			}
		}

		// A guid field that's associated with the media. Can be null if media
		// not associated with message. Used to determine alternate path to look
		// for media file in.
		public string GUID;

		private static object _mediaCacheLock = new object ();

		public static Media FindOrCreateMedia (Uri uri) {
			if (uri == null) {
				return null;
			}

			lock (_mediaCacheLock) {
				string key = uri.AbsoluteUri;
				if (MediaCache.ContainsKey (key)) {
					return MediaCache [key];
				} else {
					Media media = new Media (uri);
					MediaCache.Add (key, media);
					return media;
				}
			}
		}

		private Media (Uri url) {
			uri = url;
			RefToThumbnail = new WeakReference(null);
			RefToFullSizeImage = new WeakReference(null);
			LocalPathFromUriFunc = null;
			UploadPathFromUriFunc = null;
			ThrottleDownload = true;
		}

		public T GetThumbnail<T>() {
			if (RefToThumbnail.IsAlive) {
				var t = (T) RefToThumbnail.Target;
				return t;
			}
				
			return default(T);
		}

		public void SetThumbnail<T>(T newVal) {
			RefToThumbnail.Target = newVal;
		}

		public T GetFullSizeImage<T>() {
			if (RefToFullSizeImage.IsAlive) {
				var t = (T) RefToFullSizeImage.Target;
				return t;
			}

			return default(T);
		}

		public void SetFullSizeImage<T>(T newVal) {
			RefToFullSizeImage.Target = newVal;
		}

		public bool IsLocal() {
			string path = uri.AbsolutePath;
			return path.StartsWith ("file:");
		}

		private MediaSoundState soundState;
		public MediaSoundState SoundState {
			get {
				return this.soundState;
			}
			set {
				this.soundState = value;
				this.BackgroundDelegateDidChangeSoundState ();
			}
		}

		public void DownloadMedia(ApplicationModel applicationModel) {
			string absolute = uri.AbsoluteUri;
			if ( absolute.StartsWith("http") || absolute.StartsWith ("https"))
				ScheduleDownloadAsync (this, applicationModel);
		}

		void DoDownloadMedia(ApplicationModel applicationModel) {
			IFileSystemManager fsm = ApplicationModel.SharedPlatform.GetFileSystemManager ();
			string notificationPath = fsm.GetFilePathForNotificationCenterMedia (this.GUID);
			// First, check to see if there's a file left from GCM notification
			if (fsm.FileExistsAtPath (notificationPath)) {
				this.GetFromPath (notificationPath);
			} else {
				applicationModel.account.httpClient.SendMediaRequestAsync (this);
			}
		}

		void GetFromPath (string notificationPath) {
			IFileSystemManager fsm = ApplicationModel.SharedPlatform.GetFileSystemManager ();
			string finalPath = this.GetPathForUri (ApplicationModel.SharedPlatform);
			fsm.MoveFileAtPath (notificationPath, finalPath);
			this.mediaState = MediaState.Present;
			this.BackgroundDelegateDidCompleteDownload (finalPath);
		}

		public void BackgroundDownloadStartEvent () {
			BackgroundDelegateWillStartDownload();
			NotificationCenter.DefaultCenter.PostNotification(this, MEDIA_WILL_START_DOWNLOAD);
		}

		public void BackgroundDownloadProgressEvent (double compPerc) {
			if (compPerc - Percentage >= 0.01) {
				Percentage = compPerc;
				BackgroundDelegateDidDownloadPercentage(compPerc);

				var extra = new Dictionary<string,double>();
				extra[MEDIA_DID_DOWNLOAD_PERCENT_KEY] = compPerc;
				NotificationCenter.DefaultCenter.PostNotification(this, MEDIA_DID_DOWNLOAD_PERCENT, extra);
			}
		}

		public void BackgroundDownloadFinishEvent (string path, ApplicationModel applicationModel) {
			ClearMediaRefs ();
			MediaState = MediaState.Present;
			BackgroundDelegateDidCompleteDownload (path);

			var pathDetails = new Dictionary<string,string>();
			pathDetails[MEDIA_DID_COMPLETE_DOWNLOAD_LOCAL_PATH] = path;
			NotificationCenter.DefaultCenter.PostNotification(this, MEDIA_DID_COMPLETE_DOWNLOAD, pathDetails);

			ScheduleNextDownloadAsync (applicationModel);
		}

		public void BackgroundDownloadErrorEvent (ApplicationModel applicationModel, bool canRetryDownload) {
			// If canRetryDownload is true, the error event must have been something recoverable, so we set the media to absent so that it can be queued up as a download again.
			// Otherwise, an unrecoverable error occured, and we mark it as FailedDownload. On next session start, the media file can go through another download attempt.
			if (canRetryDownload) {
				this.MediaState = MediaState.Absent;
			} else {
				this.MediaState = MediaState.FailedDownload;
			}

			NotificationCenter.DefaultCenter.PostNotification (this, Constants.Media_DownloadFailed);
			ScheduleNextDownloadAsync (applicationModel);
		}

		public void BackgroundImageResourceUpdatedEvent () {
			NotificationCenter.DefaultCenter.PostNotification (this, MEDIA_DID_UPDATE_IMAGE_RESOURCE);
		}
			
		public string GetPathForUriScaled(ApplicationModel applicationModel, int height) {
			string path = GetPathForUri (applicationModel.platformFactory);

			return applicationModel.uriGenerator.GetFilePathForScaledMedia (path, height);
		}

		private Nullable<bool> uploadFileExists = null;

		public string GetPathForUri(PlatformFactory platformFactory) {
			// if not a message media object
			if (UploadPathFromUriFunc == null && LocalPathFromUriFunc == null) {
				return GetDownloadPath (platformFactory);
			}

			string uploadPath = GetUploadPath (platformFactory);

			if (uploadFileExists == null) {
				IFileSystemManager fileManager = platformFactory.GetFileSystemManager ();
				uploadFileExists = fileManager.FileExistsAtPath (uploadPath);
			}

			if (uploadFileExists.Value) {
				return uploadPath;
			}

			return GetDownloadPath (platformFactory);
		}

		public string GetDownloadPath (PlatformFactory platformFactory) {
			string path = LocalPathFromUriFunc != null ? LocalPathFromUriFunc (uri) : platformFactory.GetUriGenerator ().GetCachedFilePathForUri (uri);
			return platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (path);
		}

		public string GetUploadPath (PlatformFactory platformFactory) {
			string path = UploadPathFromUriFunc != null ? UploadPathFromUriFunc () : null;
			return platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (path);
		}

		static IList<Media> pending = new List<Media> ();
		static int semaphore = MAX_CONCONCURRENT_THROTTLED_MEDIA_DOWNLOADS;

		static void ScheduleDownloadAsync (Media media, ApplicationModel applicationModel) {
			// Always kick off a new task as ScheduleDownload blocks on a lock. We don't want to block any thread from continuing.
			EMTask.DispatchBackground (() => BackgroundScheduleDownload (media, applicationModel));
		}

		static void BackgroundScheduleDownload (Media media, ApplicationModel applicationModel) {
			// we limit the number of concurrent media downloads
			// this way if we get a bunch of downloads we don't
			// try to kick off all of them at once.
			bool doDownload = false;

			lock (pending) {
				// resolve media file state if unresolved
				applicationModel.mediaManager.ResolveState (media);

				// ignore this request if we are already downloading or if the media is downloaded
				if (ShouldIgnoreDownloadRequest (media))
					return;

				if (!media.ThrottleDownload) {
					doDownload = true;
					media.MediaState = MediaState.Downloading;
				} else {
					// we limit the total number of concurrent downloads
					if (semaphore == 0) {
						// ignore this request if it's already in the queue
						if (!pending.Contains (media))
							pending.Add (media);
					} else {
						media.MediaState = MediaState.Downloading;
						semaphore--;
						doDownload = true;
					}
				}
			}

			if (doDownload) {
				media.DoDownloadMedia (applicationModel);
			}
		}

		static void ScheduleNextDownloadAsync (ApplicationModel applicationModel) {
			EMTask.DispatchBackground (() => BackgroundScheduleNextDownload (applicationModel));
		}

		static void BackgroundScheduleNextDownload (ApplicationModel applicationModel) {
			Media media = null;
			lock (pending) {
				semaphore++;
				if ( pending.Count > 0 ) {
					// if there's queued up messages to download we kick it off now.
					media = pending [0];
					pending.RemoveAt (0);
				}
			}

			if (media != null)
				ScheduleDownloadAsync (media, applicationModel);
		}

		static bool ShouldIgnoreDownloadRequest (Media media) {
			MediaState mediaState = media.MediaState;
			if (mediaState != MediaState.Absent) {
				switch (mediaState) {
				case MediaState.Downloading:
					return true;
				case MediaState.Uploading:
					return true;
				case MediaState.Encoding:
					return true;
				case MediaState.FailedDownload:
					return true;
				case MediaState.FailedUpload:
					return true;
				case MediaState.Present:
					return true;
				default:
					return false;
				}
			}

			return false;
		}

		public void TryMigrateUploadFileToFinalPath () {
			EMTask.DispatchBackground (() => {
				PlatformFactory pf = ApplicationModel.SharedPlatform;
				string uploadPath = GetUploadPath (pf);
				string finalPath = GetDownloadPath (pf);

				if (uploadPath == null || finalPath == null) {
					return;
				}

				IFileSystemManager fileManager = pf.GetFileSystemManager ();

				if (!fileManager.FileExistsAtPath (uploadPath)) {
					return;
				}

				if (fileManager.FileExistsAtPath (finalPath)) {
					return;
				}

				if (!uploadPath.Equals (finalPath)) {
					fileManager.CopyFileAtPath (uploadPath, finalPath);
				}
			});
		}

		public void ClearMediaRefs () {
			// when media is finished downloading, the view updates, we don't want to pull from the reference we currently have
			NativeThumbnail = null; 
			RefToFullSizeImage.Target = null;
			RefToThumbnail.Target = null;
		}
	}

}