using System;
using System.Collections.Generic;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using GoogleAnalytics.iOS;
using UIKit;

namespace iOS {
	public class MobileVerificationViewController : SignInViewController {

		NSObject appDidBecomeActiveObserver = null;

		AppDelegate appDelegate;

		public string accountID;

		UIImageView AppIcon;
		UILabel IdentifierLabel;

		private SharedVerificationController Shared { get; set; }

		public MobileVerificationViewController () {
			appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
			this.Shared = new SharedVerificationController (appDelegate.applicationModel, this);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			Title = "VERIFICATION_TITLE".t ();

			HideFlag ();

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			AppIcon = new UIImageView (new CGRect (0, 0, 100, 100));
			AppIcon.Image = UIImage.FromFile ("Icon-100.png");
			AppIcon.Layer.MasksToBounds = true;
			View.Add (AppIcon);

			IdentifierLabel = new UILabel ();
			var IdentifierLabelFontSize = FontHelper.DefaultFontSizeForLabels - 1;
			if (IsSmallScreen)
				IdentifierLabelFontSize -= SIZE_TO_REDUCE;
			IdentifierLabel.Font = FontHelper.DefaultFontWithSize (IdentifierLabelFontSize);
			IdentifierLabel.TextColor = FontHelper.DefaultTextColor ();
			IdentifierLabel.LineBreakMode = UILineBreakMode.WordWrap;
			IdentifierLabel.Lines = 0;
			IdentifierLabel.TextAlignment = UITextAlignment.Center;
			IdentifierLabel.Text = accountID;
			var identifierFontAttributes = new UIStringAttributes ();
			identifierFontAttributes.Font = FontHelper.DefaultFontWithSize (IdentifierLabelFontSize);
			CGSize identifierSize = new NSString (IdentifierLabel.Text).GetSizeUsingAttributes (identifierFontAttributes);
			// labelSize.Height is going to return the height of the label if it was on one line. 
			// Multiply the height by 2 to get the instructionLabel's height because we're expecting the instruction label to be no longer than two lines.
			// Then add some padding to give it space between the other elements.
			IdentifierLabel.Frame = new CGRect (0, 0, View.Frame.Width - UI_CONSTANTS.LABEL_PADDING*2, identifierSize.Height);
			View.Add (IdentifierLabel);

			InstructionLabel.Text = "VERIFY_ACCOUNT_EXPLAINATION".t ();
			var labelFontAttributes = new UIStringAttributes ();
			labelFontAttributes.Font = FontHelper.DefaultFontWithSize (InstructionLabelFontSize);
			CGSize labelSize = new NSString (InstructionLabel.Text).GetSizeUsingAttributes (labelFontAttributes);
			// labelSize.Height is going to return the height of the label if it was on one line. 
			// Multiply the height by 2 to get the instructionLabel's height because we're expecting the instruction label to be no longer than two lines.
			// Then add some padding to give it space between the other elements.
			InstructionLabel.Frame = new CGRect (0, 0, View.Frame.Width - UI_CONSTANTS.LABEL_PADDING*2, labelSize.Height*2 + UI_CONSTANTS.LABEL_PADDING);

			nfloat FieldHeight = BottomBar.Frame.Height - (TEXTFIELD_PADDING * 2); // padding times 2 because we want padding on the top and bottom of the text entry field
			nfloat pixelsToSubtractToGetTextEntryWidth = PADDING*4 + ContinueButton.Frame.Width;
			TextEntryWidth = UIScreen.MainScreen.Bounds.Width - pixelsToSubtractToGetTextEntryWidth;

			TextField.Frame = new CGRect (PADDING, BottomBar.Frame.Height / 2 - FieldHeight / 2, TextEntryWidth, FieldHeight);
			TextField.AutocorrectionType = UITextAutocorrectionType.No;
			TextField.AutocapitalizationType = UITextAutocapitalizationType.None;
			TextField.KeyboardType = UIKeyboardType.Default;
			TextField.ReturnKeyType = UIReturnKeyType.Go;
			TextField.AttributedPlaceholder = new NSAttributedString ("VERIFICATION_CODE".t (), FontHelper.DefaultFontForTextFields (), iOS_Constants.GRAY_COLOR, null, null, null, NSLigatureType.Default, 0, 0, null, 0, 0);
			TextField.WeakDelegate = this;

			this.Shared.AccountID = accountID;

			UpdateContinueButtonStatus ();

			ContinueButton.TouchUpInside += (sender, e) => {
				this.Shared.TryToLogin (this.TextField.Text);
			};

			ISecurityManager securityManager = appDelegate.applicationModel.platformFactory.GetSecurityManager ();
			// reset verification code parameter here to prevent confusing behavior when the verfiy url is triggered while the app on a page other than verification.
			securityManager.RemoveSecureKeyValue (Constants.URL_QUERY_VERIFICATION_CODE_KEY);
			securityManager.removeSecureField (Constants.URL_QUERY_VERIFICATION_CODE_KEY);
		}

