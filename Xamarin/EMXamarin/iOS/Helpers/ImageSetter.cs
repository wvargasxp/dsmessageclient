using System;
using System.Collections.Generic;
using CoreGraphics;
using em;
using EMXamarin;
using Media_iOS_Extension;
using UIKit;

namespace iOS {
	public static class ImageSetter {

		static Dictionary<string, WeakReference> frequentlyUsedImages = new Dictionary<string, WeakReference> ();
		static public Dictionary<string, WeakReference> Images {
			get {
				return frequentlyUsedImages;
			}
		}

		static Dictionary<string, UIImage> appImages = new Dictionary<string, UIImage> ();
		static public Dictionary<string, UIImage> AppImages {
			get { return appImages; }
		}

		#region image search
		static Dictionary<string, UIImage> searchImages = new Dictionary<string, UIImage> ();
		public static UIImage SearchImageForKey (string key) {
			if (key == null)
				return null;
			if (searchImages.ContainsKey (key))
				return searchImages [key];
			else
				return null;
		}

		public static void AddSearchImageToCache (string key, UIImage image) {
			if (!searchImages.ContainsKey (key))
				searchImages.Add (key, image);
		}

		public static void ClearSearchImageCache () {
			searchImages.Clear ();
		}
		#endregion

		static LinkedList<UIImage> recentlyUsedMediaImages = new LinkedList<UIImage> ();
		static LinkedList<UIImage> recentlyUsedThumbnailImages = new LinkedList<UIImage> ();
		readonly static int MAX_MEDIA_IMAGE_COUNT = 250;
		readonly static int MAX_THUMBNAIL_IMAGE_COUNT = 250;

		public static void ClearLRUCache () {
			recentlyUsedMediaImages.Clear ();
			recentlyUsedThumbnailImages.Clear ();
			searchImages.Clear ();
		}

		static void TrackUsageMediaImages (UIImage image) {
			EMTask.DispatchBackground (() => {
				lock (recentlyUsedMediaImages) {
					recentlyUsedMediaImages.Remove (image);
					recentlyUsedMediaImages.AddFirst (image);
					while (recentlyUsedMediaImages.Count > MAX_MEDIA_IMAGE_COUNT)
						recentlyUsedMediaImages.RemoveLast ();
				}
			});
		}

		static void TrackUsageThumbnailImages (UIImage image) {
			EMTask.DispatchBackground (() => {
				lock (recentlyUsedThumbnailImages) {
					recentlyUsedThumbnailImages.Remove (image);
					recentlyUsedThumbnailImages.AddFirst (image);
					while (recentlyUsedThumbnailImages.Count > MAX_THUMBNAIL_IMAGE_COUNT)
						recentlyUsedThumbnailImages.RemoveLast ();
				}
			});
		}

		public static UIImage GetResourceImage (string resourceImageString) {
			Dictionary<string, WeakReference> defaultImageMap = ImageSetter.Images;
			UIImage resourceImage = null;
			if (defaultImageMap.ContainsKey (resourceImageString)) {
				WeakReference imageRef = defaultImageMap [resourceImageString];
				if (imageRef.IsAlive)
					resourceImage = (UIImage)imageRef.Target;
				else {
					resourceImage = UIImage.FromFile (resourceImageString);
					imageRef.Target = resourceImage;
				}
			} else {
				resourceImage = UIImage.FromFile (resourceImageString);
				defaultImageMap [resourceImageString] = new WeakReference (resourceImage);
			}

			return resourceImage;
		}

		public static UIImage GetBundleImage (string resourceImageString) {
			Dictionary<string, WeakReference> defaultImageMap = ImageSetter.Images;
			UIImage resourceImage = null;
			if (defaultImageMap.ContainsKey (resourceImageString)) {
				WeakReference imageRef = defaultImageMap [resourceImageString];
				if (imageRef.IsAlive)
					resourceImage = (UIImage)imageRef.Target;
				else {
					resourceImage = UIImage.FromBundle (resourceImageString);
					imageRef.Target = resourceImage;
				}
			} else {
				resourceImage = UIImage.FromBundle (resourceImageString);
				defaultImageMap [resourceImageString] = new WeakReference (resourceImage);
			}

			return resourceImage;
		}

