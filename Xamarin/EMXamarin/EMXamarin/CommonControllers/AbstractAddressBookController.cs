using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace em {
	public abstract class AbstractAddressBookController {

		public AggregateContact PopupContact { get; set; } // The current contextual contact when an aggregate contact multi select spinner pops up.

		private IList<Contact> _selectedContacts = null;
		public IList<Contact> SelectedContacts { 
			get {
				return this._selectedContacts ?? (this._selectedContacts = new List<Contact> ());
			}
		}

		public string SelectedContactDisplay {
			get {
				string b = string.Empty;
				int count = this.SelectedContacts.Count;
				for (int i = 0; i < count; i++) {
					Contact c = this.SelectedContacts [i];
					b += c.displayName + ", ";
				}

				return b;
			}
		}

		readonly ApplicationModel appModel;

		public abstract void DidDownloadThumbnail (Contact contact);
		public abstract void DidChangeThumbnail (Contact contact);

		WeakDelegateProxy didChangeColorThemeProxy;

		IList<AggregateContact> contactList;
		public IList<AggregateContact> ContactList {
			get { return contactList; }
			set {
				NotificationCenter.DefaultCenter.RemoveObserver (this);
				NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_ProcessedDifferentContacts, ContactsManagerDidChangeContacts);

				contactList = value;

				NotificationCenter.DefaultCenter.AddAssociatedObserver (null, Constants.Counterparty_DownloadCompleted, (Notification n) => {
					var extra = (Dictionary<string, CounterParty>)n.Extra;
					CounterParty counterpaty = extra [Constants.Counterparty_CounterpartyKey];
					var c = counterpaty as Contact;
					if ( c != null )
						EMTask.DispatchMain (() => DidDownloadThumbnail (c));
				}, this);
				NotificationCenter.DefaultCenter.AddAssociatedObserver (null, Constants.Counterparty_ThumbnailChanged, (Notification z) => {
					var extra = (Dictionary<string, CounterParty>)z.Extra;
					CounterParty counterpaty = extra [Constants.Counterparty_CounterpartyKey];
					var c = counterpaty as Contact;
					if ( c != null )
						EMTask.DispatchMain (() => DidChangeThumbnail (c));
				}, this);

				NotificationCenter.DefaultCenter.AddAssociatedObserver (null, Constants.Counterparty_DownloadFailed, (Notification ss) => {
					CounterParty counterparty = ss.Source as CounterParty;
					Contact c = counterparty as Contact;
					if (c != null) {
						EMTask.DispatchMain (() => {
							DidChangeThumbnail (c);
						});
					}
				}, this);
			}
		}

		protected AbstractAddressBookController (ApplicationModel theAppModel) {
			appModel = theAppModel;
			didChangeColorThemeProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (DidChangeColorTheme);

			appModel.account.accountInfo.DelegateDidChangeColorTheme += didChangeColorThemeProxy.HandleEvent<CounterParty>;
		}

		~AbstractAddressBookController () {
			Dispose (true);
		}

		bool hasDisposed = false;
		public void Dispose(bool disposing) {
			if (!hasDisposed) {
				NotificationCenter.DefaultCenter.RemoveObserver (this);
				appModel.account.accountInfo.DelegateDidChangeColorTheme -= didChangeColorThemeProxy.HandleEvent<CounterParty>;

				hasDisposed = true;
			}
		}
			
		public abstract void DidChangeColorTheme ();
		public abstract void ReloadContactSearchContacts ();
		public abstract void GoToAggregateSelection (AggregateContact contact, bool[] chosen);
		public abstract void FinishSelectingContact (AddressBookSelectionResult result);
		public abstract void UpdateContactSelectionStateRows (ContactSelectionState state);
		public abstract void UpdateToField ();

		public void SetInitialSelectedContacts (IList<Contact> contacts) {
			this._selectedContacts = contacts;
		}

		protected void DidChangeColorTheme(CounterParty accountInfo) {
			EMTask.DispatchMain (DidChangeColorTheme);
		}

		private void ContactsManagerDidChangeContacts (Notification n) {
			ReloadContactSearchContacts ();
		}

		public void HandleAggregateContactSelected (AggregateContact contact) {
			Contact c = null;
			if (contact.SingleContact) {
				c = contact.Contacts [0];
			} else if (contact.SinglePreferredContact) {
				c = contact.PreferredContact;
			} else {
				this.PopupContact = contact;

				IList<Contact> contacts = contact.Contacts;
				int count = contacts.Count;
				bool[] chosen = new bool[count];
				for (int i = 0; i < count; i++) {
					Contact x = contacts [i];
					if (this.SelectedContacts.Contains (x)) {
						chosen [i] = true;
					} else {
						chosen [i] = false;
					}
				}

				GoToAggregateSelection (this.PopupContact, chosen);
				return; // \\
			}

			bool contactIsChecked = true;
			if (this.SelectedContacts.Contains (c)) {
				contactIsChecked = false;
			}

			UpdateSelectedContactsList (c, contactIsChecked);
			UpdateToField ();
		}

		public void HandleSelectionFromPopupSpinner (bool[] selected) {
			IList<Contact> contacts = this.PopupContact.Contacts;
			int count = contacts.Count;
			for (int i = 0; i < count; i++) {
				bool isSelected = selected [i];
				Contact contact = contacts [i];
				UpdateSelectedContactsList (contact, isSelected);  
			}

			UpdateToField ();
		}

		private void UpdateSelectedContactsList (Contact contact, bool contactIsChecked) {
			if (contactIsChecked) {
				if (!this.SelectedContacts.Contains (contact)) {
					this.SelectedContacts.Add (contact);
				}
			} else {
				if (this.SelectedContacts.Contains (contact)) {
					this.SelectedContacts.Remove (contact);
				}
			}

			// When a contact is added or removed from the current list of selected contacts,
			// we want to update which rows are selectable. 
			// For example, if a group is selected, we'd want to disallow other contact selections.
			UpdateContactSelectionState ();
		}
			
		public void UpdateContactSelectionState () {
			ContactSelectionState state = ContactSelectionState.All;

			foreach (Contact contact in this.SelectedContacts) {
				if (contact.IsAGroup) {
					state = ContactSelectionState.NoSelection;
					Debug.Assert (this.SelectedContacts.Count == 1, "There should only be one selected contact is that contact is a Group contact.");
				} else {
					state = ContactSelectionState.NoGroups; 
				}
			}

			UpdateContactSelectionStateRows (state);
		}

		public void FinishContactSelection () {
			AddressBookSelectionResult result = new AddressBookSelectionResult (this.SelectedContacts);
			FinishSelectingContact (result);
		}

		public bool ContactCurrentlySelected (AggregateContact contact) {
			// We never want to disable a row, if it's been selected.
			foreach (Contact x in contact.Contacts) {
				if (this.SelectedContacts.Contains (x)) {
					return true;
				}
			}

			return false;
		}

		public bool ShouldShowCheckboxForContact (AggregateContact contact) {
			IList<Contact> selectedContacts = this.SelectedContacts;

			bool hasOneSelectedContact = false;
			foreach (Contact c in contact.Contacts) {
				if (selectedContacts.Contains (c)) {
					hasOneSelectedContact = true;
				}
			}

			return hasOneSelectedContact;
		}
	}
}