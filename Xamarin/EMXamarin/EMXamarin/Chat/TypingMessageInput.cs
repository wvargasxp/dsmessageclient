using System;
using System.Collections.Generic;

namespace em {
	public class TypingMessageInput : MessageInput {
		public string toAlias { get; set; }
		public string source { get; set; }
		public IList<string> replyTo { get; set; }
	}
}

