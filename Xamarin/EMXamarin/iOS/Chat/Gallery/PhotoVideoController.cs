using System;
using UIKit;
using System.Collections.Generic;
using em;
using System.Threading.Tasks;
using EMXamarin;
using CoreGraphics;
using UIDevice_Extension;
using Foundation;
using Media_iOS_Extension;

namespace iOS {
	public class PhotoVideoController : UIViewController {

		private const int PHOTO_VIDEO_VIEW_PLAYER_AUDIO_SESSION_ID = 2;

		public static readonly string NOTIFICATION_TOGGLING_UI = "NOTIFICATIN_TOGLISNDF_UI";

		private UIPageViewController pageViewController;
		public UIPageViewController PageViewController {
			get {
				return pageViewController;
			}

			set {
				pageViewController = value;
			}
		}

		private bool pageIsAnimating;
		public bool PageIsAnimating {
			get {
				return pageIsAnimating;
			}

			set {
				pageIsAnimating = value;
			}
		}

		private bool navigationBarStatusBarShowing;
		public bool ShowingToolbars {
			get {
				return navigationBarStatusBarShowing;
			}

			set {
				navigationBarStatusBarShowing = value;
			}
		}

		#region UI
		UIBarButtonItem addButton;
		protected UIBarButtonItem AddButton {
			get {
				return addButton;
			}

			set {
				addButton = value;
			}
		}
		#endregion

		public SharedMediaGalleryController Shared { get; set; }

		public PhotoVideoController (IMediaMessagesProvider provider) {
			this.Shared = new SharedMediaGalleryController (this, provider);
		}

		#region updating ui
		private void ThemeController () {
			ThemeController (this.InterfaceOrientation);
		}

