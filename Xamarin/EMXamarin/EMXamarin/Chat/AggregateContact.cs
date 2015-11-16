using System.Collections.Generic;

namespace em {
	public class AggregateContact {
		
		string serverContactID;
		public string ServerContactID {
			get { return serverContactID; }
		}

		bool hasPreferredContact;
		public bool HasPreferredContact {
			get { return hasPreferredContact; }
			set { hasPreferredContact = value; }
		}

		bool hasTempContact;
		public bool HasTempContact {
			get { return hasTempContact; }
			set { hasTempContact = value; }
		}

		bool isGroupContact;
		public bool IsGroupContact {
			get { return isGroupContact; }
			set { isGroupContact = value; }
		}

		IList<Contact> contacts;
		public IList<Contact> Contacts {
			get { 
				if (contacts == null)
					contacts = new List<Contact> ();
				return contacts; 
			}
			set { contacts = value; }
		}

		public bool SingleContact {
			get { return contacts.Count == 1; }
		}

		public bool SinglePreferredContact {
			get {
				if(contacts.Count > 1) {
					int preferredCount = 0;
					for(var i = 0; i < contacts.Count; i++) {
						if(contacts[i].preferred)
							preferredCount++;
					}

					return preferredCount == 1;
				}

				return false;
			}
		}

		public Contact PreferredContact {
			get {
				if (SinglePreferredContact) {
					for(var i = 0; i < contacts.Count; i++) {
						if (contacts [i].preferred)
							return contacts [i];
					}
				}

				return null;
			}
		}

		public bool HasContacts {
			get {
				return this.Contacts != null && this.Contacts.Count > 0;
			}
		}

		public AggregateContact (Contact c) {
			serverContactID = c.serverContactID;
			this.Contacts.Add (c);

			if (c.preferred)
				this.HasPreferredContact = true;

			if (c.tempContact.Value)
				this.HasTempContact = true;

			if (c.IsAGroup)
				this.IsGroupContact = true;
		}

		public bool Contains (Contact c) {
			return this.Contacts.Contains (c);
		}

		public void AddContact (Contact c) {
			if (this.HasPreferredContact) {
				// we only add another contact to the rolled up contact if it's preferred also, otherwise we don't want to show it
				if (c.preferred) {
					this.Contacts.Add (c);

					if (c.tempContact.Value)
						this.HasTempContact = true;
				}
			} else {
				this.Contacts.Add (c);
				if (c.preferred)
					this.HasPreferredContact = true;

				if (c.tempContact.Value)
					this.HasTempContact = true;
			}
		}

		public string FirstName {
			get { return this.ContactForDisplay.addressBookFirstName; }
		}

		public string LastName {
			get { return this.ContactForDisplay.addressBookLastName; }
		}

		public string DisplayName {
			get { return this.ContactForDisplay.displayName; }
		}

		public Contact ContactForDisplay {
			// the contact used for display in UI
			get {
				if(this.Contacts.Count > 1) {
					foreach(Contact c in this.Contacts) {
						if (c.preferred)
							return c;
					}
				}

				return this.Contacts [0]; 
			}
		}

		public void RemoveTempContacts () {
			IList<Contact> listExcludingTempContacts = new List<Contact> ();
			foreach (Contact c in this.Contacts) {
				if (!c.tempContact.Value)
					listExcludingTempContacts.Add (c);
			}

			this.Contacts = listExcludingTempContacts;
		}
	}
}