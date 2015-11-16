using System;

namespace em {
	public class SignInTypeModel {
		public SignInTypeModel () {}

		// A static method to set the key value for the state (Dictionary) of a UIComponent.
		public static string KeyForSignInType () {
			return "keyforsignintype";
		}

		public enum SignInTypes { Mobile, Email };

		public static SignInTypes ValueForMobile () {
			return SignInTypes.Mobile;
		}

		public static SignInTypes ValueForEmail () {
			return SignInTypes.Email;
		}

		public static string PageNameForSignInType (int type) {
			SignInTypes signintype = (SignInTypes)type;
			switch (signintype) {
			case SignInTypes.Mobile:
				return "MobileSignIn";
			case SignInTypes.Email:
				return "EmailSignIn";
			default:
				return "MobileSignIn"; // Mobile for default.
			}
		}
	}
}

