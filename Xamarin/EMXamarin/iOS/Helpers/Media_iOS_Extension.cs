using System;
using System.Diagnostics;
using AVFoundation;
using CoreGraphics;
using CoreMedia;
using em;
using EMXamarin;
using Foundation;
using iOS;
using UIKit;
using System.Collections.Generic;

namespace Media_iOS_Extension {
	using UIImageExtensions;

	public static class Media_UIImage_Extension	{
		
		public static UIImage LoadFullSizeImage (this Media media) {
			UIImage fullSizeImage = media.GetFullSizeImage<UIImage> ();
			if (fullSizeImage != null)
				return fullSizeImage;

			AppDelegate appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;

			string localpath = media.GetPathForUri (appDelegate.applicationModel.platformFactory);
			if (appDelegate.applicationModel.mediaManager.MediaOnFileSystem (media)) {
				fullSizeImage = UIImage.FromFile (localpath);
				media.SetThumbnail<UIImage> (fullSizeImage);
				return fullSizeImage;
			}

			media.DownloadMedia (appDelegate.applicationModel);
			return null;
		}

		public static UIImage LoadThumbnail (this Media media) {
			UIImage thumbnail = media.GetThumbnail<UIImage> ();
			if (thumbnail != null)
				return thumbnail;

			AppDelegate appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			string localpath = media.GetPathForUri (appDelegate.applicationModel.platformFactory);

			MediaManager mediaManager = appDelegate.applicationModel.mediaManager;
			if (mediaManager.MediaOnFileSystem (media)) {

				thumbnail = media.LoadSavedThumbnail ();
				if (thumbnail == null) {
					thumbnail = CreateThumbnail (localpath, (UIImage thumb) => {
						media.SaveAndSetUIImage (localpath, thumb);
						media.BackgroundImageResourceUpdatedEvent ();
					});

					ResolveMediaStateAfterThumbnailCreation (thumbnail, media);
				}

				thumbnail = media.SaveAndSetUIImage (localpath, thumbnail);

				if (thumbnail == null) {
					string defaultResourceImage = media.GetDefaultResourceImage ();
					if (defaultResourceImage != null) {
						thumbnail = ImageSetter.GetResourceImage (defaultResourceImage);
					}
				}

				// if it's still null there's likely something wrong with the file
				if (thumbnail != null)
					return thumbnail;

			}

			media.DownloadMedia (appDelegate.applicationModel);

			return null;
		}

		static UIImage SaveAndSetUIImage (this Media media, string localpath, UIImage thumbnail) {
			if (thumbnail != null) {
				media.SaveThumbnail (thumbnail);

				if (ContentTypeHelper.IsVideo(localpath))
					thumbnail = thumbnail.ImageWithPlaybackIconOverlay ();
			}

			if (thumbnail != null) {
				media.SetThumbnail<UIImage> (thumbnail);
			}

			return thumbnail;
		}

		public static UIImage PreloadThumbnailAsync (this Media media, Action<UIImage> onFinishCallback) {
			UIImage thumbnail = media.GetThumbnail<UIImage> ();
			if (thumbnail != null)
				return thumbnail;

			EMTask.DispatchBackground (() => {
				MediaManager mediaManager = AppDelegate.Instance.applicationModel.mediaManager;
				mediaManager.ResolveState (media);
				if (mediaManager.MediaOnFileSystem (media)) {
					string localpath = media.GetPathForUri (AppDelegate.Instance.applicationModel.platformFactory);
					thumbnail = media.LoadSavedThumbnail ();
					if (thumbnail == null) {
						thumbnail = CreateThumbnail (localpath, (UIImage thumb) => {
							media.SaveThumbnail (thumb);
							EMTask.DispatchMain (() => {
								media.SetThumbnail<UIImage> (thumb);
								onFinishCallback (thumb);
							});
						});

						ResolveMediaStateAfterThumbnailCreation (thumbnail, media);
						media.SaveThumbnail (thumbnail);
					}

					bool overlayPlaybackIcon = false;
					if (ContentTypeHelper.IsVideo(localpath))
						overlayPlaybackIcon = true;
					EMTask.DispatchMain (() => {
						if (thumbnail != null) {
							if (overlayPlaybackIcon)
								thumbnail = thumbnail.ImageWithPlaybackIconOverlay ();
							media.SetThumbnail<UIImage> (thumbnail);
							onFinishCallback (thumbnail);
						}
					});
				}
			});

			return null;
		}

		public static UIImage LoadSavedThumbnail(this Media media) {
			try {
				// try load scaled image from filesystem first, before attempting to create the thumbnail
				var applicationModel = AppDelegate.Instance.applicationModel;
				string thumbnailPath = media.GetPathForUriScaled (applicationModel, Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT);
				if (NSFileManager.DefaultManager.FileExists(thumbnailPath)) {
					Debug.WriteLine ("loading saved thumbnail " + thumbnailPath);
					return UIImage.FromFile (thumbnailPath);
				} else 
					Debug.WriteLine ("loading saved thumbnail file not exist " + thumbnailPath);
			}
			catch (Exception e) {
					Debug.WriteLine ("LoadSavedThumbnail: Exception loading saved thumbnail: " + e);
			}
			return null;
		}

