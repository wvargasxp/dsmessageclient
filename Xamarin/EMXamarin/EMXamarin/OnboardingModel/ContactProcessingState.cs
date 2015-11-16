using System;

namespace em {
	public enum ContactProcessingState {
		Inactive,
		Acquiring_Access,
		Accessing,
		Processing,
		Awaiting_Registration,
		Registering
	}
}

