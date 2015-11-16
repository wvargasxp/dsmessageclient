using System;
using System.Collections.Generic;

namespace em {
	public class MessageStatusOutbound {
		public string messageGUID { get; set; }
		public MessageStatus messageStatus { get; set; }
		public string fromAlias { get; set; }
		public string senderID { get; set; }
		public IList<string> destinations { get; set; }

		public MessageStatusOutbound () {
			messageStatus = MessageStatus.delivered;
		}
	}
}

