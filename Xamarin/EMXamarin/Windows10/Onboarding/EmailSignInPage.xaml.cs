using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using em;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Windows10.Onboarding
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EmailSignInPage : Page
    {
        private EmailSignInModel Shared { get; set; }
        public EmailSignInPage()
        {
            this.InitializeComponent();
            this.Shared = new EmailSignInModel();

            this.Shared.DidFailToRegister += () => {
            };

            this.Shared.ShouldPauseUI = () => { };
            this.Shared.ShouldResumeUI = () => { };
        }

        private void EmailTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            // todo
            DidConfirmEmail();
        }

        private void DidConfirmEmail()
        {
            string prefix = "1"; // This one is right. Hardcoding to 1 for now. this.PhonePrefix;
            string countryCode = "us";

            this.Shared.Register(App.Instance.Model.account, this.EmailTextBox.Text, countryCode, prefix, accountID => {
                HandleRegistrationComplete(accountID);
            });
        }

        private void HandleRegistrationComplete(string accountId)
        {
            Page page = new MobileVerificationPage(accountId);
            this.Frame.Navigate(typeof(MobileSignInPage), page);//this.NavigationService.Navigate(p);
        }
    }
}
