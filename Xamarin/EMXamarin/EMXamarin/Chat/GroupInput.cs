using System.Collections.Generic;

namespace em {
	public class GroupInput : ContactInput {
		/**
		 * Because GroupInput is also a ContactInput it cannot subclass
		 * MessageInput.  As such, we cut-n-paste include messageName (which
		 * would normally be inherited from MessageInput).
		 */
		public string messageName { get; set; }
		public IList<ContactInput> contacts { get; set; }
		public string ownerServerID { get; set; }
		public bool requesterIsOwner { get; set; }
		public bool requesterJoined { get; set; }
	}
}