using System;
using Android.Widget;
using System.Collections.Generic;
using em;
using Android.App;
using Android.Runtime;
using System.Linq;
using System.Diagnostics;
using EMXamarin;

namespace Emdroid {
	public class ContactSearchFilter : Filter {

		ContactSearchListAdapter contactsListAdapter;
		public ContactSearchListAdapter ContactsListAdapter {
			get { return contactsListAdapter; }
			set { contactsListAdapter = value; }
		}

		public IList<Contact> ContactsList {
			get { return this.ContactsListAdapter.Contacts; }
		}

		IList<Contact> UnfilteredContacts {
			get { return this.ContactsListAdapter.UnfilteredContacts; }
		}

		public ContactSearchFilter (ContactSearchListAdapter adapter) {
			this.ContactsListAdapter = adapter;
		}

		protected override FilterResults PerformFiltering (Java.Lang.ICharSequence _constraint) {
			Java.Lang.ICharSequence constraint = _constraint;
			FilterResults results = new FilterResults ();

			string query = string.Empty;

			try {
				query = constraint.ToString ().ToLower ();
			} catch (Exception e) {
				// Seeing jObject intPtr exception here. // TODO: Come back and figure out why memory is being released prememptively here.
				Debug.WriteLine ("ContactSearchFilter:PerformFiltering:Exception is " + e);
			}

			IList<Contact> filteredContacts = this.ContactsListAdapter.FilterAndReturnContacts (query);
			if (filteredContacts != null) {
				results.Count = filteredContacts.Count;
				results.Values = new JavaList<Contact> (filteredContacts);
			}

			return results;
		}

		protected override void PublishResults (Java.Lang.ICharSequence constraint, FilterResults results) {}
	}
}
