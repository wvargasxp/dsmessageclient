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
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Windows10.Onboarding
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MobileSignInPage : Page
    {
        private const string Tag = "MobileSignInPage:";

        private MobileSignInModel _model = null;
        private MobileSignInModel Model
        {
            get
            {
                if (this._model == null)
                {
                    this._model = new MobileSignInModel();
                    this._model.ShouldPauseUI = () => { };
                    this._model.ShouldResumeUI = () => { };
                    this._model.DidFailToRegister = HandleRegistrationFailure;
                }

                return this._model;
            }
        }
        public MobileSignInPage()
        {
            this.InitializeComponent();
        }

        private void HandleRegistrationFailure()
        {
            Debug.WriteLine("");
        }

        private void HandleRegistrationComplete(string accountId)
        {
            Page page = new MobileVerificationPage(accountId);
            this.Frame.Navigate(typeof(MobileSignInPage), page); //this.NavigationService.Navigate(p);
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            string mobileNumber = this.MobileNumberTextField.Text;
            string prefix = "1"; // This one is right. Hardcoding to 1 for now. this.PhonePrefix;
            string countryCode = "us";
            ApplicationModel appModel = App.Instance.Model;
            this.Model.Register(appModel.account, mobileNumber, countryCode, prefix, HandleRegistrationComplete);
        }

        private void MobileNumberTextField_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

    }
}
