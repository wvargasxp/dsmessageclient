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

namespace WindowsDesktop.Account {
	/// <summary>
	/// Interaction logic for AccountPage.xaml
	/// </summary>
	public partial class AccountPage : Page {

		private SharedAccountController Shared { get; set; }
		public AccountPage (bool onboarding) {
			InitializeComponent ();
			this.IU.AliasIconButton.Visibility = System.Windows.Visibility.Collapsed;

			this.Shared = new SharedAccountController (App.Instance.Model, this);
			this.Shared.IsOnboarding = onboarding;
			UpdateThumbnail ();
			SetInitialNameText ();
		}

		private void HandleWindowClosing (object sender, System.ComponentModel.CancelEventArgs e) { }

		private void NameTextBox_TextChanged (object sender, TextChangedEventArgs e) {
			this.Shared.TextInDisplayNameField = this.IU.NameTextBox.Text;
		}

		private void ColorThemeButton_Click (object sender, RoutedEventArgs e) {
			MessageBox.Show ("Color theme button clicked.");
		}

		private void UpdateThumbnail () {
			BitmapImage bm = ImageManager.Shared.GetImage (this.Shared.AccountInfo);
			this.IU.ThumbnailImage.Source = bm;
		}

		private void SetInitialNameText () {
			string newNameText = string.Empty;

			if (!this.Shared.IsOnboarding) {
				AccountInfo accountInfo = this.Shared.AccountInfo;
				if (accountInfo != null) {
					string displayName = accountInfo.displayName;
					if (displayName != null) {
						newNameText = displayName;
					}
				}
			}

			this.Shared.TextInDisplayNameField = newNameText;
			this.IU.NameTextBox.Text = this.Shared.TextInDisplayNameField;
		}

		#region SharedAccountController 
		public void DidChangeThumbnailMedia () {
			UpdateThumbnail ();
		}

		public void DidDownloadThumbnail () {
			UpdateThumbnail ();
		}

		public void DidChangeColorTheme () {
			return; // todo
		}

		public void DidChangeDisplayName () {
			this.IU.NameTextBox.Text = this.Shared.DisplayName;
		}

		public void DismissAccountController () {
			if (this.Shared.IsOnboarding) {
				MainWindow mainWindow = new MainWindow ();
				mainWindow.Show ();
			} 

			Window w = Window.GetWindow (this);
			w.Close ();
		}

		public void DisplayBlankTextInDisplayAlert () {
			return; // todo
		}
		#endregion

		private void SaveButton_Click (object sender, RoutedEventArgs e) {
			this.Shared.TrySaveAccount ();
		}
	}

	class SharedAccountController : AbstractAccountController {
		private WeakReference _r = null;
		private AccountPage Self {
			get { return this._r != null ? this._r.Target as AccountPage : null;  }
			set { this._r = new WeakReference (value); }
		}

		public SharedAccountController (ApplicationModel appModel, AccountPage p) : base (appModel) {
			this.Self = p;
		}

		public override void DidChangeThumbnailMedia () {
			AccountPage self = this.Self;
			if (self != null) {
				self.DidChangeThumbnailMedia ();
			}
		}

		public override void DidDownloadThumbnail () {
			AccountPage self = this.Self;
			if (self != null) {
				self.DidDownloadThumbnail ();
			}
		}

		public override void DidChangeColorTheme () {
			AccountPage self = this.Self;
			if (self != null) {
				self.DidChangeColorTheme ();
			}
		}

		public override void DidChangeDisplayName () {
			AccountPage self = this.Self;
			if (self != null) {
				self.DidChangeDisplayName ();
			}
		}

		public override void DismissAccountController () {
			AccountPage self = this.Self;
			if (self != null) {
				self.DismissAccountController ();
			}
		}

		public override void DisplayBlankTextInDisplayAlert () {
			AccountPage self = this.Self;
			if (self != null) {
				self.DisplayBlankTextInDisplayAlert ();
			}
		}
	}
}
