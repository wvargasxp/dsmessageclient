using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
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
using Windows.UI.Core;

namespace Windows10
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private const string Tag = "App: ";
        private ApplicationModel _applicationModel = null;
        public ApplicationModel Model
        {
            get { return this._applicationModel; }
            private set { this._applicationModel = value; }
        }

        private static App _instance = null;
        public static App Instance
        {
            get { return _instance; }
            private set { _instance = value; }
        }


        //override 
        ////
        //// Summary:
        ////     Raises the System.Windows.Application.Startup event.
        ////
        //// Parameters:
        ////   e:
        ////     A System.Windows.StartupEventArgs that contains the event data.
        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    Debug.WriteLine(string.Format("{0} OnStartup", Tag));
        //    App.Instance = this;

        //    WindowsDesktopPlatformFactory platformFactory = new WindowsDesktopPlatformFactory();
        //    this.Model = new ApplicationModel(platformFactory);
        //    this.Model.CompletedOnboardingPhase = () => { };

        //    ShowFirstWindow();
        //}

        //#region helper methods
        //private void ShowFirstWindow()
        //{
        //    Window startupWindow = null;
        //    Dictionary<string, object> sessionInfo = this.Model.GetSessionInfo();
        //    if ((bool)sessionInfo["isOnboarding"])
        //    { // todo
        //        startupWindow = new OnboardingWindow();
        //    }
        //    else
        //    {
        //        startupWindow = new MainWindow();
        //    }

        //    startupWindow.Show();
        //}
        //#endregion


        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {

            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            Debug.WriteLine(string.Format("{0} OnStartup", Tag));
            App.Instance = this;

            // TODO --- DS to be completed
            //WindowsDesktopPlatformFactory platformFactory = new WindowsDesktopPlatformFactory();
            //this.Model = new ApplicationModel(platformFactory);
            //this.Model.CompletedOnboardingPhase = () => { };            
        }


        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    rootFrame.CanGoBack ?
                    AppViewBackButtonVisibility.Visible :
                    AppViewBackButtonVisibility.Visible;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(Onboarding.LandingPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Action to Back
        /// </summary>
        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        /// <summary>
        /// Button Visibility
        /// </summary>
        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
