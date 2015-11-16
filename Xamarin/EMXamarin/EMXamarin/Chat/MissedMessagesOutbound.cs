using System;

namespace em {
	public class MissedMessagesOutbound {
		public string messagesSince { get; set; }

		public MissedMessagesOutbound () {
		}

		public void setMessagesSince(DateTime datetime) {
			if (datetime == DateTime.MinValue)
				messagesSince = null;
			else {
				DateTimeOffset offset = new DateTimeOffset(datetime,TimeSpan.Zero);
				messagesSince = offset.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
			}
		}
	}
}

