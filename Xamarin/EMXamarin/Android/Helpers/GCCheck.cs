using System;
using Android.App;
using Android.Views;

namespace Emdroid {
	public static class GCCheck {

		// Splitting up the if checks so I can breakpoint on ones I find interesting. View being null or fragment being null is not interesting.
		// But the Handle being IntPtr.Zero is.

		public static bool Gone (Java.Lang.Object obj) {
			if (obj == null) {
				return true;
			}

			if (obj.Handle == IntPtr.Zero) {
				return true;
			}

			return false;
		}

		public static bool ViewGone (Fragment fragment) {

			if (fragment == null) {
				return true;
			}

			if (fragment.Handle == IntPtr.Zero) {
				return true;
			}

			View view = fragment.View;
			if (view == null) {
				return true;
			}

			if (view.Handle == IntPtr.Zero) {
				return true;
			}

			return false;
		}
	}
}

