using System;
using UIKit;
using Foundation;
using em;

namespace iOS {
	public class ContactSearchResultsTableViewDelegate : UITableViewDelegate {
		private WeakReference controllerRef;
		private ContactSearchController ContactSearchController {
			get { return controllerRef.Target as ContactSearchController; }
			set {
				controllerRef = new WeakReference (value);
			}
		}

		public ContactSearchResultsTableViewDelegate (ContactSearchController c) {
			this.ContactSearchController = c;
		}

		public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
			return iOS_Constants.APP_CELL_ROW_HEIGHT;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
			ContactSearchController controller = this.ContactSearchController;
			if (controller != null) {
				ContactSearchResultsTableViewDataSource dataSource = controller.ContactSearchResultsTableViewDataSource;
				Contact contact = dataSource.FilteredContacts [indexPath.Row];
				controller.ContactSourceCallback (contact);
			}
		}
	}
}

