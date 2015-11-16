using System;

namespace em {
	public class QueueEntryEvent : QueueInstruction {
		QueueEntryEventType eventType;
		public QueueEntryEventType EventType {
			get {
				return eventType;
			}
		}

		QueueEntry queueEntry;
		public QueueEntry QueueEntry {
			get {
				return queueEntry;
			}
		}

		public QueueEntryEvent (QueueEntryEventType eventType, QueueEntry queueEntry) {
			this.eventType = eventType;
			this.queueEntry = queueEntry;
		}
	}
}