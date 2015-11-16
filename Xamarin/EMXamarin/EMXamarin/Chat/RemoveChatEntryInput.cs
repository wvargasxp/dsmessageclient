using System;
using System.Collections.Generic;

namespace em {
	public class RemoveChatEntryInput {
		public string toAlias { get; set; }
		public IList<string> replyTo { get; set; }
		public DateTime createDate { get; set; }
	}
}

