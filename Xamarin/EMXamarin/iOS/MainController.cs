using System.Collections.Generic;
using CoreGraphics;
using em;
using Foundation;
using JASidePanels;
using MBProgressHUD;
using UIDevice_Extension;
using UIKit;
using EMXamarin;
using System;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Diagnostics;

namespace iOS {
	public partial class MainController : UIViewController {

		private ApplicationModel AppModel { get; set; }

		private UIViewController centerViewController;

		public UIViewController ContentController {
			get { return centerViewController; }
		}

		private UIViewController SideMenuController { get; set; }
		private LandingPageViewController LandingPageController { get; set; }

        private bool _launching = true;
        protected bool Launching { get { return this._launching; } set { this._launching = value; } }

		private MTMBProgressHUD ProgressHUD { get; set; }

		private static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		JASidePanelController root;
		public JASidePanelController Root {
			get { return root; }
		}

		public MainController () : base (UserInterfaceIdiomIsPhone ? "MainController_iPhone" : "MainController_iPad", null) {
			this.AppModel = (UIApplication.SharedApplication.Delegate as AppDelegate).applicationModel;
		}

		public override void DidReceiveMemoryWarning () {
			// Releases the view if it doesn't have a superview.
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
			NSNotificationCenter.DefaultCenter.PostNotificationName (iOS_Constants.DID_RECEIVE_MEMORY_WARNING, null);
			ImageSetter.ClearLRUCache ();
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			// Perform any additional setup after loading the view, typically from a nib.

			UIApplication.SharedApplication.SetStatusBarStyle (UIStatusBarStyle.LightContent, false);

			this.root = new JASidePanelController ();

			this.root.ShouldDelegateAutorotateToVisiblePanel = false;
			this.root.AllowLeftOverpan = false;

			this.centerViewController = new UINavigationController (new InboxViewController ());
			this.SideMenuController = new SideMenuViewController (centerViewController as UINavigationController);

			this.SideMenuController.RestorationIdentifier = "leftControllerRestorationKey";
			this.centerViewController.RestorationIdentifier = "centerControllerRestorationKey";

			this.root.LeftPanel = this.SideMenuController;
			this.root.CenterPanel = this.centerViewController;
			this.root.LeftFixedWidth = iOS_Constants.LEFT_DRAWER_WIDTH;
			this.root.View.Frame = this.View.Frame;
			this.View.Add (root.View);
			AddChildViewController (root);
			this.root.DidMoveToParentViewController (this);

			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ApplicationModel_LiveServerConnectionChange, HandleApplicationModelLiveServerConnectionChange);
			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ApplicationModel_LoggedInAndWebsocketConnectedNotification, HandleApplicationModelLoggedInAndWebsocketConnected);

			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.AbstractInboxController_EntryWithNewInboundMessage, InAppShowNotificationBanner);
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			DoFirstLaunch ();
		}

		private void HandleApplicationModelLiveServerConnectionChange (em.Notification n) {
			Boolean hasConnection = (Boolean)n.Extra;
			PossibleDebugModeChangesWithConnectionChange (hasConnection: hasConnection);
		}

		private void HandleApplicationModelLoggedInAndWebsocketConnected (em.Notification n) {
			Debug.Assert (this.AppModel.platformFactory.OnMainThread, "HandleApplicationModelLoggedInAndWebsocketConnected called but not on main thread.");
			Debug.Assert (this.AppModel.account.IsLoggedIn, "HandleApplicationModelLoggedInAndWebsocketConnected called but not logged in.");
			Debug.Assert (this.AppModel.liveServerConnection.stompClientIsConnected (), "HandleApplicationModelLoggedInAndWebsocketConnected called but live server connection is not connected.");
			// This is also the entry point into contacts registration.
			TryToBindToWhosHere ();
		}

		#region binding to whoshere + contact registration
		private void TryToBindToWhosHere () {
			EMTask.DispatchMain (() => {
				ApplicationModel appModel = this.AppModel;
				EMAccount account = appModel.account;

				bool whosHereInstalled = InstalledAppsChecker.WhosHereInstalled;

				if (!whosHereInstalled) {
					if ( appModel.liveServerConnection != null )
						appModel.liveServerConnection.SendNoWhosHereAppToBindToAsync();
					FinishUpAfterWhosHereBindAttempt ();
				} else {
					if (account.ConfigurationShouldBindToWhosHere) {
						AskUserToBindToWhoshere ();
					} else {
						FinishUpAfterWhosHereBindAttempt ();
					}
				}
			});
		}

		private void FinishUpAfterWhosHereBindAttempt () {
			ApplicationModel appModel = this.AppModel;
			appModel.SendListOfInstalledApps ();
			appModel.RegisterContacts ();
		}

		private bool _askingToBind = false;
		private bool AskingToBind {
			get { return this._askingToBind; }
			set { this._askingToBind = value; }
		}

		public void AskUserToBindToWhoshere () {
			if (this.AskingToBind) {
				return;
			}

			ApplicationModel appModel = this.AppModel;

			var alert = new UIAlertView ("WELCOME_TITLE".t (), "ASSOCIATE_WITH_WHOSHERE_EXPLAINATION".t (), null, "CANCEL_BUTTON".t (), new string[] { "OK_BUTTON".t () });
			alert.Clicked += (object sender, UIButtonEventArgs e) => {

				var jobject = (JObject) appModel.account.configuration["whoshere"];

				switch ( e.ButtonIndex ) {
				default:
				case 0: // cancel
					// tell server we are done
					if ( appModel.liveServerConnection != null ) {
						appModel.liveServerConnection.SendCancelWhoshereBind ();
					}

					if ( jobject != null ) {
						JToken existing = jobject ["BindToWhosHere"];
						if (existing == null)
							jobject.Add ("BindToWhosHere", false);
						else
							jobject ["BindToWhosHere"] = false;
					}

					EMTask.DispatchMain (() => {
						FinishUpAfterWhosHereBindAttempt ();	
					});

					break;

				case 1: // okay
					JToken callbackToken = null;
					String callbackDomain = null;
					if (jobject != null && jobject.TryGetValue ("callbackDomain", out callbackToken)) {
						callbackDomain = callbackToken == null ? null : callbackToken.ToObject<string>();
					}

					string username = appModel.account.accountInfo.username;
					username = WebUtility.UrlEncode (username);
					string vendorId = UIDevice.CurrentDevice.IdentifierForVendor.AsString ();
					vendorId = WebUtility.UrlEncode (vendorId);
					callbackDomain = WebUtility.UrlEncode (callbackDomain);

					var url = String.Format ("whoshere://associateEM?username={0}&vendorId={1}&callbackDomain={2}", username, vendorId, callbackDomain);

					UIApplication.SharedApplication.OpenUrl (new NSUrl (url));
					break;
				}

				this.AskingToBind = false;
			};

			alert.Show ();
			this.AskingToBind = true;
		}
		#endregion

		private void PossibleDebugModeChangesWithConnectionChange (bool hasConnection) {
			if (this.AppModel.ShowVerboseMessageStatusUpdates) {
				if (hasConnection) {
					this.ContentController.View.Alpha = 1.0f;
				} else {
					this.ContentController.View.Alpha = .4f;
				}
			}
		}

		#region app lifecycle

		private void DoFirstLaunch () {
			// We should only do this once, on app start.
			if (this.Launching) {
				this.Launching = false;
				DoEnterForeground ();
			}
		}

		public void DoEnterForeground () {
			Dictionary<string, object> sessionInfo = this.AppModel.GetSessionInfo ();
			if ((bool)sessionInfo ["isOnboarding"]) {
				if (this.LandingPageController == null) {
					this.LandingPageController = new LandingPageViewController ();
					PresentViewControllerAsync (new UINavigationController (this.LandingPageController), false);
				} else {
					this.LandingPageController.ReloadWebViewContent ();
				}
			} else {
				this.AppModel.HasRunContactRegistrationForSession = false;
				this.AppModel.DoSessionStart ();
			}
		}

		public void DoEnterBackground () {
			this.AppModel.DoSessionSuspend ();

			if (this.LandingPageController != null) {
				this.LandingPageController.ReloadWebView = true;
			}
		}

		public void FinishOnboarding (bool askToGetHistoricalMessages) {
			if (askToGetHistoricalMessages) {
				em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.AppDelegate_DidRegisterPushNotification, HandleDidRegisterPushNotifications);
			} else {
				em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_StartAccessedDifferentContacts, ShowAccessingAddressBookSpinner);
				em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_AccessedDifferentContacts, HideAccessingAddressBookSpinnerAndStartNew);
				em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_FailedProcessedDifferentContacts, HideProcessingAddressBookSpinner);
				em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_ProcessedDifferentContacts, HideProcessingAddressBookSpinner);
			}

			this.AppModel.HasRunContactRegistrationForSession = false;
			this.AppModel.DoSessionStart ();
		}
		#endregion

		#region historical messages
		private void HandleDidRegisterPushNotifications (em.Notification n) {
			ShowDownloadHistoricalMessageOption ();
			em.NotificationCenter.DefaultCenter.RemoveObserverAction (HandleDidRegisterPushNotifications);
		}

		private void ShowDownloadHistoricalMessageOption () {
			var alert = new UIAlertView ("APP_TITLE".t (), "DOWNLOAD_HISTORICAL_MESSAGES".t (), null, "NO".t (), new [] { "YES".t () });
			alert.Show ();
			alert.Clicked += (sender2, buttonArgs) =>  { 

				// We do a dispatch to main here so that the UIAlertView can maintain its responsiveness and disappear right as the user taps an option.
				switch ( buttonArgs.ButtonIndex ) {
				case 0:
					EMTask.DispatchMain (() => {
						HandleGetHistoricalMessagesAnswer (false);
					});

					break;
				case 1:
					EMTask.DispatchMain (() => {
						HandleGetHistoricalMessagesAnswer (true);
					});

					break;
				}
			};
		}

		public void DisableRotation () {
			if (this.ParentViewController == null)
				return;
			ControlRotationNavigationController nav = (ControlRotationNavigationController)(this.ParentViewController);
			nav.DisableRotate ();
		}

		public void AllowRotation() {
			if (this.ParentViewController == null)
				return;
			ControlRotationNavigationController nav = (ControlRotationNavigationController)(this.ParentViewController);
			nav.AllowRotate ();
		}

		protected void InAppShowNotificationBanner (Notification notif) {
			EMAccount account = AppModel.account;
			if (account.IsLoggedIn && !AppModel.IsHandlingMissedMessages && account.UserSettings.IncomingBannerEnabled) {
				EMTask.DispatchMain (() => {
					InboxViewController inboxController = (InboxViewController)((UINavigationController)this.centerViewController).ViewControllers [0];
					ChatEntry entryWithNewInboundMessage = (ChatEntry)notif.Source;
					inboxController.HandleNotificationBanner (entryWithNewInboundMessage);
				});
			}
		}

		public void HandleGetHistoricalMessagesAnswer (bool getHistoricalMessages) {
			ApplicationModel appModel = this.AppModel;
			if (getHistoricalMessages) {
				appModel.RequestMissedMessages ();
			} else {
				appModel.RequestMissedNotifications ();
			}

			appModel.AwaitingGetHistoricalMessagesChoice = false;
		}
		#endregion

		#region spinner while accessing address book
		private void ShowAccessingAddressBookSpinner(em.Notification n) {
			EMTask.DispatchMain (() => {
				this.ProgressHUD = new MTMBProgressHUD (this.View) {
					LabelText = "ADDRESS_BOOK_ACCESSING".t (),
					LabelFont = FontHelper.DefaultFontForLabels(),
					RemoveFromSuperViewOnHide = true
				};
				this.View.AddSubview (ProgressHUD);
				this.View.BringSubviewToFront (ProgressHUD);
				this.ProgressHUD.Show (true);
			});

			em.NotificationCenter.DefaultCenter.RemoveObserverAction (ShowAccessingAddressBookSpinner);
		}

		private void HideAccessingAddressBookSpinnerAndStartNew(em.Notification n) {
			EMTask.DispatchMain (() => {
				if (this.ProgressHUD != null) {
					ProgressHUD.LabelText = "ADDRESS_BOOK_PROCESSING".t ();
				}
			});

			em.NotificationCenter.DefaultCenter.RemoveObserverAction (HideAccessingAddressBookSpinnerAndStartNew);
		}

		private void HideProcessingAddressBookSpinner(em.Notification n) {
			EMTask.DispatchMain (() => {
				if (this.ProgressHUD != null) {
					this.ProgressHUD.Hide (true);
				}
			});

			em.NotificationCenter.DefaultCenter.RemoveObserverAction (HideProcessingAddressBookSpinner);
		}
		#endregion

		public CGSize ScreenSizeAccordingToOrientation {
			get {
				// ios8's screen bounds rotate along with its device
				// ios7 screenbounds does not take orientation into account so we have to convert it.
				CGRect screenBounds = UIScreen.MainScreen.Bounds;
				if (!UIDevice.CurrentDevice.IsIos8Later ()) {
					if ((this.View.Frame.Height - this.View.Frame.Width) > 0) {
						// If the height is greater than the width, its frame is in portrait. In iOS7, this should match the screen bounds.
						return screenBounds.Size;
					} else {
						// If not, the screen bounds need to be reversed.
						var reverseSize = new CGSize ();
						reverseSize.Height = screenBounds.Width;
						reverseSize.Width = screenBounds.Height;
						return reverseSize;
					}
				} 

				return screenBounds.Size;
			}
		}
	}
}

