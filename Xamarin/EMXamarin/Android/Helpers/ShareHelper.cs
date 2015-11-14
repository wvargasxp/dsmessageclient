using System;
using System.IO;
using Android.Graphics;
using Android.Util;
using em;

namespace Emdroid {
	public static class ShareHelper {

		public static void GenerateInstagramSharableFile(ApplicationModel appModel, AliasInfo alias, Action callback) {
			GenerateInstagramSharableFile (appModel, alias, null, callback);
		}

		public static void GenerateInstagramSharableFile(ApplicationModel appModel, AliasInfo alias, byte[] thumbnailBytes, Action callback) {
			var color = alias.colorTheme.GetColor ();
			var name = alias.displayName;
			var actionText = "INSTAGRAM_CALL_TO_ACTION".t ();

			var resources = EMApplication.GetInstance().Resources;
			Bitmap bitmap = BitmapFactory.DecodeResource(resources, Resource.Drawable.instagramBG);
			Bitmap mutableBitmap = bitmap.Copy(Bitmap.Config.Argb8888, true);
			alias.colorTheme.GetBlankPhotoAccountResource ((string file) => {
				Bitmap blankPhotoBitmap = BitmapFactory.DecodeFile (file);
				var c = new Canvas(mutableBitmap);

				if(thumbnailBytes == null && alias.media != null && alias.media.uri != null) {
					string path = appModel.uriGenerator.GetCachedFilePathForUri(alias.media.uri);
					thumbnailBytes = appModel.platformFactory.GetFileSystemManager ().ContentsOfFileAtPath(path);
				}

				if(thumbnailBytes == null || thumbnailBytes.Length == 0) {
					#region thumbnail
					var photoThumbnailSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 280f, resources.DisplayMetrics);
					var source = new Rect(0, 0, blankPhotoBitmap.Width, blankPhotoBitmap.Height);
					var bitmapRect = new RectF((mutableBitmap.Width - photoThumbnailSize) / 2, mutableBitmap.Height * .20f, (mutableBitmap.Width + photoThumbnailSize) / 2, (mutableBitmap.Height * .20f) + photoThumbnailSize);
					c.DrawBitmap(blankPhotoBitmap, source, bitmapRect, null);
					#endregion
				} else {
					#region thumbnail
					var p = new Paint ();
					p.AntiAlias = true;
					p.Color = color;
					var backgroundCircleSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 150f, resources.DisplayMetrics);
					float cx = mutableBitmap.Width / 2f;
					float cy = mutableBitmap.Height / 2.3f;
					c.DrawCircle(cx, cy, backgroundCircleSize, p);

					Bitmap thumbnailBitmap = BitmapFactory.DecodeByteArray(thumbnailBytes, 0, thumbnailBytes.Length);
					var photoThumbnailSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 270f, resources.DisplayMetrics);
					Bitmap scaledThumbnailBitmap = Bitmap.CreateScaledBitmap(thumbnailBitmap, (int)photoThumbnailSize, (int)photoThumbnailSize, false);
					Bitmap scaledAndCircleThumbnailBitmap = CreateCircleBitmap(scaledThumbnailBitmap);
					var source = new Rect(0, 0, scaledAndCircleThumbnailBitmap.Width, scaledAndCircleThumbnailBitmap.Height);
					var bitmapRect = new RectF((mutableBitmap.Width - photoThumbnailSize) / 2, cy - (photoThumbnailSize / 2), (mutableBitmap.Width + photoThumbnailSize) / 2, cy + (photoThumbnailSize / 2));
					c.DrawBitmap(scaledAndCircleThumbnailBitmap, source, bitmapRect, null);
					#endregion
				}

				#region call to action text
				var actionPaint = new Paint();
				actionPaint.AntiAlias = true;
				actionPaint.TextSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 40f, resources.DisplayMetrics);
				actionPaint.SetTypeface(FontHelper.DefaultBoldFont);
				actionPaint.Color = Android_Constants.BLACK_COLOR;
				var actionWidth = actionPaint.MeasureText(actionText);
				float ax = (mutableBitmap.Width - actionWidth) / 2;
				float ay = mutableBitmap.Height * .76f;
				c.DrawText(actionText, ax, ay, actionPaint);
				#endregion

				#region alias name
				var textPaint = new Paint();
				textPaint.AntiAlias = true;
				textPaint.TextSize = TypedValue.ApplyDimension(ComplexUnitType.Dip, 35f, resources.DisplayMetrics);
				textPaint.SetTypeface(FontHelper.DefaultFontItalic);
				textPaint.Color = Android_Constants.BLACK_COLOR;
				var textWidth = textPaint.MeasureText(name);
				float x = (mutableBitmap.Width - textWidth) / 2;
				float y = mutableBitmap.Height * .85f;
				c.DrawText(name, x, y, textPaint);
				#endregion

				#region color band
				var colorBandPaint = new Paint();
				colorBandPaint.Color = color;
				colorBandPaint.StrokeWidth = TypedValue.ApplyDimension(ComplexUnitType.Dip, 20f, resources.DisplayMetrics);
				var rect = new RectF(mutableBitmap.Width - TypedValue.ApplyDimension(ComplexUnitType.Dip, 20f, resources.DisplayMetrics), 0, mutableBitmap.Width, mutableBitmap.Height);
				c.DrawRect(rect, colorBandPaint);
				#endregion

				c.Save();

				var stream = new MemoryStream ();
				mutableBitmap.Compress (Bitmap.CompressFormat.Jpeg, 100, stream);
				byte[] sharingBytes = stream.ToArray ();

				string aliasInstagramPath = appModel.platformFactory.GetFileSystemManager ().GetFilePathForSharingAlias (alias);
				appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (aliasInstagramPath);
				appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (aliasInstagramPath, sharingBytes, null);

				callback ();
			});
		}

		static Bitmap CreateCircleBitmap(Bitmap source) {
			Bitmap output = Bitmap.CreateBitmap (source.Width, source.Height, Bitmap.Config.Argb8888);
			var canvas = new Canvas(output);

			var paint = new Paint();
			var rect = new Rect (0, 0, source.Width, source.Height);

			paint.AntiAlias = true;
			canvas.DrawARGB (0, 0, 0, 0);

			canvas.DrawCircle (source.Width / 2, source.Height / 2, source.Width / 2, paint);
			paint.SetXfermode (new PorterDuffXfermode (PorterDuff.Mode.SrcIn));
			canvas.DrawBitmap (source, rect, rect, paint);
			return output;
		}

	}
}