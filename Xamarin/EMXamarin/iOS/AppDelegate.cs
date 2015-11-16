using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AVFoundation;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using Newtonsoft.Json.Linq;
using ObjCRuntime;
using UIDevice_Extension;
using UIKit;
using Xamarin;

namespace iOS {
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register ("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate {

		// Shared GA tracker
		public IGAITracker Tracker;
		public static string TrackingId {
			get {
				if (AppEnv.EnvType == EnvType.Release) //prod
					return "UA-52238710-2";

				return "UA-52238710-1"; //dev & staging
			}
		}

		#region model
		ApplicationModel _applicationModel;
		public ApplicationModel applicationModel { 
			get { 
				return _applicationModel;
			}

			set {
				if (_applicationModel != null) {
					_applicationModel.DidReceiveServerPrompt -= DidReceiveServerPrompt;
					_applicationModel.DidReceiveRemoteAction -= DidReceiveRemoteAction;
				}

				_applicationModel = value;

				if (_applicationModel != null) {
					_applicationModel.DidReceiveServerPrompt += DidReceiveServerPrompt;
					_applicationModel.DidReceiveRemoteAction += DidReceiveRemoteAction;
				}
			}
		}
		#endregion

		// class-level declarations
		public override UIWindow Window { get; set; }
		public UIViewController rootViewController {
			get { return Window.RootViewController; }
		}

		public MainController main;
		public MainController MainController {
			get { return main; }
			set { main = value; }
		}

		public static AppDelegate Instance {
			get { return UIApplication.SharedApplication.Delegate as AppDelegate; }
		}

		// Since we can't call UIScreen.MainScreen.Scale on a background thread,
		// we get the screen scale on the main thread when the app launches.
		public nfloat ScreenScale { get; set; }

		public override void OnActivated (UIApplication application) {
			Debug.WriteLine ("OnActivated");
			foreach (BackgroundColor color in BackgroundColor.AllColors) {
				color.GetBackgroundResource ( (UIImage image) => { });
			}
			NSNotificationCenter.DefaultCenter.PostNotificationName (em.Constants.DID_BECOME_ACTIVE, null);
		}
			
		// This method is invoked when the application is about to move from active to inactive state.
		// OpenGL applications should use this method to pause.
		public override void OnResignActivation (UIApplication application) {
			Debug.WriteLine ("OnResignActivation");
		}

		nint keepAliveTaskId = UIApplication.BackgroundTaskInvalid;
		public nint KeepAliveTaskId {
			get { return keepAliveTaskId; }
		}

		bool shouldKeepTasksAlive;
		public bool ShouldKeepTasksAlive {
			get { return shouldKeepTasksAlive; }
			set { shouldKeepTasksAlive = value; }
		}

		// This method should be used to release shared resources and it should store the application state.
		// If your application supports background exection this method is called instead of WillTerminate
		// when the user quits.
		public override void DidEnterBackground (UIApplication application) {
			this.ShouldKeepTasksAlive = true;
			Debug.WriteLine ("DidEnterBackground");
			keepAliveTaskId = UIApplication.SharedApplication.BeginBackgroundTask (() => {
				if (keepAliveTaskId != UIApplication.BackgroundTaskInvalid) {
					// App will be terminated soon, kill the keep alive task.
					if (UIApplication.SharedApplication != null) {
						if (keepAliveTaskId != UIApplication.BackgroundTaskInvalid) {
							UIApplication.SharedApplication.EndBackgroundTask (keepAliveTaskId);
							keepAliveTaskId = UIApplication.BackgroundTaskInvalid;
						}
					}
				}
			});

			(applicationModel.platformFactory as iOSPlatformFactory).WaitUntilAllOperationsAreFinished ();

			UpdateApplicationBadgeCount (applicationModel.chatList.UnreadCount);
			applicationModel.chatList.ResetCachedUnreadCountValue ();
			applicationModel.AppDidChangeState (false);
			main.DoEnterBackground ();
			NSNotificationCenter.DefaultCenter.PostNotificationName (em.Constants.DID_ENTER_BACKGROUND, null);
		}

		public bool PopToChatEntry (ChatEntry entry) {
			SideMenuViewController sideMenu = (SideMenuViewController)this.main.Root.LeftPanel;
			sideMenu.navigationController.DismissViewController (false, null);
			AppDelegate.Instance.main.Root.ShowCenterPanelAnimated (true);
			UINavigationController nav = (UINavigationController)this.main.ContentController;
			UIViewController targetController = null;
			foreach (UIViewController controller in nav.ViewControllers) {
				try {
					ChatViewController cvc = (ChatViewController)controller;
					if (cvc.ChatEntry.chatEntryID == entry.chatEntryID) {
						targetController = controller;
					}
				} catch (InvalidCastException e) {
					continue;
				}
			}
			if (targetController != null) {
				nav.PopToViewController (targetController, false);
				return true;
			}
			return false;
		}

		// This method is called as part of the transiton from background to active state.
		public override void WillEnterForeground (UIApplication application) {
			this.ShouldKeepTasksAlive = false;
			Debug.WriteLine ("WillEnterForeground");
			applicationModel.AppDidChangeState (true);
			main.DoEnterForeground ();
			NSNotificationCenter.DefaultCenter.PostNotificationName (em.Constants.DID_ENTER_FOREGROUND, null);
		}

		// This method is called when the application is about to terminate. Save data, if needed.
		public override void WillTerminate (UIApplication application) {
			Debug.WriteLine ("WillTerminate called");
		}

		public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions) {
			if (launchOptions != null && launchOptions.Count > 0)
				Debug.WriteLine ("EM finished launch with options: " + launchOptions);

			applicationModel = new ApplicationModel (new iOSPlatformFactory ());
			ChatList chatList = applicationModel.chatList;
			chatList.DelegateTotalUnreadCountDidChange += TotalUnreadCountDidChange;

			Window = new UIWindow (UIScreen.MainScreen.Bounds);
			main = new MainController ();
			Window.RootViewController = new ControlRotationNavigationController (main);
			Window.MakeKeyAndVisible ();

			applicationModel.CompletedOnboardingPhase = () => {
				EMTask.DispatchMain (HandleCompletedOnboardingNotification);
			};

			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, em.Constants.ApplicationModel_DidRegisterContactsNotification, HandleContactsDidRegisterNotification);
			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, LiveServerConnection.NOTIFICATION_DID_CONNECT_WEBSOCKET, HandleDidConnectWebsocketNotification);

			EMTask.DispatchBackground (() => {
				Insights.HasPendingCrashReport += (sender, isStartupCrash) => {
					if (isStartupCrash)
						Insights.PurgePendingCrashReports().Wait();
				};

				// Initialize Xamarin Insights
				Insights.Initialize("09dd9f3bf52ea28e15514c70234c59d5578d7826");

				if (applicationModel.account.accountInfo.username != null)
					Insights.Identify (applicationModel.account.accountInfo.username, Insights.Traits.Name, applicationModel.account.accountInfo.defaultName);

				/*
				try {
					// Initialize the Parse client with your Application ID and .NET Key found on your Parse dashboard
					ParseClient.Initialize("XILecyfJqBA6qajem4XTZ01Q7GXoEvgKMuiIePrw", "6hlJ8F8onLN1ME4t173vNHlEJEKkqwjUYYbh9a4e");
				} catch(Exception e) {
					Debug.WriteLine ("Error initializing Parse: " + e.Message);
				}

				*/
			});

			// Optional: set Google Analytics dispatch interval to e.g. 20 seconds.
			GAI.SharedInstance.DispatchInterval = 20;

			// Optional: automatically send uncaught exceptions to Google Analytics.
			GAI.SharedInstance.TrackUncaughtExceptions = true;

			// Initialize tracker.
			Tracker = GAI.SharedInstance.GetTracker (TrackingId);

			applicationModel.AppDidChangeState (true);
			SetAudioSessionToRespectSilence ();
			this.ScreenScale = UIScreen.MainScreen.Scale;

			applicationModel.platformFactory.GetAdjustHelper ().Init (); // 20ms call

			if (launchOptions != null) {
				NSDictionary remoteNotification = (NSDictionary)launchOptions [UIApplication.LaunchOptionsRemoteNotificationKey];
				if (remoteNotification != null) {
					string guidKey = "g";
					NSObject guid64Object;
					if (remoteNotification.TryGetValue (NSObject.FromObject (guidKey), out guid64Object)) {
						string guid64 = guid64Object.ToString ();
						chatList.appModel.GuidFromNotification = guid64;
					}
				}
			}
			return true;
		}

		public void HandleContactsDidRegisterNotification (em.Notification n) {
			EMTask.DispatchMain (() => {
				RegisterForPushNotifications ();
			});
		}

		public void HandleDidConnectWebsocketNotification(em.Notification n) {}
		public void HandleCompletedOnboardingNotification () {}
			
		#region Push Notifications
		public void RegisterForPushNotifications () {
			if ( UIApplication.SharedApplication.RespondsToSelector(new Selector("registerUserNotificationSettings:")) ) {
				UIUserNotificationType notificationTypes = UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound;
				var settings = UIUserNotificationSettings.GetSettingsForTypes(notificationTypes, new NSSet (new string[] {}));
				UIApplication.SharedApplication.RegisterUserNotificationSettings (settings);
			} else {
				UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound;
				UIApplication.SharedApplication.RegisterForRemoteNotificationTypes (notificationTypes);
			}
		}

		public override void RegisteredForRemoteNotifications (UIApplication application, NSData deviceToken) {
			applicationModel.platformFactory.getDeviceInfo ().SetPushToken (deviceToken.GetBase64EncodedString(NSDataBase64EncodingOptions.None));
			applicationModel.account.RegisterForPushNotifications ();
			em.NotificationCenter.DefaultCenter.PostNotification (null, em.Constants.AppDelegate_DidRegisterPushNotification);
		}

		public override void FailedToRegisterForRemoteNotifications (UIApplication application , NSError error) {
			Debug.WriteLine ("Failed to register for push notification " + error.LocalizedDescription);
			applicationModel.account.RegisterForPushNotifications ();
			em.NotificationCenter.DefaultCenter.PostNotification (null, em.Constants.AppDelegate_DidRegisterPushNotification);
		}

		public override void ReceivedRemoteNotification (UIApplication application, NSDictionary userInfo) {
			//This method gets called whenever the app is already running and receives a push notification
			// You must handle the notifications in this case.  Apple assumes if the app is running, it takes care of everything
			// this includes setting the badge, playing a sound, etc.
			string guidKey = "g";
			NSObject guid64Object = null;
			if ((application.ApplicationState != UIApplicationState.Active) && userInfo.TryGetValue (NSObject.FromObject (guidKey), out guid64Object)) {
				string guid64 = guid64Object.ToString ();
				applicationModel.GuidFromNotification = guid64;
			}
			Debug.WriteLine ("Push notification message received.");
		}

		public override void DidRegisterUserNotificationSettings (UIApplication application, UIUserNotificationSettings notificationSettings) { 
			application.RegisterForRemoteNotifications (); 
		}
		#endregion

		protected void TotalUnreadCountDidChange (int updatedCount) {
			EMTask.DispatchMain (() => UpdateApplicationBadgeCount (updatedCount));
		}

		static void UpdateApplicationBadgeCount (int updatedCount) {
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = updatedCount;
		}

		public static string STATUS_BAR_CHANGED_NOTIFICATION = "statusbarchangednotification";
		public override void ChangedStatusBarFrame (UIApplication application, CoreGraphics.CGRect oldStatusBarFrame) {
			if (application.StatusBarFrame.Height != oldStatusBarFrame.Height) {
				Debug.WriteLine ("ChangedStatusBarFrame new: {0} old: {1}", application.StatusBarFrame.Height, oldStatusBarFrame.Height);
				NSNotificationCenter.DefaultCenter.PostNotificationName (STATUS_BAR_CHANGED_NOTIFICATION, null);
			}
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations (UIApplication application, UIWindow forWindow) {
			// really quick way to disable rotation in onboarding controllers
			// basically checks for if the presented view controller is a navigationcontroller, then checks that navigation controller's top controller to see if it matches any onboarding controllers
			// can be temp or permanent depending on if a better way to rotate controllers is found
			if (main != null && main.PresentedViewController != null && main.PresentedViewController.GetType () == typeof (UINavigationController)) {
				var con = main.PresentedViewController as UINavigationController;
				Type type = con.TopViewController.GetType ();
				if (type == typeof (LandingPageViewController) || type == typeof (MobileSignInViewController) || type == typeof (EmailSignInViewController) || type == typeof (MobileVerificationViewController))
					return UIInterfaceOrientationMask.Portrait;
			}

			// This is the default case on iPad and iPhone.
			return UIDevice.CurrentDevice.IsPad () ? UIInterfaceOrientationMask.All : UIInterfaceOrientationMask.AllButUpsideDown;
		}
			
		/** hook that gets triggered by em:// calls
		 */
		public override bool OpenUrl (UIApplication application, NSUrl url, string sourceApplication, NSObject annotation) {
			Debug.WriteLine ("EM triggered by em://. Full URL: " + url);

			var customUrl = new Uri (url.ToString ());
			applicationModel.customUrlSchemeController.Handle (customUrl);

			return true;
		}

		public void SetAudioSessionToRespectSilence () {
			AVAudioSession session = AVAudioSession.SharedInstance ();
			string previousCategory = session.Category;

			// https://developer.apple.com/library/ios/documentation/UserExperience/Conceptual/MobileHIG/Sound.html
			// SoloAmbient is the default audio session category, but stops other sounds, so we use Ambient instead.
			NSError error = session.SetCategory (AVAudioSessionCategory.Ambient);
			if (error != null) {
				Debug.WriteLine ("Error setting previous audio session category {0} to default", previousCategory);
			}
		}

		protected void DidReceiveServerPrompt(UserPromptInput prompt) {
			var otherButtons = new List<string> ();
			otherButtons.Add (prompt.okayButton.label);
			if (prompt.otherButtons != null && prompt.otherButtons.Length > 0) {
				foreach (UserPromptButton button in prompt.otherButtons)
					otherButtons.Add (button.label);
			}

			EMTask.DispatchMain (() => {
				em.NotificationCenter.DefaultCenter.PostNotification (em.Constants.Model_WillShowRemotePromptFromServerNotification);
				var alert = new UIAlertView (prompt.title, prompt.message, null, prompt.cancelButton == null ? null : prompt.cancelButton.label, otherButtons.ToArray<string> ());
				alert.Clicked += (object sender, UIButtonEventArgs e) => {
					var index = e.ButtonIndex;

					if(prompt.cancelButton == null)
						index++;

					switch ( index ) {
					case 0:
						applicationModel.UserDidRespondToPrompt(prompt.cancelButton);
						break;

					case 1:
						applicationModel.UserDidRespondToPrompt(prompt.okayButton);
						break;

					default:
						applicationModel.UserDidRespondToPrompt(prompt.otherButtons[ e.ButtonIndex-1 ]);
						break;
					}
				};
				alert.Show ();
			});
		}

		protected void DidReceiveRemoteAction(RemoteActionInput remoteAction) {
			// TODO test if the UI is in a state that a modal view controller can
			// be presented.
			EMTask.DispatchMain (() => {
				switch (remoteAction.AppAction) {
				case AppAction.createAka: {
						var editAliasViewController = new EditAliasViewController (null);
						editAliasViewController.SharedController.Properties = remoteAction.parameters;
						editAliasViewController.SharedController.ResponseDestination = remoteAction.responseDestination;

						var aliasViewController = new AliasViewController();

						UINavigationBarUtil.SetBackButtonToHaveNoText (aliasViewController.NavigationItem);

						UINavigationController navController = new EMNavigationController (aliasViewController);
						navController.PushViewController (editAliasViewController, false);
						MainController.Root.ShowCenterPanelAnimated (true);
						MainController.PresentViewController (navController, true, null);
						break;
					}

				case AppAction.createGroup: {
						var editGroupViewController = new EditGroupViewController(false, null, false);
						editGroupViewController.SharedController.Properties = remoteAction.parameters;
						editGroupViewController.SharedController.ResponseDestination = remoteAction.responseDestination;

						var groupsViewController = new GroupsViewController();

						UINavigationBarUtil.SetBackButtonToHaveNoText (groupsViewController.NavigationItem);

						UINavigationController navController = new EMNavigationController (groupsViewController);
						navController.PushViewController (editGroupViewController, false);
						MainController.Root.ShowCenterPanelAnimated (true);
						MainController.PresentViewController (navController, true, null);
						break;
					}

				case AppAction.inviteFriends: {
						AddressBookArgs args = AddressBookArgs.From (excludeGroups: true, exludeTemp: true, excludePreferred: true, entry: null);
						var inviteFriendsViewController = new InviteFriendsViewController(applicationModel, args);
						inviteFriendsViewController.SharedInvite.Properties = remoteAction.parameters;

						inviteFriendsViewController.SharedInvite.ResponseDestination = remoteAction.responseDestination;

						UINavigationController navController = new EMNavigationController (inviteFriendsViewController);
						MainController.Root.ShowCenterPanelAnimated (true);
						MainController.PresentViewController (navController, true, null);
						break;
					}

				case AppAction.newMessage: {
						JObject asObject = remoteAction.parameters as JObject;
						var prepopulatedInfo = new PrepopulatedChatEntryInfo(null, null);
						if ( asObject != null ) {
							JToken tok;
							tok = asObject ["from"];
							if (tok != null)
								prepopulatedInfo.FromAKA = tok.Value<string>();
							tok = asObject ["to"];
							if (tok != null)
								prepopulatedInfo.ToAKA = tok.Value<string>();							
						}

						ChatEntry chatEntry = ChatEntry.NewUnderConstructionChatEntry (applicationModel, DateTime.Now.ToEMStandardTime(applicationModel));
						chatEntry.prePopulatedInfo = prepopulatedInfo;

						INavigationManager navigationManager = applicationModel.platformFactory.GetNavigationManager ();
						navigationManager.StartNewChat(chatEntry);

						//System.Diagnostics.Debug.WriteLine ("Send new message. To AKA: " + toAka + " From AKA: " + fromAka);
						break;
					}

				default:
					break;
				}
			});
		}
	}
}