		public override void TouchesBegan (NSSet touches, UIEvent evt) {
			// hide the keyboard when there are touches outside the keyboard view
			base.TouchesBegan (touches, evt);
			View.EndEditing (true);
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			float displacement_y = (float)TopLayoutGuide.Length;

			AppIcon.Frame = new CGRect ((View.Frame.Width - AppIcon.Frame.Width) / 2, displacement_y + UI_CONSTANTS.EXTRA_MARGIN, AppIcon.Frame.Width, AppIcon.Frame.Height);
			IdentifierLabel.Frame = new CGRect (View.Frame.Width / 2 - IdentifierLabel.Frame.Width / 2, AppIcon.Frame.Y + AppIcon.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN, IdentifierLabel.Frame.Width, IdentifierLabel.Frame.Height);
			InstructionLabel.Frame = new CGRect (View.Frame.Width / 2 - InstructionLabel.Frame.Width / 2, IdentifierLabel.Frame.Y + IdentifierLabel.Frame.Height + UI_CONSTANTS.SMALL_MARGIN, InstructionLabel.Frame.Width, InstructionLabel.Frame.Height);


			TextField.Frame = new CGRect (PADDING, BottomBar.Frame.Height / 2 - TextField.Frame.Height / 2, BottomBar.Frame.Width - (PADDING * 2) - ContinueButton.Frame.Width, TextField.Frame.Height);
			ContinueButton.Frame = new CGRect (BottomBar.Frame.Width - PADDING / 2 - ContinueButton.Frame.Width, BottomBar.Frame.Height - ContinueButton.Frame.Height, ContinueButton.Frame.Width, ContinueButton.Frame.Height);
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResource ( (UIImage image) => {
				if (View != null && LineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					LineView.BackgroundColor = mainColor.GetColor ();
				}
			});
				
			if (AppEnv.SKIP_ONBOARDING) {} 
			else {
				var alert = new UIAlertView ("APP_TITLE".t (), "SEND_VERIFICATION_EXPLAINATION".t (), null, "OK_BUTTON".t (), null);
				alert.Show();
			}

			appDidBecomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)"UIApplicationDidBecomeActiveNotification", checkVerificationCodeReceivedViaUrl);
		}
			
		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Mobile Verification View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());

			if (AppEnv.SKIP_ONBOARDING) {
				this.TextField.Text = AppEnv.SkipOnboardingVerificationCode;
				this.ContinueButton.Enabled = true;
				this.ContinueButton.SendActionForControlEvents (UIControlEvent.TouchUpInside);
			}
		}

		public override void ViewWillDisappear (bool animated) {
			NSNotificationCenter.DefaultCenter.RemoveObserver (appDidBecomeActiveObserver);

			base.ViewWillDisappear (animated);
		}

		void UpdateContinueButtonStatus () {
			ContinueButton.Enabled = this.Shared.InputIsValid (TextField.Text);
		}

		#region TextField
		[Export("textField:shouldChangeCharactersInRange:replacementString:")]
		bool ShouldChangeCharacters (UITextField textField, NSRange range, string replacementString) {
			UpdateContinueButtonStatus ();
			return true;
		}

		[Export("textFieldShouldReturn:")]
		bool ShouldReturn (UITextField textField) {
			if(this.Shared.InputIsValid (textField.Text)) {
				textField.ResignFirstResponder ();
				ContinueButton.SendActionForControlEvents (UIControlEvent.TouchUpInside);
				return true;
			}

			return false;
		}
		#endregion

		void checkVerificationCodeReceivedViaUrl (NSNotification notification) {
			this.Shared.CheckVerificationCodeReceivedViaUrl ();
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		public void TriggerContinueButton () {
			this.ContinueButton.SendActionForControlEvents (UIControlEvent.TouchUpInside);
		}

		public void UpdateTextFieldWithText (string text) {
			this.TextField.Text = text;
		}

		public void DisplayAccountError () {
			var alert = new UIAlertView ("APP_TITLE".t (), "ERROR_VERIFY_ACCOUNT_EXPLAINATION".t (), null, "OK_BUTTON".t (), null);
			alert.Clicked += (s, b) => { };
			alert.Show ();
		}

		public void DismissControllerAndFinishOnboarding () {
			if (appDelegate.MainController.PresentedViewController != null) {
				// TODO: This really hides the issue rather than solving it. Why is dispose not already called for 
				// 1) LandingPageViewController 2) MobileSignInViewController 3) MobileVerificationViewController
				foreach (UIViewController controller in ((UINavigationController)(appDelegate.MainController.PresentedViewController)).ViewControllers) {
					controller.Dispose ();
				}
				appDelegate.MainController.DismissViewController (false, null);
			}

			appDelegate.MainController.FinishOnboarding (true);
		}

		public void GoToAccountController () {
			this.NavigationController.PushViewController (new AccountViewController (true), true);
		}
	}

	 class SharedVerificationController : AbstractMobileVerificationController {
		private WeakReference _r = null;
		private MobileVerificationViewController Controller {
			get { return this._r != null ? this._r.Target as MobileVerificationViewController : null; }
			set {
				this._r = new WeakReference (value);
			}
		}

		public SharedVerificationController (ApplicationModel g, MobileVerificationViewController t) : base (g) {
			this.Controller = t;
		}

		public override void ShouldPauseUI () {
			MobileVerificationViewController c = this.Controller;
			if (c != null && c.IsViewLoaded) {
				c.PauseUI ();
			}
		}

		public override void ShouldResumeUI () {
			MobileVerificationViewController c = this.Controller;
			if (c != null && c.IsViewLoaded) {
				c.ResumeUI ();
			}
		}

		public override void TriggerContinueButton () {
			MobileVerificationViewController c = this.Controller;
			if (c != null && c.IsViewLoaded) {
				c.TriggerContinueButton ();
			}
		}

		public override void UpdateTextFieldWithText (string text) {
			MobileVerificationViewController c = this.Controller;
			if (c != null && c.IsViewLoaded) {
				c.UpdateTextFieldWithText (text);
			}
		}

		public override void DisplayAccountError () {
			MobileVerificationViewController c = this.Controller;
			if (c != null && c.IsViewLoaded) {
				c.DisplayAccountError ();
			}
		}

		public override void DismissControllerAndFinishOnboarding () {
			MobileVerificationViewController c = this.Controller;
			if (c != null && c.IsViewLoaded) {
				c.DismissControllerAndFinishOnboarding ();
			}
		}

		public override void GoToAccountController () {
			MobileVerificationViewController c = this.Controller;
			if (c != null && c.IsViewLoaded) {
				c.GoToAccountController ();
			}
		}
	}
}