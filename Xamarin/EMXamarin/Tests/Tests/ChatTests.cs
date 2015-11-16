using System;
using NUnit.Framework;

using EMXamarin;
using WebSocket4Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using SuperSocket.ClientEngine;
using System.Threading.Tasks;
using System.Net.Http;
using System.ComponentModel;
using System.Threading;
using em;

namespace Tests
{
	[TestFixture]
	public class ChatTests
	{


		private ApplicationModel appModel;
		private TestsManager testsManager;
		private string user1_accountID = "+1 3013561295";

		public ChatTests ()
		{
		}

		private Message createTextMessageForChatEntry (string messageText, ChatEntry chatEntry)
		{
			Message message = Message.NewMessage ();
			message.chatEntry = chatEntry;
			message.chatEntryID = chatEntry.chatEntryID;
			message.inbound = "N";
			message.messageStatus = MessageStatus.pending;
			message.message = messageText;
			message.sentDate = DateTime.Now;
			return message;
		}

		private ChatEntry chatEntryWithAddressBookContact (string addressBookID)
		{
			ChatEntry chatEntry = ChatEntry.NewChatEntry (DateTime.Now);
			Contact contact = Contact.FindContactByAddressBookID (addressBookID);
			chatEntry.AddContact (contact);
			chatEntry.Save ();
			return chatEntry;
		}

		public bool init ()
		{
			appModel = new ApplicationModel (new TestPlatformFactory (5));
			testsManager = new TestsManager (appModel);
			if (testsManager.LoginSync (user1_accountID, "11111")) {
				appModel.appInForeground = true;
				return testsManager.RegisterContactsFromAddressBookSync ();
			}
			return false;
		}


		/*
		 * TODO: 
		 * write sending message test from user
		 * write recieving message test
		 * brainstorm the functionality you would want to create for two users talking to each other
		 * media message
		 * //sign in with the live server connection
		 */

		[Test]
		public void testTwoSpeaking ()
		{
			bool didSend = false;
			if (init ()) {
				testsManager.StartLiveServerConnection (user1_accountID, "11111");
				ChatEntry chatEntry = chatEntryWithAddressBookContact ("3");
				Message message = createTextMessageForChatEntry ("hElLo", chatEntry);
				testsManager.SendMessageSync (chatEntry, message, true);
				didSend = EMSuccessStatus.MessageSendStatus;
				if (didSend) {
					if (testsManager.LoginSync ("anhlantruong@aol.com", "11111") && testsManager.RegisterContactsFromAddressBookSync ()) {
						testsManager.StartLiveServerConnection ("anhlantruong@aol.com", "11111");
						chatEntry = chatEntryWithAddressBookContact ("2");
						message = createTextMessageForChatEntry ("wOrLd?", chatEntry);
						testsManager.SendMessageSync (chatEntry, message, true);
						didSend = EMSuccessStatus.MessageSendStatus;
					}
				}

			}
			Assert.True (didSend);
			
		}


		[Test]
		public void sendMessage ()
		{
			bool didSend = false;
			if (init ()) {
				testsManager.StartLiveServerConnection (user1_accountID, "11111");
				ChatEntry chatEntry = chatEntryWithAddressBookContact ("3");
				Message message = createTextMessageForChatEntry ("stepping through debuggersecong ", chatEntry);
				testsManager.SendMessageSync (chatEntry, message, true);
				didSend = EMSuccessStatus.MessageSendStatus;
			}
			testsManager.EndLiveServerConnection ();
			Assert.True (didSend);
		}

		//	[TearDown]
		public void disconnect ()
		{
			testsManager.EndLiveServerConnection ();
		}
		 

	}

	public class TestQueue : OutgoingQueue
	{

		public TestQueue () : base ()
		{
		}

		override public void RemoveQueueEntry (QueueEntry queueEntry, bool successOfSend)
		{
			base.RemoveQueueEntry (queueEntry, successOfSend);
			EMSuccessStatus.MessageSendStatus = successOfSend;
			Debug.WriteLine (successOfSend);
		}
	}

	public class EMSuccessStatus
	{
		//private static object _lock;
		private static EventWaitHandle waitHandle = new AutoResetEvent (false);
		private static bool _messageSendStatus;
		private static bool messageHasBeenSet = false;

		public static bool MessageSendStatus 
		{
			get { 
				if (!messageHasBeenSet) {
					waitHandle.WaitOne ();
				}
				messageHasBeenSet = false;
				//else change the messageHasBeenSet flag to false
				return _messageSendStatus;
			}

			set {
				_messageSendStatus = value;
				messageHasBeenSet = true;
				waitHandle.Set ();
			}
		}
	}
}

