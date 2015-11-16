using System;
using UIKit;
using CoreGraphics;
using Foundation;
using iOS;

namespace UIImageExtensions {
	public static class UIImageExtension {
		
		public static UIImage ImageWithPlaybackIconOverlay (this UIImage thumbnail) {
			UIImage playback = ImageSetter.GetBundleImage ("video-icon.png");

			CGSize playbackSize = playback.Size;
			nfloat screenScale = AppDelegate.Instance.ScreenScale;
			float factor = (float) ((45 * screenScale) / playbackSize.Height);
			playbackSize.Width *= factor;
			playbackSize.Height *= factor;

			CGSize thumbnailSize = thumbnail.Size;

			UIGraphics.BeginImageContextWithOptions (thumbnail.Size, false, 0);
			thumbnail.DrawAsPatternInRect (new CGRect (0, 0, thumbnail.Size.Width, thumbnail.Size.Height));
			playback.Draw (new CGRect ((thumbnailSize.Width - playbackSize.Width)/2, (thumbnailSize.Height- playbackSize.Height)/2, playbackSize.Width, playbackSize.Height));
			UIImage newImage = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();
			return newImage;
		}

		public static UIImage ImageFromColor (UIColor color) {
			CGRect rect = new CGRect (0, 0, 1, 1);
			UIGraphics.BeginImageContext (rect.Size);
			CGContext context = UIGraphics.GetCurrentContext ();
			context.SetFillColor (color.CGColor);
			context.FillRect (rect);
			UIImage _image = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();
			return _image;
		}

		public static byte[] ImageToByteArray(this UIImage _image) {
			Byte[] byteArray;
			using(NSData nsImageData = _image.AsPNG()) {
				byteArray = new Byte[nsImageData.Length];
				System.Runtime.InteropServices.Marshal.Copy(nsImageData.Bytes,byteArray,0,Convert.ToInt32(nsImageData.Length));
			}

			return byteArray;
		}

		public static UIImage ByteArrayToImage(byte[] _imageBuffer) {
			if (_imageBuffer != null) {
				if (_imageBuffer.Length != 0) {
					using (NSData imageData = NSData.FromArray (_imageBuffer)) {
						return UIImage.LoadFromData (imageData);
					}
				} else
					return null;
			} else
				return null;
		} 
	}
}