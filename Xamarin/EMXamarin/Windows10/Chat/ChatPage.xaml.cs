using em;
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
using WindowsDesktop.Chat.PickingFromAlias;
using WindowsDesktop.Contacts;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Chat {
	/// <summary>
	/// Interaction logic for Self.xaml
	/// </summary>
	public partial class ChatPage : Page {

		private ChatEntry ChatEntry { get; set; }
		private SharedChatController Shared { get; set; }
		private FromAliasItemViewModel FromAliasPickerModel { get; set; }

		public ChatPage (ChatEntry chatEntry) {
			InitializeComponent ();
			this.DataContext = this;
			this.ChatEntry = ChatEntry;
			this.Shared = new SharedChatController (App.Instance.Model, chatEntry, chatEntry.entryOrder < 0 ? null : chatEntry, this);
			Init ();
		}

		private void Init () {
			if (!this.Shared.IsNewMessage) {
				this.Shared.HideAddContactsOption (false);
			}

			this.Shared.PossibleFromBarVisibilityChange ();

			UpdateDataSource ();
			UpdateFromAliasComboBoxDataSource ();
		}

		public void UpdateDataSource () {
			EMTask.DispatchMain (() => {
				this.ListView.ItemsSource = ChatItemViewModel.From (this.Shared.viewModel).Items;
				this.ListView.Items.Refresh (); // todo, might be slow
			});
		}

		public void UpdateFromAliasComboBoxDataSource () {
			IList<AliasInfo> aliases = App.Instance.Model.account.accountInfo.ActiveAliases;
			this.FromAliasPickerModel = FromAliasItemViewModel.From (aliases);
			this.FA.FromComboxBox.ItemsSource = this.FromAliasPickerModel.Items;
			int selectedIndex = this.FA.FromComboxBox.SelectedIndex;
			int current = this.Shared.CurrentRowForFromAliasPicker ();
			if (selectedIndex != current) {
				this.FA.FromComboxBox.SelectedIndex = current;
			}
		}

		public void ClearTextEntryArea () {
			this.ChatTextBox.Text = string.Empty;
		}

		public void DidAddNewContact (Contact contact) {
			this.Shared.AddContactToReplyTo (contact);
		}

		#region xaml event handlers
		private void SendButton_Click (object sender, RoutedEventArgs e) {
			this.Shared.SendMessage ();
		}

		private void ChatTextBox_TextChanged (object sender, TextChangedEventArgs e) {
			if (this.Shared != null) { 
				this.Shared.UpdateUnderConstructionText (this.ChatTextBox.Text);
			}
		}

		private void AddContactButton_Click (object sender, RoutedEventArgs e) {
			AddressBookArgs args = AddressBookArgs.From (excludeGroups: false, exludeTemp: true, excludePreferred: false, entry: this.Shared.sendingChatEntry);
			AddressBookPage page = new AddressBookPage (args);
			page.DelegateContactSelected += WeakDelegateProxy.CreateProxy<Contact> (DidAddNewContact).HandleEvent<Contact>;
			BasicWindow window = new BasicWindow (page);
			window.Show ();
		}

		private void FromComboBox_SelectionChanged (object sender, SelectionChangedEventArgs e) {
			int index = this.FA.FromComboxBox.SelectedIndex;
			AliasInfo aliasInfo = this.FromAliasPickerModel.InfoFromIndex (index);
			this.Shared.UpdateFromAlias (aliasInfo);
		}

		#endregion

		#region xaml binding paths
		#endregion

		#region SharedChatController

		public void HideAddContactsOptions () {
			this.ToBar.Visibility = Visibility.Collapsed;
		}

		public void UpdateFromBarVisibility (bool showFromBar) {
			this.FromBar.Visibility = showFromBar ? Visibility.Visible : Visibility.Collapsed;
		}

		public void UpdateToContactsView () {
			string t = "To";
			IList<Contact> contacts = this.Shared.sendingChatEntry.contacts;
			if (contacts != null && contacts.Count > 0) {
				t = string.Empty;
				foreach (Contact contact in contacts) {
					t += contact.displayName + " ";
				}
			}

			this.ToBarTextBox.Text = t;
		}

		#endregion

	}

	public class SharedChatController : AbstractChatController {
		private WeakReference _r = null;
		private ChatPage Self {
			get { return this._r != null ? this._r.Target as ChatPage : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedChatController (ApplicationModel appModel, ChatEntry sendingChatEntry, ChatEntry displayedChatEntry, ChatPage controller)
			: base (appModel, sendingChatEntry, displayedChatEntry) {
			this.Self = controller;
		}

		public override void ContactSearchPhotoUpdated (Contact c) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void ReloadContactSearchContacts () {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void PreloadImages (IList<Message> messages) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void ShowDetailsOption (bool animated, bool forceShow) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void DidFinishLoadingMessages () {
			ChatPage self = this.Self;
			if (self == null) return;
			self.UpdateDataSource (); // todo ?
		}

		public override void HideAddContactsOption (bool animated) {
			ChatPage self = this.Self;
			if (self == null) return;
			self.HideAddContactsOptions ();
		}

		public override void ClearTextEntryArea () {
			ChatPage self = this.Self;
			if (self == null) return;
			self.ClearTextEntryArea ();
		}

		public override float StagedMediaGetAspectRatio () {
			ChatPage self = this.Self;
			if (self == null) return 1.0f;
			return 1.0f; // todo
		}

		public override float StagedMediaGetSoundRecordingDurationSeconds () {
			ChatPage self = this.Self;
			if (self == null) return 1.0f;
			return 1.0f; // todo;
		}

		public override void StagedMediaRemovedFromStaging () {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void UpdateToContactsView () {
			ChatPage self = this.Self;
			if (self == null) return;
			self.UpdateToContactsView ();
		}

		public override void UpdateFromBarVisibility (bool showFromBar) {
			ChatPage self = this.Self;
			if (self == null) return;
			self.UpdateFromBarVisibility (showFromBar);
		}

		public override void UpdateFromAliasPickerInteraction (bool shouldAllowInteraction) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void HandleMessageUpdates (IList<ModelStructureChange<Message>> structureChanges, IList<ModelAttributeChange<Message, object>> attributeChanges, bool animated, Action animationCompletedCallback) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
			self.UpdateDataSource ();
			animationCompletedCallback ();
		}

		public override void SetSendButtonMode (bool sessageSendingAllowed, bool soundRecordingAllowed) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void ShowContactIsTyping (string typingMessage) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void HideContactIsTyping () {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void CounterpartyPhotoDownloaded (CounterParty counterparty) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void ConfirmRemoteTakeBack (int index) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void ConfirmMarkHistorical (int index) {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo
		}

		public override void DidChangeColorTheme () {
			ChatPage self = this.Self;
			if (self == null) return;
			// todo;
		}

		public override void DidChangeDisplayName () {
			ChatPage self = this.Self;
			if (self == null) return;
			return; // todo
		}

		public override void DidChangeTotalUnread (int unreadCount) {
			ChatPage self = this.Self;
			if (self == null) return;
			return; // todo;
		}

		public override void UpdateChatRows (Message message) {
			ChatPage self = this.Self;
			if (self == null) return;
			self.UpdateDataSource (); // todo
		}

		public override void GoToBottom () {
			ChatPage self = this.Self;
			if (self == null) return;
			return; // todo
		}

		public override bool CanScrollToBottom {
			get { return true; } // todo
		}

		public override void WarnLeftAdhoc () {
			ChatPage self = this.Self;
			if (self == null) return;
			return; // todo
		}

		public override void UpdateAliasText (string text) {
			ChatPage self = this.Self;
			if (self == null) return;
			// Alias text is automatically updated. We don't do anything here.
		}

		public override void ClearInProgressRemoteActionMessages () {
			return; // todo
		}

		public override void DidFinishLoadingPreviousMessages (int count) {
			return; // todo
		}

		public override void StagedMediaBegin () {
			return; // todo
		}

		public override void StagedMediaAddedToStagingAndPreload () {
			return; // todo
		}

		public override void StagedMediaEnd () {
			return; // todo
		}

		public override void ConversationContainsActive (bool active, InactiveConversationReason reason) {
			return; // todo
		}

		public override void UpdateTextEntryArea (string text) {
			return; // todo
		}

		public override void PrepopulateToWithAKA (string toAka) {
			return; // todo
		}
	}
}
