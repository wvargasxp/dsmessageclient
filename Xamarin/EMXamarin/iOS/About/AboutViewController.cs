using System;
using UIKit;
using em;
using CoreGraphics;
using GoogleAnalytics.iOS;
using EMXamarin;
using Foundation;
using String_UIKit_Extension;
using System.Collections.Generic;

namespace iOS
{
	public class AboutViewController : UIViewController {

		IList<AboutInfo> aiList;
		public IList<AboutInfo> AboutInfoList {
			get { return aiList; }
			set { aiList = value; }
		}
		
		UIView lineView, blackLineView;
		UIScrollView backgroundView;
		UIImageView appLogo;
		UILabel appTitle, appVersion, copyright;
		UITableView AboutTableView;
		UIButton facebook, twitter;

		AboutTableViewDelegate tableDelegate;
		AboutTableViewDataSource tableDataSource;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		public AboutViewController () {
			var closeBtn = new UIBarButtonItem ("CLOSE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( (sender, args) => {
				DismissViewController (true, null);
			}).HandleEvent<object,EventArgs>);
			closeBtn.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			NavigationItem.SetLeftBarButtonItem(closeBtn, true);

			AboutInfoList = new List<AboutInfo> ();

			var credits = new AboutInfo ("CREDITS".t (), "CREDITS_URL".t ());
			AboutInfoList.Add (credits);

			var privacy = new AboutInfo ("PRIVACY_POLICY".t (), "PRIVACY_POLICY_URL".t ());
			AboutInfoList.Add (privacy);

			var eula = new AboutInfo ("EULA".t (), "EULA_URL".t ());
			AboutInfoList.Add (eula);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			#region UI
			Title = "ABOUT_TITLE".t ();
			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (lineView);

			backgroundView = new UIScrollView(View.Bounds);
			backgroundView.BackgroundColor = UIColor.White;
			backgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			backgroundView.ContentSize = new CGSize(View.Frame.Width, 415);

			appLogo = new UIImageView(new CGRect(10, 10, 65, 65));
			appLogo.Image = UIImage.FromFile("Icon.png");
			backgroundView.Add(appLogo);

			appTitle = new UILabel(new CGRect(85, 10, 100, 30));
			appTitle.Text = "APP_TITLE".t ();
			appTitle.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			appTitle.Font = FontHelper.DefaultBoldFontWithSize(25f);
			appTitle.TextColor = iOS_Constants.BLACK_COLOR;
			backgroundView.Add (appTitle);

			appVersion = new UILabel(new CGRect(85, 50, 100, 20));
			appVersion.Text = string.Format ("VERSION".t (), NSBundle.MainBundle.InfoDictionary["CFBundleVersion"] + " (" + BranchInfo.BRANCH_NAME + ")");
			appVersion.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			appVersion.Font = FontHelper.DefaultFontWithSize(18f);
			appVersion.TextColor = iOS_Constants.BLACK_COLOR;
			backgroundView.Add (appVersion);

			blackLineView = new UIView(new CGRect(0, 105, View.Frame.Width, 1));
			blackLineView.BackgroundColor = iOS_Constants.BLACK_COLOR;
			blackLineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			backgroundView.Add(blackLineView);

			AboutTableView = new UITableView ();
			AboutTableView.BackgroundColor = UIColor.Clear;
			AboutTableView.SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine;
			backgroundView.Add (AboutTableView);

			tableDelegate = new AboutTableViewDelegate (this);
			tableDataSource = new AboutTableViewDataSource (this);

			AboutTableView.Delegate = tableDelegate;
			AboutTableView.DataSource = tableDataSource;

			AboutTableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

