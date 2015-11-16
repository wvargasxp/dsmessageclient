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
	public class LoginTests : EMClientTests
	{
		public TestUser init(int userIndex) 
		{
			TestUser user = TestUserDB.GetUserAtIndex (userIndex);
			if (user.testManager.RegisterSync ())
				return user;
			return null;
		}

		[Test ()]
		public void ALogin ()
		{
			TestUser user = init (1);
			if (user != null) {
				bool isLoggedIn = user.testManager.LoginSync ("11111");
				Assert.IsTrue (isLoggedIn);
			} else
				Assert.Fail ("Failed to register user");
		}

		[Test ()]
		public void ALoginFail ()
		{
			TestUser user = init (1);
			if (user != null) {
				bool isLoggedIn = user.testManager.LoginSync ("hgkhgh");
				Assert.IsFalse (isLoggedIn);
			} else
				Assert.Fail ("Failed to register user");
		}
	}
}