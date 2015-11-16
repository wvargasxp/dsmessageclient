using System;
using System.Collections.Generic;

namespace em {
	public class AddressBookArgs {
		public bool ExcludeGroups { get; set; }
		public bool ExcludeTemp { get; set; }
		public bool ExcludePreferred { get; set; }
		public ChatEntry ChatEntry { get; set; }

		private IList<Contact> _contacts = null;
		public IList<Contact> Contacts { 
			get {
				if (this._contacts == null) {
					this._contacts = new List<Contact> ();
				}

				return this._contacts;
			}

			// Create a new list from the current list of contacts.
			// This is so we don't link the the input's list of contacts with the address book's version of selected contacts.
			// Example, we set the contacts here to use the contacts from a chatentry,
			// If we do a set, any modifications on the AddressBookController on these contacts will be changed on the chat entry too.
			// Doing a copy lets us separate the two lists and managing together instead of as one.
			set { this._contacts = new List<Contact> (value); } 
		}

		public static AddressBookArgs From (
			bool excludeGroups, 
			bool exludeTemp, 
			bool excludePreferred,
			IList<Contact> contacts,
			ChatEntry entry = null) {

			AddressBookArgs args = new AddressBookArgs ();
			args.ExcludeGroups = excludeGroups;
			args.ExcludeTemp = exludeTemp;
			args.ExcludePreferred = excludePreferred;
			args.ChatEntry = entry;
			args.Contacts = contacts;
			return args;
		}

		public static AddressBookArgs From (
			bool excludeGroups, 
			bool exludeTemp, 
			bool excludePreferred,
			ChatEntry entry = null) {

			AddressBookArgs args = new AddressBookArgs ();
			args.ExcludeGroups = excludeGroups;
			args.ExcludeTemp = exludeTemp;
			args.ExcludePreferred = excludePreferred;
			args.ChatEntry = entry;
			return args;
		}

		public AddressBookArgs () {}
	}
}

