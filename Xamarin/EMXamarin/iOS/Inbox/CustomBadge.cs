using System;
using UIKit;
using CoreGraphics;
using Foundation;

namespace iOS {
	public class CustomBadge : UIView {

		public NSString BadgeText { get; set; }
		public float BadgeCornerRoundness { get; set; }
		public float BadgeScaleFactor { get; set; }
		public BadgeStyle BadgeStyle { get; set; }

		public static CustomBadge CustomBadgeWithString (string badgeString) {
			CustomBadge x = new CustomBadge (badgeString, 1.0f, BadgeStyle.DefaultStyle);
			return x;
		}

		public static CustomBadge CustomBadgeWithScale (string badgeString, float scale) {
			CustomBadge x = new CustomBadge (badgeString, scale, BadgeStyle.DefaultStyle);
			return x;
		}

		public static CustomBadge CustomBadgeWithStyle (string badgeString, BadgeStyle style) {
			CustomBadge x = new CustomBadge (badgeString, 1.0f, style);
			return x;
		}

		public static CustomBadge CustomBadgeWithOptions (string badgeString, float scale, BadgeStyle style) {
			CustomBadge x = new CustomBadge (badgeString, scale, style);
			return x;
		}

		public CustomBadge (
			string badgeString, 
			float scale,
			BadgeStyle style
		) : base (new CGRect (0, 0, 25, 25)) {
			
			this.ContentScaleFactor = UIScreen.MainScreen.Scale;
			this.BackgroundColor = UIColor.Clear;
			this.BadgeText = new NSString (badgeString);
			this.BadgeStyle = style;
			this.BadgeCornerRoundness = .4f;
			this.BadgeScaleFactor = scale;
			this.AutoBadgeSizeWithString (badgeString);
		}

		/* Don't accept any touch events. */
		public override bool PointInside (CGPoint point, UIEvent uievent) {
			return false;
		}

		private void AutoBadgeSizeWithString (string badgeString) {
			CGSize retValue;
			nfloat rectWidth;
			nfloat rectHeight;
			nfloat flexSpace;

//			NSDictionary *fontAttr = @{ NSFontAttributeName : [self fontForBadgeWithSize:12] };
//			CGSize stringSize = [badgeString sizeWithAttributes:fontAttr];
			CGSize stringSize = badgeString.StringSize (FontForBadgeWithSize (12));

			if (badgeString.Length >= 2) {
				flexSpace = badgeString.Length;
				rectWidth = 25f + stringSize.Width + flexSpace;
				rectHeight = 25f;
				retValue = new CGSize (rectWidth * this.BadgeScaleFactor, rectHeight * this.BadgeScaleFactor);
			} else {
				retValue = new CGSize (25 * this.BadgeScaleFactor, 25 * this.BadgeScaleFactor);
			}

			this.Frame = new CGRect (this.Frame.X, this.Frame.Y, retValue.Width, retValue.Height);
			this.BadgeText = new NSString (badgeString);
			this.SetNeedsDisplay ();
		}


		public override void Draw (CGRect rect) {
			CGContext context = UIGraphics.GetCurrentContext ();
			DrawRoundedRectWithContext (context, rect);

			if (this.BadgeStyle.BadgeShining) {
				DrawShineWithContext (context, rect);
			}

			if (this.BadgeStyle.BadgeFrame) {
				DrawFrameWithContext (context, rect);
			}

			if (this.BadgeText.Length > 0) {
				nfloat sizeOfFont = 13.5f * this.BadgeScaleFactor;
				if (this.BadgeText.Length < 2) {
					sizeOfFont += sizeOfFont * 0.20f;
				}

				UIFont textFont = FontForBadgeWithSize (sizeOfFont);
				UIStringAttributes attributes = new UIStringAttributes {
					Font = textFont,
					ForegroundColor = this.BadgeStyle.BadgeTextColor
				};

				CGSize textSize = this.BadgeText.GetSizeUsingAttributes (attributes);
				CGPoint textPoint = new CGPoint (
					                    rect.Size.Width / 2 - textSize.Width / 2,
					                    (rect.Size.Height / 2 - textSize.Height / 2) - 1);
				this.BadgeText.DrawString (textPoint, attributes);
			}
		}

