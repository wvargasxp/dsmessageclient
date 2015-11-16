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
using System.Windows.Shapes;

namespace WindowsDesktop.Utility {
	/// <summary>
	/// Interaction logic for BasicWindow.xaml
	/// </summary>
	public partial class BasicWindow : Window {
		public BasicWindow (Page page) {
			InitializeComponent ();
			this.Content = page;
		}
	}
}
