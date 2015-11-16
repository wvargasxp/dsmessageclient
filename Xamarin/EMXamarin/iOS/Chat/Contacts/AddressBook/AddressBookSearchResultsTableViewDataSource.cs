using System;
using UIKit;
using em;
using System.Collections.Generic;
using Foundation;
using EMXamarin;

namespace iOS {
	public class AddressBookSearchResultsTableViewDataSource : UITableViewDataSource {
		private ContactSelectionState _state = ContactSelectionState.All;
		public ContactSelectionState State { 
			get { return this._state; } 
			set { this._state = value; }
		}

		readonly IList<AggregateContact> filteredContacts;
		public IList<AggregateContact> FilteredContacts {
			get { return filteredContacts; }
		}

		private ChatEntry ce;
		protected ChatEntry ChatEntry {
			get { return ce; }
			set { ce = value; }
		}

		private WeakReference _controller = null;
		protected AddressBookViewController Controller {
			get { return this._controller != null ? this._controller.Target as AddressBookViewController : null; }
			set { this._controller = new WeakReference (value); }
		}

		public AddressBookSearchResultsTableViewDataSource (AddressBookViewController d, ChatEntry c) {
			this.ChatEntry = c;
			filteredContacts = null;
			this.Controller = d;
		}

		public AddressBookSearchResultsTableViewDataSource (AddressBookViewController d, IList<AggregateContact> queryMatches, ChatEntry c) {
			this.ChatEntry = c;
			filteredContacts = queryMatches;
			this.Controller = d;
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
			var cell = (AddressBookContactTableViewCell)tableView.DequeueReusableCell (AddressBookContactTableViewCell.Key) ?? AddressBookContactTableViewCell.Create ();
			AggregateContact contact = filteredContacts [indexPath.Row];
			cell.Contact = contact;

			AddressBookViewController d = this.Controller;
			if (d != null) {
				cell.Disabled = !d.Shared.ContactCurrentlySelected (contact) &&
					ContactSearchUtil.ShouldDisableContactCell (this.ChatEntry, contact.ContactForDisplay, this.State);
				bool shouldShowCheckbox = d.Shared.ShouldShowCheckboxForContact (contact);
				cell.UpdateCheckBox (shouldShowCheckbox);
			}

			return cell;
		}
	}
}

