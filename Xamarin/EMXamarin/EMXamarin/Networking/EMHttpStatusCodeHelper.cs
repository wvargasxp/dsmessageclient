using System;

namespace em {
	public class EMHttpStatusCodeHelper {

		public static readonly int INVALID_STATUS_CODE = -1;

		public static EMHttpStatusCode ToEMHttpStatusCode (int statusCode) {
			EMHttpStatusCode emCode;

			if (isInvalidStatusCode (statusCode)) {
				emCode = EMHttpStatusCode.GenericException;

			} else if (statusCode < 400) {
				emCode = EMHttpStatusCode.OrdinaryResponse;

			} else if (statusCode == 401) {
				emCode = EMHttpStatusCode.AuthorizationException;

			} else if (statusCode == 503) {
				emCode = EMHttpStatusCode.RetryableException;

			} else {
				emCode = EMHttpStatusCode.GenericException;

			}

			return emCode;
		}

		private static bool isInvalidStatusCode (long statusCode) {
			return statusCode <= 0 || statusCode >= 600;
		}
	}
}

