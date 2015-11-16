using System;
using CoreGraphics;
using em;
using GoogleAnalytics.iOS;
using MBProgressHUD;
using UIKit;

namespace iOS {

	public class ProfileViewController : UIViewController {
		#region UI
		public UIView LineView;
		UIView BackgroundView;
		public UIImage Thumbnail;
		public UILabel NameLabel;
		public UIButton ThumbnailBackground;
		public EMButton AddContactButton, BlockContactButton, SendMessageButton;
		#endregion

		readonly SharedProfileController sharedProfileController;

		CounterParty profile;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		MTMBProgressHUD progressHud;

		bool dismissViewControllerFlag;

		public ProfileViewController (CounterParty p, bool dismissViewController) {
			dismissViewControllerFlag = dismissViewController;

			var appDelegate = (AppDelegate) UIApplication.SharedApplication.Delegate;

			sharedProfileController = new SharedProfileController (appDelegate.applicationModel, this, p);
			profile = sharedProfileController.Profile;
		}

		public void ThemeController (UIInterfaceOrientation orientation) {
			BackgroundColor colorTheme = profile.colorTheme;
			colorTheme.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && LineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					LineView.BackgroundColor = colorTheme.GetColor ();
				}
			});


			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);

			UpdateThumbnail ();
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
			View.Add (BackgroundView);

			ThumbnailBackground = new UIButton (new CGRect (0, 20, 100, 100));
			ThumbnailBackground.ClipsToBounds = true;
			ThumbnailBackground.Layer.CornerRadius = 50f;
			UpdateThumbnail ();
			BackgroundView.Add (ThumbnailBackground);

			NameLabel = new UILabel (new CGRect (0, 135, 200, 25));
			NameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			NameLabel.Font = FontHelper.DefaultFontForTextFields ();
			NameLabel.ClipsToBounds = true;
			NameLabel.TextColor = iOS_Constants.BLACK_COLOR;
			NameLabel.TextAlignment = UITextAlignment.Center;
			NameLabel.Text = profile.displayName;
			BackgroundView.Add (NameLabel);

			AddContactButton = new EMButton (new CGRect (0, 170, 200, 34), profile.colorTheme.GetColor(), "ADD_CONTACT_BUTTON".t ());
			AddContactButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapAddContactButton ).HandleEvent<object,EventArgs>;
			sharedProfileController.DidChangeTempProperty (); // This will set the correct button title
			BackgroundView.Add (AddContactButton);

			BlockContactButton = new EMButton (new CGRect (0, 214, 200, 34), profile.colorTheme.GetColor(), "");
			BlockContactButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapBlockContactButton ).HandleEvent<object,EventArgs>;
			BackgroundView.Add (BlockContactButton);

			SendMessageButton = new EMButton (new CGRect (0, 258, 200, 34), profile.colorTheme.GetColor(), "SEND_MESSAGE_TEXT".t ());
			SendMessageButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapSendMessageButton ).HandleEvent<object,EventArgs>;
			BackgroundView.Add (SendMessageButton);

			var contact = sharedProfileController.Profile as Contact;
			UpdateBlockAndSendMessageButton (contact);
		}

		public void UpdateBlockAndSendMessageButton(Contact contact) {
			SendMessageButton.Enabled = true;
			SendMessageButton.Alpha = 1.0f;

			string blockButtonLabel = "BLOCK_CONTACT_BUTTON".t ();
			if (contact != null && contact.IsBlocked) {
				blockButtonLabel = "UNBLOCK_CONTACT_BUTTON".t ();

				SendMessageButton.Enabled = false;
				SendMessageButton.Alpha = 0.6f;
			}

			BlockContactButton.SetTitle (blockButtonLabel, UIControlState.Normal);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);

			Title = "MY_ACCOUNT_TITLE".t ();

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			View.AutosizesSubviews = true;
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Profile View");
			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, View.Frame.Width, LineView.Frame.Height);

			BackgroundView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height, View.Frame.Width, View.Frame.Height);
			ThumbnailBackground.Frame = new CGRect ((View.Frame.Width / 2) - (100 / 2), 25, 100, 100);
			NameLabel.Frame = new CGRect ((View.Frame.Width - 200)/2, 135, 200, 25);

			AddContactButton.Frame = new CGRect ((View.Frame.Width - 200) / 2, 170, 200, 34);
			BlockContactButton.Frame = new CGRect ((View.Frame.Width - 200) / 2, 214, 200, 34);
			SendMessageButton.Frame = new CGRect ((View.Frame.Width - 200) / 2, 258, 200, 34);

			if (this.Spinner != null)
				this.Spinner.Frame = new CGRect (this.View.Frame.Width / 2 - this.Spinner.Frame.Width / 2, displacement_y + 35 /*25 is the thumbnail's y from the background view + additional margin*/, this.Spinner.Frame.Width, this.Spinner.Frame.Height);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
		}

		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose(bool disposing) {
			sharedProfileController.Dispose ();
			base.Dispose (disposing);
		}

		public void UpdateThumbnail () {
			profile.colorTheme.GetBlankPhotoAccountResource ((UIImage image) => {
				if (profile != null) {
					ImageSetter.SetAccountImage (profile, image, (UIImage loadedImage) => {
						if (loadedImage != null)
							ThumbnailBackground.SetBackgroundImage (loadedImage, UIControlState.Normal);
					});
				}
			});

			UpdateThumbnailBorder ();
			UpdateProgressIndicatorVisibility (ImageSetter.ShouldShowProgressIndicator(profile));
		}

		public void UpdateThumbnailBorder () {
			if (ImageSetter.ShouldShowAccountThumbnailFrame (profile)) {
				ThumbnailBackground.Layer.BorderWidth = 3f;
				ThumbnailBackground.Layer.BorderColor = profile.colorTheme.GetColor ().CGColor;
			} else {
				ThumbnailBackground.Layer.BorderWidth = 0f;
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
				this.ThumbnailBackground.Alpha = 0;

			} else {
				if (this.Spinner != null) {
					this.Spinner.StopAnimating ();
					this.Spinner.RemoveFromSuperview ();
					this.Spinner = null;
				}

				this.ThumbnailBackground.Alpha = 1;
			}
		}
		#endregion

		protected void DidTapAddContactButton(object sender, EventArgs e) {
			ShowProgress ();
			sharedProfileController.AddContactAsync (obj => HideProgress ());
		}

		protected void DidTapBlockContactButton(object sender, EventArgs e) {
			ShowProgress ();
			sharedProfileController.DidTapBlockButton(obj => HideProgress ());
		}

		protected void DidTapSendMessageButton(object sender, EventArgs eventArgs) {
			if(dismissViewControllerFlag) {
				this.DismissViewController (true, () => {
					sharedProfileController.SendMessage ();
				});
			} else
				sharedProfileController.SendMessage ();
		}

		void ShowProgress () {
			EMTask.DispatchMain (() => {
				progressHud = new MTMBProgressHUD (View) {
					LabelText = "WAITING".t (),
					LabelFont = FontHelper.DefaultFontForLabels(),
					RemoveFromSuperViewOnHide = true
				};

				this.View.Add (progressHud);
				progressHud.Show (animated: true);
			});
		}

		void HideProgress () {
			EMTask.DispatchMain (() => {
				if (progressHud != null)
					progressHud.Hide (animated: true, delay: 0);
			});
		}
	}

	class SharedProfileController : AbstractProfileController {
		WeakReference profileViewControllerRef;
		ProfileViewController profileViewController {
			get { return profileViewControllerRef.Target as ProfileViewController; }
			set { profileViewControllerRef = new WeakReference (value); }
		}

		public SharedProfileController(ApplicationModel appModel, ProfileViewController pvc, CounterParty profile) : base (appModel, profile) {
			profileViewController = pvc;
		}

		public override void DidChangeTempProperty () {
			var contact = Profile as Contact;
			profileViewController.AddContactButton.SetTitle (contact.tempContact.Value ? "ADD_CONTACT_BUTTON".t () : "REMOVE_CONTACT_BUTTON".t (), UIControlState.Normal);
		}

		public override void DidChangeBlockStatus (Contact c) {
			profileViewController.UpdateBlockAndSendMessageButton (c);
		}

		public override void TransitionToChatController (ChatEntry chatEntry) {
			EMTask.DispatchMain (() => {
				var chatViewController = new ChatViewController (chatEntry);
				chatViewController.NEW_MESSAGE_INITIATED_FROM_NOTIFICATION = true;

				MainController mainController = AppDelegate.Instance.MainController;
				var navController = mainController.ContentController as UINavigationController;
				navController.PushViewController(chatViewController, true);
			});
		}
	}
}