using System;
using System.Collections.Generic;

namespace em {
	public class AddressBookSelectionResult {
		
		public IList<Contact> Contacts { get; private set; }

		public AddressBookSelectionResult (IList<Contact> contacts) {
			this.Contacts = contacts;
		}
	}
}

