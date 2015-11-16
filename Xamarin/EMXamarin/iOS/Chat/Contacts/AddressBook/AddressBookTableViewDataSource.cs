using System;
using System.Collections.Generic;
using UIKit;
using em;
using Foundation;
using EMXamarin;

namespace iOS {
    public class AddressBookTableViewDataSource : UITableViewDataSource {
        WeakReference controllerRef;
        public List<String> headers { get; set; }
        public List<int[]> headerBoundaries { get; set; }

        private ChatEntry ce;
        protected ChatEntry ChatEntry {
            get { return ce; }
            set { ce = value; }
        }

		private ContactSelectionState _state = ContactSelectionState.All;
		public ContactSelectionState State { 
			get { return this._state; } 
			set { this._state = value; }
		} 

        public AddressBookTableViewDataSource (AddressBookViewController g, ChatEntry c) {
            controllerRef = new WeakReference (g);
            this.ChatEntry = c;
        }

        public override nint NumberOfSections (UITableView tableView) {
            headers = new List<string> ();
            headerBoundaries = new List<int[]> ();

            AddressBookViewController controller = (AddressBookViewController)controllerRef.Target;
            if (controller != null) {
                Contact.GetHeaderGroupsForRolledUpContactList (controller.ContactList, (headersList, headerBoundariesList) => {
                    headers = headersList;
                    headerBoundaries = headerBoundariesList;
                });
            }

            return headers == null || headers.Count == 0 ? 1 : headers.Count;
        }

        public override nint RowsInSection (UITableView tableView, nint section) {
            AddressBookViewController controller = (AddressBookViewController)controllerRef.Target;
            if (controller != null) {
                if (controller.ContactList != null)
                    return headerBoundaries.Count == 0 ? controller.ContactList.Count : headerBoundaries [(int)section] [1] - headerBoundaries [(int)section] [0] + 1;
            }

            return 0;
        }

        public override String[] SectionIndexTitles (UITableView tableView) {
            return headers.ToArray ();
        }

        public override string TitleForHeader (UITableView tableView, nint section) {
            return null; //headers.Count == 0? null : headers [section]; // hardcoding headers to null so we don't see them
        }

        public override nint SectionFor (UITableView tableView, string title, nint atIndex) {
            return headers.IndexOf (title);
        }

        public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
			var cell = (AddressBookContactTableViewCell)tableView.DequeueReusableCell (AddressBookContactTableViewCell.Key) ?? AddressBookContactTableViewCell.Create ();

            AddressBookViewController controller = (AddressBookViewController)controllerRef.Target;
            if (controller != null) {
                AggregateContact contact = headers.Count == 0 ? controller.ContactList [indexPath.Row] : controller.ContactList [headerBoundaries [indexPath.Section] [0] + indexPath.Row];

                cell.Contact = contact;
                // Logic to set the odd/even row colors. 
                // If there are headers to account for, use the headerboundaries to figure out what index it is relative to the contacts list itself.
                if (headers != null && headers.Count == 0)
                    cell.SetEvenRow (indexPath.Row % 2 == 0);
                else
                    cell.SetEvenRow ((headerBoundaries [indexPath.Section] [0] + indexPath.Row) % 2 == 0);

				// we can use any contact in the aggregate contact list because an aggregated contact wouldn't have a group + an addressbook contact together

				cell.Disabled = !controller.Shared.ContactCurrentlySelected (contact) &&
					ContactSearchUtil.ShouldDisableContactCell (this.ChatEntry, contact.ContactForDisplay, this.State);

				bool shouldShowCheckbox = controller.Shared.ShouldShowCheckboxForContact (contact);
				cell.UpdateCheckBox (shouldShowCheckbox);
            }

            return cell;
        }
    }
}

