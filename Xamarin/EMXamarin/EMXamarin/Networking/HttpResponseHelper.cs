using System;

namespace em {
	public class HttpResponseHelper {
		public static void CheckUnauthorized(int statusCode) {
			if (statusCode == 401) {
				HandleUnauthorized ();
			}
		}

		private static void HandleUnauthorized() {
			em.NotificationCenter.DefaultCenter.PostNotification (Constants.EMAccount_EMHttpUnauthorizedResponseNotification);
		}
	}
}

