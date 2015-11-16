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
	/// Interaction logic for EditAliasPage.xaml
	/// </summary>
	public partial class EditAliasPage : Page {

		public string AliasServerId { get; set; }
		private SharedEditAliasController Shared { get; set; }

		public EditAliasPage (string aliasServerId) {
			InitializeComponent ();
			this.AliasServerId = aliasServerId;
			this.Shared = new SharedEditAliasController (App.Instance.Model, this);
			this.Shared.SetInitialAlias (this.AliasServerId);

			this.IU.NameTextBox.Text = this.Shared.Alias.displayName; // todo;  null check 
			UpdateImages ();
		}

		public void UpdateImages () {
			UpdateThumbnail ();
			UpdateAliasIcon ();
		}

		private void UpdateThumbnail () {
			BitmapImage bm = ImageManager.Shared.GetImage (this.Shared.Alias);
			this.IU.ThumbnailImage.Source = bm;
		}

		private void UpdateAliasIcon () {
			BitmapImage bm = ImageManager.Shared.GetImageFromMedia (this.Shared.Alias.iconMedia);
			Image img = new Image ();
			img.Source = bm;
			img.VerticalAlignment = System.Windows.VerticalAlignment.Center;
			this.IU.AliasIconButton.Content = img;
		}

		#region event handlers
		private void NameTextBox_TextChanged (object sender, TextChangedEventArgs e) {
			this.Shared.TextInDisplayNameField = this.IU.NameTextBox.Text;
		}

		private void ColorThemeButton_Click (object sender, RoutedEventArgs e) {
			MessageBox.Show ("Color theme button clicked.");
		}

		private void AliasIconButton_Click (object sender, RoutedEventArgs e) {
			MessageBox.Show ("Alias icon button clicked.");
		}
		#endregion
	}

	class SharedEditAliasController : AbstractEditAliasController {
		private WeakReference _ref;
		private EditAliasPage Self {
			get { return this._ref != null ? this._ref.Target as EditAliasPage : null; }
			set { this._ref = new WeakReference (value); }
		}

		public SharedEditAliasController (ApplicationModel m, EditAliasPage p) : base (m) {
			this.Self = p;
		}

		public override void DidChangeColorTheme () {
			EditAliasPage self = this.Self;
			if (self == null) return;
		}

		public override void DidAliasActionFail (string message) {
			EditAliasPage self = this.Self;
			if (self == null) return;
		}

		public override void DidSaveAlias (bool saved) {
			EditAliasPage self = this.Self;
			if (self == null) return;


			Window w = Window.GetWindow (self);
			w.Close ();
		}

		public override void DidDeleteAlias () {
			EditAliasPage self = this.Self;
			if (self == null) return;


			Window w = Window.GetWindow (self);
			w.Close ();
		}

		public override void ConfirmWithUserDelete (string serverID, Action<bool> onCompletion) {
			EditAliasPage self = this.Self;
			if (self == null) return;
		}

		public override void ThumbnailUpdated () {
			return; // todo
		}
	}
}
