using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WindowsDesktop.Notifications {
	/// <summary>
	/// Interaction logic for NotificationPage.xaml
	/// </summary>
	public partial class NotificationPage : Page {

		private SharedNotificationController Shared { get; set; }

		public NotificationPage () {
			InitializeComponent ();
			this.Shared = new SharedNotificationController (App.Instance.Model, this);
			UpdateDatesource ();
		}

		public void UpdateDatesource () {
			IList<NotificationEntry> entries = this.Shared.notificationList.Entries;
			this.ListView.ItemsSource = NotificationItemViewModel.From (entries).Items;
			this.ListView.Items.Refresh (); // todo
		}
	}

	class SharedNotificationController : AbstractNotificationController {
		private WeakReference _r = null;
		private NotificationPage Self {
			get { return this._r != null ? this._r.Target as NotificationPage : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedNotificationController (ApplicationModel m, NotificationPage p) : base (m) {
			this.Self = p;
		}

		public override void HandleUpdatesToNotificationList (IList<MoveOrInsertInstruction<NotificationEntry>> repositionItems, IList<ChangeInstruction<NotificationEntry>> previewUpdates, bool animated, Action<bool> callback) {
			NotificationPage self = this.Self;
			if (self == null) return;
		}

		public override void DidChangeColorTheme () {
			NotificationPage self = this.Self;
			if (self == null) return;
		}
	}
}
