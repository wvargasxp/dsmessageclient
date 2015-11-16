using System;
using System.Net.Http;

namespace em {
	public class QueueEntryInstruction : QueueInstruction {

		private QueueEntry queueEntry;
		public QueueEntry QueueEntry {
			get {
				return queueEntry;
			}
		}

		private Action<EMHttpResponse> callback;
		public Action<EMHttpResponse> Callback {
			get {
				return callback;
			}
		}

		public QueueEntryInstruction (QueueEntry queueEntry, Action<EMHttpResponse> callback) {
			this.queueEntry = queueEntry;
			this.callback = callback;
		}
	}
}

