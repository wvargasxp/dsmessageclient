using System;
using CoreGraphics;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using UIKit;

namespace iOS {

	public class EmailSignInViewController : SignInViewController {
		AppDelegate appDelegate;

		EmailSignInModel model;
			
		public EmailSignInViewController () {
			appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;

			model = new EmailSignInModel ();

			model.DidFailToRegister += () => {
				var alert = new UIAlertView ("APP_TITLE".t (), "ERROR_REGISTER_EMAIL_EXPLAINATION".t (), null, "OK_BUTTON".t (), null);
				alert.Clicked += (s, b) => { };
				alert.Show ();
			};

			model.ShouldPauseUI = PauseUI;
			model.ShouldResumeUI = ResumeUI;
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			Title = "EMAIL_TITLE".t ();

			InstructionLabel.Text = "REGISTER_EMAIL_EXPLAINATION".t ();
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
			TextField.KeyboardType = UIKeyboardType.EmailAddress;
			TextField.ReturnKeyType = UIReturnKeyType.Go;
			TextField.AttributedPlaceholder = new NSAttributedString ("EMAIL_ADDRESS".t (), FontHelper.DefaultFontForTextFields (), iOS_Constants.GRAY_COLOR, null, null, null, NSLigatureType.Default, 0, 0, null, 0, 0);
			TextField.WeakDelegate = this;

			UpdateContinueButtonStatus ();
			ContinueButton.TouchUpInside += (sender, e) => {
				var title = "VERIFICATION_TITLE".t ();
				var message = string.Format("SEND_VALIDATION_CODE_EXPLAINATION".t (), TextField.Text);
				var action = "YES".t ();

				var alert = new UIAlertView (title, message, null, "EDIT_BUTTON".t (), new [] { action });
				alert.Show ();
				alert.Clicked += (sender2, buttonArgs) =>  { 
					switch ( buttonArgs.ButtonIndex ) {
					case 1:
						DidConfirmEmail();
						break;
					}
				};
			};
		}

		public override void TouchesBegan (NSSet touches, UIEvent evt) {
			// hide the keyboard when there are touches outside the keyboard view
			base.TouchesBegan (touches, evt);
			View.EndEditing (true);
		}
			
		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			TextField.Frame = new CGRect (PADDING, BottomBar.Frame.Height / 2 - TextField.Frame.Height / 2, BottomBar.Frame.Width - (PADDING * 2) - ContinueButton.Frame.Width, TextField.Frame.Height);
			ContinueButton.Frame = new CGRect (BottomBar.Frame.Width - PADDING / 2 - ContinueButton.Frame.Width, BottomBar.Frame.Height - ContinueButton.Frame.Height, ContinueButton.Frame.Width, ContinueButton.Frame.Height);
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResource( (UIImage image) => {
				if (View != null && LineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					LineView.BackgroundColor = mainColor.GetColor ();
				}
			});
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Email Sign In View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());

			if (AppEnv.SKIP_ONBOARDING) {
				this.TextField.Text = AppEnv.SkipOnboardingEmailToRegisterWith;
				this.ContinueButton.Enabled = true;
				this.ContinueButton.SendActionForControlEvents (UIControlEvent.TouchUpInside);
			}
		}
			
		void UpdateContinueButtonStatus () {
			ContinueButton.Enabled = model.InputIsValid (TextField.Text);
		}

		void DidConfirmEmail() {
			model.Register (appDelegate.applicationModel.account, TextField.Text, SelectedCountry.countryCode, SelectedCountry.phonePrefix, accountID => {
				var mobileVerificationViewController = new MobileVerificationViewController ();
				mobileVerificationViewController.accountID = accountID;
				NavigationController.PushViewController (mobileVerificationViewController, true);
			});
		}
			
		#region TextField
		// Text Field Delegate Methods
		[Export("textFieldDidEndEditing:")]
		void EditingEnded (UITextField textField) {

		}

		[Export("textFieldDidBeginEditing:")]
		void EditingStarted (UITextField textField) {

		}

		[Export("textFieldShouldBeginEditing:")]
		bool ShouldBeginEditing (UITextField textField) {
			return true;  // stub
		}

		[Export("textField:shouldChangeCharactersInRange:replacementString:")]
		bool ShouldChangeCharacters (UITextField textField, NSRange range, string replacementString) {
			// We're managing the text field replacement ourselves, so we return false.
			var textString = new NSString (textField.Text);
			textField.Text = textString.Replace (range, new NSString (replacementString));
			UpdateContinueButtonStatus ();
			return false;
		}

		[Export("textFieldShouldClear:")]
		bool ShouldClear (UITextField textField) {
			return true;  // stub
		}

		[Export("textFieldShouldEndEditing:")]
		bool ShouldEndEditing (UITextField textField) {
			return true;  // stub
		}

		[Export("textFieldShouldReturn:")]
		bool ShouldReturn (UITextField textField) {
			if(model.InputIsValid (textField.Text)) {
				textField.ResignFirstResponder ();
				ContinueButton.SendActionForControlEvents (UIControlEvent.TouchUpInside);
				return true;
			}

			return false;
		}
		#endregion

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}
	}
}