		public static void SaveThumbnail(this Media media, UIImage thumbnail) {
			if (thumbnail == null)
				return;

			try {
				// save scaled image to filesystem
				var applicationModel = AppDelegate.Instance.applicationModel;
				string thumbnailPath = media.GetPathForUriScaled (applicationModel, Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT);

				if (NSFileManager.DefaultManager.FileExists (thumbnailPath))
					return;

				NSData thumbnailData = thumbnail.AsJPEG ();
				ApplicationModel.SharedPlatform.GetFileSystemManager ().CreateParentDirectories (thumbnailPath);
					
				NSFileManager.DefaultManager.CreateFile (thumbnailPath, thumbnailData, new NSDictionary ());
				Debug.WriteLine ("saved " + thumbnailPath);
				if (!NSFileManager.DefaultManager.FileExists (thumbnailPath)) {
					Debug.WriteLine ("file not actually saved. This shouldn't happen");
				}
			}
			catch (Exception e) {
				Debug.WriteLine ("CreateThumbnail: Exception saving thumbnail: " + e);
			}
		}
			
		public static UIImage CreateThumbnail (this Media media, Action<UIImage> uiImageCreatedCallback) {
			UIImage image = CreateThumbnail (media.uri.LocalPath, uiImageCreatedCallback);
			ResolveMediaStateAfterThumbnailCreation (image, media);
			return image;
		}

		private static void ResolveMediaStateAfterThumbnailCreation (UIImage image, Media media) {
			// If thumbnail is null, we double check and resolve media state because the file's state could have changed.
			if (image == null) {
				AppDelegate.Instance.applicationModel.mediaManager.ResolveState (media);
			}
		}

		private static HashSet<string> _corruptedImagePaths = null;
		private static HashSet<string> CorruptedImagePaths {
			get {
				Debug.Assert (ApplicationModel.SharedPlatform.OnMainThread, "Should no be using CorruptedImagePaths HashSet on a background thread.");
				if (_corruptedImagePaths == null) {
					_corruptedImagePaths = new HashSet<string> ();
				}

				return _corruptedImagePaths;
			}
		}

		public static UIImage CreateThumbnail (string localPath, Action<UIImage> uiImageCreatedCallback) {
			try {
				UIImage sourceImage = CreateUIImageFromLocalFile (localPath, uiImageCreatedCallback);
				if (sourceImage == null)
					return null;

				nfloat heightRatio = Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT / sourceImage.Size.Height;
				CGSize scaleToSize = new CGSize (Convert.ToInt32 (sourceImage.Size.Width * heightRatio), Convert.ToInt32 (Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT));
				scaleToSize = ScaledSizeToScreen (scaleToSize);
				UIImage thumbnail = sourceImage.Scale (scaleToSize);
				return thumbnail;
			}
			catch (Exception e) {
				// If we're unable to create a UIImage from the path provided, the file might be corrupted.
				// Remove the file and add it to our in memory cache. This is to prevent a loop where file -> downloaded -> unable to create thumbnail -> deleted -> downloaded -> etc.
				if (!CorruptedImagePaths.Contains (localPath)) {
					CorruptedImagePaths.Add (localPath);
					ApplicationModel.SharedPlatform.GetFileSystemManager ().RemoveFileAtPath (localPath);
				}
				Debug.WriteLine ("CreateThumbnail: Exception creating thumbnail " + localPath + ": " + e);
				return null;
			}
		}

		private static UIImage CreateUIImageFromLocalFile(string localPath, Action<UIImage> uiImageCreatedCallback) {
			if (ContentTypeHelper.IsPhoto (localPath))
				return UIImage.FromFile (localPath);

			else if (ContentTypeHelper.IsVideo (localPath)) {
				AVAsset asset = AVUrlAsset.FromUrl ( NSUrl.FromFilename( localPath ));
				using (AVAssetImageGenerator gen = new AVAssetImageGenerator (asset)) {
					gen.AppliesPreferredTrackTransform = true;
					CMTime time = CMTime.FromSeconds (0.0, 600);
					NSError error = null;
					CMTime actualTime = CMTime.Zero;

					using (CGImage image = gen.CopyCGImageAtTime (time, out actualTime, out error)) {
						UIImage thumb = new UIImage (image);
						CGSize size = thumb.Size;
						nfloat factor = 90 / size.Height;
						size.Width *= factor;
						size.Height *= factor;

						size = ScaledSizeToScreen (size);
						return thumb.Scale (size);
					}
				}
			}

			else if (ContentTypeHelper.IsAudio (localPath)) {
				EMTask.DispatchMain (() => {
					SoundWaveformGenerator generater = SoundWaveformGenerator.SharedInstance;
					generater.GenerateThumbOfWaveForm (localPath, uiImageCreatedCallback);
				});

				return null;
			}

			return null;
		}

