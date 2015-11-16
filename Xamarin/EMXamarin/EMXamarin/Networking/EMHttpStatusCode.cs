using System;

namespace em {
	public enum EMHttpStatusCode {
		GenericException,
		RetryableException,
		AuthorizationException,
		NameResolutionFailure,
		OrdinaryResponse
	}
}