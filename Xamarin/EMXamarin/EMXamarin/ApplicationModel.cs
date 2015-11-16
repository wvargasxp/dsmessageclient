using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace em
{
	public class ApplicationModel {
		static object cacheIndexLock = new object();
		static int cacheIndexCounter = 0;

		public IUriGenerator uriGenerator;
		public DaoConnection daoConnection;
		public DaoConnection outgoingQueueDaoConnection;
		public ChatEntryDao chatEntryDao;
		public NotificationEntryDao notificationEntryDao;
		public MessageDao msgDao;
		public ContactDao contactDao;
		public PreferencesDao preferenceDao;
		public OutgoingQueueDao queueDao;
		public CustomUrlSchemeController customUrlSchemeController;
		public LiveServerConnection liveServerConnection;
		public OutgoingQueue outgoingQueue;
		public MediaManager mediaManager;
		public int cacheIndex;
		public string GuidFromNotification { get; set; }

		public IAnalyticsHelper analyticsHelper;

		public delegate void DidReceiveServerPromptDelegate(UserPromptInput userPromptInput);
		public DidReceiveServerPromptDelegate DidReceiveServerPrompt = delegate(UserPromptInput userPromptInput) { };

		public void UserDidRespondToPrompt(UserPromptButton button) {
			EMTask.DispatchBackground (() => {
				if ( button != null && button.destination != null && button.responseAction != null && liveServerConnection != null ) {
					liveServerConnection.ClientSendMessage(button.responseAction.ToString(), button.destination);
				}
			});
		}

		public void UserDidRespondToRemoteActionButton (UserPromptButton button) {
			EMTask.DispatchBackground (() => {
				if ( button != null && button.destination != null && liveServerConnection != null ) {
					liveServerConnection.ClientSendMessage(button.Response, button.destination);
				}
			});
		}

		public delegate void DidReceiveRemoteActionDelegate(RemoteActionInput remoteAction);
		public DidReceiveRemoteActionDelegate DidReceiveRemoteAction = delegate(RemoteActionInput remoteAction) { };

		//TimedQueueExecutor timedQueueExecutor = new TimedQueueExecutor ();

		bool attemptingToLogin;

		double? emStandardTimeDiff = null;
		public double EMStandardTimeDiff {
			get {
				if (emStandardTimeDiff == null) {
					//try pulling from preference table
					emStandardTimeDiff = Preference.GetPreference<double> (this, Preference.EM_STANDARD_TIME);
				}

				return emStandardTimeDiff.Value;
			}
			set { 
				emStandardTimeDiff = value;
				Preference.UpdatePreference<double> (this, Preference.EM_STANDARD_TIME, emStandardTimeDiff.Value);
			}
		}
			
		public ChatList chatList { get; set; }
		public NotificationList notificationList { get; set; }

		private bool _appInForeground = false;

		public bool AppInForeground { 
			get { return this._appInForeground; }
			set {
				Debug.Assert (this.platformFactory.OnMainThread, "Should only set AppInForeground on the main thread.");
				this._appInForeground = value;
			}
		}

		public bool shouldAttemptConnect;

        private bool awaitingToGetHistoricalMessagesChoice = false;
		public bool AwaitingGetHistoricalMessagesChoice { get { return this.awaitingToGetHistoricalMessagesChoice; } set { this.awaitingToGetHistoricalMessagesChoice = value; } }

        private bool showVerboseMessageStatusUpdates = false;
		public bool ShowVerboseMessageStatusUpdates { get { return this.showVerboseMessageStatusUpdates; } set { this.showVerboseMessageStatusUpdates = value; } }

		public Action CompletedOnboardingPhase;

		PlatformFactory pf;
		public PlatformFactory platformFactory {
			get { return pf; }
			set { pf = value; }
		}

		static PlatformFactory staticPf;
		public static PlatformFactory SharedPlatform {
			get { return staticPf; }
		}

		EMAccount emAccount;
		public EMAccount account {
			get { return emAccount; }
			set { emAccount = value; }
		}

		private ContactsManager contactsManager = null;
		private ContactsManager ContactsManager {
			get {
				if (contactsManager == null) {
					contactsManager = new ContactsManager (this, platformFactory.getAddressBook ());
				}

				return contactsManager;
			}
		}
		public ContactProcessingState ContactProcessingState {
			get {
				return this.ContactsManager.ContactProcessingState;
			}
		}

		bool ishandlingMissedMessages;
		public bool IsHandlingMissedMessages {
			get { return ishandlingMissedMessages; }
			set { 
				bool old = ishandlingMissedMessages;
				ishandlingMissedMessages = value; 
				if (old != ishandlingMissedMessages) {
					var extra = new Dictionary<string, bool> ();
					extra [IS_HANDLING_MISSED_MESSAGES_STATUS_UPDATE_EXTRA] = ishandlingMissedMessages;
					NotificationCenter.DefaultCenter.PostNotification (this, ApplicationModel.IS_HANDLING_MISSED_MESSAGES_STATUS_UPDATE, extra);
				}
			}
		}

		public static readonly string IS_HANDLING_MISSED_MESSAGES_STATUS_UPDATE_EXTRA = "ishandlingmissedmessagesstatusupdateextra";
		public static readonly string IS_HANDLING_MISSED_MESSAGES_STATUS_UPDATE = "ishandlingmissedmessagesstatusupdate";

		public ApplicationModel (PlatformFactory pf) {
			long start = DateTime.Now.Ticks;
			staticPf = pf;
			platformFactory = pf;
			emAccount = new EMAccount (this, platformFactory.getPlatformType(), platformFactory.getDeviceInfo());
			uriGenerator = platformFactory.GetUriGenerator ();

			// create the Daos
			daoConnection = new DaoConnection (this, Constants.DB_MAIN);
			outgoingQueueDaoConnection = new DaoConnection (this, Constants.DB_OUTGOING_QUEUE);
			contactDao = new ContactDao (this);
			chatEntryDao = new ChatEntryDao (this);
			notificationEntryDao = new NotificationEntryDao (this);
			msgDao = new MessageDao (this);
			preferenceDao = new PreferencesDao (daoConnection);
			queueDao = new OutgoingQueueDao (this);
			mediaManager = new MediaManager (pf);

			lock (pf) {
				EMTask.DispatchBackground (() => {
					lock ( daoConnection ) {
						lock ( pf ) {
							Monitor.PulseAll(pf);
						}

						contactDao.CreateIfNeccessary();
						chatEntryDao.CreateIfNeccessary();
						notificationEntryDao.CreateIfNeccessary();
						msgDao.CreateIfNeccessary();
						preferenceDao.CreateIfNeccessary();
					}
				});

				Monitor.Wait (pf); // each background task around creation pulses us when it has its lock

				EMTask.DispatchBackground (() => {
					lock ( outgoingQueueDaoConnection ) {
						lock ( pf ) {
							Monitor.PulseAll(pf);
						}

						queueDao.CreateIfNeccessary();
					}
				});

				Monitor.Wait (pf); // each background task around creation pulses us when it has its lock
			}

			platformFactory.StartMonitoringNetworkConnectivity (OnNetworkReachable, OnNetworkUnreachable);

			customUrlSchemeController = new CustomUrlSchemeController (this);

			analyticsHelper = platformFactory.GetAnalyticsHelper ();

			lock (cacheIndexLock) {
				cacheIndex = cacheIndexCounter;
				Contact.AddCacheAtIndex (cacheIndex);
				Message.AddCacheAtIndex (cacheIndex);
				cacheIndexCounter++;
			}

			// ChatList requires the cache to be ready.
			// See BackgroundInitEntries in ChatList constructor.
			// It goes and finds chat entries which find contacts which then tries to add to the Contact cache.
			chatList = new ChatList (this);

			notificationList = new NotificationList (this);
			outgoingQueue = new OutgoingQueue ();
			outgoingQueue.appModel = this;

			recentlyPlayedTimer = null;

			// OutgoingQueue relies on the caches to be set beforehand, so call this after the caches have been created.
			outgoingQueue.DidLaunchApp ();

			this.IsHandlingMissedMessages = false;

			this.NetworkReachable = true;

			long finish = DateTime.Now.Ticks;
			Debug.WriteLine("******************** Time to initialize app model: " + TimeSpan.FromTicks(finish - start).Duration());
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.EMAccount_LoginAndHasConfigurationNotification, HandleLoginAndHasConfigurationNotification);

			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.EMAccount_EMHttpUnauthorizedResponseNotification, HandleEmHttpUnauthorizedResponse);
		}

		#region network related

		private void HandleEmHttpUnauthorizedResponse (Notification notif) {
			OnSessionUnauthorized ();
		}

		private void OnSessionUnauthorized () {
			EMTask.DispatchMain (() => {
				account.IsLoggedIn = false;
				DoSessionStart ();
			});
		}

		public bool NetworkReachable {
			get;
			set;
		}

		public void OnNetworkReachable () {
			Debug.Assert (SharedPlatform.OnMainThread, "OnNetworkReachable not being called on main thread.");
			this.NetworkReachable = true;
			if (this.AppInForeground) {
				Debug.WriteLine ("Network is now reachable. Start Session.");
				DoSessionStart ();
			} else {
				Debug.WriteLine ("Network is now reachable but app is not in foreground. - Do Nothing.");
			}
		}

		public void OnNetworkUnreachable () {
			Debug.Assert (SharedPlatform.OnMainThread, "OnNetworkUnreachable not being called on main thread.");
			Debug.WriteLine ("network is now unreachable");
			this.NetworkReachable = false;
			DoSessionStop ();
		}

		public Dictionary<string, object> GetSessionInfo () {
			ISecurityManager security = platformFactory.GetSecurityManager ();
			string acctID = security.GetSecureKeyValue (Constants.USERNAME_KEY);
			if(acctID == null) {
				acctID = security.retrieveSecureField ("accountID");
				if(acctID != null) {
					security.SaveSecureKeyValue (Constants.USERNAME_KEY, acctID);
					security.removeSecureField ("accountID");
				}
			}

			string psswd = security.GetSecureKeyValue (Constants.VERIFICATION_CODE_KEY);
			if(psswd == null) {
				psswd = security.retrieveSecureField ("password");
				if(psswd != null) {
					security.SaveSecureKeyValue (Constants.VERIFICATION_CODE_KEY, psswd);
					security.removeSecureField ("password");
				}
			}

			var sessionInfo = new Dictionary<string, object> ();
			if (acctID != null && psswd != null) {
				sessionInfo.Add ("accountID", acctID);
				sessionInfo.Add ("password", psswd);
				sessionInfo.Add ("isOnboarding", false);
			} else {
				sessionInfo.Add ("isOnboarding", true);
			}

			return sessionInfo;
		}

		private string GetSecuredAccountId () {
			ISecurityManager security = platformFactory.GetSecurityManager ();
			string acctID = security.GetSecureKeyValue (Constants.USERNAME_KEY);
			if (acctID == null) {
				acctID = security.retrieveSecureField ("accountID");
				if (acctID != null) {
					security.SaveSecureKeyValue (Constants.USERNAME_KEY, acctID);
					security.removeSecureField ("accountID");
				}
			}

			return acctID;
		}

		private string GetSecuredPassword () {
			ISecurityManager security = platformFactory.GetSecurityManager ();
			string psswd = security.GetSecureKeyValue (Constants.VERIFICATION_CODE_KEY);
			if (psswd == null) {
				psswd = security.retrieveSecureField ("password");
				if (psswd != null) {
					security.SaveSecureKeyValue (Constants.VERIFICATION_CODE_KEY, psswd);
					security.removeSecureField ("password");
				}
			}

			return psswd;
		}

		public void DoSessionStart () {
			DoSessionStartWithCallback (null);
		}

		public void DoSessionStartWithCallback (Action successCallback) {
			Debug.Assert (SharedPlatform.OnMainThread, "DoSessionStart not being called on main thread.");
			string acctID = GetSecuredAccountId ();
			string psswd = GetSecuredPassword ();

			if (acctID != null && psswd != null) {
				InitializeLiveServerConnection (acctID, psswd);

				if (this.NetworkReachable == false) {
					Debug.WriteLine ("doSessionStart: attempt to start session when network is unreachable. Aborting.");
					return;
				}

				StartLiveServerConnection ();

				if (account.IsLoggedIn) {
					Debug.WriteLine ("doSessionStart: account already logged in");
				} else {
					Debug.WriteLine ("doSessionStart: Logging in with account identifier.");

					if (attemptingToLogin) {
						return;
					}
						
					// If we're already asking the user if they want to get missed messages. Then we shouldn't get missed messages here.
					// If we aren't, then get missed messages.
					bool getMissedMessages = !this.AwaitingGetHistoricalMessagesChoice;

					attemptingToLogin = true;
					account.LoginWithAccountIdentifier (acctID, psswd, getMissedMessages, (success, existing) => {
						if (success) {
							EMTask.DispatchMain (() => {
								Debug.WriteLine ("doSessionStart: Logged in, will complete session start.");
								attemptingToLogin = false;

								NotificationCenter.DefaultCenter.PostNotification (Constants.EMAccount_EMHttpAuthorizedResponseNotification);

								if (this.AppInForeground) {
									if (successCallback != null) {
										successCallback ();
									}
								}
							});
						} else {
							Timer reconnectTimer = null;
							reconnectTimer = new Timer (o => EMTask.DispatchMain (() => {
								try {
									Debug.WriteLine ("Will attempt session start again.");
									attemptingToLogin = false;
									// This reconnect timer could potentially have ran when the user has already exited the app.
									// In that case, we wouldn't want to do a session start.
									if (this.AppInForeground) {
										DoSessionStart ();
									}
								} finally {
									reconnectTimer.Dispose ();
									reconnectTimer = null;
								}
							}), null, Constants.TIMER_INTERVAL_BETWEEN_RECONNECTS, Timeout.Infinite);
						}
					});
				}
			} else {
				// exception
				Debug.WriteLine ("Username and password null when expecting values");
			}
		}

		public void ContactsManagerBeginProcessingContacts () {
			this.ContactsManager.AccessContactsWithPermission ((ContactsUpdatedStatus updateStatus) => { }, false);
		}

		public void ContactsManagerAccessContacts() {
			this.ContactsManager.AccessContactsWithPermission ((ContactsUpdatedStatus updateStatus) => { });
		}

		void InitializeLiveServerConnection (string accountID, string password) {
			liveServerConnection = LiveServerConnection.GetInstance (accountID, password, this, OnLiveServerConnectionStatusUpdate, OnLiveServerReconnect);
		}

		void StartLiveServerConnection () {
			liveServerConnection.Start ();
		}

		void StopLiveServerConnection () {
			if (liveServerConnection != null) {
				liveServerConnection.Shutdown ();
			}
		}

		public void RequestMissedMessages () {
			Debug.WriteLine ("Requesting Missed Messages and Notifications");
			DateTime messagesSince = Preference.GetPreference<DateTime>(this, Preference.LAST_MESSAGE_UPDATE );
			account.RequestMissedMessagesAsync (messagesSince);
		}

		public void RequestMissedNotifications() {
			Debug.WriteLine ("Requesting Missed Notifications");
			DateTime messagesSince = Preference.GetPreference<DateTime>(this, Preference.LAST_MESSAGE_UPDATE );
			account.RequestMissedNotificationsAsync (messagesSince);
		}

		private void HandleLoginAndHasConfigurationNotification (Notification notif) {
			Debug.Assert (SharedPlatform.OnMainThread, "HandleLoginNotification not called on main thread.");
			bool isLoggedIn = this.account.IsLoggedIn;
			if (isLoggedIn) {
				if (this.liveServerConnection.stompClientIsConnected ()) {
					FinishUpAfterSessionStarted ();
				}
			}
		}

		private void OnLiveServerConnectionStatusUpdate (bool success) {
			Debug.Assert (SharedPlatform.OnMainThread, "OnLiveServerConnectionStatusUpdate not called on main thread.");
			if (!success) {
				outgoingQueue.DidDisconnectFromServer ();
				NotificationCenter.DefaultCenter.PostNotification (null, Constants.ApplicationModel_LiveServerConnectionChange, false);
			} else {
				CompletedOnboardingPhase ();
				outgoingQueue.DidConnectToServer ();
				NotificationCenter.DefaultCenter.PostNotification (null, Constants.ApplicationModel_LiveServerConnectionChange, true);
				bool isLoggedIn = this.account.IsLoggedIn;
				if (isLoggedIn) {
					FinishUpAfterSessionStarted ();
				}
			}
		}

		public void OnLiveServerReconnect () {
			Debug.Assert (SharedPlatform.OnMainThread, "OnLiveServerReconnect not called on main thread.");
			if (account.IsLoggedIn && !this.AwaitingGetHistoricalMessagesChoice) {
				RequestMissedMessages ();
			}
		}

		private void FinishUpAfterSessionStarted () {
			Debug.Assert (SharedPlatform.OnMainThread, "FinishUpAfterSessionStarted not called on main thread.");
			// For iOS, a live server connection callback will hit the MainController and there it will handle binding to whoshere and later contact registration as well as sending the list of installed apps.
			// For Android, we don't need to do the bind, so this is a good place to do both the contact registration and sending the list of installed apps.
			bool needsToBindToWhosHere = this.platformFactory.NeedsToBindToWhosHere ();
			if (!needsToBindToWhosHere) {
				EMTask.DispatchMain (() => {
					SendListOfInstalledApps ();
					RegisterContacts ();
				});
			} 

			NotificationCenter.DefaultCenter.PostNotification (Constants.ApplicationModel_LoggedInAndWebsocketConnectedNotification);
		}

		/* Can be called multiple times but we have a gate to run it only as many times as we want. ConfigurationShouldTrackInstalledApps will only return true once daily. */
		public void SendListOfInstalledApps () {
			EMTask.DispatchBackground (() => {
				if (this.account.ConfigurationShouldTrackInstalledApps) {
					EMTask.DispatchMain (() => {
						InstalledAppsOutbound retVal = InstalledAppsChecker.ListOfInstalledApps;
						this.liveServerConnection.SendInstalledAppsAsync (retVal);
					});
				}
			});
		}

		private bool _hasRunContactRegistrationForSession = false;
		public bool HasRunContactRegistrationForSession {
			get { return this._hasRunContactRegistrationForSession; }
			set { this._hasRunContactRegistrationForSession = value; }
		}

		/* Can be called multiple times but we have a gate to run it only as many times as we want. HasRunContactRegistrationForSession will only return true once per session. */
		public void RegisterContacts () {
			Debug.Assert (this.platformFactory.OnMainThread, "Calling RegisterContacts on a thread other than main.");

			// We only want to register contacts once per session.
			if (!this.HasRunContactRegistrationForSession) {
				this.HasRunContactRegistrationForSession = true;
				ContactsManagerAccessContacts ();
			}
		}

		public void RecordGAGoal(string preferenceKey, string category, string action, string label, int value) {
			var alreadyRecordedGoal = Preference.GetPreference<bool> (this, preferenceKey);
			if(!alreadyRecordedGoal) {
				analyticsHelper.SendEvent (category, action, label, value);
				Preference.UpdatePreference<bool> (this, preferenceKey, true);
			}
		}

		#endregion

		public void AppDidChangeState (bool appIsInForeground) {
			Debug.Assert (SharedPlatform.OnMainThread, "AppDidChangeState called, but not on main thread.");
			NotifyIfAppDidChangeState (this.AppInForeground, appIsInForeground);
			this.AppInForeground = appIsInForeground;
		}

		private void NotifyIfAppDidChangeState (bool appInForegroundOldState, bool appInForegroundNewState) {
			if (appInForegroundOldState != appInForegroundNewState) {
				if (appInForegroundNewState) {
					NotificationCenter.DefaultCenter.PostNotification (Constants.ENTERING_FOREGROUND);
				} else {
					NotificationCenter.DefaultCenter.PostNotification (Constants.ENTERING_BACKGROUND);
				}
			}
		}

		public void DoSessionStop () {
			StopLiveServerConnection ();

			//outgoingQueue.WillDisconnectFromServer ();

			account.IsLoggedIn = false;
			attemptingToLogin = false;
		}

		public void DoSessionSuspend () {
			Dictionary<string, object> sessionInfo = GetSessionInfo ();
			if (!(bool)sessionInfo ["isOnboarding"]) {
				TimeStampCache.SharedInstance.ClearCache ();
				DoSessionStop ();

				daoConnection.EnteringBackground ();
			}
		}

		public string[] GetSideMenuList () {
			string[] sideMenuNames = Enum.GetNames (typeof(SideMenuItems));
			int length = sideMenuNames.Length;
			for (int i = 0; i < length; i++)
				sideMenuNames [i] = sideMenuNames [i].ToUpper ();
			return sideMenuNames;
		}

		// great naming
		object oLock;
		public object OLock {
			get {
				if (oLock == null)
					oLock = new object ();
				return oLock;
			}
		}

		public void HandleMessage (JObject message) {
			var postHandlingTasks = new List<Action> ();
			HandleMessage (message, postHandlingTasks);

			foreach (Action task in postHandlingTasks)
				task ();
		}

		public void HandleMessage (JObject message, List<Action> postHandlingTasks) {
			lock (this.OLock) {
				try {
					string messageName = (string)message ["messageName"];
					if (messageName.Equals ("TypingMessage")) {
						TypingMessageInput typingMessage = message.ToObject<TypingMessageInput> ();

						ChatEntry chatEntry = chatList.FindChatEntryByReplyToServerIDs (typingMessage.replyTo, typingMessage.toAlias);
						if (chatEntry != null) {
							Contact sourceContact = Contact.FindContactByServerID (this, typingMessage.source);
							chatEntry.DelegateDidReceiveTypingMessage (sourceContact);
						}
					}
					else if (messageName.Equals ("Message")) {
						ChatMessageInput chatMessage = message.ToObject<ChatMessageInput> ();

						Message existing = Message.FindMessageByMessageGUID (this, chatMessage.messageGUID, chatMessage.toAlias);
						if (existing != null) {
							bool needsToUpdate = false;
							bool updatedToReadStatus = false;
							MessageStatus updatedMessageStatus = MessageStatusHelper.FromString(chatMessage.messageStatus);

							// we likely should update delegates in this case
							bool updated = existing.UpdateMessageStatus (updatedMessageStatus);
							if ( updated ) {
								needsToUpdate = true;
								updatedToReadStatus = updatedMessageStatus == MessageStatus.read;
							}

							MessageChannel updatedMessageChannel = MessageChannelHelper.FromString(chatMessage.messageChannel);
							if ( !existing.messageChannel.Equals(updatedMessageChannel) || !updatedMessageChannel.Equals(MessageChannel.em)) {
								existing.messageChannel = updatedMessageChannel;
								switch ( existing.messageChannel ) {
								default:
								case MessageChannel.email:
								case MessageChannel.sms:
									existing.messageStatus = MessageStatus.read;
									break;

								case MessageChannel.em:
									break;
								}
								needsToUpdate = true;
							}

							if (existing.mediaRef != null && chatMessage.mediaRef != null && !existing.mediaRef.Equals(chatMessage.mediaRef)) {
								existing.mediaRef = chatMessage.mediaRef;
								needsToUpdate = true;
							}

							if ( needsToUpdate )
								existing.Save();

							if ( updatedToReadStatus ) {
								ChatEntry existingEntry = chatList.FindChatEntryByChatEntryID( existing.chatEntryID );
								if ( existingEntry.hasUnread ) {
									existingEntry.hasUnread = false;
									existingEntry.BackgroundSave();

									int index = chatList.entries.IndexOf(existingEntry);
									if ( index != -1 )
										EMTask.DispatchMain (() => chatList.DelegateDidChangePreview (existingEntry, index)); // hasUnread is notified as part of the preview changing.
								}
							}
						}

						if (existing == null) {
							String alias = chatMessage.toAlias;

							// make sure Contacts exist
							var replyTo = new List<Contact> ();
							foreach (ContactInput contactInput in chatMessage.replyTo) {
								Contact contact = Contact.FindOrCreateContact (this, contactInput);
								replyTo.Add (contact);
							}
							Contact from = Contact.FindOrCreateContact (this, chatMessage.from);

							ChatEntry chatEntry = chatList.FindOrCreateChatEntryForReplyTo (replyTo, alias, chatMessage.sentDate);
							Message receivedMessage = Message.FromChatMessageInput (this, chatMessage, from, chatEntry);

							if ( receivedMessage.HasMedia () && postHandlingTasks.Count < 8 )
								// we insert our download task at the beginning of post handle tasks
								// this should give us LIFO behavior.  We only do this for the first 8
								// or so messages we run into.
								postHandlingTasks.Insert(0, () => {
									receivedMessage.media.GUID = receivedMessage.messageGUID;
									receivedMessage.media.DownloadMedia (this);
								});

							chatEntry.BackgroundAddMessage (receivedMessage, false);

							if (chatMessage.inbound && !receivedMessage.HasBeenRead ())
								chatEntry.SendStatusUpdatesAsync (new [] { receivedMessage }, MessageStatus.delivered);

							if ( receivedMessage.IsInbound() && !this.IsHandlingMissedMessages )
								PlayIncomingMessageSoundIfAppropriate();
						}

						// Messages you've sent (even if they are flowing back to you).
						if (!chatMessage.inbound) {
							// This is where we preserve our AKA (or user identifier (null)) for a particular contact.
							IList<ContactInput> replyToContacts = chatMessage.replyTo;
							foreach (ContactInput contactInput in replyToContacts) {
								string alias = chatMessage.toAlias; // Can be null, which indicates this particular contact is being messaged from our main user account and not an alias/AKA.
								Contact.FindOrCreateContactAndUpdatePreferredIdentifierToSendFrom (this, contactInput, alias);
							}
						}
					}
					else if (messageName.Equals ("StatusUpdate")) {
						MessagesUpdateInput updates = message.ToObject<MessagesUpdateInput> ();

						foreach (MessageUpdateInput messageUpdate in updates.messageUpdates) {
							MessageStatus updatedStatus = MessageStatusHelper.FromString(messageUpdate.messageStatus);
							Message msg = Message.FindMessageByMessageGUID (this, messageUpdate.messageGUID, messageUpdate.toAlias);
							if (msg != null && msg.messageStatus != updatedStatus ) {
								if ( !msg.IsInbound() ) {
									// tracking individual delivered/read/ignored status.
									Contact fromUpdater = Contact.FindContactByServerID(this, messageUpdate.updaterServerID);
									if ( fromUpdater != null )
										msgDao.UpdateMessageStatus(msg, fromUpdater, updatedStatus);
								}

								bool updated = msg.UpdateMessageStatus (updatedStatus);
								if ( !updated )
									break;

								msg.Save ();

								if ( msg.IsInbound() && msg.messageStatus == MessageStatus.read ) {
									// if this was a message we sent and the status is changed to
									// read, we mark the chatEntry as being read as well (we are
									// assuming that a message in the conversation getting marked
									// as read marks the entire conversation as read.
									ChatEntry existing = chatList.FindChatEntryByChatEntryID( msg.chatEntryID );
									if ( existing.hasUnread ) {
										existing.hasUnread = false;
										existing.BackgroundSave();

										int index = chatList.entries.IndexOf(existing);
										if ( index != -1 )
											EMTask.DispatchMain (() => chatList.DelegateDidChangePreview (existing, index)); // hasUnread is notified as part of the preview changing.
									}
								}
							}
						}
					}
					else if (messageName.Equals ("LeaveConversation")) {
						LeaveConversationInput leaveConversationMessage = message.ToObject<LeaveConversationInput> ();

						// make sure Contacts exist
						var replyTo = new List<string> ();
						foreach (ContactInput contactInput in leaveConversationMessage.replyTo) {
							Contact contact = Contact.FindOrCreateContact (this, contactInput);
							replyTo.Add (contact.serverID);
						}

						// should from be null if it's from us?
						Contact from = Contact.FindContactByServerID(this, leaveConversationMessage.from.serverID);
						AliasInfo fromAlias = account.accountInfo.AliasFromServerID(leaveConversationMessage.from.serverID);
						String alias = leaveConversationMessage.toAlias;
						ChatEntry chatEntry = chatList.FindChatEntryByReplyToServerIDs( replyTo, alias );
						if ( chatEntry != null && chatEntry.contacts.Count > 1 ) {
							bool fromUs = false;
							if ( from != null && from.me ) {
								// if no aliases involved, its from us
								if ( fromAlias == null && chatEntry.fromAlias == null )
									fromUs = true;
								// else if it is an alias and its from the same alias as this
								// chat conversation then its from us
								else if ( fromAlias != null && chatEntry.fromAlias != null && fromAlias.serverID.Equals(chatEntry.fromAlias))
									fromUs = true;
								
							}

							if ( !fromUs )
								chatEntry.ContactLeftChatEntry(from);
							else if ( !chatEntry.leftAdhoc ) {
								// otherwise this is from one of our devices
								TimeSpan diff = leaveConversationMessage.createDate.Subtract( chatEntry.createDate);
								Debug.WriteLine("Found existing ChatEntry to leave with same contacts.  Time differential is " + diff);
								if ( Math.Abs(((int) diff.TotalSeconds)) < Constants.NUM_SECONDS_RANGE_TO_COMPARE_CREATEDATES ) {
									chatEntry.leftAdhoc = true;
									chatEntry.SaveAsync();
								}
							}
						}
					}
					else if (messageName.Equals ("GroupMembers")) {
						var contactInput = message.ToObject<ContactInput>(); //can cast this to GroupInput if needed

						Contact existing = Contact.FindContactByServerID (this, contactInput.serverID);
						if (existing == null) {
							if (ContactLifecycleHelper.EMCanSendTo(contactInput.lifecycle) && GroupMemberLifecycleHelper.EMCanSendTo(contactInput.groupMemberLifecycle)) {
								Contact fromServer = Contact.FromContactInput (this, contactInput);
								fromServer.tempContact.Value = false;
								fromServer.Save ();

								// Notify via delegates group added
								EMTask.DispatchMain (() => Contact.DelegateDidAddGroup (fromServer));
							}
						} else if (Contact.UpdateFromContactInput (existing, contactInput, false, true)) {
							existing.Save();

							if (ContactLifecycleHelper.EMCanSendTo(contactInput.lifecycle) && GroupMemberLifecycleHelper.EMCanSendTo(contactInput.groupMemberLifecycle))
								EMTask.DispatchMain (() => Contact.DelegateDidUpdateGroup (existing)); // Notify via delegates group updated
							else
								EMTask.DispatchMain (() => Contact.DelegateDidDeleteGroup (existing)); // Notify via delegates group deleted
						}
					}
					else if (messageName.Equals ("ChannelUpdate")) {
						ChannelUpdateInput update = message.ToObject<ChannelUpdateInput> ();

						MessageChannel updatedMessageChannel = MessageChannelHelper.FromString (update.messageChannel);
						Message msg = Message.FindMessageByMessageGUID (this, update.messageGUID, update.toAlias);
						if (msg != null && msg.messageChannel != updatedMessageChannel ) {
							msg.messageChannel = updatedMessageChannel;
							// we mark messages as read that use alternate channels as we
							// know we won't get any updates from them
							switch ( msg.messageChannel ) {
							default:
							case MessageChannel.email:
							case MessageChannel.sms:
								msg.messageStatus = MessageStatus.read;
								break;

							case MessageChannel.em:
								break;
							}

							msg.Save ();
						}
					}
					else if (messageName.Equals ("MessageModification")) {
						MessageModificationInput update = message.ToObject<MessageModificationInput> ();
						Message msg = Message.FindMessageByMessageGUID (this, update.messageGUID, update.toAlias);

						// TODO Server is sending back a rolled up list that includes both the original message and the message modification.
						// Once we change the server to send back a properly rolled up list of message updates, we can modify this code to behave more similarly to
						// the messageName='Message' case. 
						// As of right now the null check is just to prevent throwing an NPE when the MessageModification is processed before the Message.
						if (msg == null) return;

						msg.chatEntry.HandleMessageModification(msg, update);
					}
					else if ( messageName.Equals("RemoveChatEntry")) {
						RemoveChatEntryInput removeChatEntry = message.ToObject<RemoveChatEntryInput>();

						ChatEntry findExisting = chatList.FindChatEntryByReplyToServerIDs( removeChatEntry.replyTo, removeChatEntry.toAlias );
						if ( findExisting != null ) {
							TimeSpan diff = removeChatEntry.createDate.Subtract( findExisting.createDate);
							Debug.WriteLine("Found existing ChatEntry to delete with same contacts.  Time differential is " + diff);
							if ( Math.Abs(((int) diff.TotalSeconds)) < Constants.NUM_SECONDS_RANGE_TO_COMPARE_CREATEDATES ) {
								int indexOf = chatList.entries.IndexOf(findExisting);
								chatList.RemoveChatEntry(indexOf, false);
							}
						}
					}
					else if (messageName.Equals ("NotificationUpdate")) {
						NotificationStatusInput update = message.ToObject<NotificationStatusInput> ();

						if (update != null) {
							NotificationStatus updatedStatus = NotificationStatusHelper.FromString (update.status);
							NotificationEntry existing = NotificationEntry.FindNotificationByServerID (this, update.serverID);
							if (existing != null) {
								if (updatedStatus.Equals (NotificationStatus.Deleted)) {
									existing.Delete ();
								} else if (updatedStatus.Equals (NotificationStatus.Read)) {
									existing.Read = true;
									existing.Save ();
								}
							}
						}
					}
					else if (messageName.Equals ("NotificationMessage")) {
						NotificationInput notification = message.ToObject<NotificationInput> (); 

						if (notification != null) {
							NotificationEntry existing = NotificationEntry.FindNotificationByServerID (this, notification.serverID);
							if (existing == null) {
								NotificationEntry ne = NotificationEntry.FromNotificationInput (this, notification);
								ne.Save ();
							} else {
								if (notification.deleted) {
									existing.Delete ();
								} else if (notification.read) {
									existing.Read = true;
									existing.Save ();
								}
							}

							liveServerConnection.SendNotificationStatusUpdateAsync (notification.serverID, NotificationStatus.Delivered);
						}
					}
					else if ( messageName.Equals("ContactListModified")) {
						ContactListModifiedInput contactListModifiedInput = message.ToObject<ContactListModifiedInput> ();
						Contact.ProcessModifiedContactList(this, contactListModifiedInput);
					}
					else if (messageName.Equals ("AccountInfo")) {
						account.PossibleAccountInfoUpdate (message);
					} 
					else if (messageName.Equals("CounterpartyLifecycleUpdate")) {
						CountepartyLifecycleUpdateInput update = message.ToObject<CountepartyLifecycleUpdateInput>();
						// check both contacts and aliases
						Contact c = Contact.FindContactByServerID(this, update.serverID);
						if ( c != null ) {
							c.lifecycleString = update.updatedLifecycle;
							c.Save();
						}

						AliasInfo alias = account.accountInfo.AliasFromServerID(update.serverID);
						if ( alias != null ) {
							alias.lifecycleString = update.updatedLifecycle;
							account.accountInfo.SaveAccountInfoOffline();
						}
					}
					else if ( messageName.Equals("UserPrompt")) {
						UserPromptInput userPrompt = message.ToObject<UserPromptInput>();
						DidReceiveServerPrompt(userPrompt);
					}
					else if ( messageName.Equals("ActionRequest")) {
						RemoteActionInput remoteAction = message.ToObject<RemoteActionInput>();
						if ( remoteAction.AppAction == AppAction.unknown )
							Debug.WriteLine("Received action request for unknown action " + remoteAction.action);
						else
							DidReceiveRemoteAction(remoteAction);
					}
					else {
						// what message was it?
						Debug.WriteLine ("Received unknown STOMP message: " + messageName);
					}
				} catch (Exception e) {
					Debug.WriteLine (string.Format ("Failed to Process live server connection message: {0}\n{1}", e.Message, e.StackTrace));
				}
			}
		}

		Timer recentlyPlayedTimer;
		object recentlyPlayedTimerLock = new object ();
		protected void PlayIncomingMessageSoundIfAppropriate() {
			EMTask.DispatchBackground (() => {
				lock (recentlyPlayedTimerLock) {
					if (recentlyPlayedTimer == null) {
						PlayIncomingMessageSound();
						recentlyPlayedTimer = new Timer ((object o) => {
							lock( recentlyPlayedTimerLock ) {
								recentlyPlayedTimer = null;
							}
						}, null, Constants.TIMER_INTERVAL_BETWEEN_PLAYING_SOUNDS, Timeout.Infinite);
					}
				}
			});
		}

		protected void PlayIncomingMessageSound() {
			if (this.account.UserSettings.IncomingSoundEnabled) {
				EMTask.DispatchMain (platformFactory.PlayIncomingMessageSound);
			}
		}

		public ChatEntry FindChatEntryThatMatchesGUID (string guid64) {
			ChatEntry target = null;
			IList<ChatEntry> entries = this.chatList.entries;
			for (int i = 0; i < entries.Count; i++) {
				ChatEntry entry = entries [i];
				IList<em.Message> messages = this.msgDao.FindUnreadMessagesForChatEntry (entry);
				foreach (em.Message message in messages) {
					byte[] firstFourBytes = new byte[4];
					using (Stream stream = GenerateStreamFromString (message.messageGUID)) {
						byte[] hash = ApplicationModel.SharedPlatform.GetSecurityManager ().MD5StreamToBytes (stream);
						for (int j = 0; j < 4; j++) {
							firstFourBytes [j] = hash [j];
						}
					}
					string guidInBase64 = Convert.ToBase64String (firstFourBytes);
					if (guid64.Equals (guidInBase64)) {
						target = entry;
						break;
					}
				}
			}
			return target;
		}

		// TODO: Find good place to put this function
		private Stream GenerateStreamFromString(string s) {
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}
	}
}