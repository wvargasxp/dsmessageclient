using System;

namespace em {
	public class MessageUpdateInput {
		public string messageGUID { get; set; }
		public string toAlias { get; set; }
		public string messageStatus { get; set; }
		public string updaterServerID { get; set; }
	}
}

