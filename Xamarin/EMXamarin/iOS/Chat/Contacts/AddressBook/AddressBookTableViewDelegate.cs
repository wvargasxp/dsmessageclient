using System;
using em;
using Foundation;
using UIKit;

namespace iOS {
	public class AddressBookTableViewDelegate : UITableViewDelegate {
		private WeakReference _r;
		private AddressBookViewController Controller {
			get { return this._r != null ? this._r.Target as AddressBookViewController : null; }
			set { this._r = new WeakReference (value); }
		}

		public AddressBookTableViewDelegate (AddressBookViewController c) {
			this.Controller = c;
		}

		public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
			return iOS_Constants.APP_CELL_ROW_HEIGHT;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
            var controller = (AddressBookViewController)_r.Target;
			if (controller == null) return;

			var dataSource = (AddressBookTableViewDataSource) controller.MainTableView.DataSource;
			AggregateContact contact = dataSource.headers.Count == 0 ? controller.ContactList [indexPath.Row] : controller.ContactList [dataSource.headerBoundaries [indexPath.Section] [0] + indexPath.Row];
			controller.HandleRowSelected (tableView, indexPath, contact);
			tableView.DeselectRow (indexPath, true);
		}
	}
}