		private void DrawRoundedRectWithContext (CGContext context, CGRect rect) {
			context.SaveState ();
			nfloat radius = rect.GetMaxY () * this.BadgeCornerRoundness;
			nfloat puffer = rect.GetMaxY () * 0.10f;
			nfloat maxX = rect.GetMaxX () - puffer;
			nfloat maxY = rect.GetMaxX () - puffer;
			nfloat minX = rect.GetMinX () + puffer;
			nfloat minY = rect.GetMinY () + puffer;
			nfloat pi = (nfloat)Math.PI;

			context.BeginPath ();
			context.SetFillColor (this.BadgeStyle.BadgeInsetColor.CGColor);
			context.AddArc (maxX - radius, minY + radius, radius, pi + (pi / 2), 0, false);
			context.AddArc (maxX - radius, maxY - radius, radius, 0, pi / 2, false);
			context.AddArc (minX + radius, maxY - radius, radius, pi / 2, pi, false);
			context.AddArc (minX + radius, minY + radius, radius, pi, pi + (pi / 2), false);
			if (this.BadgeStyle.BadgeShadow) {
				context.SetShadow (new CGSize (1f, 1f), 3f, UIColor.Black.CGColor);
			}

			context.FillPath ();
			context.RestoreState ();
		}

		private void DrawShineWithContext (CGContext context, CGRect rect) {
			context.SaveState ();
			nfloat radius = rect.GetMaxY () * this.BadgeCornerRoundness;
			nfloat puffer = rect.GetMaxY () * 0.10f;
			nfloat maxX = rect.GetMaxX () - puffer;
			nfloat maxY = rect.GetMaxX () - puffer;
			nfloat minX = rect.GetMinX () + puffer;
			nfloat minY = rect.GetMinY () + puffer;
			nfloat pi = (nfloat)Math.PI;

			context.BeginPath ();
			context.AddArc (maxX - radius, minY + radius, radius, pi + (pi / 2), 0, false);
			context.AddArc (maxX - radius, maxY - radius, radius, 0, pi / 2, false);
			context.AddArc (minX + radius, maxY - radius, radius, pi / 2, pi, false);
			context.AddArc (minX + radius, minY + radius, radius, pi, pi + (pi / 2), false);
			context.Clip ();

			int numLocations = 2;
			nfloat[] locations = new nfloat[2] { 0.0f, 0.4f };
			nfloat[] components = new nfloat[8] { 0.92f, 0.92f, 0.92f, 1.0f, 0.82f, 0.82f, 0.82f, 0.4f };

			CGColorSpace cspace = CGColorSpace.CreateDeviceRGB ();
			CGGradient gradient = new CGGradient (cspace, components);
			CGPoint sPoint = new CGPoint (0f, 0f);
			CGPoint ePoint = new CGPoint (0f, maxY);

			context.DrawLinearGradient (gradient, sPoint, ePoint, CGGradientDrawingOptions.None);

//			CGColorSpaceRelease(cspace);
//			CGGradientRelease(gradient);
			cspace.Dispose ();
			gradient.Dispose ();

			context.RestoreState ();
		}

		private void DrawFrameWithContext (CGContext context, CGRect rect) {
			nfloat radius = rect.GetMaxY () * this.BadgeCornerRoundness;
			nfloat puffer = rect.GetMaxY () * 0.10f;
			nfloat maxX = rect.GetMaxX () - puffer;
			nfloat maxY = rect.GetMaxX () - puffer;
			nfloat minX = rect.GetMinX () + puffer;
			nfloat minY = rect.GetMinY () + puffer;
			nfloat pi = (nfloat)Math.PI;

			context.BeginPath ();
			float lineSize = 2;
			if (this.BadgeScaleFactor > 1) {
				lineSize += this.BadgeScaleFactor * .25f;
			}

			context.SetLineWidth (lineSize);
			context.SetStrokeColor (this.BadgeStyle.BadgeFrameColor.CGColor);
			context.AddArc (maxX - radius, minY + radius, radius, pi + (pi / 2), 0, false);
			context.AddArc (maxX - radius, maxY - radius, radius, 0, pi / 2, false);
			context.AddArc (minX + radius, maxY - radius, radius, pi / 2, pi, false);
			context.AddArc (minX + radius, minY + radius, radius, pi, pi + (pi / 2), false);
			context.ClosePath ();
			context.StrokePath ();

		}

		private UIFont FontForBadgeWithSize (nfloat size) {
			switch (this.BadgeStyle.BadgeFontType) {
			default:
			case BadgeStyleFontType.BadgeStyleFontTypeHelveticaNeueLight:
				{
					return UIFont.FromName ("HelveticaNeue-Light", size);
				}
			case BadgeStyleFontType.BadgeStyleFontTypeHelveticaNeueMedium: 
				{
					return UIFont.FromName ("HelveticaNeue-Medium", size);
				}
			}
		}

	}
}

