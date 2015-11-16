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
	public class RegistrationTests : EMClientTests
	{
		//Registration test cases:
		//Register email fail - user 0
		//Register email pass - user 1
		//Register both emails as one - user 1 and 2
		//Register phone fail - user 3
		//Register phone pass - user 4 and 5

		private bool attemptEmailRegistration(int userIndex) 
		{
			TestUser user = TestUserDB.GetUserAtIndex (userIndex);
			return user.testManager.RegisterEmailSync();
		}

		private bool attemptPhoneNumberRegistration(int userIndex) 
		{
			TestUser user = TestUserDB.GetUserAtIndex (userIndex);
			return user.testManager.RegisterPhoneSync();
		}

		[Test ()]
		public void registerEmailPass() 
		{
			bool didRegister = attemptEmailRegistration(1);
			Assert.IsTrue (didRegister);
		}

		[Test ()]
		public void registerEmailFail() 
		{
			bool didRegister = attemptEmailRegistration (0);
			Assert.IsFalse (didRegister);
		}

		[Test ()]
		public void registerPhonePass() 
		{
			bool didRegister = attemptPhoneNumberRegistration(4);
			Assert.IsTrue (didRegister);
		}

		[Test ()]
		public void registerPhoneFail() 
		{
			bool didRegister = attemptPhoneNumberRegistration(3);
			Assert.IsFalse (didRegister);
		}
	}
}

