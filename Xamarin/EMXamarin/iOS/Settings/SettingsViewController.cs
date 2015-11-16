using System;
using CoreGraphics;
using em;
using Foundation;
using UIKit;
using GoogleAnalytics.iOS;

namespace iOS {
	public class SettingsViewController : BasicSettingsViewController, IUITableViewDelegate, IUITableViewDataSource {

		public SharedSettingsController Shared { get; set; }

		public SettingsViewController () {
			this.Shared = new SharedSettingsController (this);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			UIBarButtonItem leftButton = new UIBarButtonItem ("CLOSE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>(Exit).HandleEvent<object,EventArgs>);
			leftButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			this.NavigationItem.SetLeftBarButtonItem (leftButton, true);

			this.MainTableView.WeakDataSource = this;
			this.MainTableView.WeakDelegate = this;
		}

		private void Exit (object sender, EventArgs args) {
			this.DismissViewController (true, null);
		}

		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews ();
			this.MainTableView.Frame = new CGRect (0, this.BlackLineView.Frame.Y + this.BlackLineView.Frame.Height, this.View.Frame.Width, this.Shared.Settings2.Count * 50f);
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);

			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Settings View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			if (this.NavigationController.ViewControllers.Length == 1) {
				this.MainTableView.WeakDataSource = null;
				this.MainTableView.WeakDelegate = null;
			}
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		#region IUITableViewDataSource impl
		private const string Key = "SettingsTableViewCell";
		public nint RowsInSection (UITableView tableView, nint section) {
			return this.Shared.Settings2.Count;
		}

		public UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
			UITableViewCell cell = tableView.DequeueReusableCell (Key) ?? new UITableViewCell (UITableViewCellStyle.Default, Key);
			int row = indexPath.Row;
			SettingMenuItem menuItem = this.Shared.Settings2 [row];

			cell.Tag = row;
			cell.TextLabel.Font = FontHelper.DefaultFontForLabels ();

			switch (menuItem) {
			case SettingMenuItem.InAppSounds:
				{
					cell.TextLabel.Text = "SOUNDS_TITLE".t ();
					break;
				}
			case SettingMenuItem.InAppSettings:
				{
					cell.TextLabel.Text = "IN_APP_SETTINGS_TITLE".t ();
					break;
				}
			}

			return cell;
		}
		#endregion

		#region IUITableViewDelegate impl
		[Export ("tableView:didSelectRowAtIndexPath:")]
		public void RowSelected (UITableView tableView, NSIndexPath indexPath) {
			int row = indexPath.Row;
			SettingMenuItem menuItemClicked = this.Shared.Settings2 [row];
			switch (menuItemClicked) {
			default:
			case SettingMenuItem.Push:
			case SettingMenuItem.InAppSounds:
				{
					this.NavigationController.PushViewController (new InAppSoundSettingsViewController (), true);
					break;
				}
			case SettingMenuItem.InAppSettings:
				{
					this.NavigationController.PushViewController (new InAppSettingsViewController (), true);
					break;
				}
			}

			tableView.DeselectRow (indexPath, true);
		}

		[Export ("tableView:heightForRowAtIndexPath:")]
		public nfloat HeightForRowAtIndexPath (IntPtr tableView, IntPtr indexPath) {
			return 50f;
		}
		#endregion
	}

	public class SharedSettingsController : AbstractSettingsController {
		private WeakReference _r = null;
		private SettingsViewController Self {
			get { return this._r != null ? this._r.Target as SettingsViewController : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedSettingsController (SettingsViewController f) : base (AppDelegate.Instance.applicationModel) {
			this.Self = f;
		}
	}
}