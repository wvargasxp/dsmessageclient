using CoreGraphics;
using Foundation;
using UIKit;

namespace iOS {
	public static class UINavigationBarUtil {
		
		// Sets a drop shadow + sets the font attributes on the nav bar
		public static void SetDefaultAttributesOnNavigationBar (UINavigationBar bar) {
			SetTransparencyOnNavigationBar (bar);
			SetDropShadowOnNavigationBar (bar);
			SetTextAttributesOnNavigationBar (bar);
		}

		public static void RemoveDropShadowOnNavigationBar (UINavigationBar bar) {
			bar.Layer.ShadowOpacity = 0;
		}

		// transparency + a matching color for the tint
		public static void SetTransparencyOnNavigationBar (UINavigationBar bar) {
			bar.SetBackgroundImage (new UIImage(), UIBarMetrics.Default);
			bar.ShadowImage = new UIImage ();
			bar.Translucent = true;

			bar.TintColor = FontHelper.DefaultTextColor ();
		}
			
		public static void SetDropShadowOnNavigationBar (UINavigationBar bar) {
			bar.Layer.ShadowColor = iOS_Constants.BLACK_COLOR.CGColor;
			bar.Layer.ShadowOpacity = .13f;
			bar.Layer.ShadowOffset = new CGSize (0, 2);
			var shadowPath = new CGRect (bar.Bounds.X - 10, bar.Layer.Bounds.Height - 2, bar.Bounds.Width + 20, 5);
			bar.Layer.ShadowPath = UIBezierPath.FromRect (shadowPath).CGPath;
			bar.Layer.ShouldRasterize = true;
			bar.Layer.MasksToBounds = false;
		}

		public static void SetTextAttributesOnNavigationBar (UINavigationBar bar) {
			var atts = new UIStringAttributes ();
			atts.Font = FontHelper.DefaultFontForTitles ();
			atts.ForegroundColor = FontHelper.DefaultTextColor ();
			bar.TitleTextAttributes = atts;
		}

		public static void SetBackButtonToHaveNoText (UINavigationItem item) {
			// https://stackoverflow.com/questions/18870128/ios-7-navigation-bar-custom-back-button-without-title
			item.BackBarButtonItem = new UIBarButtonItem ("", UIBarButtonItemStyle.Plain, null, null);
		}

		public static void SetBackButtonWithUnreadCount (UINavigationItem item, int unreadCount) {
			if (unreadCount > 0) {
				string unread = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (unreadCount), NSNumberFormatterStyle.Decimal);
				item.BackBarButtonItem = new UIBarButtonItem (string.Format ("({0})", unread), UIBarButtonItemStyle.Plain, null, null);
			} else {
				SetBackButtonToHaveNoText (item);
			}
		}
	}
}