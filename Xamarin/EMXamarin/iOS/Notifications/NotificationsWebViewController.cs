using System;
using System.Text;
using CoreGraphics;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using MBProgressHUD;
using Newtonsoft.Json;
using UIKit;

namespace iOS
{
	public class NotificationsWebViewController : UIViewController {

		UIView lineView;
		UIWebView iosNotificationWV;

		MTMBProgressHUD progressHud;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		NotificationEntry notificationEntry;
		protected NotificationList NotificationList;
		readonly SharedNotificationsWebController sharedNotificationsWebController;

		public NotificationsWebViewController (NotificationEntry ne) {
			notificationEntry = ne;
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			NotificationList = appDelegate.applicationModel.notificationList;
			sharedNotificationsWebController = new SharedNotificationsWebController (this, appDelegate.applicationModel);
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			var mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && lineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					lineView.BackgroundColor = mainColor.GetColor ();
				}
			});


			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		public override void LoadView () {
			base.LoadView ();

			progressHud = new MTMBProgressHUD (View) {
				LabelText = "LOADING".t (),
				LabelFont = FontHelper.DefaultFontForLabels(),
				RemoveFromSuperViewOnHide = true
			};
			View.AddSubview (progressHud);
			progressHud.Show (true);

			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (lineView);
			View.BringSubviewToFront (lineView);

			iosNotificationWV = new UIWebView (new CGRect (0, 1, View.Frame.Width, View.Frame.Height - lineView.Frame.Height));
			iosNotificationWV.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

			string url = notificationEntry.Url.Replace ("http://localhost:8080", AppEnv.HTTP_BASE_ADDRESS);
			var req = new NSUrlRequest(NSUrl.FromString ( url ), NSUrlRequestCachePolicy.ReloadIgnoringLocalAndRemoteCacheData, 5);
			iosNotificationWV.LoadRequest (req);
			iosNotificationWV.Opaque = false;
			iosNotificationWV.BackgroundColor = UIColor.Clear;

			iosNotificationWV.LoadFinished += WeakDelegateProxy.CreateProxy<object, EventArgs> (HandleLoadFinished).HandleEvent<object, EventArgs>;

			iosNotificationWV.ShouldStartLoad += HandleShouldStartLoad;

			View.Add (iosNotificationWV);

			View.BringSubviewToFront (progressHud);
		}

		private void HandleLoadFinished (object sender, EventArgs e) {
			//hide the loading indicator now that the webview has loaded
			progressHud.Hide (true);

			//prevent being able to scroll / bounce when dragging web view up, down, left or right
			((UIWebView)sender).ScrollView.AlwaysBounceVertical = false;
			((UIWebView)sender).ScrollView.AlwaysBounceHorizontal = false;
		}

		private bool HandleShouldStartLoad (UIWebView webView, NSUrlRequest request, UIWebViewNavigationType navType) {
			if(request.Url.AbsoluteString.Contains("sendMessage")) {
				//record action initiated
				NotificationList.MarkNotificationEntryActionInitiatedAsync(notificationEntry);

				string uri = request.Url.AbsoluteString;
				int beginningIndex = uri.IndexOf("c=");

				string rawData = uri.Substring(beginningIndex + 2);
				string encodedBase64Contact = Uri.UnescapeDataString(rawData);

				byte[] decodedContactBytes = Convert.FromBase64String(encodedBase64Contact);
				string decodedContact = Encoding.UTF8.GetString(decodedContactBytes);

				var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;

				ContactInput contactInput = JsonConvert.DeserializeObject<ContactInput>(decodedContact);
				Contact existing = Contact.FindOrCreateContact (appDelegate.applicationModel, contactInput);

				this.DismissViewController (true, () => {
					sharedNotificationsWebController.GoToNewOrExistingChatEntry (existing);
				});
			}
			return true;
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);
			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			Title = "NOTIFICATION_TITLE".t ();

			View.AutosizesSubviews = true;
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;
			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Notification Web View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;
			lineView.Frame = new CGRect (0, displacement_y, lineView.Frame.Width, lineView.Frame.Height);

			iosNotificationWV.Frame = new CGRect (0, displacement_y + lineView.Frame.Height, View.Frame.Width, View.Frame.Height - (displacement_y + lineView.Frame.Height));
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
			iosNotificationWV.ShouldStartLoad -= HandleShouldStartLoad;
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose (bool disposing) {
			sharedNotificationsWebController.Dispose ();
			base.Dispose (disposing);
		}

		public void TransitionToChatControllerUsingChatEntry (ChatEntry chatEntry) {
			var chatViewController = new ChatViewController (chatEntry);
			chatViewController.NEW_MESSAGE_INITIATED_FROM_NOTIFICATION = true;
			MainController mainController = AppDelegate.Instance.MainController;
			var navController = mainController.ContentController as UINavigationController;
			navController.PushViewController (chatViewController, true);
		}

		class SharedNotificationsWebController : AbstractNotificationsWebController {
			WeakReference Ref { get; set; }

			public NotificationsWebViewController webviewController {
				get {
					return (Ref == null ? null : Ref.Target as NotificationsWebViewController);
				}
				set {
					Ref = new WeakReference (value);
				}
			}

			public SharedNotificationsWebController (NotificationsWebViewController pc, ApplicationModel appModel) : base (appModel) {
				webviewController = pc;
			}

			public override void DidChangeColorTheme () {
				NotificationsWebViewController controller = this.webviewController;
				if (controller == null)
					return;
				if (controller != null && controller.IsViewLoaded) {
					controller.ThemeController (webviewController.InterfaceOrientation);
				}
			}

			public override void GoToChatControllerUsingChatEntry (ChatEntry chatEntry) {
				NotificationsWebViewController controller = this.webviewController;
				if (controller == null)
					return;
				controller.TransitionToChatControllerUsingChatEntry (chatEntry);
			}
		}
	}
}