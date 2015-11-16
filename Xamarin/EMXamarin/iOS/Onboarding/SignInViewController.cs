using System;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using GoogleAnalytics.iOS;
using MBProgressHUD;
using UIDevice_Extension;
using UIKit;

namespace iOS {
	public class SignInViewController : UIViewController {

		public const int COUNTRYIMAGE_WIDTH = 150;
		public const int COUNTRYIMAGE_HEIGHT = 100;
		public const int SIZE_TO_REDUCE = 3;
		public const int TEXTFIELD_PADDING = 6; // The text entry field will have a padding of 6 above and below it.
		public const float PADDING = 10; // Default variable used when adding unspecific padding.

		// For iPhone, a size of 48 perfectly matches the font where the bottom toolbar will not be resized when a user presses a key on the keyboard.
		// For example, if it's set at 45 and the user presses a letter, the bottom toolbar will resize a few pixels to the new height of 48.
		public const int TOOLBAR_HEIGHT_IPHONE = 48;

		public readonly int TEXT_ENTRY_Y_ORIGIN = 6;
		public readonly int CONTINUE_BUTTON_SIZE = 48;

		nfloat hiddenCountryLabelFontSize = FontHelper.DefaultFontSizeForLabels;

		public nfloat TextEntryWidth;

		float keyboardOrigin;

		const int margin = -40;

		// callback methods that get called when keyboard updates its state
		NSObject keyboardWillShowCallback;
		NSObject keyboardWillHideCallback;

		// keeping track of the bottombar for when laying out subviews
		CGRect expectedBottomBarFrame;

		public bool IsSmallScreen;
		public nfloat InstructionLabelFontSize;

		public UIView LineView;
		public CountryCodePickerModel PickerModel;
		public UIPickerView Picker;
		public CountryCode SelectedCountry;
		public CountryCode CurrentCountry;
		public UILabel InstructionLabel;
		public UIImageView CountryImage;
		public UIImage ScaledImage;
		public UITextField HiddenCountryLabel;

		public UIView BottomBar;
		public UIImageView TextFieldImage;
		public UITextField TextField;
		public UIButton ContinueButton;

		MTMBProgressHUD progressHud;

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;

			IsSmallScreen = UIDevice.CurrentDevice.IsSmallScreen ();

			View.MultipleTouchEnabled = true;

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			LineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;

			InstructionLabel = new UILabel ();
			InstructionLabelFontSize = FontHelper.DefaultFontSizeForLabels - 2;
			if (IsSmallScreen)
				InstructionLabelFontSize -= SIZE_TO_REDUCE;
			InstructionLabel.Font = FontHelper.DefaultFontWithSize (InstructionLabelFontSize);
			InstructionLabel.TextColor = FontHelper.DefaultTextColor ();
			InstructionLabel.LineBreakMode = UILineBreakMode.WordWrap;
			InstructionLabel.Lines = 0;
			InstructionLabel.TextAlignment = UITextAlignment.Center;

			CountryImage = new UIImageView (new CGRect (0, 0, COUNTRYIMAGE_WIDTH, COUNTRYIMAGE_HEIGHT));
			if (IsSmallScreen)
				CountryImage.Frame = new CGRect (0, 0, CountryImage.Frame.Width * .55f, CountryImage.Frame.Height * .55f);
			CountryImage.UserInteractionEnabled = true;

			HiddenCountryLabel = new UITextField ();
			HiddenCountryLabel.Text = "UNITED_STATES".t (); // placeholder
			HiddenCountryLabel.BackgroundColor = UIColor.Clear;
			HiddenCountryLabel.TextColor = FontHelper.DefaultTextColor ();
			HiddenCountryLabel.TintColor = UIColor.Clear;
			if (IsSmallScreen)
				hiddenCountryLabelFontSize -= SIZE_TO_REDUCE;
			HiddenCountryLabel.Font = FontHelper.DefaultFontForLabels (hiddenCountryLabelFontSize);

			// This section is getting the width/height of the label and setting the button's frames to be slightly larger than that.
			var hiddenCountryAttributes = new UIStringAttributes ();
			hiddenCountryAttributes.Font = FontHelper.DefaultFontForLabels (hiddenCountryLabelFontSize);
			CGSize textSize = new NSString (HiddenCountryLabel.Text).GetSizeUsingAttributes (hiddenCountryAttributes);
			HiddenCountryLabel.Frame = new CGRect (0, 0, textSize.Width + UI_CONSTANTS.SMALL_MARGIN, textSize.Height + UI_CONSTANTS.SMALL_MARGIN);


