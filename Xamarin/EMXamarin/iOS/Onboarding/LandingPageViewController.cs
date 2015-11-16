using System;
using CoreGraphics;
using em;
using GoogleAnalytics.iOS;
using Foundation;
using UIKit;

namespace iOS {
	using UIDevice_Extension;
	public class LandingPageViewController : UIViewController {
		readonly LandingPageModel model;

		#region UI
		UIButton continueButton;
		UIWebView previewWebView;

		UIButton blueButton;
		UIButton orangeButton;
		UIButton pinkButton;
		UIButton greenButton;

		const int MOBILE_EMAIL_BUTTON_SIZE = 55;
		UIButton mobileButton;
		UIButton emailButton;

		UIView lineView;
		#endregion

		bool reloadWebView;
		public bool ReloadWebView {
			get { return reloadWebView; }
			set { reloadWebView = value; }
		}

		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public LandingPageViewController () {
			model = new LandingPageModel ();
		}

		public override void DidReceiveMemoryWarning () {
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();

			// Release any cached data, images, etc that aren't in use.
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			CGSize screenSize = UIScreen.MainScreen.Bounds.Size;
			// Background image might not be ready yet. Load this image.
			UIImage scaled = Media_iOS_Extension.Media_UIImage_Extension.ScaleImage(UIImage.FromFile ("backgrounds/bgGray@3x.jpg"), (nint)screenSize.Width);
			View.BackgroundColor = UIColor.FromPatternImage (scaled);
			mainColor.GetBackgroundResource ( (UIImage image) => { 
				if (!mainColor.ToHexString ().Equals (BackgroundColor.Gray.ToHexString ()) && View != null && lineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					lineView.BackgroundColor = mainColor.GetColor ();
				}
			});
		}

		public void ReloadWebViewContent() {
			if(previewWebView != null && ReloadWebView) {
				var req = new NSUrlRequest(NSUrl.FromString ( model.PreviewViewURL ), NSUrlRequestCachePolicy.ReloadIgnoringLocalAndRemoteCacheData, 5);
				previewWebView.LoadRequest (req);
			}
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Landing Page View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());

			if (AppEnv.SKIP_ONBOARDING) {
				mobileButton.SendActionForControlEvents (UIControlEvent.TouchDown);
			}
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			#region instantiating ui components
			// Webview and button will get their origins sized in LayoutSubviews.
			previewWebView = new UIWebView (new CGRect (0, 0, View.Frame.Width, View.Frame.Height / 2));
			var req = new NSUrlRequest(NSUrl.FromString ( model.PreviewViewURL ), NSUrlRequestCachePolicy.ReloadIgnoringLocalAndRemoteCacheData, 5);
			previewWebView.LoadRequest (req);
			previewWebView.Opaque = false;
			previewWebView.BackgroundColor = UIColor.Clear;
			previewWebView.Layer.BorderColor = UIColor.White.CGColor;

			previewWebView.LoadFinished += (sender, e) => {
				//prevent being able to scroll / bounce when dragging web view up, down, left or right
				((UIWebView)sender).ScrollView.AlwaysBounceVertical = false;
				((UIWebView)sender).ScrollView.AlwaysBounceHorizontal = false;
			};
			ReloadWebView = false;

			Title = "WELCOME_TITLE".t ();
			UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;

			View.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

			continueButton = new EMButton (UIButtonType.RoundedRect, iOS_Constants.WHITE_COLOR, "GET_STARTED_BUTTON".t ());

			// This section is getting the width/height of the label and setting the button's frames to be slightly larger than that.
			var fontAttributes = new UIStringAttributes ();
			fontAttributes.Font = FontHelper.DefaultFontForButtons ();
			CGSize textSize = new NSString (continueButton.Title (UIControlState.Normal)).GetSizeUsingAttributes (fontAttributes);
			continueButton.Frame = new CGRect (0, 0, textSize.Width + UI_CONSTANTS.BUTTON_HORIZONTAL_PADDING, textSize.Height + UI_CONSTANTS.BUTTON_VERTICAL_PADDING);
			#endregion

			continueButton.TouchUpInside += (sender, e) => UIView.Animate (.7, () => {
				continueButton.Frame = new CGRect (-continueButton.Frame.Width, continueButton.Frame.Y, continueButton.Frame.Width, continueButton.Frame.Height);
				continueButton.Alpha = 0.0f;
				mobileButton.Alpha = emailButton.Alpha = 1.0f;
				mobileButton.Frame = new CGRect (View.Frame.Width / 2 - mobileButton.Frame.Width - UI_CONSTANTS.EXTRA_MARGIN / 2, continueButton.Frame.Y, mobileButton.Frame.Width, mobileButton.Frame.Height);
				emailButton.Frame = new CGRect (View.Frame.Width / 2 + UI_CONSTANTS.EXTRA_MARGIN / 2, continueButton.Frame.Y, emailButton.Frame.Width, emailButton.Frame.Height);
			}, () => {
				model.didClickGetStarted = true;
				continueButton.RemoveFromSuperview ();
			});

			#region mobile + email buttons
			mobileButton = new UIButton (UIButtonType.Custom);
			emailButton = new UIButton (UIButtonType.Custom);

			mobileButton.Frame = emailButton.Frame = new CGRect (View.Frame.Width - UI_CONSTANTS.EXTRA_MARGIN, continueButton.Frame.Y, MOBILE_EMAIL_BUTTON_SIZE, MOBILE_EMAIL_BUTTON_SIZE);

