using System;
using CoreGraphics;
using em;
using UIKit;

namespace iOS {
	public static class BackgroundColorChanger {

		const int TAG = 13123;

		public static BackgroundColor[] backgroundColors = {
			BackgroundColor.Blue,
			BackgroundColor.Orange,
			BackgroundColor.Pink,
			BackgroundColor.Green
		};

		public static void AddColoredButtons (UIViewController controller, UIButton [] buttons, Action<BackgroundColor> callback) {
			// Here, we're assuming buttons is in the same order as backgroundColors.
			int length = backgroundColors.Length;
			for (int i=0; i<length; i++) {
				UIButton button = buttons [i];
				button.Tag = TAG + i;
				controller.View.Add (button);
				button.TouchDown += (sender, e) => {
					var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
					BackgroundColor color = appDelegate.applicationModel.account.accountInfo.colorTheme = backgroundColors [button.Tag - TAG];
					color.GetBackgroundResource ( (UIImage image) => {
						if (controller != null && controller.View != null) {
							controller.View.BackgroundColor = UIColor.FromPatternImage (image);
							callback (color);
						}
					});
				};

				button.Frame = new CGRect (0, 0, UI_CONSTANTS.COLORED_SQUARE_SIZE, UI_CONSTANTS.COLORED_SQUARE_SIZE);
			}
		}
	}
}