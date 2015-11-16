using System.Collections.Generic;
using System.Linq;

namespace em {
	public static class ContactSearchUtil {
		public static bool ShouldDisableContactCell (ChatEntry chatEntry, Contact contact) {
			return ShouldDisableContactCell (chatEntry, contact, ContactSelectionState.All);
		}

		public static bool ShouldDisableContactCell (ChatEntry chatEntry, Contact contact, ContactSelectionState state) {

			switch (state) {
			case ContactSelectionState.NoGroups:
				{
					if (contact.IsAGroup) {
						return true;
					}
					break;
				}
			case ContactSelectionState.NoSelection:
				{
					return true;
				}
			case ContactSelectionState.All:
			default:
				{
					// The selection state is All, so now we delegate to how our chat entry is set up to determine state.
					break;
				}
			}

			// Check what type of contact is currently in the send to list on this chat entry. 
			// If it's a group, we should disable a contact if they are not a group.
			// If they aren't a group, we should disable a contact if they are a group.
			if (chatEntry == null) {
				return false;
			}

			if (contact.lifecycle != ContactLifecycle.Active) {
				return true;
			}

			if (contact.IsAGroup && (contact.GroupStatus == GroupMemberStatus.Abandoned
			    || contact.GroupMemberLifeCycle == GroupMemberLifecycle.Removed)) {
				return true;
			}

			IList<Contact> contacts = chatEntry.contacts;
			if (contacts != null && contacts.Count > 0) {
				Contact c = contacts [0];
				bool sendingIsGroup = c.IsAGroup;

				bool shouldDisable = false;
				if (sendingIsGroup) {
					shouldDisable = true; // only one group can be messaged at a time, so if the current contact is a group, disable every cell
				} else {
					shouldDisable = contact.IsAGroup;
				}

				return shouldDisable;
			}

			// don't disable any cells if the contact send to list is empty or null
			return false;
		}

		public static IList<Contact> FilterContactsBySearchQuery (IList<Contact> listOfContacts, string searchQuery) {
			IList<Contact> queryMatches = new List<Contact> ();
			if (listOfContacts != null) {
				searchQuery = searchQuery.ToLower ();
				foreach (Contact contact in listOfContacts) {
					if (ContactMatchesAgainstSearchQuery (contact, searchQuery)) {
						if (contact.preferred) {
							queryMatches.Insert (0, contact);
						} else {
							queryMatches.Add (contact);
						}
					}
				}
			}

			return queryMatches;
		}

		public static IList<AggregateContact> FilterAggregateContactsBySearchQuery (IList<AggregateContact> listOfContacts, string searchQuery) {
			IList<AggregateContact> queryMatches = new List<AggregateContact> ();

			if (listOfContacts != null) {
				searchQuery = searchQuery.ToLower ();
				foreach (AggregateContact contact in listOfContacts) {
					IList<Contact> contactsInAggregateContact = contact.Contacts;
					foreach (Contact detailContact in contactsInAggregateContact) {
						if (ContactMatchesAgainstSearchQuery (detailContact, searchQuery)) {
							queryMatches.Add (contact); // add the AggregateContact if its detail matches
							break;
						}
					}
				}
			}

			return queryMatches;
		}

		static bool ContactMatchesAgainstSearchQuery (Contact contact, string searchQuery) {
			if (!string.IsNullOrWhiteSpace (contact.displayName)) {
				if (contact.displayName.ToLower ().Contains (searchQuery)) {
					return true;
				}
			}

			if (!string.IsNullOrWhiteSpace (contact.addressBookFirstName)) {
				if (contact.addressBookFirstName.ToLower ().Contains (searchQuery)) {
					return true;
				}
			}

			if (!string.IsNullOrWhiteSpace (contact.addressBookLastName)) {
				if (contact.addressBookLastName.ToLower ().Contains (searchQuery)) {
					return true;
				}
			}

			string contactDescription = contact.description;
			if (!string.IsNullOrWhiteSpace (contactDescription)) {
				if (!contactDescription.Contains ("@")) {
					contactDescription = new string (contactDescription.Cast<char> ().Where (c => char.IsDigit (c)).ToArray ());
				} 

				if (contactDescription.Contains (searchQuery)) {
					return true;
				}
			}

			return false;
		}
	}
}