		private void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;

			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
				}
			});

			if (this.NavigationController != null) {
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (this.NavigationController.NavigationBar);
				UINavigationBarUtil.RemoveDropShadowOnNavigationBar (this.NavigationController.NavigationBar);
			}
		}

		public void SetStatusbarVisibility (bool hidesUI, bool animated) {
			UIApplication.SharedApplication.SetStatusBarHidden (hidesUI, (animated ? UIStatusBarAnimation.Fade : UIStatusBarAnimation.None));
			UIApplication.SharedApplication.SetStatusBarStyle (UIStatusBarStyle.LightContent, false);
		}

		public void SetNavigationBarVisibility (bool fading) {
			if (fading)
				this.NavigationController.NavigationBarHidden = true;
			else
				this.NavigationController.NavigationBarHidden = false;
			this.NavigationController.NavigationBar.Translucent = fading;
			this.NavigationController.NavigationBar.Alpha = (fading ? 0.0f : 1.0f);
			UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (this.NavigationController.NavigationBar);
			UINavigationBarUtil.RemoveDropShadowOnNavigationBar (this.NavigationController.NavigationBar);
		}

		private void DoHide () {
			SetStatusbarVisibility (true, true);
			SetNavigationBarVisibility (true);
			this.ShowingToolbars = false;
			NSNotificationCenter.DefaultCenter.PostNotificationName (NOTIFICATION_TOGGLING_UI, new NSNumber (false));
		}

		private void DoShow () {
			SetStatusbarVisibility (false, true);
			SetNavigationBarVisibility (false);
			this.ShowingToolbars = true;
			NSNotificationCenter.DefaultCenter.PostNotificationName (NOTIFICATION_TOGGLING_UI, new NSNumber (true));
		}

		private void ImageTapped () {
			if (this.ShowingToolbars)
				DoHide ();
			else
				DoShow ();
		}
		#endregion
			
		#region ViewWill
		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			// This adds a spacer between the frames to mimic Apple's Photos App.
			var values = new NSObject[] {  NSNumber.FromFloat (10.0f) };
			var keys = new NSObject[] { new NSString ("UIPageViewControllerOptionInterPageSpacingKey") };
			var options = NSDictionary.FromObjectsAndKeys(values, keys);

			this.PageViewController = new UIPageViewController (UIPageViewControllerTransitionStyle.Scroll, UIPageViewControllerNavigationOrientation.Horizontal, options);

			UpdateDatasourceAndDelegates ();
			SetInitialController ();

			this.AddChildViewController (this.PageViewController);
			this.View.Add (this.PageViewController.View);
			this.PageViewController.DidMoveToParentViewController (this);

			ThemeController ();

			this.NavigationController.InteractivePopGestureRecognizer.Enabled = false;

			#region related to toggling UI

			// Toggle Navigationbar
			WeakDelegateProxy ImageTappedProxy = WeakDelegateProxy.CreateProxy (ImageTapped);
			UITapGestureRecognizer tapGesure = new UITapGestureRecognizer ((Action)ImageTappedProxy.HandleEvent);

			tapGesure.ShouldRecognizeSimultaneously = (thisGesure, otherGesure) => { return true; };

			tapGesure.NumberOfTapsRequired = 1;
			tapGesure.CancelsTouchesInView = false;
			this.View.AddGestureRecognizer (tapGesure);
			#endregion

			this.ShowingToolbars = true;
			this.AutomaticallyAdjustsScrollViewInsets = false;
			UpdateTitlebar (this.Shared.CurrentPage);
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			this.NavigationController.InteractivePopGestureRecognizer.Enabled = false;
			this.AddButton = new UIBarButtonItem (UIBarButtonSystemItem.Add);
			this.NavigationItem.RightBarButtonItem = this.AddButton;
			ThemeController ();
			AudioSessionHelper.SharedInstance ().PrepareAudioSessionForPlayback (PHOTO_VIDEO_VIEW_PLAYER_AUDIO_SESSION_ID);
		}

		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews ();
			nfloat displacement_y = this.TopLayoutGuide.Length;
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			if (this.AddButton != null)
				this.AddButton.Clicked += AddButtonPressed;
			ThemeController ();

			this.Shared.AddObservers ();
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			this.NavigationController.InteractivePopGestureRecognizer.Enabled = true;
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			if (this.AddButton != null)
				this.AddButton.Clicked -= AddButtonPressed;

			AudioSessionHelper.SharedInstance ().ReturnAudioSessionToDefault (PHOTO_VIDEO_VIEW_PLAYER_AUDIO_SESSION_ID);

			this.Shared.RemoveObservers ();
		}

		#endregion

		public void UpdateDatasourceAndDelegates () {
			this.PageViewController.DataSource = new PhotoVideoDataSource (this);
			this.PageViewController.Delegate = new PhotoVideoDelegate (this);
		}

		public void SetInitialController () {
			PhotoVideoItemController initialPhotoOrVideo = this.ControllerAtIndex (this.Shared.CurrentPage);
			UIViewController[] controllers = { initialPhotoOrVideo };
			this.PageViewController.SetViewControllers (controllers, UIPageViewControllerNavigationDirection.Forward, false, (bool finished) => {});
		}

		#region rotation
		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
			ThemeController ();
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
		}

		public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation) {
			base.DidRotate (fromInterfaceOrientation);

			// We're resetting the delegates so that it resets its cached list of controllers.
			// This is to prevent the case where we're caching a controller while rotating.
			// We wouldn't want to cache a controller while rotating because the cached controller might not be rotated correctly.
			UpdateDatasourceAndDelegates ();
		}
		#endregion


		public PhotoVideoItemController ControllerAtIndex (int index) {
			SharedMediaGalleryController shared = this.Shared;
			Message messageAtIndex = shared.MediaMessages [index];
			PhotoVideoItemController controller = null;

			ContentType type = ContentTypeHelper.FromMessage (messageAtIndex);

			if (ContentTypeHelper.IsVideo (type)) {
				controller = new ShowVideoViewController (index, messageAtIndex);
			} else if (ContentTypeHelper.IsAudio (type)) {
				controller = new ShowVideoViewController (index, messageAtIndex);
			} else {
				controller = new ShowPhotoViewController (index, messageAtIndex);
			}

			controller.OnAppearAction += UpdateTitlebar;

			shared.CheckIfNeedsToRequestMoreMessages ();

			return controller;
		}

		public void UpdateTitlebar (int currentMediaIndex) {
			SharedMediaGalleryController shared = this.Shared;
			shared.CurrentPage = currentMediaIndex;
			if (UIDevice.CurrentDevice.IsRightLeftLanguage ()) {
				string firstLocalizedNumber = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (shared.MediaMessages.Count), NSNumberFormatterStyle.Decimal);
				string secondLocalizedNumber = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (shared.CurrentPage + 1), NSNumberFormatterStyle.Decimal);
				this.Title = string.Format ("MEDIA_GALLERY_COUNT".t (), firstLocalizedNumber, secondLocalizedNumber);
			} else {
				string firstLocalizedNumber = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (shared.CurrentPage + 1), NSNumberFormatterStyle.Decimal);
				string secondLocalizedNumber = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (shared.MediaMessages.Count), NSNumberFormatterStyle.Decimal);
				this.Title = string.Format ("MEDIA_GALLERY_COUNT".t (), firstLocalizedNumber, secondLocalizedNumber);
			}
		}

		private void AddButtonPressed (object sender, EventArgs args) {
			UIActionSheet actionSheet = new UIActionSheet (null, null, null, null);
			actionSheet.AddButton ("SAVE_BUTTON".t ());
			actionSheet.AddButton ("CANCEL_BUTTON".t ());
			actionSheet.CancelButtonIndex = 1;

			Message m = this.Shared.MediaMessages [this.Shared.CurrentPage];
			AppDelegate appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;

			// TODO: use a UIAlertController for iOS8+
			actionSheet.Clicked += HandleAddButtonActionSheetPressed;
			actionSheet.ShowInView (this.View);
		}

		private void HandleAddButtonActionSheetPressed (object a, UIButtonEventArgs b) {
			Message m = this.Shared.MessageForCurrentPage;
			AppDelegate appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			string localpath = appDelegate.applicationModel.uriGenerator.GetFilePathForChatEntryUri(m.media.uri, m.chatEntry);

			if (b.ButtonIndex == 0) {
				ContentType type = ContentTypeHelper.FromMessage (m);

				if (ContentTypeHelper.IsVideo(type)) {
					UIVideo.SaveToPhotosAlbum (localpath, (string path, NSError error) => {
						// TODO: error handling + show user video has saved
						System.Diagnostics.Debug.WriteLine("Error saving video to photo album", error);
					});
				} else { //TODO: check if it's a photo? do we want to be able to save audio this way?
					UIImage image = m.media.LoadFullSizeImage ();
					image.SaveToPhotosAlbum ((UIImage _image, NSError error) => {
						// TODO: error handling + show user image has saved
						System.Diagnostics.Debug.WriteLine("Error saving photo or audio to photo album", error);
					});
				}
			}
		}
		protected override void Dispose (bool disposing) {
			em.NotificationCenter.DefaultCenter.RemoveObserver (this);
			this.Shared.Dispose ();
			this.Shared = null;
			base.Dispose (disposing);
		}
	}

	public class PhotoVideoDataSource : UIPageViewControllerDataSource {
		WeakReference Ref { get; set; }

		private PhotoVideoController photoVideoController {
			get {
				return (Ref == null ? null : Ref.Target as PhotoVideoController);
			}
			set {
				Ref = new WeakReference (value);
			}
		}

		public PhotoVideoDataSource (PhotoVideoController controller) {
			photoVideoController = controller;
		}

		public override UIViewController GetPreviousViewController (UIPageViewController pageViewController, UIViewController referenceViewController) {
			PhotoVideoController controller = this.photoVideoController;
			if (controller == null) return null;

			if (controller.PageIsAnimating)
				return null;

			PhotoVideoItemController photoVideoItem = (PhotoVideoItemController)referenceViewController;
			if (photoVideoItem.IndexOfContent > 0) {
				return controller.ControllerAtIndex (photoVideoItem.IndexOfContent - 1);
			}

			return null;
		}

		public override UIViewController GetNextViewController (UIPageViewController pageViewController, UIViewController referenceViewController) {
			PhotoVideoController controller = this.photoVideoController;
			if (controller == null) return null;

			if (photoVideoController.PageIsAnimating)
				return null;

			PhotoVideoItemController photoVideoItem = (PhotoVideoItemController)referenceViewController;
			if (photoVideoItem.IndexOfContent + 1 < controller.Shared.MediaMessages.Count) {
				return photoVideoController.ControllerAtIndex (photoVideoItem.IndexOfContent + 1);
			}

			return null;
		}
	}

	public class PhotoVideoDelegate : UIPageViewControllerDelegate {
		WeakReference Ref { get; set; }

		private PhotoVideoController photoVideoController {
			get {
				return (Ref == null ? null : Ref.Target as PhotoVideoController);
			}
			set {
				Ref = new WeakReference (value);
			}
		}

		public PhotoVideoDelegate (PhotoVideoController controller) {
			this.photoVideoController = controller;
		}

		public override void WillTransition (UIPageViewController pageViewController, UIViewController[] pendingViewControllers) {
			PhotoVideoController controller = this.photoVideoController;
			if (controller == null) return;
			controller.PageIsAnimating = true;
		}

		public override void DidFinishAnimating (UIPageViewController pageViewController, bool finished, UIViewController[] previousViewControllers, bool completed) {
			PhotoVideoController controller = this.photoVideoController;
			if (controller == null) return;
			if (finished || completed) {
				controller.PageIsAnimating = false;
			}
		}
	}

	public class SharedMediaGalleryController : AbstractMediaGalleryController {
		private WeakReference _r = null;
		private PhotoVideoController Self {
			get { return this._r != null ? this._r.Target as PhotoVideoController : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedMediaGalleryController (PhotoVideoController self, IMediaMessagesProvider provider) : base (provider, AppDelegate.Instance.applicationModel) {
			this.Self = self;
		}

		public override void UpdateDatasourceAndDelegates () {
			PhotoVideoController self = this.Self;
			if (self == null) return;
			self.UpdateDatasourceAndDelegates ();
		}

		public override void SetInitialController () {
			PhotoVideoController self = this.Self;
			if (self == null) return;
			self.SetInitialController ();
		}

		public override void UpdateTitleBar () {
			PhotoVideoController self = this.Self;
			if (self == null) return;
			self.UpdateTitlebar (this.CurrentPage);
		}
	}
}

