using System;
using Android.Content;
using Android.App;

namespace Emdroid {
	public static class UserPrompter {

		public static void PromptUserWithAction (string title, string message, string action, Action callback, Action cancelCallback, Context ctx) {
			var builder = new AlertDialog.Builder (ctx);

			builder.SetTitle (title);
			builder.SetMessage (message);
			builder.SetPositiveButton(action, (sender, dialogClickEventArgs) => callback ());
			builder.SetNegativeButton("CANCEL_BUTTON".t (), (sender, dialogClickEventArgs) => { cancelCallback (); });
			builder.Create ();
			builder.Show ();
		}

		public static void PromptUserWithAction (string title, string message, string action, Action callback, Context ctx) {
			var builder = new AlertDialog.Builder (ctx);

			builder.SetTitle (title);
			builder.SetMessage (message);
			builder.SetPositiveButton(action, (sender, dialogClickEventArgs) => callback ());
			builder.SetNegativeButton("CANCEL_BUTTON".t (), (sender, dialogClickEventArgs) => { });
			builder.Create ();
			builder.Show ();
		}

		public static void PromptUserWithActionNoNegative (string title, string message, Action callback, Context ctx) {
			var builder = new AlertDialog.Builder (ctx);

			builder.SetTitle (title);
			builder.SetMessage (message);
			builder.SetPositiveButton ("OK_BUTTON".t (), (sender, dialogClickEventArgs) => callback ());
			builder.Create ();
			builder.Show ();
		}

		public static void PromptUser (string title, string message, Context ctx) {
			var builder = new AlertDialog.Builder (ctx);

			builder.SetTitle (title);
			builder.SetMessage (message);
			builder.SetNegativeButton("OK_BUTTON".t (), (sender, dialogClickEventArgs) => { });
			builder.Create ();
			builder.Show ();
		}
	}
}