			BottomBar = new UIView (new CGRect (0, View.Frame.Height - TOOLBAR_HEIGHT_IPHONE, View.Frame.Width, TOOLBAR_HEIGHT_IPHONE));
			BottomBar.BackgroundColor = UIColor.FromRGBA (Constants.RGB_TOOLBAR_COLOR [0], Constants.RGB_TOOLBAR_COLOR[1], Constants.RGB_TOOLBAR_COLOR[2], 255);
			BottomBar.Layer.BorderColor = UIColor.Gray.ColorWithAlpha (0.5f).CGColor;
			BottomBar.Layer.BorderWidth = 0.5f;
			BottomBar.AutoresizingMask = UIViewAutoresizing.FlexibleBottomMargin;


			TextField = new UITextField ();
			TextField.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			TextField.ClipsToBounds = true;
			TextField.Font = FontHelper.DefaultFontForTextFields ();
			TextField.BackgroundColor = UIColor.Clear;
			TextField.Opaque = false;

			var padding = new UIView (new CGRect (0, 0, 5, 5));
			TextField.LeftView = padding;
			TextField.LeftViewMode = UITextFieldViewMode.Always;


			TextFieldImage = new UIImageView (TextField.Frame);
			TextFieldImage.Image = UIImage.FromFile ("chat/text-field.png").StretchableImage (UI_CONSTANTS.ONBOARDING_TEXT_FIELD_LEFT_CAP, UI_CONSTANTS.ONBOARDING_TEXT_FIELD_TOP_CAP);
			TextFieldImage.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			BottomBar.Add (TextFieldImage);
			BottomBar.Add (TextField); //add text field after text field image so it's on top & you can see the characters you type

			UIEdgeInsets imageEdgeInsets = new UIEdgeInsets (margin, margin, margin, margin);

			ContinueButton = new UIButton (UIButtonType.Custom);
			ContinueButton.Frame = new CGRect (0, 0, CONTINUE_BUTTON_SIZE, CONTINUE_BUTTON_SIZE);
			appDelegate.applicationModel.account.accountInfo.colorTheme.GetChatSendButtonResource ((UIImage image) => {
				if (ContinueButton != null) {
					ContinueButton.SetImage (image, UIControlState.Normal);
				}
			});
			ContinueButton.SetImage (ImageSetter.GetResourceImage ("chat/iconSendDisabled.png"), UIControlState.Disabled);
			ContinueButton.ImageView.ContentMode = UIViewContentMode.Center;
			ContinueButton.ImageEdgeInsets = imageEdgeInsets;
			ContinueButton.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			ContinueButton.TitleLabel.Font = UIFont.BoldSystemFontOfSize (17.5f); // trying to get the size close to iMessage app
			ContinueButton.SetTitleColor (UIColor.Gray, UIControlState.Normal);
			BottomBar.Add (ContinueButton);


			View.Add (LineView);
			View.Add (InstructionLabel);
			View.Add (CountryImage);
			View.Add (HiddenCountryLabel);
			View.Add (BottomBar);

			string countryCode = NSLocale.CurrentLocale.CountryCode.ToLower ();
			CurrentCountry = CountryCode.getCountryFromCode (countryCode) ?? CountryCode.getCountryFromCode ("us");
			SelectedCountry = CurrentCountry;

			HiddenCountryLabel.Text = CurrentCountry.translationKey.t ();
			HiddenCountryLabel.Placeholder = CurrentCountry.countryCode;

			var img = UIImage.FromFile ("flags/" + CurrentCountry.photoUrl);
			ScaledImage = MaxResizeImage (img, COUNTRYIMAGE_WIDTH, COUNTRYIMAGE_HEIGHT);
			CountryImage.Image = ScaledImage;

			Picker = new UIPickerView ();
			PickerModel = new CountryCodePickerModel (CountryCode.countries);
			Picker.Model = PickerModel;
			Picker.ShowSelectionIndicator = true;

