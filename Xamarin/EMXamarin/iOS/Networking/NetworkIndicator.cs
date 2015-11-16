using System.Threading;
using UIKit;

namespace iOS {
	public static class NetworkIndicator {
		static int _counter;

		public static void ShowNetworkIndicator () {
			Interlocked.Increment (ref _counter);
			RefreshIndicator ();
		}

		public static void HideNetworkIndicator () {
			Interlocked.Decrement (ref _counter);
			RefreshIndicator ();
		}

		static void RefreshIndicator () {
			UIApplication.SharedApplication.NetworkActivityIndicatorVisible = (_counter > 0);
		}
	}
}