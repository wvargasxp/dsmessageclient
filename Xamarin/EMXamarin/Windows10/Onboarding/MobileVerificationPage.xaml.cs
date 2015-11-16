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
using Windows10.Account;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Windows10.Onboarding
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MobileVerificationPage : Page
    {
        private SharedVerificationController Shared { get; set; }

        public MobileVerificationPage(string accountId)
        {
            this.InitializeComponent();
            this.Shared = new SharedVerificationController(App.Instance.Model, this);
            this.Shared.AccountID = accountId;
        }
        private void VerificationTextBox_TextChanged(object sender, TextChangedEventArgs e) { }

        #region SharedVerificationController 
        public void ShouldPauseUI() { }

        public void ShouldResumeUI() { }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            string verificationCode = this.VerificationTextBox.Text;
            this.Shared.TryToLogin(verificationCode);
        }

        public void TriggerContinueButton() { }

        public void UpdateTextFieldWithText(string text)
        {
            this.VerificationTextBox.Text = text;
        }

        public void DisplayAccountError() { }

        public void DismissControllerAndFinishOnboarding()
        {
            // todo
            MainWindow mainWindow = new MainWindow();
            //mainWindow.Show();

            //Window window = Window.GetWindow(this);
            //window.Close();
        }

        public void GoToAccountController()
        {
            AccountPage page = new AccountPage(true);
            this.Frame.Navigate(typeof(AccountPage), page); //this.NavigationService.Navigate((page));
        }
        #endregion
    }

    class SharedVerificationController : AbstractMobileVerificationController
    {
        private WeakReference _r = null;
        private MobileVerificationPage Self
        {
            get { return this._r != null ? this._r.Target as MobileVerificationPage : null; }
            set { this._r = new WeakReference(value); }
        }

        public SharedVerificationController(ApplicationModel g, MobileVerificationPage t) : base(g)
        {
            this.Self = t;
        }

        public override void ShouldPauseUI()
        {
            MobileVerificationPage c = this.Self;
            if (c == null) return;
            c.ShouldPauseUI();
        }

        public override void ShouldResumeUI()
        {
            MobileVerificationPage c = this.Self;
            if (c == null) return;
            c.ShouldResumeUI();
        }

        public override void UpdateTextFieldWithText(string text)
        {
            MobileVerificationPage c = this.Self;
            if (c == null) return;
            c.UpdateTextFieldWithText(text);
        }

        public override void TriggerContinueButton()
        {
            MobileVerificationPage c = this.Self;
            if (c == null) return;
            c.TriggerContinueButton();
        }

        public override void DisplayAccountError()
        {
            MobileVerificationPage c = this.Self;
            if (c == null) return;
            c.DisplayAccountError();
        }

        public override void DismissControllerAndFinishOnboarding()
        {
            MobileVerificationPage c = this.Self;
            if (c == null) return;
            c.DismissControllerAndFinishOnboarding();
        }

        public override void GoToAccountController()
        {
            MobileVerificationPage c = this.Self;
            if (c == null) return;
            c.GoToAccountController();
        }
    }
}
