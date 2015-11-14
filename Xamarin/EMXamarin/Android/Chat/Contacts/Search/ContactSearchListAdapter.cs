using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class ContactSearchListAdapter : ArrayAdapter<Contact>, IFilterable {
		private IList<Contact> unfilteredContacts = null;
		public IList<Contact> UnfilteredContacts { 
			get { return unfilteredContacts; } 
			set { unfilteredContacts = value; }
		} 

		public IList<Contact> Contacts { 
			get { return this.Source.ContactList; } 
			set { this.Source.ContactList = value; }
		}
			
		private string SearchQuery { get; set; }
		public IList<Contact> SearchResults { get; set; }

		private ChatEntry ce;
		protected ChatEntry ChatEntry {
			get { return ce; }
			set { ce = value; }
		}

		private bool excludeG;
		protected bool ExcludeGroups {
			get { return excludeG; }
			set { excludeG = value; }
		}

		private bool excludeT = true;
		public bool ExcludeTemp {
			get { return excludeT; }
			set { excludeT = value; }
		}

		IContactSource cSource;
		protected IContactSource Source {
			get { return cSource; }
			set { cSource = value; }
		}

		// After filtering with a search query, do we have results?
		bool hasResults = false;
		public bool HasResults {
			get { return hasResults; }
			set { hasResults = value; }
		}
			
		public ContactSearchListAdapter (IContactSource source, int resource, ChatEntry c, bool excludeGroups, bool excludeTemps) : base(source.Context, resource, new List<Contact>()) {
			// Adapter requires a list in its constructor, so pass in an empty contact list and then load the contacts.
			this.Source = source;
			this.ChatEntry = c;
			this.ExcludeGroups = excludeGroups;
			this.ExcludeTemp = excludeTemps;
			FindAllContacts ();
		}

		public void FindAllContacts () {
			Contact.FindAllContactsWithServerIDsAsync (EMApplication.GetInstance().appModel, (IList<Contact> loadedContacts) => {
				this.Contacts = loadedContacts;
				this.UnfilteredContacts = new List<Contact>(this.Contacts);
				this.NotifyDataSetChanged ();
				FilterContacts (string.Empty);

				if (InitialQueryResultsFinished != null) {
					InitialQueryResultsFinished ();
					InitialQueryResultsFinished = null;
				}
			}, this.ExcludeGroups, this.ExcludeTemp);
		}

		private void UpdateDataSource () {
			this.NotifyDataSetChanged ();
		}

		public void UpdateSearchContacts (IList<Contact> listOfContacts, string currentSearchFilter) {
			this.SearchResults = listOfContacts;
			this.SearchQuery = currentSearchFilter; // keep track of the search query
			FilterContacts (currentSearchFilter);
		}

		private void FilterContacts (string query) {
			IList<Contact> filtered = FilterAndReturnContacts (query);
		}

		// Returns sorted list of contacts filtered by query.
		public IList<Contact> FilterAndReturnContacts (string query) {
			// If query is 0, it's empty, no need to search.
			if (query.Length <= 0) {
				return new List<Contact> ();
			}

			IList<Contact> unfilteredContacts = this.UnfilteredContacts;
			if (unfilteredContacts == null) {
				return new List<Contact> ();
			}

			// Sort them alphabetically by display name.
			IList<Contact> sortedResults = unfilteredContacts.OrderBy (c => c.displayName).ToList ();

			// Filter out results from local (db/inmemory).
			IList<Contact> localSearchResults = ContactSearchUtil.FilterContactsBySearchQuery (sortedResults, query);

			// We check the original search query that got us a contact from the server with the current query. 
			// If the current query matches, we add the server result contact to the results list.
			// Ex. Type 'Bob' -> Server sends back Bob contact. Current SearchQuery is 'Bob'.
			// Now if we backspace, the query is 'Bo'. Since the current SearchQuery is 'Bob', it matches and we add the result in.
			// We do this because we want to automatically add any server contacts returned from the server (as in don't apply a filter to it).
			// At the same time, we need to preserve the last server contact returned.
			if (!string.IsNullOrWhiteSpace (this.SearchQuery) && this.SearchQuery.Contains (query)) {
				IList<Contact> fromServerResults = this.SearchResults;

				// Add the ones that came from server to results.
				foreach (Contact g in fromServerResults) {
					localSearchResults.Insert (0, g);
				}
			}

			// Reload and track of we have results or not.
			EMTask.DispatchMain (() => {
				ReloadForMatches (localSearchResults);
			});

			return sortedResults;
		}

		public void ReloadForMatches (IList<Contact> contacts) {
			EMTask.DispatchMain (() => {
				this.Contacts.Clear ();
				foreach (Contact c in contacts) {
					this.Contacts.Add (c);
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

		public Contact ResultFromPosition (int position) {
			// After user makes a selection, take the position, find the contact and return a dictionary with server id.
			Contact contact = this.Contacts [position];
			return contact;
		}

		public override int Count {
			get { return this.Contacts == null ? 0 : this.Contacts.Count; }
		}

		public override long GetItemId(int position) {
			return position;
		}

		public override View GetView (int position, View convertView, ViewGroup parent) {
			View retVal = convertView;
			ContactListViewHolder holder;

			if (convertView == null) {
				retVal = LayoutInflater.From (this.Source.Context).Inflate (Resource.Layout.contact_entry, parent, false);
				holder = new ContactListViewHolder ();
				holder.DisplayNameTextView = retVal.FindViewById<TextView> (Resource.Id.contactTextView);
				holder.DescriptionTextView = retVal.FindViewById<TextView> (Resource.Id.contactDescriptionView);
				holder.PhotoFrame = retVal.FindViewById<ImageView> (Resource.Id.photoFrame);
				holder.ThumbnailView = retVal.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				holder.AliasIcon = retVal.FindViewById<ImageView> (Resource.Id.aliasIcon);
				holder.ProgressBar = retVal.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
				holder.CheckBox = retVal.FindViewById<CheckBox> (Resource.Id.contactCheckBox);
				holder.CheckBox.Visibility = ViewStates.Gone;
				retVal.Tag = holder;
			} else {
				holder = (ContactListViewHolder)convertView.Tag;
			}

			holder.Position = position;

			Contact contact = this.Contacts [position];
			holder.CounterParty = contact;

			BitmapSetter.SetThumbnailImage (holder, contact, this.Source.Context.Resources, holder.ThumbnailView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
			holder.PossibleShowProgressIndicator (contact);

			BasicRowColorSetter.SetEven (position % 2 == 0, retVal);

			retVal.Alpha = ContactSearchUtil.ShouldDisableContactCell (this.ChatEntry, contact) ? .4f : 1.0f;

			holder.PossibleShowAliasIcon (contact);

			return retVal;
		}

		public override bool IsEnabled (int position) {
			Contact contact = this.Contacts [position];
			bool shouldDisableCell = ContactSearchUtil.ShouldDisableContactCell (this.ChatEntry, contact);
			return !shouldDisableCell;
		}

		protected override void Dispose (bool disposing) {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
			base.Dispose (disposing);
		}

		public Action QueryResultsFinished { get; set; }
		public Action InitialQueryResultsFinished { get; set; }

		private Filter filter;
		public override Filter Filter {
			get {
				if (filter == null)
					filter = new ContactSearchFilter (this);
				return filter;
			}
		}
	}
}