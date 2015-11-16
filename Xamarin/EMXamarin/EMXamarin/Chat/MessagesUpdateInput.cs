using System;
using System.Collections.Generic;

namespace em {
	public class MessagesUpdateInput : MessageInput {
		public IList<MessageUpdateInput> messageUpdates { get; set; }
	}
}

