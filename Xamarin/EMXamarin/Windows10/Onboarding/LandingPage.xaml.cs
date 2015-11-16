using em;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Windows10.Onboarding
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LandingPage : Page
    {
        private LandingPageModel Shared { get; set; }
        public LandingPage()
        {
            this.InitializeComponent();
            this.Shared = new LandingPageModel();
            this.WebBrowser.Navigate(new Uri(this.Shared.PreviewViewURL));
        }

        private void MobileButton_Click(object sender, RoutedEventArgs e)
        {
            MobileSignInPage page = new MobileSignInPage();
            // page.TopAppBar.Visibility = Visibility.Visible;  // page.ShowsNavigationUI = true;
            this.Frame.Navigate(typeof(MobileSignInPage), page);    //this.Content.NavigationService.Navigate(page);
        }

        private void EmailButton_Click(object sender, RoutedEventArgs e)
        {
            EmailSignInPage page = new EmailSignInPage();
            //   page.ShowsNavigationUI = true;
            this.Frame.Navigate(typeof(EmailSignInPage), page);//  this.NavigationService.Navigate(page);
        }
    }
}