		public static void PreloadMediaListAsync (IList<Message> messageList) {
			EMTask.DispatchBackground (() => {
				if (messageList != null) {
					List<Message> copy = new List<Message> (messageList);
					copy.Reverse (); // reverse the list so we preload the last messages first
					foreach (Message message in copy) {
						if (message.HasMedia ()) {
							Media media = message.media;
							if (media != null) {
								UIImage image = media.PreloadThumbnailAsync ((UIImage preloadedImage) => {
									if (preloadedImage != null) {
										TrackUsageMediaImages (preloadedImage);
									}
								});

								if (image != null) {
									TrackUsageMediaImages (image);
								}
							}
						}
					}
				}
			});
		}

		public static void SetImage (Media media, Action<UIImage> hasImageCallback) {
			SetImage (media, media.GetDefaultResourceImage (), hasImageCallback);
		}

		public static void SetImage (Media media, string defaultResourceImage, Action<UIImage> hasImageCallback) {
			UIImage defaultImage = null;

			if (defaultResourceImage != null) {
				defaultImage = GetResourceImage (defaultResourceImage);
			}

			if (media == null) {
				hasImageCallback (defaultImage);
				return;
			}

			UIImage image = media.LoadThumbnail ();

			if (image == null) {
				hasImageCallback (defaultImage);
			} else {
				TrackUsageMediaImages (image);
				hasImageCallback (image);
			}
		}

		public static UIImage UseSoundWaveFormMask (BackgroundColor color, UIImage image) {
			CGRect rect = new CGRect (0, 0, image.Size.Width, image.Size.Height);
			CGRect paintRect = new CGRect (image.Size.Height, 0, image.Size.Width - image.Size.Height, image.Size.Height);
			UIGraphics.BeginImageContext (rect.Size);
			CGContext context = UIGraphics.GetCurrentContext ();
			context.SetFillColor (color.GetColor ().CGColor);
			context.FillRect (paintRect);
			UIImage bg = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();

			return bg.ApplyMask (image);
		}

		public static UIImage ApplyMask (this UIImage image, UIImage maskImage) {
			CGImage maskRef = maskImage.CGImage;
			CGImage mask = CGImage.CreateMask (
				(int) maskRef.Width, 
				(int) maskRef.Height, 
				(int) maskRef.BitsPerComponent, 
				(int) maskRef.BitsPerPixel, 
				(int) maskRef.BytesPerRow, 
				maskRef.DataProvider,
				null, false);

			CGImage maskedImageRef = image.CGImage.WithMask (mask);
			UIImage maskedImage = UIImage.FromImage (maskedImageRef);

			return maskedImage;
		}

		public static void SetAccountImage (CounterParty counterparty, UIImage defaultImage, Action<UIImage> hasImageCallback) {
			if (counterparty == null || counterparty.media == null) {
				hasImageCallback (defaultImage);
				return;
			}

			if (!AppDelegate.Instance.applicationModel.mediaManager.MediaOnFileSystem (counterparty.media)) {
				if (counterparty.media.MediaState != em.MediaState.Downloading)
					counterparty.media.DownloadMedia (AppDelegate.Instance.applicationModel);
			} else {
				UIImage image = counterparty.media.LoadThumbnail ();
				if (image != null) {
					hasImageCallback (image);
					return;
				}
			}

			if (counterparty.PriorMedia != null) {
				UIImage priorImage = counterparty.PriorMedia.LoadThumbnail ();
				if (priorImage != null) {
					defaultImage = priorImage;
				}
			}

			hasImageCallback (defaultImage);
		}
			
		public static void SetThumbnailImage (CounterParty counterparty, string defaultResourceImage, Action<UIImage> hasImageCallback) {
			UIImage defaultImage = null;

			if (defaultResourceImage != null) {
				defaultImage = GetResourceImage (defaultResourceImage);
			}

			if (counterparty == null || counterparty.media == null) {
				hasImageCallback (defaultImage);
				return;
			}

			MediaManager mediaManager = AppDelegate.Instance.applicationModel.mediaManager;
			Media media = counterparty.media;

			if (!mediaManager.MediaOnFileSystem (media)) {
				if (mediaManager.ShouldDownloadMedia (media)) {
					media.DownloadMedia (AppDelegate.Instance.applicationModel);
				}
			} else {
				UIImage image = media.LoadThumbnail ();
				if (image != null) {
					TrackUsageThumbnailImages (image);
					hasImageCallback (image);
					return;
				}
			}

			Media priorMedia = counterparty.PriorMedia;
			if (priorMedia != null) {
				UIImage priorImage = priorMedia.LoadThumbnail ();
				if (priorImage != null) {
					TrackUsageThumbnailImages (priorImage);
					defaultImage = priorImage;
				}
			}

			hasImageCallback (defaultImage);
		}

