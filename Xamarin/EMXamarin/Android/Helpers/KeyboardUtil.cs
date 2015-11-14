using Android.App;
using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using Java.Lang;

namespace Emdroid {
	public static class KeyboardUtil {
		private const string Tag = "KeyboardUtil: ";
		private const int DelayBeforeShowingKeyboard = 200; // millis

		public static void ShowKeyboard (View view) {
			Activity activity = EMApplication.GetCurrentActivity ();
			ShowKeyboard (activity, view);
		}

		public static void ShowKeyboard (Activity activity, View view) {
			if (activity == null || view == null) {
				System.Diagnostics.Debug.WriteLine (string.Format ("{0} ShowKeyboard - Attempted to show keyboard but activity or view is null.", Tag));
			} else {
				var imgr = (InputMethodManager)activity.GetSystemService (Context.InputMethodService);
				// Showing the keyboard with a delay.
				view.PostDelayed (DelayKeyboardRunnable.From (imgr, view), DelayBeforeShowingKeyboard);
			}
		}

		/*
		 * Workaround for disabling keyboards programatically 100% of the time.
		 * https://stackoverflow.com/questions/5520085/android-show-softkeyboard-with-showsoftinput-is-not-working
		 * Gist is that if we try to call ShowSoftInput immediately, there's a good chance the keyboard doesn't show.
		 * Use a Runnable and request the focus after a certain delay gives it a 100% success rate.
		 */
		private class DelayKeyboardRunnable : Java.Lang.Object, IRunnable {
			private InputMethodManager Manager { get; set; }
			private View View { get; set; }

			public static DelayKeyboardRunnable From (InputMethodManager imm, View view) {
				DelayKeyboardRunnable z = new DelayKeyboardRunnable ();
				z.Manager = imm;
				z.View = view;
				return z;
			}

			public void Run () {
				this.View.RequestFocus ();
				this.Manager.ShowSoftInput (this.View, ShowFlags.Implicit);
			}
		}

		public static void ShowKeyboard (Activity activity) {
			var imgr = activity != null ? (InputMethodManager) activity.GetSystemService (Context.InputMethodService) : null;
			if (imgr == null) {
				System.Diagnostics.Debug.WriteLine (string.Format ("{0} ShowKeyboard - Attempted to show keyboard but imgr is null.", Tag));
			} else {
				imgr.ToggleSoftInput (ShowFlags.Implicit, HideSoftInputFlags.None);
			}
		}

		public static void HideKeyboard (View view) {
			Activity activity = EMApplication.GetCurrentActivity ();
			HideKeyboard (activity, view);
		}

		public static void HideKeyboard (Activity activity, View view) {
			if (activity == null || view == null) {
				System.Diagnostics.Debug.WriteLine (string.Format ("{0} HideKeyboard - Attempted to hide keyboard but activity or view is null.", Tag));
			} else {
				var imgr = (InputMethodManager)activity.GetSystemService (Context.InputMethodService);
				imgr.HideSoftInputFromWindow (view.WindowToken, HideSoftInputFlags.None);
				view.ClearFocus ();
			}
		}

		public static void HideKeyboard (Activity activity) {
			var imgr = activity != null ? (InputMethodManager) activity.GetSystemService (Context.InputMethodService) : null;
			if (imgr == null) {
				System.Diagnostics.Debug.WriteLine (string.Format ("{0} HideKeyboard - Attempted to hide keyboard but imgr is null.", Tag));
			} else {
				View currentView = activity.CurrentFocus;
				if (currentView != null) {
					imgr.HideSoftInputFromWindow (currentView.WindowToken, HideSoftInputFlags.None);
					currentView.ClearFocus ();
				} else {
					imgr.ToggleSoftInput (ShowFlags.Implicit, HideSoftInputFlags.None);
				}
			}
		}
	}
}