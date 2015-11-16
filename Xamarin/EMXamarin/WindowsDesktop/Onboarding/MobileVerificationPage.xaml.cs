using EMXamarin;
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
using WindowsDesktop.Account;
using WindowsDesktop.Inbox;
using em;

namespace WindowsDesktop.Onboarding {
	/// <summary>
	/// Interaction logic for MobileVerificationPage.xaml
	/// </summary>
	public partial class MobileVerificationPage : Page {
		private SharedVerificationController Shared { get; set; }

		public MobileVerificationPage (string accountId) {
			InitializeComponent ();
			this.Shared = new SharedVerificationController (App.Instance.Model, this);
			this.Shared.AccountID = accountId;
		}

		private void VerificationTextBox_TextChanged (object sender, TextChangedEventArgs e) { }

		#region SharedVerificationController 
		public void ShouldPauseUI () { }

		public void ShouldResumeUI () { }

		private void ContinueButton_Click (object sender, RoutedEventArgs e) {
			string verificationCode = this.VerificationTextBox.Text;
			this.Shared.TryToLogin (verificationCode);
		}

		public void TriggerContinueButton () { }

		public void UpdateTextFieldWithText (string text) {
			this.VerificationTextBox.Text = text;
		}

		public void DisplayAccountError () { }

		public void DismissControllerAndFinishOnboarding () {
			// todo
			MainWindow mainWindow = new MainWindow ();
			mainWindow.Show ();

			Window window = Window.GetWindow (this);
			window.Close ();
		}

		public void GoToAccountController () {
			AccountPage page = new AccountPage (true);
			this.NavigationService.Navigate ((page));
		}
		#endregion
	}

	class SharedVerificationController : AbstractMobileVerificationController {
		private WeakReference _r = null;
		private MobileVerificationPage Self {
			get {  return this._r != null ? this._r.Target as MobileVerificationPage : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedVerificationController (ApplicationModel g, MobileVerificationPage t) : base (g) {
			this.Self = t;
		}

		public override void ShouldPauseUI () {
			MobileVerificationPage c = this.Self;
			if (c == null) return;
			c.ShouldPauseUI ();
		}

		public override void ShouldResumeUI () {
			MobileVerificationPage c = this.Self;
			if (c == null) return;
			c.ShouldResumeUI ();
		}

		public override void UpdateTextFieldWithText (string text) {
			MobileVerificationPage c = this.Self;
			if (c == null) return;
			c.UpdateTextFieldWithText (text);
		}

		public override void TriggerContinueButton () {
			MobileVerificationPage c = this.Self;
			if (c == null) return;
			c.TriggerContinueButton ();
		}

		public override void DisplayAccountError () {
			MobileVerificationPage c = this.Self;
			if (c == null) return;
			c.DisplayAccountError ();
		}

		public override void DismissControllerAndFinishOnboarding () {
			MobileVerificationPage c = this.Self;
			if (c == null) return;
			c.DismissControllerAndFinishOnboarding ();
		}

		public override void GoToAccountController () {
			MobileVerificationPage c = this.Self;
			if (c == null) return;
			c.GoToAccountController ();
		}
	}
}
