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
using EMXamarin;

namespace Tests
{
	[TestFixture]
	public class LoginTests
	{

		[Test]
		public void ALogin ()
		{
			TestUser user = TestUserDB.GetUserAtIndex (1);
			bool isLoggedIn = user.testManager.LoginSync (user.identifier, "11111");
			Assert.True (isLoggedIn);
		}

		[Test]
		public void ALoginFail ()
		{
			TestUser user = TestUserDB.GetUserAtIndex (1);
			bool isLoggedIn = user.testManager.LoginSync (user.identifier, "hgkhgh");
			Assert.False (isLoggedIn);
		}

	}

}
