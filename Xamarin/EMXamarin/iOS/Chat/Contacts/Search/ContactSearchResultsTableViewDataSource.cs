using System;
using UIKit;
using em;
using System.Collections.Generic;
using Foundation;
using EMXamarin;

namespace iOS {
	public class ContactSearchResultsTableViewDataSource : UITableViewDataSource {
		private IList<Contact> filteredContacts;
		public IList<Contact> FilteredContacts {
			get { return filteredContacts; }
		}

		private ChatEntry ce;
		protected ChatEntry ChatEntry {
			get { return ce; }
			set { ce = value; }
		}

		public ContactSearchResultsTableViewDataSource (ChatEntry c) {
			this.ChatEntry = c;
			filteredContacts = null;
		}

		public ContactSearchResultsTableViewDataSource (IList<Contact> queryMatches, ChatEntry c) {
			this.ChatEntry = c;
			filteredContacts = queryMatches;
		}

		public override nint NumberOfSections (UITableView tableView) {
			return 1;
		}

		public override nint RowsInSection (UITableView tableView, nint section) {
			if (this.FilteredContacts != null)
				return this.FilteredContacts.Count;
			return 0;
		}

		public override String[] SectionIndexTitles (UITableView tableView) {
			return null;
		}

		public override string TitleForHeader (UITableView tableView, nint section) {
			return null;
		}

		public override nint SectionFor (UITableView tableView, string title, nint atIndex) {
			return 0;
		}

		public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
			var cell = (ContactTableViewCell)tableView.DequeueReusableCell (ContactTableViewCell.Key) ?? ContactTableViewCell.Create ();
			Contact contact = filteredContacts [indexPath.Row];
			cell.Contact = contact;
			cell.Disabled = ContactSearchUtil.ShouldDisableContactCell (this.ChatEntry, contact);
			return cell;
		}


	}
}

