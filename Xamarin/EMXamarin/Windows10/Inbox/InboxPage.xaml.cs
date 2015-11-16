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
using em;
using WindowsDesktop.Chat;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Inbox {
	/// <summary>
	/// Interaction logic for Self.xaml
	/// </summary>
	public partial class InboxPage : Page {

		private WindowsInbox SharedInbox { get; set; }
		private InboxItemViewModel ViewModel { get; set; }

		public InboxPage () {
			InitializeComponent ();
			this.SharedInbox = new WindowsInbox (this);
			this.ViewModel = new InboxItemViewModel ();
			UpdateDatasource ();
		}

		public void UpdateDatasource () {
			IList<ChatEntry> chatEntries = this.SharedInbox.viewModel;
			this.ViewModel.UpdateSource (chatEntries);

			this.ListView.ItemsSource = this.ViewModel.List; 
			this.ListView.Items.Refresh (); // todo
		}

		private void ListView_SelectionChanged (object sender, SelectionChangedEventArgs e) {
			int index = this.ListView.SelectedIndex;
			if (index < 0) {
				return;
			}

			if (this.ViewModel.HasNewChat) {
				if (index == 0) {
					ChatPage page = new ChatPage (App.Instance.Model.chatList.underConstruction);
					this.ChatPageFrame.Navigate (page);
				} else {
					ChatPage page = new ChatPage (this.SharedInbox.viewModel [--index]);
					this.ChatPageFrame.Navigate (page);
				}
			} else {
				ChatPage page = new ChatPage (this.SharedInbox.viewModel [index]);
				this.ChatPageFrame.Navigate (page);
			}
		}

		private void ListView_MouseDoubleClick (object sender, MouseButtonEventArgs e) {
			int index = this.ListView.SelectedIndex;
			ChatPage page = new ChatPage (this.SharedInbox.viewModel [index]);
			BasicWindow window = new BasicWindow (page);
			window.Show ();
		}

		private void ComposeButton_Click (object sender, RoutedEventArgs e) {
			ApplicationModel appModel = App.Instance.Model;
			ChatEntry chatEntry = ChatEntry.NewUnderConstructionChatEntry (appModel, DateTime.Now.ToEMStandardTime (appModel));
			appModel.chatList.underConstruction = chatEntry;

			UpdateDatasource ();

			ChatPage page = new ChatPage (chatEntry);
			this.ChatPageFrame.Navigate (page);
		}
	}

	public class WindowsInbox : AbstractInBoxController {
		private InboxPage Self { get; set; }

		public WindowsInbox (InboxPage page) : base (App.Instance.Model.chatList) {
			this.Self = page;
		}

		public override void HandleUpdatesToChatList (IList<ModelStructureChange<ChatEntry>> repositionChatItems, IList<ModelAttributeChange<ChatEntry, object>> previewUpdates, bool animated, Action callback) {
			this.Self.UpdateDatasource (); // todo
			callback (); // todo
		}

		public override void DidChangeColorTheme () {
			return; // todo
		}

		public override void UpdateTitleProgressIndicatorVisibility () {
			return; // todo
		}

		public override void ShowNotificationBanner (ChatEntry entry) {
			return; // todo
		}

		public override void GoToChatEntry (ChatEntry chatEntry) {
			return; // todo
		}

		public override void UpdateBurgerUnreadCount (int unreadCount) {
			return; // todo
		}
	}

}
