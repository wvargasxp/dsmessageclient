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
using WindowsDesktop.Utility;
using em;

namespace WindowsDesktop.Groups {
	/// <summary>
	/// Interaction logic for GroupsPage.xaml
	/// </summary>
	public partial class GroupsPage : Page {

		private SharedGroupsController Shared { get; set; }
		public GroupsPage () {
			InitializeComponent ();
			this.Shared = new SharedGroupsController (App.Instance.Model, this);
			UpdateDatasource ();
		}

		public void UpdateDatasource () {
			this.ListView.ItemsSource = GroupItemViewModel.From (this.Shared.Groups).Items;
			this.ListView.Items.Refresh ();
		}

		#region xaml event handlers
		private void ListView_SelectionChanged (object sender, SelectionChangedEventArgs e) {

		}

		private void ListView_MouseDoubleClick (object sender, MouseButtonEventArgs e) {
			int index = this.ListView.SelectedIndex;
			Group group = this.Shared.Groups [index];
			EditGroupPage page = new EditGroupPage (true, group);
			BasicWindow window = new BasicWindow (page);
			window.Show ();
		}
		#endregion
	}

	class SharedGroupsController : AbstractGroupsController {
		private WeakReference _r = null;
		private GroupsPage Self {
			get { return this._r != null ? this._r.Target as GroupsPage : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedGroupsController (ApplicationModel appModel, GroupsPage p) : base (appModel) {
			this.Self = p;
		}

		public override void GroupsValuesDidChange () {
			GroupsPage self = this.Self;
			if (self == null) return;

			// todo
			self.UpdateDatasource ();
		}

		public override void ReloadGroup (Contact group) {
			GroupsPage self = this.Self;
			if (self == null) return;

			// todo
			self.UpdateDatasource ();
		}

		public override void TransitionToChatController (ChatEntry chatEntry) {
			GroupsPage self = this.Self;
			if (self == null) return;

			// todo
		}

		public override void DidChangeColorTheme () {
			GroupsPage self = this.Self;
			if (self == null) return;

			// todo
			self.UpdateDatasource ();
		}
	}
}
