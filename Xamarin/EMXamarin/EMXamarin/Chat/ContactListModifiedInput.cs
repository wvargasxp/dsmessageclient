using System.Collections.Generic;

namespace em {
	public class ContactListModifiedInput {
		public IList<ContactInput> contacts { get; set; }
		public string type { get; set; }
	}
}