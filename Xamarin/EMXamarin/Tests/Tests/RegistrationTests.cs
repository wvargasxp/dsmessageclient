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
	public class RegistrationTests
	{
		//Registration test cases:
		//Register email fail - user 0
		//Register email pass - user 1
		//Register both emails as one - user 1 and 2
		//Register phone fail - user 3
		//Register phone pass - user 4 and 5

		public RegistrationTests ()
		{
		}


		private Task<bool> attemptEmailRegistration(int userIndex) 
		{
			ApplicationModel appModel = TestUserDB.GetUserAtIndex(userIndex).appModel;
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
			string emailAddress = TestUserDB.GetUserAtIndex(userIndex).identifier;
			appModel.account.RegisterEmailAddress(emailAddress, (bool isSuccessful, string accountID) => {
				tcs.SetResult(isSuccessful);
			});
			return tcs.Task;
		}

		private Task<bool> attemptPhoneNumberRegistration(int userIndex) 
		{
			ApplicationModel appModel = TestUserDB.GetUserAtIndex(userIndex).appModel;
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
			string identifier = TestUserDB.GetUserAtIndex (userIndex).identifier;
			char[] delimiter = {' '};
			string[] identifierArr = identifier.Split (delimiter, 2);
			string countryCode = identifierArr [0];
			string phoneNumber = identifierArr.Length == 2 ? identifierArr [1] : "";
			appModel.account.RegisterMobileNumber(phoneNumber, countryCode, ((bool isSuccessful, string accountID) => {
				Debug.WriteLine(accountID);
				tcs.SetResult(isSuccessful);
			}));
			return tcs.Task;
		}

		[Test]
		public async void registerEmailPass() 
		{
			bool didRegister = await attemptEmailRegistration(1);
			Assert.True (didRegister);
		}

		[Test]
		public async void registerEmailFail() 
		{
			bool didRegister = await attemptEmailRegistration (0);
			Assert.False (didRegister);
		}

		[Test]
		public async void registerPhonePass() 
		{
			bool didRegister = await attemptPhoneNumberRegistration(4);
			Assert.True (didRegister);
		}

		[Test]
		public async void registerPhoneFail() 
		{
			bool didRegister = await attemptPhoneNumberRegistration(3);
			Assert.False (didRegister);
		}
	}
}

