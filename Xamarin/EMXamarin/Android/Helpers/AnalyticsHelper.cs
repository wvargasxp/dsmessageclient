using Google.Analytics.Tracking;
using em;

namespace Emdroid {
	
	public class AnalyticsHelper : IAnalyticsHelper {

		public static void SendView(string name) {
			EMTask.DispatchBackground (() => {
				Tracker tracker = EasyTracker.GetInstance (EMApplication.GetMainContext ());

				// This screen name value will remain set on the tracker and sent with
				// hits until it is set to a new value or to null.
				tracker.Set (Fields.ScreenName, name);

				tracker.Send (MapBuilder
				.CreateAppView ()
				.Build ()
				);
			});
		}

		public static void SendField(string field, string value) {
			EMTask.DispatchBackground (() => {
				Tracker tracker = EasyTracker.GetInstance (EMApplication.GetMainContext ());

				tracker.Set (field, value);

				tracker.Send (MapBuilder
				.CreateAppView ()
				.Build ()
				);
			});
		}

		public void SendEvent(string category, string action, string label, int value) {
			EMTask.DispatchBackground (() => {
				Tracker tracker = EasyTracker.GetInstance (EMApplication.GetMainContext ());

				tracker.Send (MapBuilder
				.CreateEvent (category, action, label, new Java.Lang.Long (value))
				.Build ()
				);
			});
		}
	}
}