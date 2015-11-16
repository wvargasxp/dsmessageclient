using em;
using GoogleAnalytics.iOS;

namespace iOS {
	
	public class AnalyticsHelper : IAnalyticsHelper {
		
		public void SendEvent(string category, string action, string label, int value) {
			EMTask.DispatchBackground (() => {
				GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateEvent (category, action, label, value).Build ());
			});
		}
	}
}