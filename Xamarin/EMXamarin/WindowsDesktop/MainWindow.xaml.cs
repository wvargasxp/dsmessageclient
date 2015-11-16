using em;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsDesktop.Account;
using WindowsDesktop.Aliases;
using WindowsDesktop.Groups;
using WindowsDesktop.Help;
using WindowsDesktop.Inbox;
using WindowsDesktop.Notifications;
using WindowsDesktop.Onboarding;
using WindowsDesktop.Utility;
using WindowsDesktop.About;
using WindowsDesktop.Contacts;
using System;

namespace WindowsDesktop {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private const string ClassName = "MainWindow:";

		private static MainWindow _instance = null;
		public static MainWindow Instance {
			get { return _instance; }
			private set { _instance = value;  }
		}

		public MainWindow () {
			InitializeComponent ();
			Instance = this;
			InitializeFirstPage ();
			AskToGetHistoricalMessages ();
		}

		private void InitializeFirstPage () {
			ApplicationModel applicationModel = App.Instance.Model;
			applicationModel.DoSessionStart ();
			Page page = new InboxPage ();
			page.ShowsNavigationUI = false;
			this.AppFrame.Content = page;
		}


		private void AskToGetHistoricalMessages () {
			ApplicationModel appModel = App.Instance.Model;
			if (appModel.AwaitingGetHistoricalMessagesChoice) {
				appModel.AwaitingGetHistoricalMessagesChoice = false;
				MessageBoxResult result = MessageBox.Show ("Get historical messages?", "Yes", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes) {
					appModel.RequestMissedMessages ();
				} else {
					appModel.RequestMissedNotifications ();
				}
			}
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.Activated event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnActivated (EventArgs e) {
			base.OnActivated (e);
			Debug.WriteLine (string.Format ("{0} OnActivated", ClassName));
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.Closed event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnClosed (EventArgs e) {
			base.OnClosed (e);
			Debug.WriteLine (string.Format ("{0} OnClosed", ClassName));
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.Closing event.
		//
		// Parameters:
		//   e:
		//     A System.ComponentModel.CancelEventArgs that contains the event data.
		protected override void OnClosing (CancelEventArgs e) {
			base.OnClosing (e);
			Debug.WriteLine (string.Format ("{0} OnClosing", ClassName));
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
			Debug.WriteLine (string.Format ("{0} OnContentChanged", ClassName));
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.ContentRendered event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnContentRendered (EventArgs e) {
			base.OnContentRendered (e);
			Debug.WriteLine (string.Format ("{0} OnContentRendered", ClassName));
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.Deactivated event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnDeactivated (EventArgs e) {
			base.OnDeactivated (e);
			Debug.WriteLine (string.Format ("{0} OnDeactivated", ClassName));
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.LocationChanged event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnLocationChanged (EventArgs e) {
			base.OnLocationChanged (e);
			Debug.WriteLine (string.Format ("{0} OnLocationChanged", ClassName));
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.SourceInitialized event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnSourceInitialized (EventArgs e) {
			base.OnSourceInitialized (e);
			Debug.WriteLine (string.Format ("{0} OnSourceInitialized", ClassName));
		}

		//
		// Summary:
		//     Raises the System.Windows.Window.StateChanged event.
		//
		// Parameters:
		//   e:
		//     An System.EventArgs that contains the event data.
		protected override void OnStateChanged (EventArgs e) {
			base.OnStateChanged (e);
			Debug.WriteLine (string.Format ("{0} OnStateChanged", ClassName));
		}

		#region Menu item event handlers
		private void SettingsMenuItem_Click (object sender, RoutedEventArgs e) {
			MessageBox.Show ("Setting menu item clicked.");
		}

		private void ContactsMenuItem_Click (object sender, RoutedEventArgs e) {
			AddressBookArgs args = AddressBookArgs.From (excludeGroups: false, exludeTemp: true, excludePreferred: false, entry: null);
			Page page = new AddressBookPage (args);
			ShowPageInBasicWindow (page);
		}

		private void ProfileMenuItem_Click (object sender, RoutedEventArgs e) {
			//MessageBox.Show ("Profile menu item clicked.");
			Page page = new AccountPage (onboarding: false);
			ShowPageInBasicWindow (page);
		}

		private void AKAMenuItem_Click (object sender, RoutedEventArgs e) {
			//MessageBox.Show ("AKA menu item clicked.");
			Page page = new AliasPage ();
			ShowPageInBasicWindow (page);
		}

		private void GroupsMenuItem_Click (object sender, RoutedEventArgs e) {
			//MessageBox.Show ("Groups menu item clicked.");
			Page page = new GroupsPage ();
			ShowPageInBasicWindow (page);
		}

		private void NotificationsMenuItem_Click (object sender, RoutedEventArgs e) {
			//MessageBox.Show ("Notifications menu item clicked.");
			Page page = new NotificationPage ();
			ShowPageInBasicWindow (page);
		}

		private void HelpMenuItem_Click (object sender, RoutedEventArgs e) {
			//MessageBox.Show ("Help menu item clicked.");
			Page page = new HelpPage ();
			ShowPageInBasicWindow (page);
		}

		private void AboutMenuItem_Click (object sender, RoutedEventArgs e) {
			//MessageBox.Show ("About menu item clicked.");
			Page page = new AboutPage ();
			ShowPageInBasicWindow (page);
		}

		private void ShowPageInBasicWindow (Page page) {
			BasicWindow window = new BasicWindow (page);
			window.Show ();
		}
		#endregion

	}
}
