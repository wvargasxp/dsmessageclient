using System;

namespace em {
	public class UserPromptInput : MessageInput {
		public string title { get; set; }
		public string message { get; set; }
		public UserPromptButton okayButton { get; set; }
		public UserPromptButton cancelButton { get; set; }
		public UserPromptButton[] otherButtons { get; set; }
	}
}