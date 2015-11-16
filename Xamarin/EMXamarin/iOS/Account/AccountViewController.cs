using System;
using CoreGraphics;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using String_UIKit_Extension;
using UIKit;

namespace iOS {

	public class AccountViewController : AbstractAcquiresImagesController {

		#region UI
		public UIView LineView { get; set; }

		UIView BackgroundView { get; set; }

		public UIButton ThumbnailButton { get; set; }
		public UIImage Thumbnail { get; set; }
		public UIButton ColorThemeButton { get; set; }
		public UILabel ColorThemeLabel { get; set; }

		public UITextField NameTextField { get; set; }
		public UILabel NameLabel { get; set; }
		#endregion

		AppDelegate appDelegate;

		readonly SharedAccountController sharedAccountController;

		AccountInfo accountInfo;

		public AccountViewController (bool onboarding) {
			
			appDelegate = (AppDelegate) UIApplication.SharedApplication.Delegate;

			sharedAccountController = new SharedAccountController (appDelegate.applicationModel, this);
			sharedAccountController.IsOnboarding = onboarding;

			accountInfo = sharedAccountController.AccountInfo;

			// image picker related
			this.AllowVideoOnImageSelection = false;
			this.AllowImageEditingOnImageSelection = true;
			//
		}

		public void ThemeController (UIInterfaceOrientation orientation) {
			BackgroundColor colorTheme = sharedAccountController.ColorTheme;
			colorTheme.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && LineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					LineView.BackgroundColor = colorTheme.GetColor ();
				}
			});

			UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);

			/* set dynamic color properties for this view */
			colorTheme.GetColorThemeSelectionImageResource ((UIImage image) => {
				ColorThemeButton.SetBackgroundImage (image, UIControlState.Normal);
				UpdateThumbnail ();
			});
		}

		public override void TouchesBegan (NSSet touches, UIEvent evt) {
			// hide the keyboard when there are touches outside the keyboard view
			base.TouchesBegan (touches, evt);
			View.EndEditing (true);
		}

		public override void LoadView () {
			base.LoadView ();

			LineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (LineView);
			View.BringSubviewToFront (LineView);

			BackgroundView = new UIView(new CGRect (0, 0, View.Frame.Width, View.Frame.Height));
			BackgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			BackgroundView.BackgroundColor = iOS_Constants.WHITE_COLOR;

			ThumbnailButton = new UIButton (new CGRect (0, 20, 100, 100));
			ThumbnailButton.ClipsToBounds = true;
			ThumbnailButton.Layer.CornerRadius = 50f;

			BackgroundView.Add (ThumbnailButton);

			ColorThemeButton = new UIButton (new CGRect (0, 0, 35, 35));
			BackgroundView.Add (ColorThemeButton);

			ColorThemeLabel = new UILabel (new CGRect(0, 0, View.Frame.Width, 20));
			ColorThemeLabel.Font = FontHelper.DefaultItalicFont (9f);
			ColorThemeLabel.Text = "COLOR_THEME".t ();
			ColorThemeLabel.TextColor = iOS_Constants.BLACK_COLOR;
			BackgroundView.Add (ColorThemeLabel);

			NameTextField = new EMTextField (new CGRect (0, 135, 200, 34), "DISPLAY_NAME".t (), UITextAutocorrectionType.Yes, UITextAutocapitalizationType.Words, UIReturnKeyType.Done);
			if (sharedAccountController.IsOnboarding)
				NameTextField.Text = "";
			else
				NameTextField.Text = accountInfo != null && !string.IsNullOrEmpty (accountInfo.displayName) ? accountInfo.displayName : "";
			
			NameTextField.ShouldReturn += textField => { 
				// Dismiss the keyboard when pressing the done key
				textField.ResignFirstResponder ();
				return true; 
			};
			NameTextField.EditingChanged += WeakDelegateProxy.CreateProxy<object,EventArgs>((sender, e) => {
				sharedAccountController.TextInDisplayNameField = NameTextField.Text;
			}).HandleEvent<object,EventArgs>;
			// to validate after the user has entered text and moved away from that text field, use EditingDidEnd
			NameTextField.EditingDidEnd += (sender, e) => {
				// perform validation; removed for now per Bryant
			};
			NameTextField.BecomeFirstResponder ();
			BackgroundView.Add (NameTextField);

			sharedAccountController.TextInDisplayNameField = NameTextField.Text;

			NameLabel = new UILabel (new CGRect(0, 185, View.Frame.Width, 20));
			NameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			NameLabel.Font = FontHelper.DefaultItalicFont (9f);
			NameLabel.Text = "DISPLAY_NAME".t ();
			NameLabel.TextColor = iOS_Constants.BLACK_COLOR;
			NameLabel.TextAlignment = UITextAlignment.Center;
			BackgroundView.Add (NameLabel);

			View.Add (BackgroundView);

			UpdateThumbnail ();
		}
			
		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);

			Title = "MY_ACCOUNT_TITLE".t ();

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			var closeButton = new UIBarButtonItem ("DONE_BUTTON".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapSaveButton).HandleEvent<object,EventArgs>);
			closeButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			NavigationItem.SetLeftBarButtonItem(closeButton, true);

			ThumbnailButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapImageButton).HandleEvent<object,EventArgs>;
			ColorThemeButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapColorThemeButton).HandleEvent<object,EventArgs>;

			View.AutosizesSubviews = true;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			//NameTextField.Selected = true;
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, this.sharedAccountController.IsOnboarding ? "Account (Onboarding) View" : "Account View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, View.Frame.Width, LineView.Frame.Height);

			BackgroundView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height, View.Frame.Width, View.Frame.Height);
			ThumbnailButton.Frame = new CGRect ((View.Frame.Width / 2) - (100 / 2), 25, 100, 100);
			NameTextField.Frame = new CGRect ((View.Frame.Width - 200)/2, 135, 200, 34);

			CGSize sizeNL = NameLabel.Text.SizeOfTextWithFontAndLineBreakMode (NameLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
			sizeNL = new CGSize ((float)((int)(sizeNL.Width + 1.5)), (float)((int)(sizeNL.Height + 1.5)));
			var xCoordNL = (View.Frame.Width - sizeNL.Width) / 2;
			NameLabel.Frame = new CGRect (new CGPoint(xCoordNL, NameTextField.Frame.Y + NameTextField.Frame.Height + 5), sizeNL);

			var xCoordCTB = ((View.Frame.Width / 2) - (ThumbnailButton.Frame.Width / 2) - ColorThemeButton.Frame.Width) / 2;
			ColorThemeButton.Frame = new CGRect (xCoordCTB, ((ThumbnailButton.Frame.Height - ColorThemeButton.Frame.Height)/2) - 10, ColorThemeButton.Frame.Width, ColorThemeButton.Frame.Height);

			CGSize sizeCTL = ColorThemeLabel.Text.SizeOfTextWithFontAndLineBreakMode (ColorThemeLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
			sizeCTL = new CGSize ((float)((int)(sizeCTL.Width + 1.5)), (float)((int)(sizeCTL.Height + 1.5)));
			var xCoordCTL = ((View.Frame.Width / 2) - (ThumbnailButton.Frame.Width / 2) - sizeCTL.Width) / 2;
			ColorThemeLabel.Frame = new CGRect (new CGPoint(xCoordCTL, ColorThemeButton.Frame.Y + ColorThemeButton.Frame.Height + 5), sizeCTL);

			if (this.Spinner != null)
				this.Spinner.Frame = new CGRect (this.View.Frame.Width / 2 - this.Spinner.Frame.Width / 2, displacement_y + 35 /*25 is the thumbnail's y from the background view + additional margin*/, this.Spinner.Frame.Width, this.Spinner.Frame.Height);
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			if (NameTextField.IsFirstResponder)
				NameTextField.ResignFirstResponder ();
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
		}

		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose(bool disposing) {
			base.Dispose (disposing);
		}

		public void UpdateThumbnail () {
			sharedAccountController.ColorTheme.GetBlankPhotoAccountResource ((UIImage image) => {
				if (accountInfo != null) {
					ImageSetter.SetAccountImage (accountInfo, image, (UIImage loadedImage) => {
						if (loadedImage != null)
							ThumbnailButton.SetBackgroundImage (loadedImage, UIControlState.Normal);
					});
				}
			});

			UpdateProgressIndicatorVisibility (ImageSetter.ShouldShowProgressIndicator (accountInfo));
			UpdateThumbnailBorder ();
		}

		public void UpdateThumbnailBorder () {
			if (ImageSetter.ShouldShowAccountThumbnailFrame (accountInfo)) {
				ThumbnailButton.Layer.BorderWidth = 3f;
				ThumbnailButton.Layer.BorderColor = sharedAccountController.ColorTheme.GetColor ().CGColor;
			} else {
				ThumbnailButton.Layer.BorderWidth = 0f;
			}
		}

		#region progress spinner
		// TODO: maybe we can refactor these account pages..
		UIActivityIndicatorView spinner;
		protected UIActivityIndicatorView Spinner {
			get { return spinner; }
			set { spinner = value; }
		}

		public void UpdateProgressIndicatorVisibility (bool showProgressIndicator) {
			if (showProgressIndicator) {
				if (this.Spinner == null) {
					this.Spinner = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.WhiteLarge);
					this.Spinner.Color = UIColor.Black;
					this.Add (this.Spinner);
				}

				CGRect f = this.Spinner.Frame;
				f.Location = new CGPoint (this.View.Frame.Width / 2 - this.spinner.Frame.Width / 2, 70);
				this.Spinner.Frame = f;

				if (!this.Spinner.IsAnimating)
					this.Spinner.StartAnimating ();

				this.View.BringSubviewToFront (this.Spinner);
				this.ThumbnailButton.Alpha = 0;

			} else {
				if (this.Spinner != null) {
					this.Spinner.StopAnimating ();
					this.Spinner.RemoveFromSuperview ();
					this.Spinner = null;
				}

				this.ThumbnailButton.Alpha = 1;
			}
		}
		#endregion

		protected void DidTapColorThemeButton(object sender, EventArgs eventArgs) {
			var picker = new ColorThemePickerViewControllerController ();
			picker.DidPickColorDelegate += DidPickColor;
			picker.Title = "COLOR_THEME_PLURAL".t ();
			NavigationController.PushViewController (picker, true);
		}

		protected void DidPickColor(BackgroundColor updatedColor) {
			sharedAccountController.ColorTheme = updatedColor; // will kick off a delegate call that will theme the controller
			NavigationController.PopViewController (true);
		}

		protected void DidTapSaveButton (object sender, EventArgs e) {
			this.sharedAccountController.TrySaveAccount ();
		}

		public void DismissAccountController () {
			if (this.sharedAccountController.IsOnboarding) {
				if (this.appDelegate.MainController.PresentedViewController != null) 
					this.appDelegate.MainController.DismissViewControllerAsync (false);

				this.appDelegate.MainController.FinishOnboarding (false);
			} else {
				DismissViewController (true, null);
			}
		}

		public void DisplayBlankTextInDisplayAlert () {
			var title = "MY_ACCOUNT_TITLE".t ();
			var message = string.Format ("BLANK_DISPLAY_NAME".t (), accountInfo.username.Replace(" ", ""));
			var action = "CONTINUE_BUTTON".t ();

			var alert = new UIAlertView (title, message, null, "EDIT_BUTTON".t (), new [] { action });
			alert.Show ();
			alert.Clicked += (sender2, buttonArgs) => { 
				switch (buttonArgs.ButtonIndex) {
				case 1:
					sharedAccountController.SaveAccountAsync ();
					break;
				}
			};
		}

		#region media selection
		public override string ImageSearchSeedString {
			get {
				if (NameTextField != null && !string.IsNullOrWhiteSpace (NameTextField.Text)) {
					return NameTextField.Text;
				} else {
					return string.Empty;
				}
			}
		}

		protected void DidTapImageButton(object sender, EventArgs eventArgs) {
			this.StartAcquiringImage ();
		}

		protected override void HandleImageSelected (object sender, UIImagePickerMediaPickedEventArgs e, bool isImage) {
			// get the original image
			var editedImage = e.Info[UIImagePickerController.EditedImage] as UIImage;
			ScaleImageAndSave (editedImage);

			// dismiss the picker
			NavigationController.DismissViewController (true, null);
		}

		protected override void HandleSearchImageSelected (UIImage originalImage) {
			ScaleImageAndSave (originalImage);
		}

		public void ScaleImageAndSave (UIImage editedImage) {
			if(editedImage != null) {
				Thumbnail = ScaleImage (editedImage, 200);
				NSData imageData = Thumbnail.AsJPEG (0.75f);
				byte[] updatedMedia = imageData != null ? imageData.ToByteArray () : null;
				if (updatedMedia != null) {
					ApplicationModel appModel = (UIApplication.SharedApplication.Delegate as AppDelegate).applicationModel;
					string accountInfoThumbnailPath = appModel.uriGenerator.GetStagingPathForAccountInfoThumbnailLocal ();
					appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (accountInfoThumbnailPath);
					appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (accountInfoThumbnailPath, updatedMedia, null);
					sharedAccountController.AccountInfo.UpdateThumbnailUrlAfterMovingFromCache (accountInfoThumbnailPath);
					UpdateThumbnail ();
					sharedAccountController.UpdatedThumbnail = updatedMedia;
				}
			}
		}
		#endregion
	}

	class SharedAccountController : AbstractAccountController {

		readonly AccountViewController accountViewController;

		public SharedAccountController(ApplicationModel appModel, AccountViewController avc) : base (appModel) {
			accountViewController = avc;
		}

		public override void DidChangeThumbnailMedia () {
			if (accountViewController.IsViewLoaded)
				accountViewController.UpdateThumbnail ();
		}

		public override void DidDownloadThumbnail () {
			if (accountViewController.IsViewLoaded)
				accountViewController.UpdateThumbnail ();
		}

		public override void DidChangeColorTheme() {
			if (accountViewController.IsViewLoaded)
				accountViewController.ThemeController (accountViewController.InterfaceOrientation);
		}

		public override void DidChangeDisplayName () {
			if (accountViewController.IsViewLoaded)
				accountViewController.NameTextField.Text = this.DisplayName;
		}

		public override void DismissAccountController () {
			if (this.accountViewController.IsViewLoaded) {
				this.accountViewController.DismissAccountController ();
			}
		}

		public override void DisplayBlankTextInDisplayAlert () {
			if (this.accountViewController.IsViewLoaded) {
				this.accountViewController.DisplayBlankTextInDisplayAlert (); 
			}
		}
	}
}