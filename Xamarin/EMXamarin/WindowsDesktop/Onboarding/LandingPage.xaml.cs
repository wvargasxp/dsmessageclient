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
using em;

namespace WindowsDesktop.Onboarding {
	/// <summary>
	/// Interaction logic for LandingPage.xaml
	/// </summary>
	public partial class LandingPage : Page {
		private LandingPageModel Shared { get; set; }

		public LandingPage () {
			InitializeComponent ();
			this.Shared = new LandingPageModel ();
			this.WebBrowser.Navigate (this.Shared.PreviewViewURL);
		}

		private void MobileButton_Click (object sender, RoutedEventArgs e) {
			MobileSignInPage page = new MobileSignInPage ();
			page.ShowsNavigationUI = true;
			this.NavigationService.Navigate (page);
		}

		private void EmailButton_Click (object sender, RoutedEventArgs e) {
			EmailSignInPage page = new EmailSignInPage ();
			page.ShowsNavigationUI = true;
			this.NavigationService.Navigate (page);
		}
	}
}
