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

namespace WindowsDesktop.Contacts {
	/// <summary>
	/// Interaction logic for AddressBookPage.xaml
	/// </summary>
	public partial class AddressBookPage : Page {

		public delegate void DelegateDidSelectContact(Contact contact);
		public DelegateDidSelectContact DelegateContactSelected = delegate {};

		private SharedAddressBookController Shared { get; set; }
		private AddressBookArgs Args { get; set; }

		public AddressBookPage (AddressBookArgs args) {
			InitializeComponent ();
			this.Args = args;
			this.Shared = new SharedAddressBookController (App.Instance.Model, this);
			LoadContactsAsync ();
		}

		public void LoadContactsAsync () {
			Contact.FindAllContactsWithServerIDsRolledUpAsync (App.Instance.Model, (IList<AggregateContact> loadedContacts) => {
				this.Shared.ContactList = loadedContacts;
				UpdateDatasource ();
			}, this.Args);
		}

		public void UpdateDatasource () {
			this.ListView.ItemsSource = AddressBookItemViewModel.From (this.Shared.ContactList).Items;
			this.ListView.Items.Refresh ();
		}

		#region xaml event handlers
		private void ListView_SelectionChanged (object sender, SelectionChangedEventArgs e) {

		}

		private void ListView_MouseDoubleClick (object sender, MouseButtonEventArgs e) {
			int index = this.ListView.SelectedIndex;
			AggregateContact contact = this.Shared.ContactList [index];
			this.Shared.HandleAggregateContactSelected (contact);
		}
		#endregion

		#region SharedAddressBookController
		public void GoToAggregateSelection (AggregateContact contact) {
			MessageBox.Show ("Aggregate contact selected. TODO"); // todo
		}

		public void FinishSelectingContact (Contact contact) {
			DelegateContactSelected (contact);
			Window window = Window.GetWindow (this);
			window.Close ();
		}
		#endregion
	}


	class SharedAddressBookController : AbstractAddressBookController {
		private WeakReference _r;
		private AddressBookPage Self {
			get { return this._r != null ? this._r.Target as AddressBookPage : null;  }
			set { this._r = new WeakReference (value); }
		}

		public SharedAddressBookController (ApplicationModel m, AddressBookPage p) : base (m) {
			this.Self = p;
		}

		public override void DidDownloadThumbnail (Contact contact) {
			AddressBookPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void DidChangeThumbnail (Contact contact) {
			AddressBookPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void DidChangeColorTheme () {
			AddressBookPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void ReloadContactSearchContacts () {
			AddressBookPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void GoToAggregateSelection (AggregateContact contact, bool[] chosen) {
			return; // todo
		}

		public override void FinishSelectingContact (AddressBookSelectionResult result) {
			return; // todo
		}

		public override void UpdateContactSelectionStateRows (ContactSelectionState state) {
			return; // todo
		}

		public override void UpdateToField () {
			return; // todo
		}
	}
}
