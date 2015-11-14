using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Net;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using AndroidHUD;
using em;
using EMXamarin;
using Google.Analytics.Tracking;
using System.Threading;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Emdroid {

	[Activity (Label = "MainActivity", Theme = "@style/AppTheme.Dark", WindowSoftInputMode = SoftInput.AdjustResize, ConfigurationChanges = (Android.Content.PM.ConfigChanges.Orientation | Android.Content.PM.ConfigChanges.ScreenSize), LaunchMode = Android.Content.PM.LaunchMode.SingleTask)]	
	[IntentFilter (new[] {"com.em.whoshere.integration"}, Categories=new[]{Intent.CategoryDefault})]
	public class MainActivity : Activity {

		//static variable to tell MainActivity which view to show (Inbox or Account). This is used for Onboarding.
		public static bool IsOnboarding;
		public static bool AskToGetHistoricalMessages;

		DrawerLayout drawer;
		MyActionBarDrawerToggle drawerToggle;
		ListView drawerList;
		LinearLayout drawerLayout;
		string[] listTitles;

		int additionalActivities;
		bool startSessionOnResume;

		const string AccountFragmentTag = "ACCOUNT_FRAGMENT_TAG";
		bool showingHUD { get; set; }
		bool dismissedHUD { get; set; }

        private Action _afterDrawerClose = null;
		private Action AfterDrawerClose { get { return this._afterDrawerClose; } set { this._afterDrawerClose = value; } }

        private bool _shouldAddContactProcessingObservers = false;
		private bool ShouldAddContactProcessingObservers { get { return this._shouldAddContactProcessingObservers; } set { this._shouldAddContactProcessingObservers = value; } }

		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			// The app model blocks during account creation so its possible this would block the main thread.
			// as a work around we move this to a background call so the main thead can continue being set up
			// while the account is initializing.
			EMTask.DispatchBackground (() => {
				EMApplication.GetInstance ().appModel.account.accountInfo.DelegateDidChangeColorTheme += DidChangeColorTheme;
			});

			EMApplication.SetCurrentActivity (this);
			// Create your application here
			EMApplication application = EMApplication.GetInstance ();

			SetContentView (Resource.Layout.Main);
			listTitles = application.appModel.GetSideMenuList ();

			drawer = FindViewById<DrawerLayout> (Resource.Id.drawer_layout);
			drawer.SetDrawerShadow(Resource.Drawable.drawer_shadow_dark, (int)GravityFlags.Start);

			drawerList = FindViewById<ListView> (Resource.Id.left_drawer);
			drawerList.Adapter = new SideMenuAdapter<string> (this, Resource.Layout.DrawerListItem, new List<string> (listTitles));
			drawerList.ItemClick += (sender, args) => SelectItem(args.Position);

			drawerLayout = FindViewById<LinearLayout> (Resource.Id.layoutForDrawer);
			drawerLayout.SetBackgroundColor (BackgroundColor.Gray.GetColor ());

			//DrawerToggle is the animation that happens with the indicator next to the
			//ActionBar icon. You can choose not to use this.
			drawerToggle = new MyActionBarDrawerToggle(this, drawer,
				Resource.Drawable.ic_drawer_light,
				Resource.String.OPEN,
				Resource.String.CLOSE);

			//You can alternatively use _drawer.DrawerOpened here
			drawerToggle.DrawerOpened += delegate {
				RefreshUnreadNotifications ();
			};

			drawerToggle.DrawerClosed += (object s, ActionBarDrawerEventArgs e) => {
				if (this.AfterDrawerClose != null) {
					this.AfterDrawerClose ();
					this.AfterDrawerClose = null;
				}
			};

			drawer.SetDrawerListener(drawerToggle);

			additionalActivities = 0;
			startSessionOnResume = true;

			var connReceiver = new NetworkConnectivityReceiver ();
			RegisterReceiver (connReceiver, new IntentFilter(ConnectivityManager.ConnectivityAction));

			if (IsOnboarding) {
				this.ShouldAddContactProcessingObservers = true;
				LaunchingExternalActivity ();
				ShowAccount ();
			} else {
				EnableSideMenu ();
				if (!IsArgumentsFromNotificationIntent (Intent)) {
					ShowInbox (Intent);
				}

			}
			if (AskToGetHistoricalMessages) {
				AskToGetHistoricalMessages = false;
				ShowConfirmGetHistoricalMessages ();
			}

			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ApplicationModel_LiveServerConnectionChange, HandleApplicationModelLostLiveServerConnection);
			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.AbstractInboxController_EntryWithNewInboundMessage, InAppShowNotificationBanner);
			if (Intent != null && Intent.Extras != null) {
				var toAka = Intent.Extras.GetString ("toAlias");
				var fromAka = Intent.Extras.GetString ("fromAlias");

				HandleNewMessage (toAka, fromAka);
			}

			EasyTracker.GetInstance (EMApplication.GetMainContext ()).ActivityStart (this);


			LinearLayout rootLayout = (LinearLayout) this.FindViewById (Resource.Id.rootLayout);
			rootLayout.ViewTreeObserver.AddOnGlobalLayoutListener (new SoftKeyboardListener (rootLayout, this));
		}

		private void HandleApplicationModelLostLiveServerConnection (em.Notification n) {
			if (EMApplication.Instance.appModel.ShowVerboseMessageStatusUpdates) {
				Boolean hasConnection = (Boolean)n.Extra;
				if (hasConnection) {
					Toast.MakeText (this, "Obtained connection", ToastLength.Short).Show ();
					drawerLayout.SetBackgroundColor (BackgroundColor.Gray.GetColor ());
				} else {
					Toast.MakeText (this, "Lost connection", ToastLength.Short).Show ();
					drawerLayout.SetBackgroundColor (BackgroundColor.Blue.GetColor ());
				}
			}
		}

		void RefreshUnreadNotifications() {
			var adapter = drawerList.Adapter as SideMenuAdapter<string>;
			adapter.UpdateUnreadNotificationCount ();
		}

		public void OpenDrawer () {
			drawer.OpenDrawer (drawerLayout);
		}

		public void LaunchingExternalActivity() {
			additionalActivities++;
		}

		public void CompletedExternalActivity() {
			additionalActivities--;
		}

		public void ShowAccount () {
			FragmentManager.BeginTransaction ()
				.SetTransition (FragmentTransit.FragmentOpen)
				.Replace (Resource.Id.content_frame, AccountFragment.NewInstance (true), AccountFragmentTag)
				.AddToBackStack (null)
				.Commit();
		}

		public int InboxFragmentBackStackID { get; set; }
		public InboxFragment InboxFragment { get; set; }

		public void ShowInbox (Intent intent) {
			this.InboxFragment = InboxFragment.NewInstance ();
			Bundle savedInstanceState = new Bundle ();
			if (IsArgumentsFromShareIntent (intent)) {
				string mediaURIString = (string)(intent.Extras.Get (ShareIntentActivity.MEDIA_INTENT_KEY));
				string textString = (string)(intent.Extras.Get (ShareIntentActivity.TEXT_INTENT_KEY));
				if (mediaURIString != null) {
					savedInstanceState.PutString (ShareIntentActivity.MEDIA_INTENT_KEY, mediaURIString);
					this.Intent.RemoveExtra (ShareIntentActivity.MEDIA_INTENT_KEY);
				} else if (textString != null) {
					savedInstanceState.PutString (ShareIntentActivity.TEXT_INTENT_KEY, textString);
					this.Intent.RemoveExtra (ShareIntentActivity.TEXT_INTENT_KEY);
				}
			} else if (IsArgumentsFromNotificationIntent (intent)) {
				string guid64 = (string)(intent.Extras.Get (GcmService.NOTIFICATION_GUID_INTENT_KEY));
				EMApplication.Instance.appModel.GuidFromNotification = guid64;
				this.Intent.RemoveExtra (GcmService.NOTIFICATION_GUID_INTENT_KEY);
			}
			this.InboxFragment.Arguments = savedInstanceState;
			this.InboxFragmentBackStackID = FragmentManager.BeginTransaction ()
			.SetTransition (FragmentTransit.FragmentOpen)
			.Replace (Resource.Id.content_frame, this.InboxFragment)
			.AddToBackStack (null)
			.Commit ();
		}

		public void HandleNewMessage(string toAka, string fromAka) {
			if(!string.IsNullOrEmpty(toAka)) {
				var prepopulatedInfo = new PrepopulatedChatEntryInfo(toAka, fromAka);

				ApplicationModel appModel = EMApplication.Instance.appModel;

				ChatEntry chatEntry = ChatEntry.NewUnderConstructionChatEntry (appModel, DateTime.Now.ToEMStandardTime(appModel));
				chatEntry.prePopulatedInfo = prepopulatedInfo;

				INavigationManager navigationManager = appModel.platformFactory.GetNavigationManager ();
				navigationManager.StartNewChat(chatEntry);

				System.Diagnostics.Debug.WriteLine ("Send new message. To AKA: " + toAka + " From AKA: " + fromAka);
			}
		}

		public void ReplaceWithInbox(bool showAccessingAddressBookSpinner) {
			FragmentManager.BeginTransaction ()
				.SetTransition (FragmentTransit.FragmentOpen)
				.Replace (Resource.Id.content_frame, InboxFragment.NewInstance ())
				.Commit ();

			FragmentManager.ExecutePendingTransactions ();

			if (showAccessingAddressBookSpinner)
				ShowContactProcessingState ();
		}

		public void DisableSideMenu () {
			drawer.SetDrawerLockMode (DrawerLayout.LockModeLockedClosed);
			drawerToggle.DrawerIndicatorEnabled = false;
		}

		public void EnableSideMenu () {
			drawer.SetDrawerLockMode (DrawerLayout.LockModeUnlocked);
			drawerToggle.DrawerIndicatorEnabled = true;
		}

		#region historical messages
		public void ShowConfirmGetHistoricalMessages() {
			var builder = new AlertDialog.Builder (this);

			builder.SetTitle ("APP_TITLE".t ());
			builder.SetMessage ("DOWNLOAD_HISTORICAL_MESSAGES".t ());
			builder.SetPositiveButton ("YES".t (), (sender, dialogClickEventArgs) => {
				EMTask.DispatchMain (() => {
					HandleGetHistoricalMessagesAnswer (getHistoricalMessages: true);
				});
			});

			builder.SetNegativeButton("NO".t (), (sender, dialogClickEventArgs) => {
				EMTask.DispatchMain (() => {
					HandleGetHistoricalMessagesAnswer (getHistoricalMessages: false);
				});
			});

			// If they cancel out of the alert, just proceed as if they entered NO.
			builder.SetOnCancelListener (new GetHistoricalMessageChoiceCancelListener (this));
			builder.Create ();
			builder.Show ();
		}

		public void HandleGetHistoricalMessagesAnswer (bool getHistoricalMessages) {
			ApplicationModel appModel = EMApplication.Instance.appModel;
			if (getHistoricalMessages) {
				appModel.RequestMissedMessages ();
			} else {
				appModel.RequestMissedNotifications ();
			}

			appModel.AwaitingGetHistoricalMessagesChoice = false;
		}

		protected void InAppShowNotificationBanner (em.Notification notif) {
			ApplicationModel appModel = EMApplication.Instance.appModel;
			em.EMAccount account = appModel.account;
			if (account.IsLoggedIn && !appModel.IsHandlingMissedMessages && account.UserSettings.IncomingBannerEnabled) {
				InboxFragment inbox = this.InboxFragment;
				if (inbox == null) {
					return;
				}

				ChatEntry entryWithNewInboundMessage = (ChatEntry)notif.Source;
				inbox.HandleNotificationBanner (entryWithNewInboundMessage);
			}
		}

		class GetHistoricalMessageChoiceCancelListener : Java.Lang.Object, IDialogInterfaceOnCancelListener {

			private WeakReference mainActivityRef;
			private MainActivity MainActivity {
				get { return mainActivityRef.Target as MainActivity; }
				set { mainActivityRef = new WeakReference (value); }
			}

			public GetHistoricalMessageChoiceCancelListener (MainActivity c) {
				this.MainActivity = c;
			}

			public void OnCancel (IDialogInterface dialog) {
				MainActivity actvit = this.MainActivity;
				if (actvit != null) {
					actvit.HandleGetHistoricalMessagesAnswer (false);
				}
			}
		}
		#endregion

		#region spinner while accessing address book
		private bool showContactProcessingStateEnabled = true;

		void HandleStartAccessedDifferentContacts (em.Notification n) {
			if (!this.showContactProcessingStateEnabled) {
				return;
			}

			ShowContactProcessingState ();
		}

		void HandleAccessedDifferentContacts (em.Notification n) {
			if (!this.showContactProcessingStateEnabled) {
				return;
			}

			ShowContactProcessingState ();
		}

		void HandleShowContactProcessingState (em.Notification n) {
			if (!this.showContactProcessingStateEnabled) {
				return;
			}

			ShowContactProcessingState ();
		}

		void HandleProcessedDifferentContacts (em.Notification n) {
			// showing the contact processing progress is a one-off done during the onboarding process, so we disable this once it's run through once.
			this.showContactProcessingStateEnabled = false;

			ShowContactProcessingState ();

			NotificationCenter.DefaultCenter.RemoveObserverAction (HandleStartAccessedDifferentContacts);
			NotificationCenter.DefaultCenter.RemoveObserverAction (HandleAccessedDifferentContacts);
			NotificationCenter.DefaultCenter.RemoveObserverAction (HandleShowContactProcessingState);
			NotificationCenter.DefaultCenter.RemoveObserverAction (HandleProcessedDifferentContacts);
		}

		public void ShowContactProcessingState () {
			ApplicationModel applicationModel = EMApplication.Instance.appModel;
			ContactProcessingState state = applicationModel.ContactProcessingState;
			ShowContactProcessingState (state);
		}

		public void ShowContactProcessingState (ContactProcessingState state) {
			switch (state) {
			case ContactProcessingState.Inactive:
				HideProcessingAddressBookSpinner ();
				break;
			case ContactProcessingState.Acquiring_Access:
			case ContactProcessingState.Accessing:
				ShowAccessingAddressBookSpinner ();
				break;
			case ContactProcessingState.Processing:
			case ContactProcessingState.Awaiting_Registration:
			case ContactProcessingState.Registering:
				ShowProcessingAddressBookSpinner ();
				break;
			} 
		}

		void ShowAccessingAddressBookSpinner() {
			var frag = FragmentManager.FindFragmentByTag (AccountFragmentTag);
			if(frag == null || !frag.IsVisible) {
				EMTask.DispatchMain (() => {
					if (!this.IsFinishing) {
						AndHUD.Shared.Dismiss (this);
						AndHUD.Shared.Show (this, "ADDRESS_BOOK_ACCESSING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null);
					}
				});
			}
		}

		void ShowProcessingAddressBookSpinner () {
			var frag = FragmentManager.FindFragmentByTag (AccountFragmentTag);
			if (frag == null || !frag.IsVisible) {
				EMTask.DispatchMain (() => {
					if (!this.IsFinishing) {
						AndHUD.Shared.Dismiss (this);
						AndHUD.Shared.Show (this, "ADDRESS_BOOK_PROCESSING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null);
					}
				});
			}
		}

		void HideProcessingAddressBookSpinner () {
			var frag = FragmentManager.FindFragmentByTag (AccountFragmentTag);
			if (frag == null || !frag.IsVisible) {
				EMTask.DispatchMain (() => {
					if (!this.IsFinishing) {
						//need to ensure the keyboard isn't shown after Account / onboarding
						KeyboardUtil.HideKeyboard (this);
						AndHUD.Shared.Dismiss (this);
					}
				});
			}
		}
		#endregion

		protected override void OnPostCreate(Bundle savedInstanceState) {
			base.OnPostCreate(savedInstanceState);
			drawerToggle.SyncState();
		}

		protected override void OnStart () {
			base.OnStart ();
		}

		protected override void OnRestart () {
			base.OnRestart ();
		}

		private bool IsArgumentsFromShareIntent (Intent intent) {
			return (intent.Action != null) && (intent.Extras != null) && (intent.Action).Equals (Intent.ActionSend);
		}

		// Was MainActivity spawned from tapping entry in notification panel?
		private bool IsArgumentsFromNotificationIntent (Intent intent) {
			if (intent.Extras == null) {
				return false;
			}
			return (intent.Extras != null) && (intent.Extras.Get (GcmService.NOTIFICATION_GUID_INTENT_KEY) != null);
		}

		protected override void OnNewIntent (Intent intent) {
			if (IsArgumentsFromShareIntent (intent)) {
				ShowInbox (intent);
			}
		}

		public void RedirectToChatEntryWithGuid(string guid64) {
			EMTask.DispatchBackground (() => {
				ApplicationModel appModel = EMApplication.GetInstance ().appModel;
				if (guid64 != null) {
					ChatEntry chatEntry = appModel.FindChatEntryThatMatchesGUID (guid64);
					if (chatEntry != null) {
						EMTask.DispatchMain ( () => {
							this.InboxFragment.GoToChatEntry (chatEntry);
						});
						appModel.GuidFromNotification = null;
					}
				}
			});
		}

		protected override void OnResume () {
			base.OnResume ();
			EMApplication.SetCurrentActivity (this);

			WeakReference thisRef = new WeakReference (this);
			Action connectionHandler = () => {
				MainActivity self = thisRef.Target as MainActivity;
				if (GCCheck.Gone (self)) return;

				if (self.IsArgumentsFromNotificationIntent (self.Intent)) {
					self.ShowInbox (self.Intent);
				}	
			};

			DoPossibleConnectWithCallback (connectionHandler);
			GcmService.ResetUnreadCount ();
			GcmService.ClearNotificationsFromStatusBar ();

			if (this.ShouldAddContactProcessingObservers) {
				NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_StartAccessedDifferentContacts, HandleStartAccessedDifferentContacts);
				NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_AccessedDifferentContacts, HandleAccessedDifferentContacts);
				NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_FailedProcessedDifferentContacts, HandleShowContactProcessingState);
				NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_ProcessedDifferentContacts, HandleProcessedDifferentContacts);
				this.ShouldAddContactProcessingObservers = false;
			}

			AndroidAdjustHelper.Shared.Resume ();

			ApplicationModel applicationModel = EMApplication.Instance.appModel;
			applicationModel.DidReceiveServerPrompt += DidReceiveServerPrompt;
			applicationModel.DidReceiveRemoteAction += DidReceiveRemoteAction;
		}
			
		protected override void OnPause () {
			DoPossibleDisconnect ();
			base.OnPause ();
			AndroidAdjustHelper.Shared.Pause ();

			ApplicationModel applicationModel = EMApplication.Instance.appModel;
			applicationModel.DidReceiveServerPrompt -= DidReceiveServerPrompt;
			applicationModel.DidReceiveRemoteAction -= DidReceiveRemoteAction;
		}

		protected override void OnStop () {
			base.OnStop ();
		}

		protected override void OnDestroy () {
			EMApplication.GetInstance().appModel.account.accountInfo.DelegateDidChangeColorTheme -= DidChangeColorTheme;

			//TODO
			//EMApplication.GetInstance().appModel.account.accountInfo.DelegateDidChangeAddOrRemoveAlias -= Did

			MemoryUtil.ClearReferences (this);
			EasyTracker.GetInstance (EMApplication.GetMainContext ()).ActivityStop (this);
			base.OnDestroy ();
		}

		public override void OnConfigurationChanged(Configuration newConfig) {
			base.OnConfigurationChanged(newConfig);
			drawerToggle.OnConfigurationChanged(newConfig);
		}

		public override bool OnCreateOptionsMenu(IMenu menu) {
			//MenuInflater.Inflate(Resource.Menu.main, menu);
			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnPrepareOptionsMenu(IMenu menu) {
			//var drawerOpen = drawer.IsDrawerOpen(Resource.Id.left_drawer);
			return base.OnPrepareOptionsMenu(menu);
		}

		private void SelectItem(int position) {
			Fragment toFragment = null;
			var sideMenuItem = (SideMenuItems)position;
			switch (sideMenuItem) {
			case SideMenuItems.Account:
				toFragment = AccountFragment.NewInstance (false);
				break;
			case SideMenuItems.Alias:
				toFragment = AliasFragment.NewInstance ();
				break;
			case SideMenuItems.Notifications:
				toFragment = NotificationsFragment.NewInstance ();
				break;
			case SideMenuItems.Groups:
				toFragment = GroupsFragment.NewInstance ();
				break;
			case SideMenuItems.Invite:
				toFragment = InviteFriendsFragment.NewInstance (EMApplication.Instance.appModel, AddressBookArgs.From (excludeGroups: true, exludeTemp: true, excludePreferred: true, entry: null));
				break;
			/*
			case SideMenuItems.Search:
				toFragment = SearchFragment.NewInstance ();
				break;
			*/
			case SideMenuItems.Help:
				toFragment = HelpFragment.NewInstance ();
				break;
			case SideMenuItems.Settings:
				toFragment = SettingsFragment.NewInstance ();
				break;
			case SideMenuItems.About:
				toFragment = AboutFragment.NewInstance ();
				break;
			default: // inbox
				toFragment = InboxFragment.NewInstance ();
				break;
			}

			this.AfterDrawerClose = () => {
				EMTask.DispatchMain (() => {
					this.FragmentManager.BeginTransaction ()
						.SetCustomAnimations (Resource.Animation.slide_up, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.slide_down)
						.Replace (Resource.Id.content_frame, toFragment)
						.AddToBackStack (null)
						.Commit();
				});
			};

			drawer.CloseDrawer (drawerLayout);
			drawerList.SetItemChecked(position, true);
		}
			
		public override void OnBackPressed () {
			// If there's more than one fragment on the stack, we pop back to the previous one.
			// If the back button is pressed while there's only one fragment, we exit the app.
			if (FragmentManager.BackStackEntryCount > 1)
				FragmentManager.PopBackStack ();
			else
				Finish ();
		}

		protected override void OnSaveInstanceState (Bundle savedInstanceState) {
			base.OnSaveInstanceState (savedInstanceState);
		}

		protected override void OnRestoreInstanceState (Bundle savedInstanceState) {
			base.OnRestoreInstanceState (savedInstanceState);
		}

		protected void DidChangeColorTheme(CounterParty accountInfo) {}

		public void SetSoftInputToAlwaysShow() {
			Window.SetSoftInputMode (SoftInput.StateAlwaysVisible|SoftInput.AdjustResize);
		}

		public void ResetSoftInput() {
			Window.SetSoftInputMode (SoftInput.StateUnspecified|SoftInput.AdjustUnspecified);
		}

		private void DoPossibleConnect () {
			DoPossibleConnectWithCallback (null);
		}

		private void DoPossibleConnectWithCallback (Action callback) {
			if (startSessionOnResume) {
				ApplicationModel applicationModel = EMApplication.Instance.appModel;
				applicationModel.AppDidChangeState (true);
				applicationModel.HasRunContactRegistrationForSession = false;
				applicationModel.DoSessionStartWithCallback (callback);
				startSessionOnResume = false;
			}
		}

        private Int64 _mostRecentTimerState = 0;
        private Int64 MostRecentTimerState { get { return this._mostRecentTimerState; } set { this._mostRecentTimerState = value; } }

		private const int TimeToWaitBeforeDisconnectionSessionWhileInsideExternalActivity = 30000; //ms

		private void DoPossibleDisconnect () {
			if (additionalActivities <= 0) {
				InnerDoDisconnect ();
			} else {
				new Timer ((object o) => {
					EMTask.DispatchMain (() => {
						Int64 state = (Int64)o;
						// System.Diagnostics.Debug.WriteLine ("state {0} most recent state {1}", state, this.MostRecentTimerState);
						if (state == this.MostRecentTimerState) {
							if (additionalActivities > 0) {
								System.Diagnostics.Debug.WriteLine ("Waited {0}ms to Resume: Disconnecting Session.", TimeToWaitBeforeDisconnectionSessionWhileInsideExternalActivity);
								InnerDoDisconnect ();
							}
						}
					});
				}, ++this.MostRecentTimerState, TimeToWaitBeforeDisconnectionSessionWhileInsideExternalActivity, Timeout.Infinite);
			}
		}

		private void InnerDoDisconnect () {
			ApplicationModel applicationModel = EMApplication.Instance.appModel;
			applicationModel.AppDidChangeState (false); // We'll also consider the app backgrounded if it reaches this point.
			applicationModel.DoSessionSuspend ();
			startSessionOnResume = true;
		}

		protected void DidReceiveServerPrompt(UserPromptInput prompt) {
			List<string> otherButtons = new List<string> ();
			if (prompt.otherButtons != null && prompt.otherButtons.Length > 0) {
				foreach (UserPromptButton button in prompt.otherButtons)
					otherButtons.Add (button.label);
			}
				
			EMTask.DispatchMain (() => {
				em.NotificationCenter.DefaultCenter.PostNotification (Constants.Model_WillShowRemotePromptFromServerNotification);
				var dialog = new AndroidModalDialogs ();
				dialog.ShowMessageWithButtons (prompt.title, prompt.message, prompt.okayButton.label, prompt.cancelButton.label, otherButtons.ToArray<string>(),
					(sender, args) => {
						ApplicationModel applicationModel = EMApplication.Instance.appModel;

						switch ( args.Which ) {
						case 0:
							applicationModel.UserDidRespondToPrompt(prompt.cancelButton);
							break;

						case 1:
							applicationModel.UserDidRespondToPrompt(prompt.okayButton);
							break;

						default:
							applicationModel.UserDidRespondToPrompt(prompt.otherButtons[ args.Which - 1]);
							break;
						}
					});
			});
		}

		protected void DidReceiveRemoteAction(RemoteActionInput remoteAction) {
			// TODO test if the UI is in a state that a modal view controller can
			// be presented.
			EMTask.DispatchMain (() => {
				switch (remoteAction.AppAction) {
				case AppAction.createAka: {
						//bug 415: Push Action not setting back button correctly so user leaves create screen back to original location
						//push the alias fragment first, so when you go back from add alias, it shows the AKA screen
						AliasFragment alias = AliasFragment.NewInstance();
						FragmentManager.BeginTransaction ()
							.SetTransition (FragmentTransit.FragmentOpen)
							.Replace (Resource.Id.content_frame, alias)
							.AddToBackStack (null)
							.Commit();
						
						EditAliasFragment fragment = EditAliasFragment.NewInstance (null);
						fragment.SharedController.Properties = remoteAction.parameters;
						fragment.SharedController.ResponseDestination = remoteAction.responseDestination;

						FragmentManager.BeginTransaction ()
							.SetTransition (FragmentTransit.FragmentOpen)
							.Replace (Resource.Id.content_frame, fragment)
							.AddToBackStack (null)
							.Commit();
						
						break;
					}

				case AppAction.createGroup: {
						//bug 415: Push Action not setting back button correctly so user leaves create screen back to original location
						//push the groups fragment first, so when you go back from add group, it shows the groups screen
						GroupsFragment groups = GroupsFragment.NewInstance();
						FragmentManager.BeginTransaction ()
							.SetTransition (FragmentTransit.FragmentOpen)
							.Replace (Resource.Id.content_frame, groups)
							.AddToBackStack (null)
							.Commit();
						
						EditGroupFragment fragment = EditGroupFragment.NewInstance(false, null);
						fragment.SharedController.Properties = remoteAction.parameters;
						fragment.SharedController.ResponseDestination = remoteAction.responseDestination;

						FragmentManager.BeginTransaction ()
							.SetTransition (FragmentTransit.FragmentOpen)
							.Replace (Resource.Id.content_frame, fragment)
							.AddToBackStack (null)
							.Commit();
						
						break;
					}

				case AppAction.inviteFriends: {
						InviteFriendsFragment inviteFriendsFragment = InviteFriendsFragment.NewInstance (EMApplication.Instance.appModel, AddressBookArgs.From (excludeGroups: true, exludeTemp: true, excludePreferred: true, entry: null));
						inviteFriendsFragment.SharedInvite.Properties = remoteAction.parameters;
						FragmentManager.BeginTransaction ()
							.SetTransition (FragmentTransit.FragmentOpen)
							.Replace (Resource.Id.content_frame, inviteFriendsFragment)
							.AddToBackStack (null)
							.Commit();

						break;
					}

				case AppAction.newMessage: {
						ApplicationModel appModel = EMApplication.GetInstance().appModel;
						ChatEntry chatEntry = ChatEntry.NewUnderConstructionChatEntry (appModel, DateTime.Now.ToEMStandardTime(appModel));
						JObject asObject = remoteAction.parameters as JObject;
						PrepopulatedChatEntryInfo prepopulatedInfo = new PrepopulatedChatEntryInfo(null, null);
						if ( asObject != null ) {
							JToken tok;
							tok = asObject ["from"];
							if (tok != null)
								prepopulatedInfo.FromAKA = tok.Value<string>();
							tok = asObject ["to"];
							if (tok != null)
								prepopulatedInfo.ToAKA = tok.Value<string>();							
						}

						chatEntry.prePopulatedInfo = prepopulatedInfo;

						INavigationManager navigationManager = appModel.platformFactory.GetNavigationManager ();
						navigationManager.StartNewChat(chatEntry);
						break;
					}

				default:
					break;
				}
			});
		}
	}
}