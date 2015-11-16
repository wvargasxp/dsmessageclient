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

namespace WindowsDesktop.UserControls {
	/// <summary>
	/// Interaction logic for AccountControl.xaml
	/// </summary>
	public partial class AccountControl : UserControl {

		/// <example>
		/// <controls:AccountControl
        /// ColorThemeButtonClick="ColorThemeButton_Click"
        /// NameTextBoxTextChanged="NameTextBox_TextChanged"/>
		/// </example>
		public event RoutedEventHandler ColorThemeButtonClick;
		public event TextChangedEventHandler NameTextBoxTextChanged;
		public event RoutedEventHandler AliasIconButtonClick;

		public AccountControl () {
			InitializeComponent ();
		}

		private void ColorThemeButton_Click (object sender, RoutedEventArgs e) {
			if (this.ColorThemeButtonClick != null) {
				this.ColorThemeButtonClick (this, e);
			}
		}

		private void NameTextBox_TextChanged (object sender, TextChangedEventArgs e) {
			if (this.NameTextBoxTextChanged != null) {
				this.NameTextBoxTextChanged (this, e);
			}
		}

		private void AliasIconButton_Click (object sender, RoutedEventArgs e) {
			if (this.AliasIconButtonClick != null) {
				this.AliasIconButtonClick (this, e);
			}
		}
	}
}
