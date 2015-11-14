using System;
using System.Diagnostics;
using System.IO;
using Emdroid;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Android.Provider;
using em;
using EMXamarin;

namespace Media_Android_Extension {
	public static class BitmapUtils {
		static readonly int OVERLAY_REDUCTION_FACTOR = 2;

		public static void ScaleAndFixOrientationOfBitmap (string path) {
			var ei = new ExifInterface (path);
			var orientation = (Android.Media.Orientation)ei.GetAttributeInt (ExifInterface.TagOrientation, (int)Android.Media.Orientation.Normal);

			float rotation = 0;
			switch (orientation) {
			case Android.Media.Orientation.Rotate90:
				rotation = 90;
				break;
			case Android.Media.Orientation.Rotate180:
				rotation = 180;
				break;
			case Android.Media.Orientation.Rotate270:
				rotation = 270;
				break;
			}

			// Checking photo size and reducing size of photo if greater than MAX_DIMENSION_SENT_PHOTO.
			Android.Graphics.BitmapFactory.Options opts = new Android.Graphics.BitmapFactory.Options ();
			opts.InJustDecodeBounds = true;
			BitmapFactory.DecodeFile (path, opts);

			int outHeight = opts.OutHeight;
			int outWidth = opts.OutWidth;
			int biggerSize = outHeight >= outWidth ? outHeight : outWidth;

			// Calculate the largest inSampleSize value that is a power of 2 and keeps both
			// height and width larger than the requested height and width.
			int inSampleSize = 1;
			while ((biggerSize / inSampleSize) > Constants.MAX_DIMENSION_SENT_PHOTO) {
				inSampleSize *= 2;
			}

			if (inSampleSize != 1) {
				opts.InJustDecodeBounds = false;
				opts.InSampleSize = inSampleSize;
			}

			if (rotation != 0 || inSampleSize != 1) {
				Bitmap rotatedBitmap;
				using (Bitmap source = BitmapFactory.DecodeFile (path, opts)) {
					var matrix = new Matrix ();
					matrix.PostRotate (rotation);
					rotatedBitmap = Bitmap.CreateBitmap (source, 0, 0, source.Width, source.Height, matrix, true);

					using (var writeStream = new FileStream (path, FileMode.Create, FileAccess.Write)) {
						rotatedBitmap.Compress (Bitmap.CompressFormat.Jpeg, Android_Constants.JPEG_CONVERSION_QUALITY, writeStream);
					}
				}
			}
		}
	}
}