		public static void UpdateThumbnailFromMediaState (CounterParty c, BasicThumbnailView thumbnail) {
			MediaState currentMediaState = MediaState.Unknown;

			if (c == null || (c.media == null && c.PriorMedia == null)) {
				thumbnail.UpdateProgressIndicatorVisibility (false);
				thumbnail.ShowDebuggingInformation (true);
				return;
			}

			Media media = c.media;
			Media priorMedia = c.PriorMedia;
			MediaManager mediaManager = AppDelegate.Instance.applicationModel.mediaManager;

			if (media != null) {
				mediaManager.ResolveState (media);
				currentMediaState = media.MediaState;
			} else {
				if (priorMedia != null)
					currentMediaState = MediaState.Absent;
			}

			switch (currentMediaState) {
			case MediaState.Absent:
				{
					if (priorMedia != null) {
						if (mediaManager.MediaOnFileSystem (priorMedia)) {
							thumbnail.UpdateProgressIndicatorVisibility (false);
							thumbnail.ShowDebuggingInformation (true);
							return;
						}
					}

					thumbnail.UpdateProgressIndicatorVisibility (true);
					thumbnail.ShowDebuggingInformation (true);
					break;
				}
			case MediaState.Downloading:
				{
					if (priorMedia != null) {
						if (mediaManager.MediaOnFileSystem (priorMedia)) {
							thumbnail.UpdateProgressIndicatorVisibility (false);
							thumbnail.ShowDebuggingInformation (true);
							return;
						}
					}

					thumbnail.UpdateProgressIndicatorVisibility (true);
					thumbnail.ShowDebuggingInformation (true);
					break;
				}
			case MediaState.Present:
				{
					thumbnail.UpdateProgressIndicatorVisibility (false);
					thumbnail.ShowDebuggingInformation (true);
					break;
				}
			case MediaState.FailedDownload:
				{
					thumbnail.UpdateProgressIndicatorVisibility (false);
					thumbnail.ShowDebuggingInformation (false);
					break;
				}
			default:
				{
					thumbnail.UpdateProgressIndicatorVisibility (false);
					thumbnail.ShowDebuggingInformation (false);
					break;
				}
			}
		}

		public static bool ShouldShowAccountThumbnailFrame (CounterParty c) {
			MediaState currentMediaState = MediaState.Unknown;

			if (c == null || (c.media == null && c.PriorMedia == null)) {
				return false;
			}

			Media media = c.media;
			Media priorMedia = c.PriorMedia;
			MediaManager mediaManager = AppDelegate.Instance.applicationModel.mediaManager;

			if (media != null) {
				mediaManager.ResolveState (media);
				currentMediaState = media.MediaState;
			} else {
				if (priorMedia != null)
					currentMediaState = priorMedia.MediaState;
			}

			if (currentMediaState != MediaState.Present)
				return false;
			else
				return true;
		}

		public static bool ShouldShowProgressIndicator (CounterParty c) {
			MediaState currentMediaState = MediaState.Unknown;

			if (c == null || (c.media == null && c.PriorMedia == null)) {
				return false;
			}

			Media media = c.media;
			Media priorMedia = c.PriorMedia;
			MediaManager mediaManager = AppDelegate.Instance.applicationModel.mediaManager;

			if (media != null) {
				mediaManager.ResolveState (media);
				currentMediaState = media.MediaState;
			} else {
				if (priorMedia != null)
					currentMediaState = MediaState.Absent;
			}

			switch (currentMediaState) {
			case MediaState.Absent:
				{
					if (priorMedia != null) {
						if (mediaManager.MediaOnFileSystem (priorMedia)) {
							return false;
						}
					}

					return true;
				}
			case MediaState.Downloading:
				{
					if (priorMedia != null) {
						if (mediaManager.MediaOnFileSystem (priorMedia)) {
							return false;
						}
					}

					return true;
				}
			case MediaState.Present:
				{
					return false;
				}
			case MediaState.FailedDownload:
				{
					return false;
				}
			default:
				{
					return false;
				}
			}
		}
	}
}