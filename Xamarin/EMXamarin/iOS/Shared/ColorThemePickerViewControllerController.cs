using em;
using UIKit;
using CoreGraphics;

namespace iOS {
	public class ColorThemePickerViewControllerController : UITableViewController {

		public delegate void DidPickColorDelegateType(BackgroundColor color);
		public DidPickColorDelegateType DidPickColorDelegate = delegate(BackgroundColor color) { };

		public ColorThemePickerViewControllerController () : base (UITableViewStyle.Grouped) { }

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			// Register the TableView's data source
			TableView.Source = new ColorThemePickerViewControllerSource (this);
			//TableView.BackgroundColor = UIColor.Clear;
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();
			ThemeController (InterfaceOrientation);
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
				}
			});

			UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}
	}
}