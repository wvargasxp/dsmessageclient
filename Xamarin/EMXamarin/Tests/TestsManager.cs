using System;
using em;
using EMXamarin;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;


namespace Tests
{
	public class TestsManager
	{
		private ApplicationModel appModel;
		Timer reconnectTimer;
		public ContactsManager contactsManager;

		public TestsManager (ApplicationModel applicationModel)
		{
			appModel = applicationModel;

			appModel.outgoingQueue = new TestQueue ();
			appModel.outgoingQueue.appModel = appModel; 

			contactsManager = new ContactsManager (appModel, appModel.platformFactory.getAddressBook (), appModel.contactDao);
		}

		//live server connection
		public void StartLiveServerConnection (string accountID, string password) 
		{
			appModel.liveServerConnection = new LiveServerConnection (accountID, password, appModel);
			appModel.liveServerConnection.DelegateLiveServerHasConnection = (bool liveServerHasConnection) => {
				OnLiveServerConnectionStatusUpdate (liveServerHasConnection);
			};
			if (appModel.appInForeground && appModel.liveServerConnection.ShouldAttemptConnect ()) {
				appModel.liveServerConnection.Connect ();
			}
		}

		public void EndLiveServerConnection () 
		{
			appModel.doSessionSuspend();
		}

		private void OnLiveServerConnectionStatusUpdate (bool success) 
		{
			if (!appModel.appInForeground)
				return;

			// Creating a timer (to reconnect) if live server connection failed to create a connection.
			if (!success) {
				Debug.WriteLine ("Starting a timer for {0} seconds before attempting reconnect.", Constants.TIMER_INTERVAL_BETWEEN_RECONNECTS / 1000);
				appModel.account.isLoggedIn = false;
				if ( reconnectTimer != null )
					reconnectTimer.Dispose ();
				reconnectTimer = new Timer ((object o) => {
					Dictionary<string, object> sessionInfo = appModel.GetSessionInfo ();
					if (sessionInfo.ContainsKey ("accountID") && sessionInfo.ContainsKey ("password")) {
						appModel.account.LoginWithStoredAccountIdentifierAndVerificationCodeOnCompletion ((bool loggedIn) => {
							if (loggedIn)
								StartLiveServerConnection (sessionInfo ["accountID"] as string, sessionInfo ["password"] as string);
							else
								OnLiveServerConnectionStatusUpdate (false);
						});
					}
				}, null, Constants.TIMER_INTERVAL_BETWEEN_RECONNECTS, Timeout.Infinite);
			} else {

				appModel.outgoingQueue.DidEstablishServerConnection ();
			}
		}

		public bool RegisterSync;


		public bool LoginSync (string accountID, string password) 
		{
			EventWaitHandle loginWaitHandle = new ManualResetEvent (false);
			bool didLogin = false;
			Task.Factory.StartNew (delegate {
				appModel.account.LoginWithAccountIdentifier (accountID, password, ((bool success) => {
					didLogin = success;
					loginWaitHandle.Set ();
				}));
			});
			loginWaitHandle.WaitOne ();
			if (didLogin) {
				appModel.platformFactory.storeSecureField ("accountID", accountID);
				appModel.platformFactory.storeSecureField ("password", password);
			}
			return didLogin;
		}


		public bool RegisterContactsFromAddressBookSync()
		{
			EventWaitHandle contactsWaitHandle = new AutoResetEvent (false);
			bool didRegisterAll = false;
			contactsManager.AccessContactsWithPermission(()=>{
				appModel.platformFactory.getAddressBook().ListOfContacts((bool accessGranted, List<AddressBookContact> abContacts)=> {
					bool oneNotRegistered = false;
					foreach (AddressBookContact abContact in abContacts) {
						Contact contact = Contact.FindContactByAddressBookID(abContact.clientID);
						if (contact == null) 
							oneNotRegistered = true;
					}
					didRegisterAll = (oneNotRegistered == false) ? true : false;
					contactsWaitHandle.Set();
				});
			});

			contactsWaitHandle.WaitOne ();
			return didRegisterAll;
		}
	
		public void SendMessageSync(ChatEntry chatEntry, Message message, bool sendToServer)
		{
			EventWaitHandle waitHandle = new AutoResetEvent (false);
			Task.Factory.StartNew (() => {
				chatEntry.AddMessage (message, sendToServer);
				waitHandle.Set();
			});
			waitHandle.WaitOne();
		}

	}
}

