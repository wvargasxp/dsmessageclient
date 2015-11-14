using System;
using Android.App;
using Android.Content;

namespace Emdroid {
	class AndroidModalDialogs : DialogFragment {
		public void ShowBasicOKMessage (string title, string message, EventHandler<DialogClickEventArgs> okHandler, bool cancellable = true) {
			Context context = EMApplication.GetCurrentActivity ();
			if (context == null) {
				System.Diagnostics.Debug.WriteLine ("AndroidModalDialogs: ShowBasicOKMessage - Context is null.");
			} else {
				var builder = new AlertDialog.Builder (context);
				builder.SetTitle (title);
				builder.SetMessage (message);
				builder.SetPositiveButton ("OK_BUTTON".t (), okHandler);
				builder.SetCancelable (cancellable);
				AlertDialog dialog = builder.Create ();
				dialog.Show ();
			}
		}

		public void ShowMessageWithButtons (string title, string message, string okayButton, string cancelButton, string[] otherButtons, EventHandler<DialogClickEventArgs> okHandler) {
			Context context = EMApplication.GetCurrentActivity ();
			if (context == null) {
				System.Diagnostics.Debug.WriteLine ("AndroidModalDialogs: ShowMessageWithButtons - Context is null.");
			} else {
				var builder = new AlertDialog.Builder (context);
				builder.SetTitle (title);
				builder.SetMessage (message);
				builder.SetPositiveButton (okayButton, (sender, e) => {
					DialogClickEventArgs copyArgs = new DialogClickEventArgs(1);
					okHandler.Invoke(sender,copyArgs);
				});
				builder.SetNegativeButton(cancelButton, (sender, em) => {
					DialogClickEventArgs copyArgs = new DialogClickEventArgs(0);
					okHandler.Invoke(sender,copyArgs);
				});
				if (otherButtons != null && otherButtons.Length > 0) {
					int i = 2;
					foreach ( string label in otherButtons ) {
						builder.SetNeutralButton(label, (sender, em) => {
							int which = i;
							DialogClickEventArgs copyArgs = new DialogClickEventArgs(which);
							okHandler.Invoke(sender,copyArgs);		
						});
						i++;
					}
				}
				AlertDialog dialog = builder.Create ();
				dialog.Show ();
			}
		}
	}
}