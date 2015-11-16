using System;
using System.Collections.Generic;
using System.Net;
using CoreGraphics;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using String_UIKit_Extension;
using UIKit;

namespace iOS {
	public class HelpViewController : UIViewController {

		IList<HelpInfo> hList;
		public IList<HelpInfo> HelpInfoList {
			get { return hList; }
			set { hList = value; }
		}

		UIView lineView, blackLineView;
		UIScrollView backgroundView;
		UIImageView appLogo;
		UILabel appTitle;
		UITableView HelpTableView;

		HelpTableViewDelegate tableDelegate;
		HelpTableViewDataSource tableDataSource;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		public HelpViewController () {
			var closeBtn = new UIBarButtonItem ("CLOSE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( (sender, args) => DismissViewController (true, null)).HandleEvent<object,EventArgs>);
			closeBtn.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			NavigationItem.SetLeftBarButtonItem(closeBtn, true);

			HelpInfoList = new List<HelpInfo> ();

			var privacy = new HelpInfo ("GETTING_STARTED".t (), "GETTING_STARTED_URL".t ());
			HelpInfoList.Add (privacy);

			var eula = new HelpInfo ("TIPS".t (), "TIPS_URL".t ());
			HelpInfoList.Add (eula);

			var faq = new HelpInfo ("FAQ".t (), "FAQ_URL".t ());
			HelpInfoList.Add (faq);

			var support = new HelpInfo ("SUPPORT".t (), "SUPPORT_URL".t ());
			HelpInfoList.Add (support);

			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			string username = appDelegate.applicationModel.account.accountInfo.username;
			username = WebUtility.UrlEncode (username);
			string vendorId = UIDevice.CurrentDevice.IdentifierForVendor.AsString ();
			vendorId = WebUtility.UrlEncode (vendorId);
			var url = String.Format ("whoshere://associateEM?username={0}&vendorId={1}", username, vendorId);
			if ( UIApplication.SharedApplication.CanOpenUrl(NSUrl.FromString(url))) {
				var whoshere = new HelpInfo ("ASSOCIATE_WITH_WHOSHERE".t (), url);
				HelpInfoList.Add (whoshere);
			}
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			#region UI
			Title = "HELP_TITLE".t ();
			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (lineView);

			backgroundView = new UIScrollView(View.Bounds);
			backgroundView.BackgroundColor = UIColor.White;
			backgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			backgroundView.ContentSize = new CGSize(View.Frame.Width, 250);

			appLogo = new UIImageView(new CGRect(10, 10, 65, 65));
			appLogo.Image = UIImage.FromFile("Icon.png");
			backgroundView.Add(appLogo);

			appTitle = new UILabel(new CGRect(85, 22, 100, 30));
			appTitle.Text = "APP_TITLE".t ();
			appTitle.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			appTitle.Font = FontHelper.DefaultBoldFontWithSize(25f);
			appTitle.TextColor = iOS_Constants.BLACK_COLOR;
			backgroundView.Add (appTitle);

			blackLineView = new UIView(new CGRect(0, 105, View.Frame.Width, 1));
			blackLineView.BackgroundColor = iOS_Constants.BLACK_COLOR;
			blackLineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			backgroundView.Add(blackLineView);

			HelpTableView = new UITableView ();
			HelpTableView.BackgroundColor = UIColor.Clear;
			HelpTableView.SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine;
			backgroundView.Add (HelpTableView);

			tableDelegate = new HelpTableViewDelegate (this);
			tableDataSource = new HelpTableViewDataSource (this);

			HelpTableView.Delegate = tableDelegate;
			HelpTableView.DataSource = tableDataSource;

			HelpTableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

			View.Add(backgroundView);
			#endregion
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			HelpTableView.ReloadData ();
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Help View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			lineView.Frame = new CGRect (0, displacement_y, lineView.Frame.Width, lineView.Frame.Height);

			backgroundView.Frame = new CGRect (0, displacement_y + lineView.Frame.Height, View.Bounds.Width, View.Bounds.Height);

			appLogo.Frame = new CGRect (UI_CONSTANTS.SMALL_MARGIN, UI_CONSTANTS.SMALL_MARGIN, 65, 65);

			CGSize titleSize = appTitle.Text.SizeOfTextWithFontAndLineBreakMode (appTitle.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 30), UILineBreakMode.Clip);
			titleSize = new CGSize ((float)((int)(titleSize.Width + 1.5)), (float)((int)(titleSize.Height + 1.5)));
			appTitle.Frame = new CGRect (appLogo.Frame.X + appLogo.Frame.Width + UI_CONSTANTS.SMALL_MARGIN, (appLogo.Frame.Y + appLogo.Frame.Height - titleSize.Height) / 2, titleSize.Width, titleSize.Height);

			blackLineView.Frame = new CGRect (0, appLogo.Frame.Y + appLogo.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN, View.Frame.Width, 1);

			HelpTableView.Frame = new CGRect (0, blackLineView.Frame.Y + blackLineView.Frame.Height, View.Frame.Width, HelpInfoList.Count * 50f);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
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

		void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && lineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					lineView.BackgroundColor = mainColor.GetColor ();
				}
			});

			UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		public class HelpInfo {
			public string title { get; set; }
			public string url { get; set; }

			public HelpInfo(string t, string u) {
				this.title = t;
				this.url = u;
			}
		}

		class HelpTableViewDelegate : UITableViewDelegate {
			WeakReference helpViewControllerRef;
			HelpViewController helpViewController {
				get { return helpViewControllerRef.Target as HelpViewController; }
				set { helpViewControllerRef = new WeakReference (value); }
			}

			public HelpTableViewDelegate(HelpViewController controller) {
				helpViewController = controller;
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return 50f;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				HelpInfo row = helpViewController.HelpInfoList [indexPath.Row];

				if (row.url.Equals ("SUPPORT_URL".t ())) {
					var ah = new AnalyticsHelper();
					ah.SendEvent(AnalyticsConstants.CATEGORY_UI_ACTION, AnalyticsConstants.ACTION_BUTTON_PRESS, string.Format(AnalyticsConstants.SUPPORT_LINK, NSLocale.CurrentLocale.LanguageCode), 0);

					UIApplication.SharedApplication.OpenUrl (new NSUrl ("SUPPORT_URL".t ()));
				}
				else if (row.url.StartsWith ("whoshere")) {
					AppDelegate.Instance.MainController.AskUserToBindToWhoshere (); // TODO This would trigger contacts registration + sending list of installed apps too.
				}
				else {
					var controller = new HelpWebViewController (row.title, row.url);
					helpViewController.NavigationController.PushViewController (controller, true);
				}

				tableView.DeselectRow (indexPath, true);
			}
		}

		class HelpTableViewDataSource : UITableViewDataSource {
			const string key = "HELPVIEWTABLEVIEWCELL";
			WeakReference helpViewControllerRef;
			HelpViewController helpViewController {
				get { return helpViewControllerRef.Target as HelpViewController; }
				set { helpViewControllerRef = new WeakReference (value); }
			}

			public HelpTableViewDataSource(HelpViewController controller) {
				helpViewController = controller;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				return helpViewController == null || helpViewController.HelpInfoList == null ? 0 : helpViewController.HelpInfoList.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				var cell = tableView.DequeueReusableCell (key) ?? new UITableViewCell (UITableViewCellStyle.Default, key);
				HelpInfo about = helpViewController.HelpInfoList [indexPath.Row];

				cell.Tag = indexPath.Row;
				cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				cell.TextLabel.Text = about.title;
				cell.TextLabel.Font = FontHelper.DefaultFontForLabels ();

				return cell;
			}
		}
	}
}