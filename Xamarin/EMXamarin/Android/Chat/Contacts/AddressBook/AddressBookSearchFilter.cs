using System;
using System.Collections.Generic;
using System.Diagnostics;
using Android.App;
using Android.Runtime;
using Android.Widget;
using em;
using EMXamarin;

namespace Emdroid {
	public class AddressBookSearchFilter : Filter {

		AddressBookListAdapter contactsListAdapter;
		public AddressBookListAdapter ContactsListAdapter {
			get { return contactsListAdapter; }
			set { contactsListAdapter = value; }
		}

		public IList<AggregateContact> ContactsList {
			get { return this.ContactsListAdapter.ContactList; }
		}

		IList<AggregateContact> UnfilteredContacts {
			get { return this.ContactsListAdapter.UnfilteredContacts; }
		}

		Action queryResultFinished = () => {};
		public Action QueryResultFinished {
			get { return queryResultFinished; }
			set { queryResultFinished = value; }
		}

		public AddressBookSearchFilter (AddressBookListAdapter adapter) {
			this.ContactsListAdapter = adapter;
		}
			
		protected override FilterResults PerformFiltering (Java.Lang.ICharSequence _constraint) {
			var results = new FilterResults ();

			if (this.UnfilteredContacts != null) {
				Java.Lang.ICharSequence constraint = _constraint;

				string query = string.Empty;

				try {
					query = constraint.ToString ().ToLower ();
				} catch (Exception e) {
					// Seeing jObject intPtr exception here. // TODO: Come back and figure out why memory is being released prememptively here.
					Debug.WriteLine ("AddressBookListAdapter:PerformFiltering:Exception is " + e);
				}

				IList<AggregateContact> filteredContacts = this.ContactsListAdapter.FilterAndReturnContacts (query);
				if (filteredContacts != null) {
					results.Count = filteredContacts.Count;
					results.Values = new JavaList<AggregateContact> (filteredContacts);
				}
			}

			return results;
		}

		protected override void PublishResults (Java.Lang.ICharSequence constraint, FilterResults results) {}
	}
}
