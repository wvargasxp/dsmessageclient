using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using em;
using EMXamarin;
using System.Threading;
using System.Windows.Controls;
using WindowsDesktop.Utility;
using WindowsDesktop.Onboarding;

namespace WindowsDesktop {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {

		private const string Tag = "App: ";
		private ApplicationModel _applicationModel = null;
		public ApplicationModel Model {
			get { return this._applicationModel; }
			private set { this._applicationModel = value; }
		}

		private static App _instance = null;
		public static App Instance {
			get { return _instance; }
			private set { _instance = value; }
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.Activated event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnActivated (EventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnActivated", Tag));

			this.Model.AppInForeground = true;
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.Deactivated event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnDeactivated (EventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnDeactivated", Tag));


			this.Model.AppInForeground = false;
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.Exit event.
		//
		// Parameters:
		//   e:
		//     An System.Windows.ExitEventArgs that contains the event data.
		protected override void OnExit (ExitEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnExit", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.FragmentNavigation event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.Navigation.FragmentNavigationEventArgs that contains the
		//     event data.
		protected override void OnFragmentNavigation (FragmentNavigationEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnFragmentNavigation", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.LoadCompleted event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.Navigation.NavigationEventArgs that contains the event data.
		protected override void OnLoadCompleted (NavigationEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnLoadCompleted", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.Navigated event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.Navigation.NavigationEventArgs that contains the event data.
		protected override void OnNavigated (NavigationEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnNavigated", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.Navigating event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.Navigation.NavigatingCancelEventArgs that contains the event
		//     data.
		protected override void OnNavigating (NavigatingCancelEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnNavigating", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.NavigationFailed event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.Navigation.NavigationFailedEventArgs that contains the event
		//     data.
		protected override void OnNavigationFailed (NavigationFailedEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnNavigationFailed", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.NavigationProgress event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.Navigation.NavigationProgressEventArgs that contains the
		//     event data.
		protected override void OnNavigationProgress (NavigationProgressEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnNavigationProgress", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.NavigationStopped event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.Navigation.NavigationEventArgs that contains the event data.
		protected override void OnNavigationStopped (NavigationEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnNavigationStopped", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.SessionEnding event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.SessionEndingCancelEventArgs that contains the event data.
		protected override void OnSessionEnding (SessionEndingCancelEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnSessionEnding", Tag));
		}

		//
		// Summary:
		//     Raises the System.Windows.Application.Startup event.
		//
		// Parameters:
		//   e:
		//     A System.Windows.StartupEventArgs that contains the event data.
		protected override void OnStartup (StartupEventArgs e) {
			Debug.WriteLine (string.Format ("{0} OnStartup", Tag));
			App.Instance = this;

			WindowsDesktopPlatformFactory platformFactory = new WindowsDesktopPlatformFactory ();
			this.Model = new ApplicationModel (platformFactory);
			this.Model.CompletedOnboardingPhase = () => { };

			ShowFirstWindow ();
		}

		#region helper methods
		private void ShowFirstWindow () {
			Window startupWindow = null;
			Dictionary<string, object> sessionInfo = this.Model.GetSessionInfo ();
			if ((bool)sessionInfo ["isOnboarding"]) { // todo
				startupWindow = new OnboardingWindow ();
			} else {
				startupWindow = new MainWindow ();
			}

			startupWindow.Show ();
		}
		#endregion
	}
}
