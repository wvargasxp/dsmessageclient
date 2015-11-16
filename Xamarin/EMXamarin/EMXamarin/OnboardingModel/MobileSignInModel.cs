using System;

namespace em {
	public class MobileSignInModel : SignInModel {

		public void Register (EMAccount account, string mobileNumber, string countryCode, string phonePrefix, Action<string> completion) {
			ShouldPauseUI ();
			account.RegisterMobileNumber (mobileNumber, countryCode, phonePrefix, (success, accountID) => EMTask.DispatchMain (() => {
				if (success)
					completion (accountID);
				else
					DidFailToRegister ();
				
				ShouldResumeUI ();
			})); 
		}

		public bool InputIsValid (string phonePrefix, string mobileNumber) {
			return phonePrefix.Length >= 1 && mobileNumber.Length >= 7;
		}
	}
}