			var toolbar = new UIToolbar ();
			toolbar.BarStyle = UIBarStyle.Default;
			toolbar.Translucent = true;
			toolbar.SizeToFit ();

			var doneButton = new UIBarButtonItem ("DONE_BUTTON".t (), UIBarButtonItemStyle.Done, DidTapDone);
			doneButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			toolbar.SetItems (new []{ doneButton }, true);

			HiddenCountryLabel.InputView = Picker;
			HiddenCountryLabel.InputAccessoryView = toolbar;
			HiddenCountryLabel.TouchDown += SetPicker;

			var imageTapped = new UITapGestureRecognizer (() => {
				// Piggy back off hiddenCountryLabel's actions.
				// The first call, sends a TouchDownEvent which gets the SetPicker function to be called (this gets the correct flag).
				// The second call then makes the hiddenCountryLabel (is a UITextField) become first responder, which raises the UIPickerView.
				HiddenCountryLabel.SendActionForControlEvents (UIControlEvent.TouchDown);
				HiddenCountryLabel.BecomeFirstResponder ();
			});
			CountryImage.AddGestureRecognizer (imageTapped);
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			#region keyboard callbacks
			keyboardWillShowCallback = UIKeyboard.Notifications.ObserveWillShow ((UpdateViewFramesBasedOnKeyboardUpdate));
			keyboardWillHideCallback = UIKeyboard.Notifications.ObserveWillHide ((UpdateViewFramesBasedOnKeyboardUpdate));
			#endregion
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Sign In View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());

