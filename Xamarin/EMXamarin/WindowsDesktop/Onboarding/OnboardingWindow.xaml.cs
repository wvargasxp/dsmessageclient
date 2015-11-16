using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace WindowsDesktop.Onboarding {
	/// <summary>
	/// Interaction logic for OnboardingWindow.xaml
	/// </summary>
	public partial class OnboardingWindow : NavigationWindow {
		public OnboardingWindow () {
			InitializeComponent ();
			Page p = new LandingPage ();
			p.ShowsNavigationUI = false;
			this.Content = p;
		}

		// Summary:
		//     Raises the System.Windows.Window.Activated event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected virtual void OnActivated (EventArgs e) {
			base.OnActivated (e);
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.Closed event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected virtual void OnClosed (EventArgs e) {
			base.OnClosed (e);
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.Closing event.
		//
		// Parameters:
		//   e:
		//     A System.ComponentModel.CancelEventArgs that contains the event data.
		protected virtual void OnClosing (CancelEventArgs e) {
			base.OnClosing (e);
		}

		//
		// Summary:
		//     Called when the System.Windows.Controls.ContentControl.Content property changes.
		//
		// Parameters:
		//   oldContent:
		//     A reference to the root of the old content tree.
		//
		//   newContent:
		//     A reference to the root of the new content tree.
		protected override void OnContentChanged (object oldContent, object newContent) {
			base.OnContentChanged (oldContent, newContent);
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.ContentRendered event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected virtual void OnContentRendered (EventArgs e) {
			base.OnContentRendered (e);
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.Deactivated event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected virtual void OnDeactivated (EventArgs e) {
			base.OnDeactivated (e);
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.LocationChanged event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected virtual void OnLocationChanged (EventArgs e) {
			base.OnLocationChanged (e);
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.SourceInitialized event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected virtual void OnSourceInitialized (EventArgs e) {
			base.OnSourceInitialized (e);
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.StateChanged event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected virtual void OnStateChanged (EventArgs e) {
			base.OnStateChanged (e);
		}
	}
}
