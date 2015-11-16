using NUnit.Framework;
using System;
using EMXamarin;
using em;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace TestsHeadless
{
	[TestFixture ()]
	public class NotificationTests : EMClientTests
	{
		[Test ()]
		public void SendNotification() {
			TestUser user = TestUserDB.GetUserAtIndex (4);
			user.testManager.RegisterSync ();
			bool loginSuccess = user.testManager.LoginSync ("11111");
			if (loginSuccess) {
				user.testManager.StartLiveServerConnection ("11111");
				StompClientMessageListener listener = StompClientMessageListener.ListenersPerAppModel [user.appModel.cacheIndex];
				bool notificationWasSent = listener.WaitForSignal ("NotificationMessage", user.deviceName);
				Assert.IsTrue (notificationWasSent);
			} else
				Assert.Fail ("Failed to log in user");
		}

		[Test ()]
		public void MarkNotificationAsRead() {
			TestUser user = TestUserDB.GetUserAtIndex (5);
			user.testManager.RegisterSync ();
			bool loginSuccess = user.testManager.LoginSync ("11111");
			if (loginSuccess) {
				EventWaitHandle waitHandle = new AutoResetEvent (false);
				user.testManager.StartLiveServerConnection ("11111");
				waitHandle.WaitOne (30000);
				IList<NotificationEntry> allEntries = user.appModel.notificationList.Entries;
				if (allEntries.Count == 0)
					Assert.Fail ("Error: No notifications found in notificationList.Entries but 1 expected");
				NotificationEntry mostRecentEntry = allEntries [allEntries.Count - 1];
				user.appModel.notificationList.MarkNotificationEntryReadAsync (mostRecentEntry);
				StompClientMessageListener listener = StompClientMessageListener.ListenersPerAppModel [user.appModel.cacheIndex];
				bool updateWasProccesed = listener.WaitForSignal ("NotificationUpdate", mostRecentEntry.NotificationEntryID.ToString () + ":R");
				Assert.IsTrue (updateWasProccesed);
			} else
				Assert.Fail ("Failed to register or log in user");
		}

		[Test ()]
		public void DeleteNotification() {
			TestUser user = TestUserDB.GetUserAtIndex (1);
			user.testManager.RegisterSync ();
			bool loginSuccess = user.testManager.LoginSync ("11111");
			if (loginSuccess) {
				EventWaitHandle waitHandle = new AutoResetEvent (false);
				user.testManager.StartLiveServerConnection ("11111");
				waitHandle.WaitOne (30000);
				IList<NotificationEntry> allEntries = user.appModel.notificationList.Entries;
				if (allEntries.Count == 0)
					Assert.Fail ("Error: No notifications found in notificationList.Entries but 1 expected");
				NotificationEntry mostRecentEntry = allEntries [allEntries.Count - 1];
				user.appModel.notificationList.RemoveNotificationEntryAtAsync (mostRecentEntry);
				StompClientMessageListener listener = StompClientMessageListener.ListenersPerAppModel [user.appModel.cacheIndex];
				bool updateWasProccesed = listener.WaitForSignal ("NotificationUpdate", mostRecentEntry.NotificationEntryID.ToString () + ":D");
				Assert.IsTrue (updateWasProccesed);
			} else
				Assert.Fail ("Failed to register or log in user");
		}
	}
}