		/*
		 * When a thumbnail is initially created in CreateThumbnail, it is scaled according to PORTRAIT_CHAT_THUMBNAIL_HEIGHT.
		 * This is for the case where it should've been scaled to LANDSCAPE_CHAT_THUMBNAIL_WIDTH.
		 * The containing view would be a view that's been sized to match either PORTRAIT_CHAT_THUMBNAIL_HEIGHT or LANDSCAPE_CHAT_THUMBNAIL_WIDTH.
		 */
		public static UIImage GetResizedThumbnailIfSmallerThanView (this UIImage sourceThumbnail, UIView containingView) {
			if (sourceThumbnail != null && containingView != null) {
				CGSize imageSize = sourceThumbnail.Size;
				if (imageSize.Height < containingView.Frame.Height) {
					int newWidth = Convert.ToInt32 (imageSize.Width * (Constants.LANDSCAPE_CHAT_THUMBNAIL_WIDTH / imageSize.Height));
					int newHeight = Convert.ToInt32 (Constants.LANDSCAPE_CHAT_THUMBNAIL_WIDTH);
					CGSize size = ScaledSizeToScreen (new CGSize (newWidth, newHeight));
					sourceThumbnail = sourceThumbnail.Scale (size);
				}
			}

			return sourceThumbnail;
		}

		public static UIImage ScaleImage (this UIImage image, nint maxSize) {
			UIImage res;

			using (CGImage imageRef = image.CGImage) {
				CGImageAlphaInfo alphaInfo = imageRef.AlphaInfo;
				CGColorSpace colorSpaceInfo = CGColorSpace.CreateDeviceRGB();
				// https://stackoverflow.com/questions/26071578/cgbitmapcontextcreate-unsupported-parameter-combination-8-integer-bits-compone
				// gist is to use CGImageAlphaInfo.NoneSkipLast to fill in the last 8 bits to get to 32 bits as 24 bits per pixel is not supported
				alphaInfo = CGImageAlphaInfo.PremultipliedLast;

				nint width, height;

				width = imageRef.Width;
				height = imageRef.Height;

				if (height >= width) {
					if (height < maxSize)
						return image;

					width = (int)Math.Floor((double)width * ((double)maxSize / (double)height));
					height = maxSize;
				}
				else {
					if (width < maxSize)
						return image;

					height = (int)Math.Floor((double)height * ((double)maxSize / (double)width));
					width = maxSize;
				}
				nfloat scale = AppDelegate.Instance.ScreenScale;
				height *= (nint)scale;
				width *= (nint)scale;

				CGBitmapContext bitmap;

				if (image.Orientation == UIImageOrientation.Up || image.Orientation == UIImageOrientation.Down)
					bitmap = new CGBitmapContext(IntPtr.Zero, width, height, imageRef.BitsPerComponent, 0, colorSpaceInfo, alphaInfo);
				else
					bitmap = new CGBitmapContext(IntPtr.Zero, height, width, imageRef.BitsPerComponent, 0, colorSpaceInfo, alphaInfo);

				switch (image.Orientation) {
				case UIImageOrientation.Left:
					bitmap.RotateCTM((float)Math.PI / 2);
					bitmap.TranslateCTM(0, -height);
					break;
				case UIImageOrientation.Right:
					bitmap.RotateCTM(-((float)Math.PI / 2));
					bitmap.TranslateCTM(-width, 0);
					break;
				case UIImageOrientation.Up:
					break;
				case UIImageOrientation.Down:
					bitmap.TranslateCTM(width, height);
					bitmap.RotateCTM(-(float)Math.PI);
					break;
				}

				CGSize size = new CGSize (width, height);
				bitmap.DrawImage (new CGRect (new CGPoint (0,0), size), imageRef);

				res = UIImage.FromImage(bitmap.ToImage());
				bitmap = null;
			}

			return res;
		}

		public static CGSize ScaledSizeToScreen (CGSize size) {
			nfloat width = size.Width;
			nfloat height = size.Height;
			nfloat screenScale = AppDelegate.Instance.ScreenScale;
			width *= screenScale;
			height *= screenScale;
			return new CGSize (width, height);
		}

		public static string GetDefaultResourceImage (this Media media) {
			if (ContentTypeHelper.IsAudio(media.contentType) || ContentTypeHelper.IsAudio(media.GetPathForUri (ApplicationModel.SharedPlatform))) {
				return "sound-recording-inline-waveform-blank.png";
			}

			return null;
		}
	}
}