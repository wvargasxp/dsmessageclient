using System;
using System.Text.RegularExpressions;

namespace em {
	public class EmailSignInModel : SignInModel {

		static bool StringIsValidEmail(string strIn) {
			// Return true if strIn is in valid e-mail format.
			return Regex.IsMatch(strIn, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"); 
		}

		public bool InputIsValid (string checkStr) {
			return StringIsValidEmail (checkStr);
		}

		public void Register ( EMAccount account, string emailAddress, string countryCode, string phonePrefix, Action<string> completion) {
			ShouldPauseUI ();
			account.RegisterEmailAddress (emailAddress, countryCode, phonePrefix, (bool success, string accountID) => EMTask.DispatchMain (() => {
				if (success)
					completion (accountID);
				else
					DidFailToRegister ();
				
				ShouldResumeUI ();
			}));
		}
	}
}