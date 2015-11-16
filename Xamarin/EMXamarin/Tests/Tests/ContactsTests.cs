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

namespace Tests
{
	[TestFixture]
	public class ContactsTest
	{			
		private TestUser user;
		public bool init(int userIndex) 
		{
			user = TestUserDB.GetUserAtIndex (userIndex);
			return user.testManager.LoginSync(user.identifier, "11111");
		}

		[Test]
		public void registerContactsFromAddressBook () 
		{
			bool didRegisterAll = false;
			if (init (4)) 
				didRegisterAll = user.testManager.RegisterContactsFromAddressBookSync ();
			Assert.True (didRegisterAll);
		}


	}
}

