using System;
using System.Collections.Generic;
using System.Diagnostics;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Com.EM.Android;
using em;
using EMXamarin;

namespace Emdroid {
	public static class BitmapSetter {

		public static int MaxHeightForMediaInPixels (Media media, float heightToWidth = 1.1f /* portrait default */) {
			int heightInDp = 0;
			if (media != null) {
				if (ContentTypeHelper.IsAudio(media.contentType) || ContentTypeHelper.IsAudio (media.uri.AbsolutePath)) {
					heightInDp = Android_Constants.AUDIO_WAVEFORM_THUMBNAIL_HEIGHT;
				} else {
					if (heightToWidth > 1.0f) {
						heightInDp = Android_Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT;
					} else {
						heightInDp = Android_Constants.LANDSCAPE_CHAT_THUMBNAIL_WIDTH;
					}
				}
			}

			int heightInPixels = heightInDp.DpToPixelUnit ();
			return heightInPixels;
		}

		public static void PreloadMediaListAsync (Resources resources, IList<Message> messageList) {
			EMTask.DispatchBackground (() => {
				if (messageList != null) {
					List<Message> copy = new List<Message> (messageList);
					HashSet<Contact> contacts = new HashSet<Contact> ();
					copy.Reverse (); // reverse the list so we preload the last messages first

					foreach (Message message in copy) {
						Contact fromContact = message.fromContact;
						if (fromContact != null) {
							if (!contacts.Contains (fromContact)) {
								BitmapSetter.SetThumbnailImage (null, fromContact, resources, null, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
								contacts.Add (fromContact);
							}
						}

						// Don't have to preload media images as they are loaded from the background.
						// Can delete this code whenever.
						continue;
						if (message.HasMedia ()) {
							ContentType type = ContentTypeHelper.FromMessage(message);
							// Don't preload audio media, we don't have the color theme available yet.
							bool isAudioMedia = ContentTypeHelper.IsAudio (type);

							Media media = message.media;

							if (media != null && !isAudioMedia) {
								int maxHeightInPixels = MaxHeightForMediaInPixels (media, message.heightToWidth);
								EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
								if (bitmapWrapper == null) {
									bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
									media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
								}

								try {
									bitmapWrapper.PreloadMediaImage (maxHeightInPixels, BackgroundColor.Default.GetColor ());
								} catch (Exception e) {
									Debug.WriteLine ("INFO:SetThumbnailImage:" + e);
									bitmapWrapper.Dispose ();
									bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
									media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
									bitmapWrapper.PreloadMediaImage (maxHeightInPixels, BackgroundColor.Default.GetColor ());
								}
							}
						}
						//\\
					}
				}
			});
		}

		public static void SetSearchImage (EMBitmapViewHolder holder, View view, byte[] imageInBytes, string key, Resources resources, int maxDimensionInPixels) {
			EMNativeBitmapWrapper.SetSearchImageOnListItem (holder, view, imageInBytes, key, resources, maxDimensionInPixels);
		}

		public static void SetBackground (View view, Resources resources, int resourceId) {
			EMNativeBitmapWrapper.SetResourceImage (view, resources, resourceId);
		}

		public static void SetBackgroundFromFile (View view, Resources resources, string filepath) {
			EMNativeBitmapWrapper.SetBackgroundFromFile (view, resources, filepath);
		}

		public static void SetBackgroundFromFile (View view, Resources resources, string filepath, int targetHeight) {
			EMNativeBitmapWrapper.SetBackgroundFromFile (view, resources, filepath, targetHeight);
		}

		public static void SetBackgroundFromFileWithMaxWidth (View view, Resources resources, string filepath, int targetHeight, int maxWidth) {
			EMNativeBitmapWrapper.SetBackgroundFromFileWithMaxWidth (view, resources, filepath, targetHeight, maxWidth);
		}

		public static void SetImageFromFile (ImageView view, Resources resources, string filepath) {
			EMNativeBitmapWrapper.SetImageFromFile (view, resources, filepath);
		}

		public static void SetBackgroundFromNinePatch (View view, Resources resources, string filepath, byte[] chunk) {
			EMNativeBitmapWrapper.SetImageFromNinePatch (view, resources, filepath, chunk);
		}
		public static void SetImageFromFileWithMaxWidth(ImageView v, Resources resource, String filepath, int targetHeight, int maxWidth) {
			EMNativeBitmapWrapper.SetImageFromFileWithMaxWidth(v, resource, filepath, targetHeight, maxWidth);
		}

		public static void SetBackground (View view, Drawable drawable) {
			if (EMApplication.SDK_VERSION < Android.OS.BuildVersionCodes.JellyBean) {
				view.SetBackgroundDrawable (drawable);
			} else {
				view.Background = drawable;
			}
		}

		public static void SetAccountImage (CounterParty counterparty, Resources resources, View view, string file, int diameter) {
			if (counterparty == null || counterparty.media == null) {
				EMNativeBitmapWrapper.SetDefaultImage (null, view, file, diameter, resources, false);
				return;
			}

			Media media = counterparty.media;
			EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
			if (bitmapWrapper == null) {
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media, counterparty.PriorMedia), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
			}

			try {
				bitmapWrapper.SetAccountImage (view, file, diameter);
			} catch (Exception e) {
				Debug.WriteLine ("INFO:BitmapSetter:SetAccountImage:" + e);
				bitmapWrapper.Dispose ();
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media, counterparty.PriorMedia), DrawableResources.Default);
				media.SetNativeThumbnail <EMNativeBitmapWrapper>(bitmapWrapper);
				bitmapWrapper.SetAccountImage (view, file, diameter);
			}
		}

		public static void SetThumbnailImage (EMBitmapViewHolder holder, CounterParty counterparty, Resources resources, View view, int defaultResource, int diameter) {
			if (counterparty == null || counterparty.media == null) {
				EMNativeBitmapWrapper.SetDefaultImage (holder, view, defaultResource, diameter, resources, true);
				return;
			}

			Media media = counterparty.media;
			EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
			if (bitmapWrapper == null) {
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media, counterparty.PriorMedia), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
			}

			try {
				bitmapWrapper.SetThumbnailImage (holder, view, defaultResource, diameter);
			} catch (Exception e) {
				Debug.WriteLine ("INFO:SetThumbnailImage:" + e);
				bitmapWrapper.Dispose ();
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media, counterparty.PriorMedia), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
				bitmapWrapper.SetThumbnailImage (holder, view, defaultResource, diameter);
			}
		}

		public static void SetThumbnailImage (BitmapRequest request) {
			CounterParty counterparty = request.CounterParty;
			Resources resources = request.Resources;

			if (counterparty == null || counterparty.media == null) {
				request.ClipDefaultResource = true;
				EMNativeBitmapWrapper.SetDefaultImage (request);
				return;
			}

			Media media = counterparty.media;
			EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
			if (bitmapWrapper == null) {
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media, counterparty.PriorMedia), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
			}

			try {
				bitmapWrapper.SetThumbnailImage (request);
			} catch (Exception e) {
				Debug.WriteLine ("INFO:SetThumbnailImage:" + e);
				bitmapWrapper.Dispose ();
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media, counterparty.PriorMedia), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
				bitmapWrapper.SetThumbnailImage (request);
			}
		}

		public static void SetMediaImage (BitmapRequest request) {
			Media media = request.Media;
			View view = request.View;
			Resources resources = request.Resources;

			if (media == null) {
				SetBackground (view, null);
				return;
			}

			EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
			if (bitmapWrapper == null) {
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
			} else {
				bitmapWrapper.UpdateMediaDescription (new EMMediaDescription (media));
			}

			try {
				bitmapWrapper.SetMediaImage (request);
			} catch (Exception e) {
				Debug.WriteLine ("INFO:SetThumbnailImage:" + e);
				bitmapWrapper.Dispose ();
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
				bitmapWrapper.SetMediaImage (request);
			}
		}

		public static void SetImage (Media media, Resources resources, View view, int defaultResource, int maxHeight) {
			SetImage (null, media, resources, view, defaultResource, maxHeight);
		}

		public static void SetImage (EMBitmapViewHolder holder, Media media, Resources resources, View view, int defaultResource, int maxHeight) {
			// TODO delete this function after we've fully moved to using BitmapRequest
			bool isSingleView = holder == null;
			if (media == null) {
				BitmapRequest request = BitmapRequest.From (holder: holder, resources: resources, view: view, defaultResource: defaultResource, maxHeight: maxHeight);
				EMNativeBitmapWrapper.SetDefaultImageFromRequest (request);
			} else {
				EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
				if (bitmapWrapper == null) {
					bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
					media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
				}

				try {
					if (isSingleView)
						bitmapWrapper.SetImageOnSingleView (view, defaultResource, maxHeight);
					else
						bitmapWrapper.SetImageOnListItem (holder, view, defaultResource, maxHeight);
				} catch (Exception e) {
					Debug.WriteLine ("INFO:BitmapSetter:SetImage:" + e);
					bitmapWrapper.Dispose ();
					bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
					media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
					if (isSingleView)
						bitmapWrapper.SetImageOnSingleView (view, defaultResource, maxHeight);
					else
						bitmapWrapper.SetImageOnListItem (holder, view, defaultResource, maxHeight);
				}
			}
		}

		public static void SetImage (BitmapRequest request) {
			Media media = request.Media;
			if (media == null) {
				EMNativeBitmapWrapper.SetDefaultImageFromRequest (request);
			} else {
				View view = request.View;
				int defaultResource = request.DefaultResource;
				int maxHeight = request.MaxHeight;
				Resources resources = request.Resources;
				EMBitmapViewHolder holder = request.EMHolder;

				EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
				if (bitmapWrapper == null) {
					bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
					media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
				}

				try {
					if (request.SingleView)
						bitmapWrapper.SetImageOnSingleView (view, defaultResource, maxHeight);
					else
						bitmapWrapper.SetImageOnListItem (holder, view, defaultResource, maxHeight);
				} catch (Exception e) {
					Debug.WriteLine ("INFO:BitmapSetter:SetImage:" + e);
					bitmapWrapper.Dispose ();
					bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
					media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
					if (request.SingleView)
						bitmapWrapper.SetImageOnSingleView (view, defaultResource, maxHeight);
					else
						bitmapWrapper.SetImageOnListItem (holder, view, defaultResource, maxHeight);
				}
			}
		}

		public static void SetStagedMedia (Media media, Resources resources, View view, int maxHeight, Color preferredColor) {
			EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
			if (bitmapWrapper == null) {
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
			}

			try {
				bitmapWrapper.SetStagedMediaInChat (view, maxHeight, preferredColor);
			} catch (Exception e) {
				Debug.WriteLine ("INFO:BitmapSetter:SetImage:" + e);
				bitmapWrapper.Dispose ();
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
				bitmapWrapper.SetStagedMediaInChat (view, maxHeight, preferredColor);
			}
		}

		public static void SetImageVideoView (EMBitmapViewHolder holder, Media media, Resources resources, VideoView view, string localMediaPath) {
			if (media == null)
				return;

			EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
			if (bitmapWrapper == null) {
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
			}

			try {
				bitmapWrapper.SetBackgroundOnVideoView (holder, view, localMediaPath);
			} catch (Exception e) {
				Debug.WriteLine ("INFO:SetImageVideoView:" + e);
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
				bitmapWrapper.SetBackgroundOnVideoView (holder, view, localMediaPath);
			}
		}

		public static void SetFullSizeImageView (EMBitmapViewHolder holder, Media media, Resources resources, ImageView view, string localMediaPath) {
			EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
			if (bitmapWrapper == null) {
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
			}

			try {
				bitmapWrapper.SetBackgroundOnImageView (holder, view, localMediaPath);
			} catch (Exception e) {
				Debug.WriteLine ("INFO:SetFullSizeImageView:" + e);
				bitmapWrapper = new EMNativeBitmapWrapper (resources, new EMMediaDescription (media), DrawableResources.Default);
				media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
				bitmapWrapper.SetBackgroundOnImageView (holder, view, localMediaPath);
			}
		}

		public static bool ShouldShowProgressIndicator (CounterParty c) {
			MediaState currentMediaState = MediaState.Unknown;

			if (c == null || (c.media == null && c.PriorMedia == null)) {
				return false;
			}

			Media media = c.media;
			Media priorMedia = c.PriorMedia;
			MediaManager mediaManager = EMApplication.Instance.appModel.mediaManager;

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

