using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace em {
	public class LeaveConversationInput {
		public IList<ContactInput> replyTo { get; set; }
		public ContactInput from { get; set; }
		public string toAlias { get; set; }
		public DateTime createDate { get; set; }
	}
}

