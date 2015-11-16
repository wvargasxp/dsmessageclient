using UIKit;

namespace UIDevice_Extension {
	public static class UIDevice_Extension_DeviceType {
		public static bool IsPad (this UIDevice device) {
			return device.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;
		}

		public static bool IsPhone (this UIDevice device) {
			return device.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone;
		}

		public static bool IsSmallScreen (this UIDevice device) {
			// Checking for the 3.5 inch iphone screen. IPads always have big screens.
			return UIScreen.MainScreen.Bounds.Height < 568;
		}

		public static bool IsRightLeftLanguage (this UIDevice device) {
			return UIApplication.SharedApplication.UserInterfaceLayoutDirection == UIUserInterfaceLayoutDirection.RightToLeft;
		}

		public static bool IsIos8Later (this UIDevice device) {
			if (device.CheckSystemVersion (8, 0))
				return true;
			else
				return false;
		}


		public static bool IsIos8v3Later (this UIDevice device) {
			if (device.CheckSystemVersion (8, 3))
				return true;
			else
				return false;
		}

		public static bool IsIos9Later (this UIDevice device) {
			if (device.CheckSystemVersion (9, 0)) {
				return true;
			} else {
				return false;
			}
		}
			
	}
}