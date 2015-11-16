using System;

namespace em {
	public class NotificationInput {
		public int serverID { get; set; }
		public string actionID { get; set; }
		public DateTime timestamp { get; set; }
		public string title { get; set; }
		public string url { get; set; }
		public bool read { get; set; }
		public bool deleted { get; set; }
	}
}