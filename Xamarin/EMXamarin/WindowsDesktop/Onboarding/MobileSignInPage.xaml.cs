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
using EMXamarin;
using em;
using System.Diagnostics;

namespace WindowsDesktop.Onboarding {
	/// <summary>
	/// Interaction logic for MobileSignInPage.xaml
	/// </summary>
	public partial class MobileSignInPage : Page {

		private const string Tag = "MobileSignInPage:";

		private MobileSignInModel _model = null;
		private MobileSignInModel Model {
			get {
				if (this._model == null) {
					this._model = new MobileSignInModel ();
					this._model.ShouldPauseUI = () => { };
					this._model.ShouldResumeUI = () => { };
					this._model.DidFailToRegister = HandleRegistrationFailure;
				}

				return this._model;
			}
		}
		
		public MobileSignInPage () {
			InitializeComponent ();
		}

		private void HandleRegistrationFailure () {
			Debug.WriteLine ("");
		}

		private void HandleRegistrationComplete (string accountId) {
			Page p = new MobileVerificationPage (accountId);
			this.NavigationService.Navigate (p);
		}

		private void ContinueButton_Click (object sender, RoutedEventArgs e) {
			string mobileNumber = this.MobileNumberTextField.Text;
			string prefix = "1"; // This one is right. Hardcoding to 1 for now. this.PhonePrefix;
			string countryCode = "us";
			ApplicationModel appModel = App.Instance.Model;
			this.Model.Register (appModel.account, mobileNumber, countryCode, prefix, HandleRegistrationComplete);
		}

		private void MobileNumberTextField_TextChanged (object sender, TextChangedEventArgs e) {
		}
	}
}
