using System.Collections.Generic;
using System.Threading;

namespace em {
	public abstract class SharedContactSearchDelegate {
	
		// "John Do[e], James Foste[r], ", where the [enclosed] is stored
		List<int> lastContactPositions = null;
		public List<int> LastContactPositions {
			get { return lastContactPositions; } 
			set { lastContactPositions = value; }
		}

		// "(J)ohn Do[e], (J)ames Foste[r], ", where (enclosed) is the value and [enclosed] is the key
		Dictionary<int, int> lastFirst = null;
		public Dictionary<int, int> LastFirst {
			get { return lastFirst; }
			set { lastFirst = value; }
		}

		// Flag used to delete a contact after highlighting the contact's name.
		bool shouldDeleteContactNextTime = false;
		public bool PossibleDeleteInProgress {
			get { return shouldDeleteContactNextTime; }
			set { shouldDeleteContactNextTime = value; }
		}
			
		// Index that matches a particular contact in a chatentry's contact list.
		int indexToDelete = -1;
		public int IndexToDelete {
			get { return indexToDelete; }
			set { indexToDelete = value; }
		}

		string searchFilter = "";
		public string CurrentSearchFilter {
			get { return searchFilter; }

			set {
				//Debug.WriteLine ("Current search filter is " + value);
				searchFilter = value;
			}
		}

		int ignoreSelectionCounter = 0;
		public bool IgnoreSelectionChange {
			get {
				if (ignoreSelectionCounter != 0)
					return true;
				else
					return false;
			}

			set {
				if (value == false) {
					ignoreSelectionCounter--;
					if (ignoreSelectionCounter < 0)
						ignoreSelectionCounter = 0;
				} else
					ignoreSelectionCounter++;
			}
		}

		int currentInitialPosition = -1;
		public int CurrentInitialPosition {
			get { return currentInitialPosition; }
			set { currentInitialPosition = value; }
		}

		int contactPositionNearCursor = -1;
		public int ContactPositionNearCursor {
			get { return contactPositionNearCursor; }
			set { contactPositionNearCursor = value; }
		}

		int previousCurrentInitialPosition = -1;
		public int PreviousCurrentInitialPosition {
			get { return previousCurrentInitialPosition; }
			set { previousCurrentInitialPosition = value; }
		}

		int previousContactPositionNearCursor = -1;
		public int PreviousContactPositionNearCursor {
			get { return previousContactPositionNearCursor; }
			set { previousContactPositionNearCursor = value; }
		}

		bool debuggingModeFlag = false;
		public bool DebuggingMode {
			get { return debuggingModeFlag; }
			set { debuggingModeFlag = value; }
		}

		#region timer for search on servers
		Timer timer;
		protected Timer Timer {
			get { return timer; }
			set { timer = value; }
		}

		readonly int TIMER_BEFORE_SEARCHING = 1000; // 1 second in milliseconds
		int sequence = 0;
		protected int Sequence {
			get { return sequence; }
			set { sequence = value; }
		}
		#endregion

		protected SharedContactSearchDelegate () {
			this.DebuggingMode = false; // AppModel.DebuggingMode;
		}

		public void PossibleReplaceContact () {
			// This would be case where a user was selected to be deleted and we selected another user to replace them.
			if (this.PossibleDeleteInProgress) {
				this.IndexToDelete = lastContactPositions.IndexOf (this.ContactPositionNearCursor-1);
				RemoveContactAtIndex (this.IndexToDelete);
				this.PossibleDeleteInProgress = false;
			}
		}

		public void UpdateEndPoints () {
			string text = this.CurrentText;
			char[] textArray = text.ToCharArray ();
			int length = textArray.Length;
			int value = 0;
			int key = 0;

			this.LastContactPositions = new List<int> ();
			this.LastFirst = new Dictionary<int, int> ();

			for (int i = 1; i < length; i++) {
				char possibleComma = textArray [i];
				if (possibleComma.Equals (',')) {
					key = i;
					this.LastContactPositions.Add (key);
					this.LastFirst.Add (key, value);
					value = i + 2; // the next beginning contact value is two letters over  ; ex. "James, John"
				}
			}
		}

		public int LastPositionInText () {
			int lastPosition = 0;
			if (this.LastContactPositions != null && this.LastContactPositions.Count != 0)
				lastPosition = this.LastContactPositions [this.LastContactPositions.Count - 1];
			return lastPosition;
		}

		public void PossibleShowHide () {
			if (this.CurrentSearchFilter.Length == 0)
				ShowMainList ();
			else
				ShowContactList ();
		}

		public void TimedSearchForContactsOnServer () {
			this.Sequence++;
			this.Timer = new Timer ((object o) => {
				int state = (int)o;
				if (state == sequence) {
					SearchForContactsOnServer ();
				}
			}, this.Sequence, TIMER_BEFORE_SEARCHING, Timeout.Infinite);
		}

		private void SearchForContactsOnServer () {
			string searchString = this.CurrentSearchFilter;
			if (string.IsNullOrWhiteSpace (searchString))
				return;
			this.AppModel.account.SearchForContact (searchString, (SearchResponseInput response) => {
				List<ContactInput> listContactInputs = response.Contacts;
				bool isAliasResult = response.AliasSearchResult;
				if (listContactInputs != null && listContactInputs.Count > 0) {
					IList<Contact> listOfContacts = new List<Contact> ();
					foreach (ContactInput input in listContactInputs) {
						Contact contact = Contact.FindOrCreateContactAfterSearch (this.AppModel, input);

						// If they were a non-temp contact, we'd already be able to search for them.
						// So we only use contacts from search that are considered temp.
						// This would resolve any duplicates that would show up from a result list (one from contact list, one from server search).
						if (contact.tempContact.Value) {
							listOfContacts.Add (contact);
						}
					}

					if (listOfContacts.Count > 0)
						ReloadContactsAfterSearch (listOfContacts);
				}
			});
		}

		public abstract ApplicationModel AppModel { get; }
		public abstract string CurrentText { get; }
		public abstract void RemoveContactAtIndex(int indexToDelete);
		public abstract void ShowMainList ();
		public abstract void ShowContactList ();
		public abstract void ReloadContactsAfterSearch (IList<Contact> listOfContacts);
	}
}