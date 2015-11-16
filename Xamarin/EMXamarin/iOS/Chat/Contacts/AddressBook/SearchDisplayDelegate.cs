using System;
using UIKit;
using CoreGraphics;
using System.Collections.Generic;
using Foundation;
using em;
using EMXamarin;

namespace iOS {
	public class SearchDisplayDelegate : UISearchDisplayDelegate {
		private ChatEntry ce;
		protected ChatEntry ChatEntry {
			get { return ce; }
			set { ce = value; }
		}
			
		WeakReference controllerRef;

		public SearchDisplayDelegate (AddressBookViewController g, ChatEntry c) {
			this.ChatEntry = c;
			controllerRef = new WeakReference (g);
		}

		public override bool ShouldReloadForSearchString (UISearchDisplayController searchDisplayController, string searchQuery) {
            AddressBookViewController controller = (AddressBookViewController)controllerRef.Target;
			if (controller != null) {
				if (searchQuery.Length > 0) {
					IList<AggregateContact> queryMatches = ContactSearchUtil.FilterAggregateContactsBySearchQuery (controller.ContactList, searchQuery);

					controller.SearchSource = new AddressBookSearchResultsTableViewDataSource (controller, queryMatches, this.ChatEntry);
					controller.SearchDelegate = new AddressBookSearchResultsTableViewDelegate (controller);
					searchDisplayController.SearchResultsDataSource = controller.SearchSource;
					searchDisplayController.SearchResultsDelegate = controller.SearchDelegate;
				}

				return true;
			}

			return false;
		}
			
		void setCorrectSearchBarFrames(UISearchDisplayController searchDisplayController) {
			CGRect searchDisplayerFrame = searchDisplayController.SearchResultsTableView.Frame;
			searchDisplayController.SearchResultsTableView.Frame = searchDisplayerFrame;
		}

		public override void WillBeginSearch (UISearchDisplayController searchDisplayController) {
			setCorrectSearchBarFrames (searchDisplayController);
		}

		public override void DidBeginSearch (UISearchDisplayController searchDisplayController) {
			setCorrectSearchBarFrames (searchDisplayController);
		}
	}
}

