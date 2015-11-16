using System;
using CoreGraphics;
using em;
using Foundation;
using UIKit;
using GoogleAnalytics.iOS;

namespace iOS {
	public class InAppSoundSettingsViewController : BasicSettingsViewController, IUITableViewDelegate, IUITableViewDataSource {

		public SharedInAppSoundSettingsController Shared { get; set; }

		public InAppSoundSettingsViewController () {
			this.Shared = new SharedInAppSoundSettingsController (this);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			this.Title = "SOUNDS_TITLE".t ();

			this.MainTableView.WeakDataSource = this;
			this.MainTableView.WeakDelegate = this;
		}

		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews ();
			this.MainTableView.Frame = new CGRect (0, this.BlackLineView.Frame.Y + this.BlackLineView.Frame.Height, this.View.Frame.Width, this.Shared.Settings.Count * 50f);
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);

			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "In App Sound Settings View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			if (this.NavigationController == null) {
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
			return this.Shared.Settings.Count;
		}

		public UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
			// TODO If we use a custom cell, we should put all our custom UI code there instead.
			UITableViewCell cell = tableView.DequeueReusableCell (Key);

			if (cell == null) {
				cell = new UITableViewCell (UITableViewCellStyle.Default, Key);
				UISwitch switchView = new UISwitch (CGRect.Empty);
				switchView.Tag = indexPath.Row;
				cell.AccessoryView = switchView;
			}

			int row = indexPath.Row;
			SoundSetting menuItem = this.Shared.Settings [row];

			cell.Tag = row;
			cell.TextLabel.Font = FontHelper.DefaultFontForLabels ();

			bool settingEnabled = AppDelegate.Instance.applicationModel.account.UserSettings.EnabledForSoundSetting (menuItem);

			UISwitch switchty = cell.AccessoryView as UISwitch;
			switchty.On = settingEnabled;
			switchty.ValueChanged += WeakDelegateProxy.CreateProxy<object, EventArgs>(SwitchValueChanged).HandleEvent<object, EventArgs>;

			switch (menuItem) {
			case SoundSetting.ReceiveIncomingMessagesSound:
				{
					cell.TextLabel.Text = "INCOMING_MESSAGES_SOUND".t ();
					break;
				}
			}

			return cell;
		}
		#endregion

		private void SwitchValueChanged (object sender, EventArgs e) {
			UISwitch switchView = sender as UISwitch;
			bool enabled = switchView.On;

			nint row = switchView.Tag;
			SoundSetting menuItem = this.Shared.Settings [(int)row];

			SettingChange<SoundSetting> settingChange = SettingChange<SoundSetting>.From (menuItem, enabled);
			this.Shared.HandlePushSettingChangeResult (settingChange);
		}

		#region IUITableViewDelegate impl
		[Export ("tableView:didSelectRowAtIndexPath:")]
		public void RowSelected (UITableView tableView, NSIndexPath indexPath) {
			tableView.DeselectRow (indexPath, true);
		}

		[Export ("tableView:heightForRowAtIndexPath:")]
		public nfloat HeightForRowAtIndexPath (IntPtr tableView, IntPtr indexPath) {
			return 50f;
		}
		#endregion
	}

	public class SharedInAppSoundSettingsController : AbstractInAppSoundSettingsController {
		private WeakReference _r = null;
		private InAppSoundSettingsViewController Self {
			get { return this._r != null ? this._r.Target as InAppSoundSettingsViewController : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedInAppSoundSettingsController (InAppSoundSettingsViewController f) : base (AppDelegate.Instance.applicationModel) {
			this.Self = f;
		}
	}
}