using System;
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using em;
using EMXamarin;
using System.Linq;

namespace Emdroid {
	public class AddressBookListAdapter : ArrayAdapter<Contact>, IFilterable {
		
		IList<AggregateContact> unfilteredContacts;
		public IList<AggregateContact> UnfilteredContacts { 
			get { return unfilteredContacts; } 
			set { unfilteredContacts = value; }
		} 

		public IList<AggregateContact> ContactList { 
			get { 
				var fragment = (AddressBookFragment)fragmentRef.Target;
				return fragment != null ? fragment.ContactList : null;
			} 

			set { 
				var fragment = (AddressBookFragment)fragmentRef.Target;
				if (fragment != null)
					fragment.ContactList = value; 
			}
		}

		private ContactSelectionState _state = ContactSelectionState.All;
		public ContactSelectionState State {
			get { return this._state; }
			set { 
				ContactSelectionState old = this._state;
				this._state = value;
				if (old != this._state) {
					this.NotifyDataSetChanged ();
				}
			}
		}

		private string SearchQuery { get; set; }
		public IList<Contact> SearchResults { get; set; }

		WeakReference fragmentRef;

		private AddressBookArgs Args { get; set; }

		public AddressBookListAdapter (AddressBookFragment g, int resource, AddressBookArgs args) : base (g.Context, resource, new List<Contact>()) {
			// Adapter requires a list in its constructor, so pass in an empty contact list and then load the contacts.
			fragmentRef = new WeakReference (g);
			this.Args = args;
			LoadContactsAsync ();
		}

		public void LoadContactsAsync () {
			Contact.FindAllContactsWithServerIDsRolledUpAsync (EMApplication.GetInstance().appModel, (IList<AggregateContact> loadedContacts) => {
				this.ContactList = loadedContacts;
				this.UnfilteredContacts = new List<AggregateContact>(this.ContactList);
				this.NotifyDataSetChanged ();
			}, this.Args);
		}

		void UpdateDataSource () {
			this.NotifyDataSetChanged ();
		}

		public void ReloadContactsAndFilter (IList<AggregateContact> listOfContacts, string currentSearchFilter) {
			Contact.FindAllContactsWithServerIDsRolledUpAsync (EMApplication.GetInstance().appModel, (IList<AggregateContact> loadedContacts) => {
				this.ContactList = loadedContacts;
				this.UnfilteredContacts = new List<AggregateContact>(this.ContactList);
				ReloadContactsAfterFiltering (listOfContacts, currentSearchFilter);
			}, this.Args);
		}

		public void UpdateSearchContacts (IList<Contact> listOfContacts, string currentSearchFilter) {
			this.SearchResults = listOfContacts;
			this.SearchQuery = currentSearchFilter; // keep track of the search query
			FilterContacts (currentSearchFilter);
		}

		private void FilterContacts (string query) {
			IList<AggregateContact> filtered = FilterAndReturnContacts (query);
		}

		// Returns sorted list of contacts filtered by query.
		public IList<AggregateContact> FilterAndReturnContacts (string query) {
			// Sort them alphabetically by display name.

			IList<AggregateContact> unfilteredContacts = this.UnfilteredContacts;
			if (unfilteredContacts == null) {
				return new List<AggregateContact> ();
			}

			IList<AggregateContact> sortedResults = unfilteredContacts.OrderBy (c => c.DisplayName).ToList ();

			// Filter out results from local (db/inmemory).
			IList<AggregateContact> localSearchResults = sortedResults;

			// If query is 0, it's empty, no need to search.
			if (query.Length > 0) {
				localSearchResults = ContactSearchUtil.FilterAggregateContactsBySearchQuery (sortedResults, query);
			}

			// We check the original search query that got us a contact from the server with the current query. 
			// If the current query matches, we add the server result contact to the results list.
			// Ex. Type 'Bob' -> Server sends back Bob contact. Current SearchQuery is 'Bob'.
			// Now if we backspace, the query is 'Bo'. Since the current SearchQuery is 'Bob', it matches and we add the result in.
			// We do this because we want to automatically add any server contacts returned from the server (as in don't apply a filter to it).
			// At the same time, we need to preserve the last server contact returned.
			if (query != null && this.SearchQuery != null && this.SearchQuery.Contains (query)) {
				IList<Contact> fromServerResults = this.SearchResults;

				// Add the ones that came from server to results.
				foreach (Contact g in fromServerResults) {
					AggregateContact agg = new AggregateContact (g);
					localSearchResults.Insert (0, agg);
				}
			}

			// Reload and track of we have results or not.
			ReloadForMatches (localSearchResults);

			return sortedResults;
		}

		public void ReloadContactsAfterFiltering (IList<AggregateContact> listOfContacts, string currentSearchFilter) {
			IList<AggregateContact> filteredContacts = ContactSearchUtil.FilterAggregateContactsBySearchQuery (this.UnfilteredContacts, currentSearchFilter);
			foreach (AggregateContact c in listOfContacts) {
				filteredContacts.Add (c);
			}
				
			ReloadForMatches (filteredContacts);
		}

		public void ReloadForMatches (IList<AggregateContact> contacts) {
			EMTask.DispatchMain (() => {
				this.ContactList.Clear ();
				foreach (AggregateContact c in contacts) {
					this.ContactList.Add (c);
				}
					
				this.NotifyDataSetChanged();

				if (contacts.Count == 0) {
					this.HasResults = false;
				} else {
					this.HasResults = true;
				}

				if (this.QueryResultsFinished != null) {
					this.QueryResultsFinished ();
				}
			});
		}

