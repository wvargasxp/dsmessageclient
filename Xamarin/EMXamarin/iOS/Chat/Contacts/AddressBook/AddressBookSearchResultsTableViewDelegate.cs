using System;
using em;
using Foundation;
using UIKit;

namespace iOS {
	public class AddressBookSearchResultsTableViewDelegate : UITableViewDelegate {
		private WeakReference _r;

		private AddressBookViewController Controller {
			get { return this._r != null ? this._r.Target as AddressBookViewController : null; }
			set { this._r = new WeakReference (value); }
		}

		public AddressBookSearchResultsTableViewDelegate (AddressBookViewController c) {
			this.Controller = c;
		}

		public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
			return iOS_Constants.APP_CELL_ROW_HEIGHT;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
			AddressBookViewController controller = this.Controller;
			if (controller == null) return;
			AddressBookSearchResultsTableViewDataSource dataSource = controller.SearchSource;
			AggregateContact contact = dataSource.FilteredContacts [indexPath.Row];
			controller.HandleRowSelected (tableView, indexPath, contact);
			tableView.DeselectRow (indexPath, true);
		}
	}
}