			facebook = new UIButton(new CGRect(backgroundView.Frame.Width / 3, 285, 50, 50));
			facebook.SetBackgroundImage (ImageSetter.GetResourceImage ("about/facebook.png"), UIControlState.Normal);
			facebook.SetBackgroundImage (ImageSetter.GetResourceImage ("about/facebookPressed.png"), UIControlState.Selected);
			facebook.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapFacebookButton).HandleEvent<object,EventArgs>;
			backgroundView.Add(facebook);

			twitter = new UIButton(new CGRect(backgroundView.Frame.Width / 3 + 75, 285, 50, 50));
			twitter.SetBackgroundImage (ImageSetter.GetResourceImage ("about/twitter.png"), UIControlState.Normal);
			twitter.SetBackgroundImage (ImageSetter.GetResourceImage ("about/twitterPressed.png"), UIControlState.Selected);
			twitter.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapTwitterButton).HandleEvent<object,EventArgs>;
			backgroundView.Add(twitter);

			copyright = new UILabel(new CGRect(0, View.Frame.Height - 30, View.Frame.Width, 30));
			copyright.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			copyright.Font = FontHelper.DefaultFontForLabels (10f);
			copyright.Text = "COPYRIGHT".t ();
			copyright.TextColor = iOS_Constants.BLACK_COLOR;
			copyright.TextAlignment = UITextAlignment.Center;
			copyright.AdjustsFontSizeToFitWidth = false;
			copyright.LineBreakMode = UILineBreakMode.WordWrap;
			copyright.Lines = 0;
			backgroundView.Add (copyright);

			View.Add(backgroundView);
			#endregion

			Action<UITapGestureRecognizer> SecretTapActuated = (UITapGestureRecognizer re) => {
				AppDelegate.Instance.applicationModel.ShowVerboseMessageStatusUpdates = true;
				UIAlertView alert = new UIAlertView ("APP_TITLE".t (), 
					" ._____A_____,\n |`          :\\\n | `         : B\n |  `        :  \\\n C   +-----A-----+\n |   :       :   :\n |__ : _A____:   :\n `   :        \\  :\n  `  :         B :\n   ` :          \\:\n    `:_____A_____>", 
					null, 
					"OK_BUTTON".t (), 
					null);
				alert.AlertViewStyle = UIAlertViewStyle.PlainTextInput;
				alert.Clicked += (s, b) => {
					UIAlertView sender = s as UIAlertView;
					if (sender == null) return;
					UITextField textField = sender.GetTextField (0);
					if (textField == null) return;
					string text = textField.Text;
					if (text.Length > 0) {
						AppEnv.SetDomainTo (text);
						AppEnv.SwitchHttpProtocolToHTTP ();
						AppEnv.SwitchSecureWebsocketsToUnsecured ();
					}
				};
				alert.Show ();
			};
            UITapGestureRecognizer SecretTapGestureRecognizer = new UITapGestureRecognizer (SecretTapActuated); // todo; check for memory leak
			SecretTapGestureRecognizer.NumberOfTapsRequired = 10;
			View.AddGestureRecognizer (SecretTapGestureRecognizer);
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			AboutTableView.ReloadData ();
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "About View");

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
			appTitle.Frame = new CGRect (appLogo.Frame.X + appLogo.Frame.Width + UI_CONSTANTS.SMALL_MARGIN, UI_CONSTANTS.SMALL_MARGIN, titleSize.Width, titleSize.Height);

			CGSize versionSize = appVersion.Text.SizeOfTextWithFontAndLineBreakMode (appVersion.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
			versionSize = new CGSize ((float)((int)(versionSize.Width + 1.5)), (float)((int)(versionSize.Height + 1.5)));
			appVersion.Frame = new CGRect (appLogo.Frame.X + appLogo.Frame.Width + UI_CONSTANTS.SMALL_MARGIN, appTitle.Frame.Y + appTitle.Frame.Height + UI_CONSTANTS.SMALL_MARGIN, versionSize.Width, versionSize.Height);
		
			blackLineView.Frame = new CGRect (0, appVersion.Frame.Y + appVersion.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN, View.Frame.Width, 1);

			AboutTableView.Frame = new CGRect (0, blackLineView.Frame.Y + blackLineView.Frame.Height, View.Frame.Width, AboutInfoList.Count * 50f);

			facebook.Frame = new CGRect (backgroundView.Frame.Width / 2 - 65, AboutTableView.Frame.Y + AboutTableView.Frame.Height + 50, 50, 50);
			twitter.Frame = new CGRect (backgroundView.Frame.Width / 2 + 15, AboutTableView.Frame.Y + AboutTableView.Frame.Height + 50, 50, 50);

			CGSize copyrightSize = copyright.Text.SizeOfTextWithFontAndLineBreakMode (copyright.Font, new CGSize (backgroundView.Frame.Width - 16, backgroundView.Frame.Height), UILineBreakMode.WordWrap);
			copyrightSize = new CGSize ((float)((int)(copyrightSize.Width + 1.5)), (float)((int)(copyrightSize.Height + 1.5)));
			if (backgroundView.Frame.Width > copyrightSize.Width) {
				if(backgroundView.Frame.Height > displacement_y + twitter.Frame.Y + twitter.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN)
					copyright.Frame = new CGRect (new CGPoint((backgroundView.Frame.Width - copyrightSize.Width) / 2, backgroundView.Frame.Height - displacement_y - copyrightSize.Height - 8), copyrightSize);
				else
					copyright.Frame = new CGRect (new CGPoint((backgroundView.Frame.Width - copyrightSize.Width) / 2, twitter.Frame.Y + twitter.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN), copyrightSize);
			}
			else {
				if(backgroundView.Frame.Height > displacement_y + twitter.Frame.Y + twitter.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN)
					copyright.Frame = new CGRect (new CGPoint(8, backgroundView.Frame.Height - displacement_y - copyrightSize.Height - 8), copyrightSize);
				else
					copyright.Frame = new CGRect (new CGPoint(8, twitter.Frame.Y + twitter.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN), copyrightSize);
			}
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

		protected void DidTapFacebookButton(object sender, EventArgs e) {
			var language = NSLocale.PreferredLanguages [0];
			var id = language.ToLower().Equals("ar") ? Constants.EM_FACEBOOK_ID_ARABIA : Constants.EM_FACEBOOK_ID_DEFAULT;

			var facebookUrl = new NSUrl ("fb://profile/" + id);

			if (UIApplication.SharedApplication.CanOpenUrl (facebookUrl))
				UIApplication.SharedApplication.OpenUrl (facebookUrl);
			else
				UIApplication.SharedApplication.OpenUrl (new NSUrl ("FACEBOOK_URL".t ()));
		}

		protected void DidTapTwitterButton(object sender, EventArgs e) {
			var language = NSLocale.PreferredLanguages [0];
			var id = language.ToLower().Equals("ar") ? Constants.EM_TWITTER_NAME_ARABIA : Constants.EM_TWITTER_NAME_DEFAULT;

			var twitterUrl = new NSUrl ("twitter:///user?screen_name=" + id);

			if (UIApplication.SharedApplication.CanOpenUrl (twitterUrl))
				UIApplication.SharedApplication.OpenUrl (twitterUrl);
			else
				UIApplication.SharedApplication.OpenUrl (new NSUrl ("TWITTER_URL".t ()));
		}

		public class AboutInfo {
			public string title { get; set; }
			public string url { get; set; }

			public AboutInfo(string t, string u) {
				this.title = t;
				this.url = u;
			}
		}

		class AboutTableViewDelegate : UITableViewDelegate {
			WeakReference aboutViewControllerRef;
			AboutViewController aboutViewController {
				get { return aboutViewControllerRef.Target as AboutViewController; }
				set { aboutViewControllerRef = new WeakReference (value); }
			}

			public AboutTableViewDelegate(AboutViewController controller) {
				aboutViewController = controller;
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return 50f;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				AboutInfo about = aboutViewController.AboutInfoList [indexPath.Row];

				var controller = new AboutWebViewController (about.title, about.url);
				aboutViewController.NavigationController.PushViewController (controller, true);

				tableView.DeselectRow (indexPath, true);
			}
		}

		class AboutTableViewDataSource : UITableViewDataSource {
			const string key = "ABOUTVIEWTABLEVIEWCELL";
			WeakReference aboutViewControllerRef;
			AboutViewController aboutViewController {
				get { return aboutViewControllerRef.Target as AboutViewController; }
				set { aboutViewControllerRef = new WeakReference (value); }
			}

			public AboutTableViewDataSource(AboutViewController controller) {
				aboutViewController = controller;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				return aboutViewController == null || aboutViewController.AboutInfoList == null ? 0 : aboutViewController.AboutInfoList.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				var cell = tableView.DequeueReusableCell (key) ?? new UITableViewCell (UITableViewCellStyle.Default, key);
				AboutInfo about = aboutViewController.AboutInfoList [indexPath.Row];

				cell.Tag = indexPath.Row;
				cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
				cell.TextLabel.Text = about.title;
				cell.TextLabel.Font = FontHelper.DefaultFontForLabels ();

				return cell;
			}
		}
	}
}