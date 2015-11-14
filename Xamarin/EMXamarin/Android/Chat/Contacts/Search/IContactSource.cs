using System;
using em;
using System.Collections.Generic;
using Android.Content;

namespace Emdroid {
	public interface IContactSource {
		IList<Contact> ContactList { get; set; }
		Context Context { get; }
	}
}

