using System;
using CoreGraphics;
using em;
using String_UIKit_Extension;
using UIKit;

namespace iOS {
	public class BasicSettingsViewController : UIViewController {

		protected UIView LineView { get; set; }
		protected UIView BlackLineView{ get; set; }
		protected UIScrollView BackgroundView { get; set; }
		protected UIImageView AppLogo { get; set; }
		protected UILabel AppTitleLabel { get; set; }
		protected UITableView MainTableView { get; set; }

		public BasicSettingsViewController () {}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			UINavigationBarUtil.SetBackButtonToHaveNoText (this.NavigationItem);

			#region UI
			this.Title = "SETTINGS_TITLE".t ();
			this.LineView = new UINavigationBarLine (new CGRect (0, 0, this.View.Frame.Width, 1));
			this.LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			this.View.Add (this.LineView);

			this.BackgroundView = new UIScrollView (this.View.Bounds);
			this.BackgroundView.BackgroundColor = UIColor.White;
			this.BackgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			this.BackgroundView.ContentSize = new CGSize (this.View.Frame.Width, 250);

			this.AppLogo = new UIImageView (new CGRect (10, 10, 65, 65));
			this.AppLogo.Image = UIImage.FromFile ("Icon.png");
			this.BackgroundView.Add (AppLogo);

			this.AppTitleLabel = new UILabel (new CGRect (85, 22, 100, 30));
			this.AppTitleLabel.Text = "APP_TITLE".t ();
			this.AppTitleLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			this.AppTitleLabel.Font = FontHelper.DefaultBoldFontWithSize (25f);
			this.AppTitleLabel.TextColor = iOS_Constants.BLACK_COLOR;
			this.BackgroundView.Add (this.AppTitleLabel);

			this.BlackLineView = new UIView (new CGRect (0, 105, this.View.Frame.Width, 1));
			this.BlackLineView.BackgroundColor = iOS_Constants.BLACK_COLOR;
			this.BlackLineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			this.BackgroundView.Add (BlackLineView);

			this.MainTableView = new UITableView ();
			this.MainTableView.BackgroundColor = UIColor.Clear;
			this.MainTableView.SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine;
			this.MainTableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

			this.BackgroundView.Add (this.MainTableView);

			this.View.Add(BackgroundView);
			#endregion
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			ThemeController (this.InterfaceOrientation);
		}

		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews ();

			nfloat displacement_y = this.TopLayoutGuide.Length;
			this.LineView.Frame = new CGRect (0, displacement_y, this.LineView.Frame.Width, this.LineView.Frame.Height);
			this.BackgroundView.Frame = new CGRect (0, displacement_y + this.LineView.Frame.Height, this.View.Bounds.Width, this.View.Bounds.Height);
			this.AppLogo.Frame = new CGRect (UI_CONSTANTS.SMALL_MARGIN, UI_CONSTANTS.SMALL_MARGIN, 65, 65);

			CGSize titleSize = this.AppTitleLabel.Text.SizeOfTextWithFontAndLineBreakMode (this.AppTitleLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 30), UILineBreakMode.Clip);
			titleSize = new CGSize ((float)((int)(titleSize.Width + 1.5)), (float)((int)(titleSize.Height + 1.5)));
			this.AppTitleLabel.Frame = new CGRect (this.AppLogo.Frame.X + this.AppLogo.Frame.Width + UI_CONSTANTS.SMALL_MARGIN, (this.AppLogo.Frame.Y + this.AppLogo.Frame.Height - titleSize.Height) / 2, titleSize.Width, titleSize.Height);
			this.BlackLineView.Frame = new CGRect (0, this.AppLogo.Frame.Y + this.AppLogo.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN, View.Frame.Width, 1);
		}

		#region Rotation
		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}
		#endregion

		private void ThemeController (UIInterfaceOrientation orientation) {
			AppDelegate appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (this.View != null && this.LineView != null) {
					this.View.BackgroundColor = UIColor.FromPatternImage (image);
					this.LineView.BackgroundColor = mainColor.GetColor ();
				}
			});

			UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (this.NavigationController.NavigationBar);
		}
	}
}