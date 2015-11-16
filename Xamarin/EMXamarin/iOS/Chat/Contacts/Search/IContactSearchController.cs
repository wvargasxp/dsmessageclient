using System;
using System.Collections.Generic;
using em;

namespace iOS {
	public interface IContactSearchController {
		void UpdateContactsAfterSearch (IList<Contact> listOfContacts, string currentSearchFilter);
		void ShowList (bool shouldShowMainList);
		void RemoveContactAtIndex (int index);
		void InvokeFilter (string currentSearchFilter);
		string GetDisplayLabelString ();
		bool HasResults ();
	}
}

