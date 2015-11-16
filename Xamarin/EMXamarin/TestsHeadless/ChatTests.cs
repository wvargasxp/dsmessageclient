using NUnit.Framework;
using System;
using EMXamarin;
using em;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace TestsHeadless
{
	[TestFixture ()]
	public class ChatTests : EMClientTests
	{
		private Message createTextMessageForChatEntry (TestUser user, string messageText, ChatEntry chatEntry)
		{
			Message message = Message.NewMessage (user.appModel);
			message.chatEntry = chatEntry;
			message.chatEntryID = chatEntry.chatEntryID;
			message.inbound = "N";
			message.messageStatus = MessageStatus.pending;
			message.message = messageText;
			message.sentDate = DateTime.Now;
			return message;
		}

		private Message createMediaMessageForChatEntry (TestUser user, ChatEntry chatEntry)
		{
			byte[] mediaByteArr = new byte[2];
			new Random ().NextBytes (mediaByteArr);
			File.WriteAllBytes ("DummyTestFile.jpg", mediaByteArr);
			string finalPath = user.appModel.platformFactory.MoveStagingContentsToFileStore ("DummyTestFile.jpg", chatEntry);
			Message message = Message.NewMessage (user.appModel);
			message.chatEntry = chatEntry;
			message.chatEntryID = chatEntry.chatEntryID;
			message.inbound = "N";
			message.messageStatus = MessageStatus.pending;
			message.message = "<media>";
			message.sentDate = DateTime.Now;
			message.mediaRef = "file://" + Uri.EscapeUriString (finalPath);
			message.heightToWidth = 1f;
			message.contentType = "image/jpeg";
			return message;
		}

		private ChatEntry chatEntryWithAddressBookContact (TestUser user, string addressBookID)
		{
			ChatEntry chatEntry = ChatEntry.NewChatEntry (user.appModel, DateTime.Now);
			Contact contact = Contact.FindContactByAddressBookIDAndDescription (user.appModel, addressBookID,""); // using "" will likely break the tests
			chatEntry.AddContact (contact);
			chatEntry.fromAlias = chatEntry.contacts [0].fromAlias;
			chatEntry.Save ();
			return chatEntry;
		}

		private TestUser init(int userIndex) 
		{
			TestUser user = TestUserDB.GetUserAtIndex (userIndex);
			if (user.testManager.RegisterPhoneSync () && user.testManager.LoginSync ("11111") && 
				user.testManager.RegisterContactsFromAddressBookSync () == ContactsUpdatedStatus.RegisteredNewContacts)
				return user;
			return null;
		}

		[Test ()]
		public void SendMessage ()
		{
			bool didReceive = false;
			TestUser sender = init (4);
			TestUser receiver = init (5);
			if (sender != null && receiver != null) {
				sender.testManager.StartLiveServerConnection ("11111");
				receiver.testManager.StartLiveServerConnection ("11111");
				ChatEntry chatEntry = chatEntryWithAddressBookContact (sender, "2");
				Message message = createTextMessageForChatEntry (sender, "Mooo", chatEntry);
				sender.testManager.SendMessageSync (chatEntry, message, true);
				StompClientMessageListener stompListener = StompClientMessageListener.ListenersPerAppModel [receiver.appModel.cacheIndex];
				didReceive = stompListener.WaitForSignal ("Message", message.messageGUID);
			} else
				Assert.Fail ("Failed to successfully register, log in or load contacts for users");

			Assert.IsTrue (didReceive);
		}



		[Test ()]
		public void SendMediaMessage ()
		{
			bool didReceive = false;
			TestUser sender = init (4);
			TestUser receiver = init (5);
			if (sender != null && receiver != null) {
				sender.testManager.StartLiveServerConnection ("11111");
				receiver.testManager.StartLiveServerConnection ("11111");
				ChatEntry chatEntry = chatEntryWithAddressBookContact (sender, "2");
				Message message = createMediaMessageForChatEntry (sender, chatEntry);
				sender.testManager.SendMessageSync (chatEntry, message, true);
				StompClientMessageListener stompListener = StompClientMessageListener.ListenersPerAppModel [receiver.appModel.cacheIndex];
				didReceive = stompListener.WaitForSignal ("Message", message.messageGUID);
			} else
				Assert.Fail ("Failed to successfully register, log in or load contacts for users");
			Assert.IsTrue (didReceive);
		}
	}
}