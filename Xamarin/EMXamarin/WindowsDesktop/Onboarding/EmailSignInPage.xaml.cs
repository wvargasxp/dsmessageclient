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
    /// Interaction logic for EmailSignInPage.xaml
    /// </summary>
    public partial class EmailSignInPage : Page {

		private EmailSignInModel Shared { get; set; }

        public EmailSignInPage() {
            InitializeComponent();
			this.Shared = new EmailSignInModel ();

			this.Shared.DidFailToRegister += () => {
			};

			this.Shared.ShouldPauseUI = () => { };
			this.Shared.ShouldResumeUI = () => { };

        }

		private void EmailTextBox_TextChanged (object sender, TextChangedEventArgs e) {

		}

		private void ContinueButton_Click (object sender, RoutedEventArgs e) {
			// todo
			DidConfirmEmail (); 
		}

		private void DidConfirmEmail() {
			string prefix = "1"; // This one is right. Hardcoding to 1 for now. this.PhonePrefix;
			string countryCode = "us";

			this.Shared.Register (App.Instance.Model.account, this.EmailTextBox.Text, countryCode, prefix, accountID => {
				HandleRegistrationComplete (accountID);
			});
		}

		private void HandleRegistrationComplete (string accountId) {
			Page p = new MobileVerificationPage (accountId);
			this.NavigationService.Navigate (p);
		}
    }
}
