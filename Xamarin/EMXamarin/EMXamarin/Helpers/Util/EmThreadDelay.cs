using System;
using System.Threading;

namespace em {
	public static class EmThreadDelay {
		public static void Wait (int seconds) {
			// Set is never called, so we wait always until the timeout occurs
			using (EventWaitHandle tmpEvent = new ManualResetEvent (false)) {
				tmpEvent.WaitOne (TimeSpan.FromSeconds (seconds));
			}
		}

		public static void WaitMilli (int milliseconds) {
			// Set is never called, so we wait always until the timeout occurs
			using (EventWaitHandle tmpEvent = new ManualResetEvent (false)) {
				tmpEvent.WaitOne (TimeSpan.FromMilliseconds (milliseconds));
			}
		}
	}
}

