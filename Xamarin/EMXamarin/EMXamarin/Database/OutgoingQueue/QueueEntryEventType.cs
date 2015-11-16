using System;

namespace em {
	public enum QueueEntryEventType {
		Removed, 
		Acked, 
		Failed,
		EndEncoding
	}
}