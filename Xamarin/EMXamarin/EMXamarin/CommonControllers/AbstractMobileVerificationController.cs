using System;
using System.Collections.Generic;

namespace em {
	public abstract class AbstractMobileVerificationController {

		public string AccountID { get; set; }
		public abstract void ShouldPauseUI ();
		public abstract void ShouldResumeUI ();
		public abstract void UpdateTextFieldWithText (string text);
		public abstract void TriggerContinueButton ();
		public abstract void DisplayAccountError ();
		public abstract void DismissControllerAndFinishOnboarding ();
		public abstract void GoToAccountController ();

		private ApplicationModel AppModel { get; set; }

		public AbstractMobileVerificationController (ApplicationModel model) {
			this.AppModel = model;
		}

		public void TryToLogin (string verification) {
			LoginWithAccountInfo (this.AppModel.account, verification, (acctID, password) => {
				var dictionary = new Dictionary<string, string>();
				dictionary.Add (Constants.USERNAME_KEY, acctID);
				dictionary.Add (Constants.VERIFICATION_CODE_KEY, password);
				ISecurityManager security = this.AppModel.platformFactory.GetSecurityManager ();
				security.SaveSecureKeyValue (dictionary);
			});
		}

		private void LoginWithAccountInfo (EMAccount account, string verificationCode, Action<string, string> completion) {
			ShouldPauseUI ();

			bool isExistingAccount = account.accountInfo.existingAccount;
			if (isExistingAccount) {
				// If we have an existing account, we need to ask the user if they want to restore previous messages.
				// Part of the reconnection flow requests missed messages after a reconnect.
				// This opens us up to the case where a reconnect happens while the alert is up which causes RequestMissedMessages to be called.
				// We set a flag here to indicate that we're waiting for an answer from the user indicating whether or not they want to receive missed messages.
				// If the reconnection flow is triggered, it can check this flag to see if it should request missed messages or not.
				this.AppModel.AwaitingGetHistoricalMessagesChoice = true;
			}

			account.LoginWithAccountIdentifier (AccountID, verificationCode, !isExistingAccount, (success, existing) => EMTask.DispatchMain (() => {
				if (success) {
					SendVerifiedEventToAdjust ();
					completion (AccountID, verificationCode);
					DidVerifyCode (true, existing);
				} else {
					DidVerifyCode (false, false);
				}

				ShouldResumeUI ();
			}));
		}

		private void SendVerifiedEventToAdjust () {
			IAdjustHelper adjustHelper = ApplicationModel.SharedPlatform.GetAdjustHelper ();
			Dictionary<string, string> parameters = new Dictionary<string, string> ();
			parameters.Add (EmAdjustParamKey.AccountKey, this.AccountID);
			adjustHelper.SendEvent (EmAdjustEvent.Verified, parameters);
		}

		public bool InputIsValid (string chkStr) {
			return chkStr.Length >= 3;
		}

		public void CheckVerificationCodeReceivedViaUrl () {
			ISecurityManager security = this.AppModel.platformFactory.GetSecurityManager ();

			string verificationCode = security.GetSecureKeyValue (Constants.URL_QUERY_VERIFICATION_CODE_KEY);
			if (verificationCode == null) {
				verificationCode = security.retrieveSecureField (Constants.URL_QUERY_VERIFICATION_CODE_KEY);

				if (verificationCode == null) {
					return;
				}

				security.SaveSecureKeyValue (Constants.URL_QUERY_VERIFICATION_CODE_KEY, verificationCode);
				security.removeSecureField (Constants.URL_QUERY_VERIFICATION_CODE_KEY);
			}

			UpdateTextFieldWithText (verificationCode);

			security.RemoveSecureKeyValue (Constants.URL_QUERY_VERIFICATION_CODE_KEY);
			security.removeSecureField (Constants.URL_QUERY_VERIFICATION_CODE_KEY);

			TriggerContinueButton ();
		}

		private void DidVerifyCode (bool didVerify, bool existing) {
			if (didVerify) {
				if (existing) {
					DismissControllerAndFinishOnboarding ();
				} else {
					GoToAccountController ();
				}
			} else {
				DisplayAccountError ();
			}
		}
	}
}

