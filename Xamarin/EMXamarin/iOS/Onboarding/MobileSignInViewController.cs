using System;
using CoreGraphics;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using UIKit;

namespace iOS {
	using UIDevice_Extension;
	public class MobileSignInViewController : SignInViewController {

		readonly AppDelegate appDelegate;
		readonly MobileSignInModel model;

		UILabel DialingCodeLabel;

		public MobileSignInViewController () {
			appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;

			model = new MobileSignInModel ();

			model.DidFailToRegister += () => {
				var alert = new UIAlertView ("APP_TITLE".t (), "ERROR_REGISTER_MOBILE_EXPLAINATION".t (), null, "OK_BUTTON".t (), null);
				alert.Clicked += (s, b) => { };
				alert.Show ();
			};

			model.ShouldPauseUI = PauseUI;
			model.ShouldResumeUI = ResumeUI;
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			Title = "MOBILE_TITLE".t ();

			InstructionLabel.Text = "REGISTER_MOBILE_EXPLAINATION".t ();
			var labelFontAttributes = new UIStringAttributes ();
			labelFontAttributes.Font = FontHelper.DefaultFontWithSize (InstructionLabelFontSize);
			CGSize labelSize = new NSString (InstructionLabel.Text).GetSizeUsingAttributes (labelFontAttributes);
			// labelSize.Height is going to return the height of the label if it was on one line. 
			// Multiply the height by 2 to get the instructionLabel's height because we're expecting the instruction label to be no longer than two lines.
			// Then add some padding to give it space between the other elements.
			InstructionLabel.Frame = new CGRect (0, 0, View.Frame.Width - UI_CONSTANTS.LABEL_PADDING*2, labelSize.Height*2 + UI_CONSTANTS.LABEL_PADDING);

			DialingCodeLabel = new UILabel ();
			DialingCodeLabel.Text = "+" + CurrentCountry.phonePrefix;
			DialingCodeLabel.TextAlignment = UITextAlignment.Center;
			nfloat dialingCodeLabelFontSize = FontHelper.DefaultFontSizeForLabels-3;
			DialingCodeLabel.Font = FontHelper.DefaultFontForLabels (dialingCodeLabelFontSize);
			DialingCodeLabel.TextColor = FontHelper.BlackTextColor ();
			DialingCodeLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			DialingCodeLabel.LineBreakMode = UILineBreakMode.Clip;
			var dialingLabelAttributes = new UIStringAttributes ();
			dialingLabelAttributes.Font = FontHelper.DefaultFontForLabels (dialingCodeLabelFontSize);
			CGSize dialingLabelSize = new NSString (DialingCodeLabel.Text).GetSizeUsingAttributes (dialingLabelAttributes);
			DialingCodeLabel.Frame = new CGRect (0, 0, dialingLabelSize.Width, dialingLabelSize.Height);
			BottomBar.Add (DialingCodeLabel);

			nfloat FieldHeight = BottomBar.Frame.Height - (TEXTFIELD_PADDING * 2); // padding times 2 because we want padding on the top and bottom of the text entry field
			nfloat pixelsToSubtractToGetTextEntryWidth = PADDING*4 + DialingCodeLabel.Frame.Width + ContinueButton.Frame.Width;
			TextEntryWidth = UIScreen.MainScreen.Bounds.Width - pixelsToSubtractToGetTextEntryWidth;

			TextField.Frame = new CGRect (DialingCodeLabel.Frame.X + DialingCodeLabel.Frame.Width + PADDING, BottomBar.Frame.Height / 2 - FieldHeight / 2, TextEntryWidth, FieldHeight);
			TextField.KeyboardType = UIKeyboardType.PhonePad;
			TextField.AttributedPlaceholder = new NSAttributedString ("MOBILE_NUMBER".t (), FontHelper.DefaultFontForTextFields (), iOS_Constants.GRAY_COLOR, null, null, null, NSLigatureType.Default, 0, 0, null, 0, 0);
			TextField.WeakDelegate = this;

			ContinueButton.Enabled = EnableContinueButton ();
			ContinueButton.TouchUpInside += (sender, e) => {
				if (AppEnv.SKIP_ONBOARDING) {
					DidConfirmMobile ();
					return;
				}

				var title = "VERIFICATION_TITLE".t ();
				var numberToMessage = string.Format("+{0}{1}", SelectedCountry.phonePrefix, TextField.Text);
				var message = string.Format("SEND_VALIDATION_CODE_EXPLAINATION".t (), numberToMessage);
				var action = "YES".t ();

				var alert = new UIAlertView (title, message, null, "EDIT_BUTTON".t (), new [] { action });
				alert.Show ();
				alert.Clicked += (sender2, buttonArgs) =>  { 
					switch ( buttonArgs.ButtonIndex ) {
					case 1:
						DidConfirmMobile();
						break;
					}
				};
			};
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			DialingCodeLabel.Frame = new CGRect (PADDING, BottomBar.Frame.Height / 2  - DialingCodeLabel.Frame.Height / 2, DialingCodeLabel.Frame.Width, DialingCodeLabel.Frame.Height);
			TextField.Frame = new CGRect (DialingCodeLabel.Frame.X + DialingCodeLabel.Frame.Width + PADDING, BottomBar.Frame.Height / 2 - TextField.Frame.Height / 2, BottomBar.Frame.Width - (PADDING * 3) - DialingCodeLabel.Frame.Width - ContinueButton.Frame.Width, TextField.Frame.Height);
			ContinueButton.Frame = new CGRect (BottomBar.Frame.Width - PADDING / 2 - ContinueButton.Frame.Width, BottomBar.Frame.Height - ContinueButton.Frame.Height, ContinueButton.Frame.Width, ContinueButton.Frame.Height);
		
			// make the text field image slightly larger than the text entry so that text isn't out of the image frame
			CGRect textFieldImageFrame = TextField.Frame;
			textFieldImageFrame.Y -= 1.5f;
			textFieldImageFrame.Height += 3f;
			TextFieldImage.Frame = textFieldImageFrame;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			var mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResource ( (UIImage image) => {
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
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Mobile Sign In View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());

			if (AppEnv.SKIP_ONBOARDING) {
				this.TextField.Text = AppEnv.SkipOnboardingMobileToRegisterWith;
				this.ContinueButton.Enabled = true;
				this.ContinueButton.SendActionForControlEvents (UIControlEvent.TouchUpInside);
			}
		}

		public override void DidTapDone(object sender, EventArgs e) {
			base.DidTapDone (sender, e);

			DialingCodeLabel.Text = UIDevice.CurrentDevice.IsRightLeftLanguage() ? SelectedCountry.phonePrefix + "+" : "+" + SelectedCountry.phonePrefix;
			DialingCodeLabel.SizeToFit ();
		}

		public override void TouchesBegan (NSSet touches, UIEvent evt) {
			// hide the keyboard when there are touches outside the keyboard view
			base.TouchesBegan (touches, evt);
			View.EndEditing (true);
		}

		#region TextField
		[Export("textField:shouldChangeCharactersInRange:replacementString:")]
		bool ShouldChangeCharacters (UITextField textField, NSRange range, string replacementString) {
			// We're managing the text field replacement ourselves, so we return false.
			var textString = new NSString (textField.Text);
			textField.Text = textString.Replace (range, new NSString (replacementString));
			UpdateContinueButtonStatus ();
			return false;
		}

		[Export("textFieldShouldReturn:")]
		bool ShouldReturn (UITextField textField) {
			textField.ResignFirstResponder ();
			ContinueButton.SendActionForControlEvents (UIControlEvent.TouchUpInside);
			return true;
		}
		#endregion

		void UpdateContinueButtonStatus () {
			ContinueButton.Enabled = EnableContinueButton ();
		}

		bool EnableContinueButton () {
			return SelectedCountry != null && model.InputIsValid (SelectedCountry.phonePrefix, TextField.Text);
		}

		void DidConfirmMobile() {
			model.Register (appDelegate.applicationModel.account, TextField.Text, SelectedCountry.countryCode, SelectedCountry.phonePrefix, accountID => {
				var mobileVerificationViewController = new MobileVerificationViewController ();
				mobileVerificationViewController.accountID = accountID;
				NavigationController.PushViewController (mobileVerificationViewController, true);
			});
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}
	}
}