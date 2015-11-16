using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace em {
	public class ChatMessageInput : MessageInput {
		public IList<ContactInput> replyTo { get; set; }
		public ContactInput from { get; set; }
		public string toAlias { get; set; }
		public string message { get; set; }
		public string mediaRef { get; set; }
		public string contentType { get; set; }
		public JToken attributes { get; set; }
		public string messageGUID { get; set; }
		public DateTime sentDate { get; set; }
		public bool inbound { get; set; }
		public string messageStatus { get; set; }
		public string messageLifecycle { get; set; }
		public string messageChannel { get; set; }
	}
}