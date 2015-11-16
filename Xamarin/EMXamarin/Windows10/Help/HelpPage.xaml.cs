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

namespace WindowsDesktop.Help {
	/// <summary>
	/// Interaction logic for HelpPage.xaml
	/// </summary>
	public partial class HelpPage : Page {
		public HelpPage () {
			InitializeComponent ();
			this.HelpInfoList = new List<HelpInfo> ();
			UpdateDatasource ();
		}

		private IList<HelpInfo> HelpInfoList { get; set; }

		public void UpdateDatasource () {
			var privacy = new HelpInfo ("Getting Started", "todo");
			HelpInfoList.Add (privacy);

			var eula = new HelpInfo ("Tips", "todo");
			HelpInfoList.Add (eula);

			var faq = new HelpInfo ("Faq", "todo");
			HelpInfoList.Add (faq);

			var support = new HelpInfo ("Support", "todo");
			HelpInfoList.Add (support);

			this.ListView.ItemsSource = HelpItemViewModel.From (this.HelpInfoList).Items;
			this.ListView.Items.Refresh ();
		}
	}

	public class HelpInfo {
		public string Title { get; set; }
		public string Url { get; set; }

		public HelpInfo(string t, string u) {
			this.Title = t;
			this.Url = u;
		}
	}
}