		public AggregateContact ResultFromPosition (int position) {
			// After user makes a selection, take the position, find the contact and return a dictionary with server id.
			AggregateContact contact = this.ContactList [position];
			return contact;
		}

		public override int Count {
			get { return this.ContactList == null ? 0 : this.ContactList.Count; }
		}

		public override long GetItemId(int position) {
			return position;
		}

		public override View GetView (int position, View convertView, ViewGroup parent) {
			View retVal = convertView;
			ContactListViewHolder holder;

			var fragment = (AddressBookFragment)fragmentRef.Target;
			if (fragment == null)
				return retVal;

			if (convertView == null) {
				retVal = LayoutInflater.From (fragment.Context).Inflate (Resource.Layout.contact_entry, parent, false);
				holder = new ContactListViewHolder ();
				holder.DisplayNameTextView = retVal.FindViewById<TextView> (Resource.Id.contactTextView);
				holder.DescriptionTextView = retVal.FindViewById<TextView> (Resource.Id.contactDescriptionView);
				holder.PhotoFrame = retVal.FindViewById<ImageView> (Resource.Id.photoFrame);
				holder.ThumbnailView = retVal.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				holder.AliasIcon = retVal.FindViewById<ImageView> (Resource.Id.aliasIcon);
				holder.ProgressBar = retVal.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
				holder.CheckBox = retVal.FindViewById<CheckBox> (Resource.Id.contactCheckBox);
				retVal.Tag = holder;
			} else {
				holder = (ContactListViewHolder)convertView.Tag;
			}

			holder.Position = position;

			AggregateContact contact = this.ContactList [position];
			Contact contactForDisplay = contact.ContactForDisplay;
			holder.CounterParty = contactForDisplay;

			BitmapSetter.SetThumbnailImage (holder, contactForDisplay, fragment.Context.Resources, holder.ThumbnailView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
			holder.PossibleShowProgressIndicator (contactForDisplay);

			BasicRowColorSetter.SetEven (position % 2 == 0, retVal);

			ChatEntry chatEntry = this.Args.ChatEntry;

			AbstractAddressBookController addressBookController = fragment.Shared;
			if (addressBookController.ContactCurrentlySelected (contact)) {
				retVal.Alpha = 1.0f;
			} else {
				retVal.Alpha = ContactSearchUtil.ShouldDisableContactCell (chatEntry, contactForDisplay, this.State) ? .4f : 1.0f;
			}
				
			holder.PossibleShowAliasIcon (contact.Contacts);

			holder.PossibleHideDescription (contact);

			UpdateCheckboxStateAtCheckbox (position, holder.CheckBox);

			return retVal;
		}

		public void UpdateCheckboxForContact (AggregateContact contact, ListView listView) {
			int index = this.ContactList.IndexOf (contact);
			UpdateCheckboxAtIndex (index, listView);
		}

		public void UpdateCheckboxAtIndex (int index, ListView listView) {
			View view = listView.GetChildAt (index - listView.FirstVisiblePosition);
			if (view == null) return;

			ContactListViewHolder holder = (ContactListViewHolder)view.Tag;
			CheckBox checkBox = null;
			if (holder == null) {
				checkBox = view.FindViewById<CheckBox> (Resource.Id.contactCheckBox);
			} else {
				checkBox = holder.CheckBox;
			}

			UpdateCheckboxStateAtCheckbox (index, checkBox);
		}
			
		private void UpdateCheckboxStateAtCheckbox (int position, CheckBox checkBox) {
			var fragment = (AddressBookFragment)fragmentRef.Target;
			if (fragment == null) {
				return;
			}
			
			AbstractAddressBookController addressBookController = fragment.Shared;
			AggregateContact contact = this.ContactList [position];
			bool shouldShowAsChecked = addressBookController.ShouldShowCheckboxForContact (contact);
			checkBox.Checked = shouldShowAsChecked;
		}

		public override bool IsEnabled (int position) {
			var fragment = (AddressBookFragment)fragmentRef.Target;
			if (fragment == null) return false;

			AbstractAddressBookController addressBookController = fragment.Shared;
			IList<Contact> selectedContacts = addressBookController.SelectedContacts;

			// We never want to disable a row, if it's been selected.
			AggregateContact contact = this.ContactList [position];
			if (addressBookController.ContactCurrentlySelected (contact)) {
				return true;
			}

			Contact contactForDisplay = contact.ContactForDisplay;
			ChatEntry chatEntry = this.Args.ChatEntry;
			bool shouldDisableCell = ContactSearchUtil.ShouldDisableContactCell (chatEntry, contactForDisplay, this.State);
			return !shouldDisableCell;
		}

		protected override void Dispose (bool disposing) {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
			base.Dispose (disposing);
		}

		public bool HasResults { get; set; }

		public Action QueryResultsFinished {
			get { return (this.Filter as AddressBookSearchFilter).QueryResultFinished; }
			set { (this.Filter as AddressBookSearchFilter).QueryResultFinished = value; }
		}

		Filter filter;
		public override Filter Filter {
			get {
				if (filter == null)
					filter = new AddressBookSearchFilter (this);
				return filter;
			}
		}
	}
}