			mobileButton.SetImage (UIImage.FromFile ("onboarding/loginPhone.png"), UIControlState.Normal);
			emailButton.SetImage (UIImage.FromFile ("onboarding/loginMail.png"), UIControlState.Normal);

			mobileButton.TouchDown += (sender, e) => NavigationController.PushViewController (new MobileSignInViewController (), true);
			emailButton.TouchDown += (sender, e) => NavigationController.PushViewController (new EmailSignInViewController (), true);

			mobileButton.Alpha = emailButton.Alpha = 0.0f;

			View.Add (lineView);
			View.Add (mobileButton);
			View.Add (emailButton);

			#endregion

			#region colored buttons
			blueButton = new UIButton (UIButtonType.Custom);
			orangeButton = new UIButton (UIButtonType.Custom);
			pinkButton = new UIButton (UIButtonType.Custom);
			greenButton = new UIButton (UIButtonType.Custom);

			blueButton.SetImage (UIImage.FromFile ("onboarding/squareBlue.png"), UIControlState.Normal);
			orangeButton.SetImage (UIImage.FromFile ("onboarding/squareGold.png"), UIControlState.Normal);
			pinkButton.SetImage (UIImage.FromFile ("onboarding/squarePink.png"), UIControlState.Normal);
			greenButton.SetImage (UIImage.FromFile ("onboarding/squareGreen.png"), UIControlState.Normal);

			UIButton [] buttons = {blueButton, orangeButton, pinkButton, greenButton};
			BackgroundColorChanger.AddColoredButtons (this, buttons, changeToColor => {
				lineView.BackgroundColor = changeToColor.GetColor ();
			});
			#endregion

			View.Add (continueButton);
			View.Add (previewWebView);

			View.BringSubviewToFront (lineView);
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();
			nfloat displacement_y = this.TopLayoutGuide.Length;

			lineView.Frame = new CGRect (0, displacement_y, lineView.Frame.Width, lineView.Frame.Height);

			//load subviews with displacement
			var webViewHeight = View.Frame.Height / 2;
			if (webViewHeight < 342) {
				webViewHeight = 342;

				if (UIDevice.CurrentDevice.IsSmallScreen ())
					webViewHeight = 250;
			}
				
			previewWebView.Frame = new CGRect (0, displacement_y + UI_CONSTANTS.TINY_MARGIN, View.Frame.Width, webViewHeight);

			if (continueButton != null)
				continueButton.Frame = new CGRect (View.Frame.Width / 2 - continueButton.Frame.Width / 2, previewWebView.Frame.Y + webViewHeight + UI_CONSTANTS.SMALL_MARGIN, continueButton.Frame.Width, continueButton.Frame.Height);	

			if (model.didClickGetStarted) {
				mobileButton.Frame = new CGRect (View.Frame.Width / 2 - mobileButton.Frame.Width - UI_CONSTANTS.EXTRA_MARGIN / 2, continueButton.Frame.Y, mobileButton.Frame.Width, mobileButton.Frame.Height);
				emailButton.Frame = new CGRect (View.Frame.Width / 2 + UI_CONSTANTS.EXTRA_MARGIN / 2, continueButton.Frame.Y, emailButton.Frame.Width, emailButton.Frame.Height);
			} else {
				mobileButton.Frame = new CGRect (continueButton.Frame.X + continueButton.Frame.Width + UI_CONSTANTS.EXTRA_MARGIN, continueButton.Frame.Y, MOBILE_EMAIL_BUTTON_SIZE, MOBILE_EMAIL_BUTTON_SIZE);
				emailButton.Frame = new CGRect (continueButton.Frame.X + continueButton.Frame.Width + UI_CONSTANTS.EXTRA_MARGIN + mobileButton.Frame.X + mobileButton.Frame.Width + UI_CONSTANTS.EXTRA_MARGIN, continueButton.Frame.Y, MOBILE_EMAIL_BUTTON_SIZE, MOBILE_EMAIL_BUTTON_SIZE);
			}

			nfloat positionOfColoredButtons = continueButton.Frame.Y + (View.Frame.Height - continueButton.Frame.Y) / 2 - UI_CONSTANTS.TINY_MARGIN;
			orangeButton.Frame = new CGRect (View.Frame.Width / 2 - UI_CONSTANTS.BUTTON_HORIZONTAL_PADDING/2 - orangeButton.Frame.Width, positionOfColoredButtons, UI_CONSTANTS.COLORED_SQUARE_SIZE, UI_CONSTANTS.COLORED_SQUARE_SIZE);
			blueButton.Frame = new CGRect (orangeButton.Frame.X - UI_CONSTANTS.BUTTON_HORIZONTAL_PADDING - blueButton.Frame.Width, positionOfColoredButtons, UI_CONSTANTS.COLORED_SQUARE_SIZE, UI_CONSTANTS.COLORED_SQUARE_SIZE);
			pinkButton.Frame = new CGRect (View.Frame.Width / 2 + UI_CONSTANTS.BUTTON_HORIZONTAL_PADDING / 2, positionOfColoredButtons, UI_CONSTANTS.COLORED_SQUARE_SIZE, UI_CONSTANTS.COLORED_SQUARE_SIZE);
			greenButton.Frame = new CGRect (pinkButton.Frame.X + pinkButton.Frame.Width + UI_CONSTANTS.BUTTON_HORIZONTAL_PADDING, positionOfColoredButtons, UI_CONSTANTS.COLORED_SQUARE_SIZE, UI_CONSTANTS.COLORED_SQUARE_SIZE);
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}
	}
}