using System;
using System.Collections.Generic;

namespace em {
	public class ChannelUpdateInput : MessageInput {
		public string messageGUID { get; set; }
		public string toAlias { get; set; }
		public string messageChannel { get; set; }
	}
}

