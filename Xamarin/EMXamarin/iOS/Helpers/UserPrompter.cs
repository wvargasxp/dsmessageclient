using System;
using UIKit;

namespace iOS {
	public static class UserPrompter {
		public static void PromptUserWithAction (string title, string message, string action, Action callback) {
			var alert = new UIAlertView (title, message, null, "CANCEL_BUTTON".t (), new [] { action });
			alert.Show ();
			alert.Clicked += (sender2, buttonArgs) =>  { 
				switch ( buttonArgs.ButtonIndex ) {
				case 1:
					callback();
					break;
				}
			};
		}

		public static void PromptUserWithActionNoNegative (string title, string message, Action callback) {
			var alert = new UIAlertView (title, message, null, null, new [] { "OK_BUTTON".t () });
			alert.Show ();
			alert.Clicked += (sender2, buttonArgs) => callback ();
		}

		public static void PromptUser (string title, string message) {
			var alert = new UIAlertView (title, message, null, "OK_BUTTON".t (), null);
			alert.Show ();
		}
	}
}

