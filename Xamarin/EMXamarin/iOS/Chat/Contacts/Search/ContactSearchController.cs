using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using em;
using Foundation;
using UIKit;

namespace iOS {
	public class ContactSearchController : UIViewController {
		public IList<Contact> Contacts {
			get { 
				IContactSource source = this.Source;
				if (source == null) return null;
				return source.ContactList;
			}

			set { 
				IContactSource source = this.Source;
				if (source == null) return;
				source.ContactList = value;
			}
		}

		public IList<Contact> SearchResults { get; set; }
		private string SearchQuery { get; set; }

		private UITableView contactSearchTableView;
		public UITableView ContactSearchTableView {
			get {
				if (contactSearchTableView == null) {
					contactSearchTableView = new UITableView (new CGRect (0, 0, View.Frame.Width, View.Frame.Height), UITableViewStyle.Plain);
					contactSearchTableView.DataSource = this.ContactSearchResultsTableViewDataSource;
					contactSearchTableView.Delegate = this.ContactSearchResultsTableViewDelegate;
					contactSearchTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
					contactSearchTableView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin;
					contactSearchTableView.Alpha = 0; // hidden at first
				}
				return contactSearchTableView;
			}
		}

		#region filtered datasource | when the user searches for something
		private ContactSearchResultsTableViewDataSource contactSearchResultsTableViewDataSource;
		public ContactSearchResultsTableViewDataSource ContactSearchResultsTableViewDataSource {
			get { return contactSearchResultsTableViewDataSource; }
			set { contactSearchResultsTableViewDataSource = value; }
		}

		private ContactSearchResultsTableViewDelegate contactSearchResultsTableViewDelegate;
		public ContactSearchResultsTableViewDelegate ContactSearchResultsTableViewDelegate {
			get {
				if (contactSearchResultsTableViewDelegate == null)
					contactSearchResultsTableViewDelegate = new ContactSearchResultsTableViewDelegate (this);
				return contactSearchResultsTableViewDelegate;
			}

			set { contactSearchResultsTableViewDelegate = value;}
		}

		#endregion

		WeakDelegateProxy callbackProxy;
		public Action<Contact> ContactSourceCallback { 
			get {
				return callbackProxy.HandleEvent<Contact>;
			}

			set {
				callbackProxy = WeakDelegateProxy.CreateProxy<Contact> (value);
			}
		}

		private AddressBookArgs Args { get; set; }

        private string _lastQuery = string.Empty; // keep track of the last query so we can search with it again if the contact list changes
        private string LastQuery { get { return this._lastQuery; } set { this._lastQuery = value; } }

		private WeakReference contactSource;
		protected IContactSource Source {
			get { return this.contactSource.Target as IContactSource; }
			set { this.contactSource = new WeakReference (value); }
		}

		public ContactSearchController (IContactSource source, CGRect frame, AddressBookArgs args) {
			this.Source = source;
			this.Args = args;
			this.View.Frame = frame;
			this.View.Add (this.ContactSearchTableView);
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		public void UpdateContact(Contact c) {
			UITableView tableView = this.ContactSearchTableView;
			ContactSearchResultsTableViewDataSource dataSource = this.ContactSearchResultsTableViewDataSource;
			if (dataSource == null || dataSource.FilteredContacts == null) return;
			int contactIndex = dataSource.FilteredContacts.IndexOf (c);
			if (contactIndex == -1) {
				return;
			}
			NSIndexPath[] visibleIndices = tableView.IndexPathsForVisibleRows;
			if (visibleIndices == null || visibleIndices.Count () == 0) {
				return;
			}
			int minIndex = visibleIndices[0].Row;
			int maxIndex = visibleIndices[visibleIndices.Length - 1].Row;
			if (contactIndex >= minIndex && contactIndex <= maxIndex) {
				NSIndexPath updateIndexPath = NSIndexPath.FromRowSection (contactIndex, 0);
				((ContactTableViewCell)tableView.CellAt (updateIndexPath)).Contact = c;
			}
		}

		public void LoadContactsAsync (bool filterAfterwards = false) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			Contact.FindAllContactsWithServerIDsAsync (appDelegate.applicationModel, loadedContacts => {
				this.Contacts = loadedContacts; 
				if (filterAfterwards) {
					ShouldReloadForSearchString (this.LastQuery);
				}
			}, this.Args.ExcludeGroups, this.Args.ExcludeTemp);
		}

		#region actual search filtering 
		bool hasResults = false;
		public bool HasResults {
			get { return hasResults; }
			set { hasResults = value; }
		}

		public void ShouldReloadForSearchString (string searchQuery) {
			this.LastQuery = searchQuery;
			FilterContacts (searchQuery);
		}

		public void ReloadForMatches (IList<Contact> queryMatches) {
			this.ContactSearchResultsTableViewDataSource = new ContactSearchResultsTableViewDataSource (queryMatches, this.Args.ChatEntry);
			this.ContactSearchResultsTableViewDelegate = new ContactSearchResultsTableViewDelegate (this);
			this.ContactSearchTableView.Delegate = this.ContactSearchResultsTableViewDelegate;
			this.ContactSearchTableView.DataSource = this.ContactSearchResultsTableViewDataSource;
			this.ContactSearchTableView.ReloadData ();
		}

		public void UpdateSearchContacts (IList<Contact> listOfContacts, string currentSearchFilter) {
			this.SearchResults = listOfContacts;
			this.SearchQuery = currentSearchFilter;
			FilterContacts (currentSearchFilter);
		}

		private void FilterContacts (string query) {
			// If query is 0, it's empty, no need to search.
			if (query.Length <= 0) {
				return;
			}

			// Sort them alphabetically by display name.
			IList<Contact> contacts = this.Contacts;
			if (contacts == null) return;

			IList<Contact> sortedResults = contacts.OrderBy (c => c.displayName).ToList ();

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

			if (localSearchResults.Count == 0) {
				this.HasResults = false;
			} else {
				this.HasResults = true;
			}

			// Reload and track of we have results or not.
			EMTask.DispatchMain (() => {
				ReloadForMatches (localSearchResults);
			});
		}
		#endregion

	}
}

