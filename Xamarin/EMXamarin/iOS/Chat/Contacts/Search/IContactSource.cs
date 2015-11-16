using System;
using em;
using System.Collections.Generic;

namespace iOS {
	public interface IContactSource {
		IList<Contact> ContactList { get; set; }
	}
}