			TextField.BecomeFirstResponder (); //show the keyboard
		}

		public override void ViewWillDisappear (bool animated) {
			TextField.ResignFirstResponder ();

			base.ViewWillDisappear (animated);
		}

		public override void ViewDidDisappear (bool animated) {
			keyboardWillHideCallback.Dispose ();
			keyboardWillShowCallback.Dispose ();

			base.ViewDidDisappear (animated);
		}

		public override void ViewWillLayoutSubviews () {
			base.ViewDidLayoutSubviews ();
			float displacement_y = (float)TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, LineView.Frame.Width, LineView.Frame.Height);
			InstructionLabel.Frame = new CGRect (View.Frame.Width / 2 - InstructionLabel.Frame.Width / 2, displacement_y + UI_CONSTANTS.TINY_MARGIN, InstructionLabel.Frame.Width, InstructionLabel.Frame.Height);
			CountryImage.Frame = new CGRect (View.Frame.Width / 2 - CountryImage.Frame.Width / 2, InstructionLabel.Frame.Y + InstructionLabel.Frame.Height + UI_CONSTANTS.TINY_MARGIN, CountryImage.Frame.Width, CountryImage.Frame.Height);
			HiddenCountryLabel.Frame = new CGRect (View.Frame.Width / 2 - HiddenCountryLabel.Frame.Width / 2, CountryImage.Frame.Y + CountryImage.Frame.Height + UI_CONSTANTS.SMALL_MARGIN, HiddenCountryLabel.Frame.Width, HiddenCountryLabel.Frame.Height);


			nfloat BottomBarHeight = TextField.Frame.Height + (TEXT_ENTRY_Y_ORIGIN * 2);

			nfloat BottomBarY = View.Frame.Height - TOOLBAR_HEIGHT_IPHONE;
			if(keyboardOrigin > 0)
				BottomBarY = keyboardOrigin - BottomBarHeight;

			BottomBar.Frame = new CGRect (BottomBar.Frame.X, BottomBarY, View.Frame.Width, TOOLBAR_HEIGHT_IPHONE);

			// make the text field image slightly larger than the text entry so that text isn't out of the image frame
			CGRect textFieldImageFrame = TextField.Frame;
			textFieldImageFrame.Y -= 1.5f;
			textFieldImageFrame.Height += 3f;
			TextFieldImage.Frame = textFieldImageFrame;
		}

		public void UpdateViewFramesBasedOnKeyboardUpdate (object sender, UIKeyboardEventArgs args) {
			CGRect convertedFrame = View.ConvertRectFromView (args.FrameEnd, View.Window);
			keyboardOrigin = (float)convertedFrame.Y;

			UIViewAnimationOptions option = UIViewAnimationOptions.BeginFromCurrentState;

			// This if/else is handling the case where the textentrybar/bottombar is taller than it should be (past the topbar's y origin, or past the top layout guide), it sizes it down before animating
			// This prevents a choppy up -> down animation from doing the animation below, and then laying out subviews to another frame size.
			float minYValue = (float) (CountryImage.Frame.Y + CountryImage.Frame.Height);
			float BottomBarY = (float) (keyboardOrigin - BottomBar.Frame.Height);
			float BottomBarHeightDifference = 0;
			if (BottomBarY < minYValue) {
				// The bottom bar has grown past (above) the top bar.
				float oldBottomBarY = BottomBarY;
				BottomBarY = minYValue; // Set the chattyTopBar as the max it can grow to.
				BottomBarHeightDifference = BottomBarY - oldBottomBarY;
			}
			expectedBottomBarFrame = new CGRect (BottomBar.Frame.X, BottomBarY, View.Frame.Width, BottomBar.Frame.Height - BottomBarHeightDifference);

			UIView.AnimateNotify (args.AnimationDuration, 0, option, () => {
				UIView.SetAnimationCurve (args.AnimationCurve);
				BottomBar.Frame = expectedBottomBarFrame;
			}, finished => EMTask.DispatchMain (View.SetNeedsLayout));
		}

		public void HideFlag() {
			CountryImage.Alpha = 0;
			HiddenCountryLabel.Alpha = 0;
		}

		void SetPicker(object sender, EventArgs e) {
			var field = (UITextField)sender;
			var cc2 = CountryCode.getCountryFromCode (field.Placeholder);
			Picker.Select (PickerModel.values.IndexOf (cc2), 0, true);
		}

		public virtual void DidTapDone(object sender, EventArgs e) {
			UITextField textview = HiddenCountryLabel;
			CountryCode cc = PickerModel.values[(int)Picker.SelectedRowInComponent (0)];
			SelectedCountry = cc;
			textview.Text = SelectedCountry.translationKey.t ();
			textview.Placeholder = SelectedCountry.countryCode;
			textview.SizeToFit ();
			textview.ResignFirstResponder ();
			ScaledImage = MaxResizeImage (UIImage.FromFile ("flags/" + SelectedCountry.photoUrl), COUNTRYIMAGE_WIDTH, COUNTRYIMAGE_HEIGHT);
			CountryImage.Image = ScaledImage;

			// This section is getting the width/height of the label and setting the button's frames to be slightly larger than that.
			var hiddenCountryAttributes = new UIStringAttributes ();
			hiddenCountryAttributes.Font = FontHelper.DefaultFontForLabels (hiddenCountryLabelFontSize);
			CGSize textSize = new NSString (textview.Text).GetSizeUsingAttributes (hiddenCountryAttributes);
			textview.Frame = new CGRect (0, 0, textSize.Width + UI_CONSTANTS.SMALL_MARGIN, textSize.Height + UI_CONSTANTS.SMALL_MARGIN);


			View.SetNeedsLayout ();
		}

		public void PauseUI () {
			EMTask.DispatchMain (() => {
				ContinueButton.Enabled = false;
				View.EndEditing (true);
				progressHud = new MTMBProgressHUD (View) {
					LabelText = "WAITING".t (),
					LabelFont = FontHelper.DefaultFontForLabels(),
					RemoveFromSuperViewOnHide = true
				};

				View.Add (progressHud);
				progressHud.Show (animated: true);
			});
		}

		public void ResumeUI () {
			EMTask.DispatchMain (() => {
				ContinueButton.Enabled = true;
				progressHud.Hide (animated: true, delay: 1);
			});
		}

		public static UIImage MaxResizeImage(UIImage sourceImage, float maxWidth, float maxHeight) {
			var sourceSize = sourceImage.Size;
			var maxResizeFactor = Math.Max(maxWidth / sourceSize.Width, maxHeight / sourceSize.Height);
			if (maxResizeFactor > 1) return sourceImage;
			var width = (nfloat) (maxResizeFactor * sourceSize.Width);
			var height = (nfloat) (maxResizeFactor * sourceSize.Height);
			UIGraphics.BeginImageContext(new CGSize(width, height));
			sourceImage.Draw(new CGRect(0, 0, width, height));
			var resultImage = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();
			return resultImage;
		}

		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations () {
			return UIInterfaceOrientationMask.Portrait;
		}
	}
}