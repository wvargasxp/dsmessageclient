using System;

namespace em {
	public class SignInModel {
		public SignInModel () {}

		// Platforms can be notified of when the model fails to register.
		public Action DidFailToRegister { get; set; }
		public Action ShouldPauseUI { get; set; }
		public Action ShouldResumeUI { get; set; }
	}
}