using System;
using System.IO;
using CoreGraphics;
using CoreText;
using em;
using EMXamarin;
using Foundation;
using UIKit;

namespace iOS {
	public static class ShareHelper {

		static readonly int size = 640;
		static readonly nfloat actionFontSize = 40f;
		static readonly nfloat nameFontSize = 35f;
		static readonly float photoThumbnailSize = 300f;

		public static void GenerateInstagramSharableFile(ApplicationModel appModel, AliasInfo alias, byte[] thumbnailBytes, int index, Action<int> callback) {
			var color = alias.colorTheme.GetColor ();
			var name = alias.displayName;
			var actionText = "INSTAGRAM_CALL_TO_ACTION".t ();

			UIGraphics.BeginImageContext (new CGSize (size, size));
			using(CGContext g = UIGraphics.GetCurrentContext()) {
				// scale and translate the CTM so the image appears upright
				g.ScaleCTM (1, -1);
				g.TranslateCTM (0, -size);
				var wholeRect = new CGRect (0, 0, size, size);
				g.DrawImage (wholeRect, UIImage.FromFile ("instagramBG.png").CGImage);


				#region color band
				g.SetLineWidth(20f);
				color.SetStroke();
				var colorBandPath = new CGPath ();
				colorBandPath.MoveToPoint(size - 10f, 0);
				colorBandPath.AddLineToPoint(size - 10f, size);
				g.AddPath(colorBandPath);     
				g.DrawPath(CGPathDrawingMode.Stroke);
				#endregion

				var scaledSize = new CGSize(photoThumbnailSize, photoThumbnailSize);

				if(thumbnailBytes == null && alias.media != null && alias.media.uri != null) {
					string path = appModel.uriGenerator.GetCachedFilePathForUri(alias.media.uri);
					thumbnailBytes = appModel.platformFactory.GetFileSystemManager ().ContentsOfFileAtPath(path);
				}

				if(thumbnailBytes == null || thumbnailBytes.Length == 0) {
					#region thumbnail background
					alias.colorTheme.GetBlankPhotoAccountResource ( (UIImage image) => {
						CGImage blankPhoto = image.Scale(scaledSize).CGImage;
						var blankPhotoRect = new CGRect((wholeRect.Width - blankPhoto.Width) / 2, 250, blankPhoto.Width, blankPhoto.Height);
						if (g != null) {
							g.DrawImage(blankPhotoRect, blankPhoto);
						}
					});
					#endregion
				} else {
					#region thumbnail background
					alias.colorTheme.GetBlankPhotoAccountResource ( (UIImage image) => {
						CGImage blankPhoto = image.Scale(scaledSize).CGImage;
						var blankPhotoRect = new CGRect((wholeRect.Width - blankPhoto.Width) / 2, 250, blankPhoto.Width, blankPhoto.Height);
						if (g != null) {
							g.DrawImage(blankPhotoRect, blankPhoto);
						}
					});
					#endregion

					#region thumbnail
					UIImage thumbnail;
					var thumbnailSize = new CGSize(450f, 450f);
					UIGraphics.BeginImageContext(thumbnailSize);
					using(CGContext g2 = UIGraphics.GetCurrentContext()) {
						NSData imageData = NSData.FromArray(thumbnailBytes);
						var thumbnailImage = UIImage.LoadFromData(imageData).Scale(thumbnailSize);

						UIBezierPath.FromRoundedRect (new CGRect (0, 0, thumbnailImage.Size.Width/2, thumbnailImage.Size.Height/2), thumbnailImage.Size.Width / 2).AddClip(); 
						thumbnailImage.Draw (new CGRect (0, 0, thumbnailImage.Size.Width/2, thumbnailImage.Size.Height/2)); 
						thumbnail = UIGraphics.GetImageFromCurrentImageContext ();
					}
					UIGraphics.EndImageContext ();
					var photoRect = new CGRect((wholeRect.Width - 250) / 2, 30, 500, 500);
					g.DrawImage(photoRect, thumbnail.CGImage);
					#endregion
				}

				#region call to action text
				g.SetFont(CGFont.CreateWithFontName(Constants.FONT_FOR_DEFAULT_BOLD));
				g.SetAllowsFontSmoothing(true);
				g.SetFontSize(actionFontSize);

				var actionAttributedString = new NSAttributedString (actionText,
					new CTStringAttributes {
						ForegroundColorFromContext =  true,
						Font = new CTFont (Constants.FONT_FOR_DEFAULT_BOLD, actionFontSize)
					}
				);

				// Get size of target string using the label's font.
				var asString = new NSString(actionText);
				//need to call NSString.StringSize because it can run on a background thread
				var actionSize = asString.StringSize(FontHelper.DefaultBoldFontForButtons(actionFontSize));
				var axCoord = (size - actionSize.Width) / 2;
				g.TranslateCTM(axCoord, 180);

				using (var actionLine = new CTLine (actionAttributedString)) {
					actionLine.Draw (g);
				}
				#endregion
				
				#region alias name
				g.SetFont(CGFont.CreateWithFontName(Constants.FONT_LIGHT_ITALIC));
				g.SetAllowsFontSmoothing(true);
				g.SetFontSize(nameFontSize);

				var nameAttributedString = new NSAttributedString (name,
					new CTStringAttributes {
						ForegroundColorFromContext =  true,
						Font = new CTFont (Constants.FONT_LIGHT_ITALIC, nameFontSize)
					}
				);

				// Get size of target string using the label's font.
				var nsString = new NSString(name);
				//need to call NSString.StringSize because it can run on a background thread
				var nameSize = nsString.StringSize(FontHelper.DefaultItalicFont(nameFontSize));
				var nxCoord = (g.TextPosition.X - nameSize.Width) / 2;

				g.TextPosition = new CGPoint(nxCoord, -80);

				using (var nameLine = new CTLine (nameAttributedString)) {
					nameLine.Draw (g);
				}
				#endregion

				var screenshot = UIGraphics.GetImageFromCurrentImageContext ();

				byte[] sharingBytes = screenshot.AsJPEG ().ToByteArray ();

				string aliasInstagramPath = appModel.platformFactory.GetFileSystemManager ().GetFilePathForSharingAlias (alias);
				appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (aliasInstagramPath);
				appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (aliasInstagramPath, sharingBytes, null);
			}
			UIGraphics.EndImageContext ();

			callback (index);
		}
	}
}