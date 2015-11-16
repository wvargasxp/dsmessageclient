using System;

namespace em {
	public enum InactiveConversationReason {
		Other, // any other reason why 
		FromAliasInActive, // when the from alias is in active
		Success, // when the conversation is active
	}
}

