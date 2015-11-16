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
using em;

namespace TestsHeadless
{
	[TestFixture ()]
	public class ContactsTests : EMClientTests
	{
		public TestUser init(int userIndex) 
		{
			TestUser user = TestUserDB.GetUserAtIndex (userIndex);
			if (user.testManager.RegisterSync () && user.testManager.LoginSync ("11111"))
				return user;
			return null;
		}

		[Test ()]
		public void registerContactsFromAddressBook () 
		{
			ContactsUpdatedStatus registrationSuccessStatus = ContactsUpdatedStatus.FailedToProccess;
			TestUser user = init (4);
			if (user != null) 
				registrationSuccessStatus = user.testManager.RegisterContactsFromAddressBookSync ();
			else 
				Assert.Fail ("Failed to successfully register or log in users");
			Assert.AreEqual (ContactsUpdatedStatus.RegisteredNewContacts, registrationSuccessStatus);
		}

		[Test ()]
		public void reregisterContactsNoChanges () 
		{
			ContactsUpdatedStatus reregistrationSuccessStatus = ContactsUpdatedStatus.FailedToProccess;
			TestUser user = init (1);
			if (user != null) {
				if (user.testManager.RegisterContactsFromAddressBookSync () == ContactsUpdatedStatus.RegisteredNewContacts)
					reregistrationSuccessStatus = user.testManager.RegisterContactsFromAddressBookSync ();
			} else 
				Assert.Fail ("Failed to successfully register or log in users");
			Assert.AreEqual (ContactsUpdatedStatus.RegisteredNoChanges, reregistrationSuccessStatus);
		}

		[Test ()]
		public void searchForContactPass () 
		{
			TestUser user = init (4);
			TestUser newContact = init (1);
			if (user != null) {
				ContactInput responseInput = user.testManager.SearchForContactSync (newContact.identifier);
				Assert.IsNotNull (responseInput);
			} else 
				Assert.Fail ("Failed to successfully register or log in users");
		}

		[Test ()]
		public void searchForContactFail ()
		{
			TestUser user = init (4);
			if (user != null) {
				ContactInput responseInput = user.testManager.SearchForContactSync ("adsafadd");
				Assert.IsNull (responseInput);
			} else 
				Assert.Fail ("Failed to successfully register or log in users");
		}
	}
}