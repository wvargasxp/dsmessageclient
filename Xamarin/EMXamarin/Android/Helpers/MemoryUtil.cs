using Android.App;

namespace Emdroid {
	public static class MemoryUtil {
		public static void ClearReferences (Activity activityToClear) {
			// When getting the current context from Application, be sure to clear references so the GC can garbage collect properly.
			// http://stackoverflow.com/questions/11411395/how-to-get-current-foreground-activity-context-in-android
			Activity curActivity = EMApplication.GetCurrentActivity ();
			if (curActivity != null && curActivity.Equals (activityToClear)) {
				EMApplication.SetCurrentActivity (null);
			}
		}
	}
}