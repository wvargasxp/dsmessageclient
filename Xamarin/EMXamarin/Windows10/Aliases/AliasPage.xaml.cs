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
using WindowsDesktop.Utility;

namespace WindowsDesktop.Aliases {
	/// <summary>
	/// Interaction logic for AliasPage.xaml
	/// </summary>
	public partial class AliasPage : Page {

		private SharedAliasController Shared { get; set; }

		public AliasPage () {
			InitializeComponent ();

			this.Shared = new SharedAliasController (App.Instance.Model, this);
			this.ListView.ItemsSource = AliasItemViewModel.From (this.Shared.Aliases).Items;
		}

		public void UpdateDatasource () {
			EMTask.DispatchMain (() => {
				this.ListView.ItemsSource = AliasItemViewModel.From (this.Shared.Aliases).Items;
				this.ListView.Items.Refresh (); // todo
			});
		}

		#region xaml event handlers
		private void ListView_SelectionChanged (object sender, SelectionChangedEventArgs e) {

		}

		private void ListView_MouseDoubleClick (object sender, MouseButtonEventArgs e) {
			int index = this.ListView.SelectedIndex;
			AliasInfo alias = this.Shared.Aliases[index];
			EditAliasPage page = new EditAliasPage (alias.serverID);
			BasicWindow window = new BasicWindow (page);
			window.Show ();
		}
		#endregion
	}

	class SharedAliasController : AbstractAliasController {

		private WeakReference _r = null;
		private AliasPage Self {
			get { return this._r != null ? this._r.Target as AliasPage : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedAliasController (ApplicationModel appModel, AliasPage g) : base (appModel) {
			this.Self = g;
		}

		public override void DidChangeAliasList () {
			AliasPage self = this.Self;
			if (self == null) return;
			self.UpdateDatasource ();
		}

		public override void DidChangeColorTheme () {
			return; // todo
		}

		public override void DidChangeThumbnailMedia () {
			AliasPage self = this.Self;
			if (self == null) return;

			self.UpdateDatasource ();
		}

		public override void DidDownloadThumbnail () {
			AliasPage self = this.Self;
			if (self == null) return;
			self.UpdateDatasource ();
		}

		public override void DidChangeIconMedia () {
			AliasPage self = this.Self;
			if (self == null) return;
			return; // todo
		}

		public override void DidDownloadIcon () {
			AliasPage self = this.Self;
			if (self == null) return;
			return; // todo
		}

		public override void DidUpdateLifecycle () {
			AliasPage self = this.Self;
			if (self == null) return;
			return; // todo
		}
	}
}
