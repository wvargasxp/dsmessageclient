using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EMXamarin;
using em;
using System.Diagnostics;

namespace TestsHeadless {
	public class TestsManager {
		public static object TestLock = new object ();
		static readonly int WAIT_HANDLE_TIMEOUT = 30000;
		TestUser user;
		ApplicationModel appModel;
		Timer reconnectTimer;
		public ContactsManager contactsManager;

		public TestsManager (ApplicationModel applicationModel, TestUser testUser)
		{
			appModel = applicationModel;
			user = testUser;
			StompClientMessageListener.ListenersPerAppModel.Insert (appModel.cacheIndex, new StompClientMessageListener ());
			contactsManager = new ContactsManager (appModel, appModel.platformFactory.getAddressBook ());
		}

		//live server connection
		public void StartLiveServerConnection (string password) 
		{
			appModel.liveServerConnection = new TestLiveServerConnection (user.identifier, password, appModel);
			appModel.liveServerConnection.DelegateLiveServerHasConnection = OnLiveServerConnectionStatusUpdate;
			if (appModel.liveServerConnection.ShouldAttemptConnect ())
				appModel.liveServerConnection.Connect ();
		}

		public void EndLiveServerConnection () 
		{
			appModel.doSessionSuspend();
		}

		void OnLiveServerConnectionStatusUpdate (bool success) 
		{
			// Creating a timer (to reconnect) if live server connection failed to create a connection.
			if (!success) {
				Debug.WriteLine ("Starting a timer for {0} seconds before attempting reconnect.", Constants.TIMER_INTERVAL_BETWEEN_RECONNECTS / 1000);
				appModel.account.isLoggedIn = false;
				if ( reconnectTimer != null )
					reconnectTimer.Dispose ();
				reconnectTimer = new Timer (o => {
					Dictionary<string, object> sessionInfo = appModel.GetSessionInfo ();
					if (sessionInfo.ContainsKey ("accountID") && sessionInfo.ContainsKey ("password")) {
						appModel.account.LoginWithStoredAccountIdentifierAndVerificationCodeOnCompletion (loggedIn => {
							if (loggedIn)
								StartLiveServerConnection (sessionInfo ["password"] as string);
							else
								OnLiveServerConnectionStatusUpdate (false);
						});
					}
				}, null, Constants.TIMER_INTERVAL_BETWEEN_RECONNECTS, Timeout.Infinite);
			} else {
				appModel.outgoingQueue.DidEstablishServerConnection ();
			}
		}

		public bool RegisterSync() 
		{
			switch (user.identifierType) {
			case SignInTypeModel.SignInTypes.Email:
				return RegisterEmailSync ();
			case SignInTypeModel.SignInTypes.Mobile:
				return RegisterPhoneSync ();
			default:
				return false;
			}
		}

		public bool RegisterPhoneSync() 
		{
			EventWaitHandle registerHandle = new ManualResetEvent (false);
			bool didRegister = false;
			char[] delimiter = {' '};
			string[] identifierArr = user.identifier.Split (delimiter, 2);
			string countryCode = identifierArr [0];
			string phoneNumber = identifierArr.Length == 2 ? identifierArr [1] : "";
			appModel.account.RegisterMobileNumber(phoneNumber, countryCode, ((isSuccessful, accountID) => {
				didRegister = isSuccessful;
				registerHandle.Set ();
			}));
			registerHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
			return didRegister;
		}

		public bool RegisterEmailSync()
		{
			string emailAddress = user.identifier;
			EventWaitHandle registerHandle = new ManualResetEvent (false);
			bool didRegister = false;
			appModel.account.RegisterEmailAddress(emailAddress, (isSuccessful, accountID) => {
				didRegister = isSuccessful;
				registerHandle.Set ();
			});
			registerHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
			return didRegister;
		}
			
		public bool LoginSync (string password) 
		{
			string accountID = user.identifier;
			EventWaitHandle loginWaitHandle = new ManualResetEvent (false);
			bool didLogin = false;
			EMTask.Dispatch (() => {
				appModel.account.LoginWithAccountIdentifier (accountID, password, (success, existing) => {
					didLogin = success;
					loginWaitHandle.Set ();
				});
			});
			loginWaitHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
			if (didLogin) {
				appModel.platformFactory.storeSecureField ("accountID", accountID);
				appModel.platformFactory.storeSecureField ("password", password);
			}
			return didLogin;
		}
			
		public ContactsUpdatedStatus RegisterContactsFromAddressBookSync()
		{
			EventWaitHandle contactsWaitHandle = new AutoResetEvent (false);
			ContactsUpdatedStatus registrationStatus = ContactsUpdatedStatus.FailedToProccess;
			contactsManager.AccessContactsWithPermission(successStatus => {
				registrationStatus = successStatus;
				if (registrationStatus == ContactsUpdatedStatus.RegisteredNewContacts || registrationStatus == ContactsUpdatedStatus.RegisteredNoChanges)
					appModel.platformFactory.getAddressBook ().ListOfContacts ((accessGranted, abContacts) => {
						bool oneNotRegistered = false;
						foreach (AddressBookContact abContact in abContacts) {
							Contact contact = Contact.FindContactByAddressBookIDAndDescription (appModel, abContact.clientID,""); // empty string could possibly break this test
							if (contact == null) {
								oneNotRegistered = true;
								break;
							}
						}
						if (oneNotRegistered)
							registrationStatus = ContactsUpdatedStatus.FailedToProccess;
						contactsWaitHandle.Set ();
					});
				else
					contactsWaitHandle.Set ();
			});

			contactsWaitHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
			return registrationStatus;
		}

		public void SaveGroupSync (GroupUpdateOutbound groupInfo, Action<Contact> completionHandler)
		{
			EventWaitHandle waitHandle = new AutoResetEvent (false);
			appModel.account.SaveGroup (groupInfo, null, (group) => {
				completionHandler (group);
				waitHandle.Set ();
			});
			waitHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
		}

		public ContactInput SearchForContactSync(string searchString)
		{
			EventWaitHandle searchWaitHandle = new AutoResetEvent (false);
			ContactInput contactInput = null;
			appModel.account.SearchForContact (searchString, response => {
				contactInput = new ContactInput(); // FIXME not sure what to do here contact;
				searchWaitHandle.Set ();
			});
			searchWaitHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
			return contactInput;
		}

		public void FindAllGroupsSync (Action<IList<Group>> completionHandler) {
			EventWaitHandle waitHandle = new AutoResetEvent (false);
			Contact.FindAllGroupsAsync (appModel, groupsList => {
				completionHandler (groupsList);
				waitHandle.Set ();
			});
			waitHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
		}

		public void LookupGroupSync(string groupID, Action<GroupInput> completionHander) {
			EventWaitHandle waitHandle = new AutoResetEvent (false);
			appModel.account.LookupGroup (groupID, grp => {
				completionHander (grp);
				waitHandle.Set ();
			});
			waitHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
		}

		public void SendMessageSync(ChatEntry chatEntry, Message message, bool sendToServer)
		{
			EventWaitHandle waitHandle = new AutoResetEvent (false);
			EMTask.Dispatch (() => {
				chatEntry.AddMessage (message, sendToServer);
				waitHandle.Set ();
			});
			waitHandle.WaitOne (WAIT_HANDLE_TIMEOUT);
		}
	}
}