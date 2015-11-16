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
	/// Interaction logic for FromAliasComboBox.xaml
	/// </summary>
	public partial class FromAliasComboBox : UserControl {
		public event SelectionChangedEventHandler FromComboBoxSelectionChanged;

		public FromAliasComboBox () {
			InitializeComponent ();
		}

		private void FromComboxBox_SelectionChanged (object sender, SelectionChangedEventArgs e) {
			if (FromComboBoxSelectionChanged != null) {
				FromComboBoxSelectionChanged (this, e);
			}
		}
	}
}
