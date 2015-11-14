using System;
using em;
using System.Collections.Generic;

namespace Emdroid {
	public interface IContactSearchController {
		void UpdateContactsAfterSearch (IList<Contact> listOfContacts, string currentSearchFilter);
		void ShowList (bool shouldShowMainList);
		void RemoveContactAtIndex (int index);
		void InvokeFilter (string currentSearchFilter);
		string GetDisplayLabelString ();
		void SetQueryResultCallback (Action callback);
		bool HasResults ();
	}
}

