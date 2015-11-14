using System;
using Android.Views;
using Android.Widget;
using Android.App;
using em;

namespace Emdroid {
	public class SoftKeyboardListener : Java.Lang.Object, ViewTreeObserver.IOnGlobalLayoutListener {

		private WeakReference _rootLayoutRef = null;
		private LinearLayout RootLayout { 
			get { return this._rootLayoutRef != null ? this._rootLayoutRef.Target as LinearLayout : null; }
			set { this._rootLayoutRef = new WeakReference (value); }
		}

		private WeakReference _activityRef = null;
		private Activity Activity { 
			get { return this._activityRef != null ? this._activityRef.Target as Activity : null; }
			set { this._activityRef = new WeakReference (value); }
		}

		public static string SHOW = "SoftKeyboardListener.Show";
		public static string HIDE = "SoftKeyboardListener.Hide";

		private int PreviousDifference { get; set; }

		public SoftKeyboardListener (LinearLayout layout, Activity activity) {
			this.RootLayout = layout;
			this.Activity = activity;
			this.PreviousDifference = 0;
		}

		#region IOnGlobalLayoutListener implementation
		public void OnGlobalLayout () {
			LinearLayout rootLayout = this.RootLayout;
			Activity activity = this.Activity;

			if (rootLayout == null || activity == null) {
				return;
			}

			int heightDiff = rootLayout.RootView.Height - rootLayout.Height;
			int contentViewTop = activity.Window.FindViewById (Window.IdAndroidContent).Top;

			int newDifference = heightDiff - contentViewTop;

			if (newDifference != this.PreviousDifference) {
				this.PreviousDifference = newDifference;

				if (heightDiff <= contentViewTop) {
					NotificationCenter.DefaultCenter.PostNotification (HIDE);
				} else {
					NotificationCenter.DefaultCenter.PostNotification (SHOW);
				}
			}

		}
		#endregion
	}
}

