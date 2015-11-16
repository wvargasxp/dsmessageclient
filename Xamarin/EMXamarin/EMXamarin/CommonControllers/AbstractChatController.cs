using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace em {
	/**
	 * Object that encapsulates the standard behavior of a chat conversation view
	 * (without having any actual view logic of it's own).  This abstract class defines
	 * several callbacks to implement to respond to various UI related events.
	 * 
	 * It also provides several helper methods that automate working with the model
	 * layer to perform actions requiried for chat.
	 */
	public abstract class AbstractChatController : IDisposable, IMediaMessagesProvider {
		public static readonly string MESSAGE_ATTRIBUTE_MESSAGE_STATUS = "status";
		public static readonly string MESSAGE_ATTRIBUTE_TAKEN_BACK = "takenBack";

		public ChatList chatList { get; set; }
		ChatEntry dce;
		ChatEntry sce;

		int su;
		public void SuspendUpdates() {
			lock(this) {
				su++;
			}
		}

		public void ResumeUpdates() {
			lock(this) {
				su--;

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges ();
			}
		}

		protected bool SuspendedUpdates {
			get {
				lock (this) {
					return su > 0;
				}
			}
		}

		IList<ModelStructureChange<Message>> pendingStructureChanges = new List<ModelStructureChange<Message>>();
		IList<ModelAttributeChange<Message,object>> pendingAttribute = new List<ModelAttributeChange<Message,object>> ();

		// The sendingChatEntry and displayChatEntry are often the same object
		// however, when creating a new message, they can be different.  The
		// displayedChatEntry might correspond to an existing conversation that
		// matches the same contacts list.  This of course would change if a
		// contact was added or removed.
		// The sendingChatEntry just contains the staged content.
		// After a message is sent they are typically merged.
		public ChatEntry sendingChatEntry { 
			get { return sce; }
			set { sce = value; }
		}

		public ChatEntry displayedChatEntry {
			get {
				return dce;
			}

			set {
				if (dce != null) {
					dce.DelegateDidAddMessage -= DidAddMessageAtProxy.HandleEvent<Message>;
					dce.DelegateDidRemoteModifyMessage -= DidRemoteModifyMessageAtProxy.HandleEvent<Message,int>;
					dce.DelegateDidMarkMessageHistoricalAt -= DidMarkMessageHistoricalAtProxy.HandleEvent<Message>;
					dce.DelegateDidChangeStatusOfMessage -= DidChangeStatusOfMessageProxy.HandleEvent<Message>;
					dce.DelegateDidReceiveTypingMessage -= DidReceiveTypingMessageProxy.HandleEvent<Contact>;

					dce.DelegateDidStartUpload -= DidReceiveStartUploadMessageProxy.HandleEvent<Message>;
					dce.DelegateDidUpdateUploadPercentComplete -= DidReceiveUploadPercentCompleteUpdateProxy.HandleEvent<Message,double>;
					dce.DelegateDidCompleteUpload -= DidReceiveCompleteUploadMessageProxy.HandleEvent<Message>;
					dce.DelegateDidFailUpload -= DidReceiveFailUploadMessageProxy.HandleEvent<Message>;

					dce.DelegateDidStartDownload -= DidReceiveStartDownloadMessageProxy.HandleEvent<Message>;
					dce.DelegateDidUpdateDownloadPercentComplete -= DidReceiveDownloadPercentCompleteUpdateProxy.HandleEvent<Message,double>;
					dce.DelegateDidCompleteDownload -= DidReceiveCompleteDownloadMessageProxy.HandleEvent<Message>;
					dce.DelegateDidChangeSoundState -= DidReceiveSoundStateChangeMessageProxy.HandleEvent<Message>;

					NotificationCenter.DefaultCenter.RemoveObserverAction (ChatEntryCounterpartyDidChangeLifecycle);
				}

				dce = value;
				if (dce != null) {
					dce.DelegateDidAddMessage += DidAddMessageAtProxy.HandleEvent<Message>;
					dce.DelegateDidRemoteModifyMessage += DidRemoteModifyMessageAtProxy.HandleEvent<Message,int>;
					dce.DelegateDidMarkMessageHistoricalAt += DidMarkMessageHistoricalAtProxy.HandleEvent<Message>;
					dce.DelegateDidChangeStatusOfMessage += DidChangeStatusOfMessageProxy.HandleEvent<Message>;
					dce.DelegateDidReceiveTypingMessage += DidReceiveTypingMessageProxy.HandleEvent<Contact>;

					dce.DelegateDidStartUpload += DidReceiveStartUploadMessageProxy.HandleEvent<Message>;
					dce.DelegateDidUpdateUploadPercentComplete += DidReceiveUploadPercentCompleteUpdateProxy.HandleEvent<Message,double>;
					dce.DelegateDidCompleteUpload += DidReceiveCompleteUploadMessageProxy.HandleEvent<Message>;
					dce.DelegateDidFailUpload += DidReceiveFailUploadMessageProxy.HandleEvent<Message>;

					dce.DelegateDidStartDownload += DidReceiveStartDownloadMessageProxy.HandleEvent<Message>;
					dce.DelegateDidUpdateDownloadPercentComplete += DidReceiveDownloadPercentCompleteUpdateProxy.HandleEvent<Message,double>;
					dce.DelegateDidCompleteDownload += DidReceiveCompleteDownloadMessageProxy.HandleEvent<Message>;
					dce.DelegateDidChangeSoundState += DidReceiveSoundStateChangeMessageProxy.HandleEvent<Message>;


					NotificationCenter.DefaultCenter.AddWeakObserver (dce, ChatEntry.CHATENTRY_COUNTERPARTY_LIFECYCLE_HAS_CHANGED, ChatEntryCounterpartyDidChangeLifecycle);
				}

				if (dce == null) {
					messages = null;
					viewModel = null;
				}
				else {
					messages = dce.CachedConversation.CachedMessages;
					if (messages != null) {
						viewModel = new List<Message> (messages);
						// This setter enters a property that will set a flag on CachedConversation's flag here. Harmless.
						this.CanLoadMorePreviousMessages = dce.CachedConversation.CanLoadMorePreviousMessages; 
					} else {

						// If the cached conversation was released at some point (or hasn't been instantiated), reset the last read index on the chat entry to zero.
						dce.LastReadIndex = 0;

						this.CanLoadMorePreviousMessages = true;
						WeakReference thisRef = new WeakReference (this);
						dce.LoadRecentMessagesAsync (msgs => {
							AbstractChatController self = thisRef.Target as AbstractChatController;
							if (self != null) {
								if (msgs.Count < Constants.INITIAL_NUMBER_OF_MESSAGES_TO_RETRIEVE_IN_CHAT) {
									this.CanLoadMorePreviousMessages = false;
								}

								self.messages = msgs;
								self.viewModel = new List<Message> (self.messages); // TODO make sure this is on a main thread
								self.DidFinishLoadingMessages ();
								self.PreloadImages (self.messages);
							}
						}, Constants.INITIAL_NUMBER_OF_MESSAGES_TO_RETRIEVE_IN_CHAT);
					}
				}
			}
		}

		private bool _canLoadMorePreviousMessages = false;
		public bool CanLoadMorePreviousMessages { 
			get { return this._canLoadMorePreviousMessages; } 
			set {
				this._canLoadMorePreviousMessages = value;

				// Set it on the CachedConversation object also so if we re-enter the chatentry, we'll have the information saved.
				if (this.dce != null) {
					this.dce.CachedConversation.CanLoadMorePreviousMessages = this._canLoadMorePreviousMessages;
				}
			}
		}

		private bool TryingToLoadMessages { get; set; }

		public void LoadMorePreviousMessages () {
			if (this.TryingToLoadMessages) {
				return;
			}

			this.TryingToLoadMessages = true;
			WeakReference thisRef = new WeakReference (this);
			ChatEntry displayedChatEntry = this.displayedChatEntry;
			if (displayedChatEntry != null) {
				displayedChatEntry.LoadMorePreviousMessages (msgs => {
					AbstractChatController self = thisRef.Target as AbstractChatController;
					HandleRetrievedPreviousMessagse (self, msgs);
				});
			}
		}

		private void HandleRetrievedPreviousMessagse (AbstractChatController self, IList<Message> msgs) {
			if (self != null) {
				int previousMessagesCount = msgs.Count;

				// If the count is 0 for this query, we don't have any more previous messages we can load.
				// If the count is less than the query amount, that means we've exhausted all previous messages.
				// For example, the database has 49 messages but we query for 50.
				// The previous message count would be 49 while the query amount would be 50. Database would have no more messages.
				if (previousMessagesCount == 0 || previousMessagesCount < Constants.NUMBER_OF_PREVIOUS_MESSAGES_TO_RETRIEVE) {
					this.CanLoadMorePreviousMessages = false;
				}
					
				IList<Message> messages = self.messages;
				if (messages != null) {
					self.viewModel = new List<Message> (messages); // TODO make sure this is on a main thread
				}

				self.DidFinishLoadingPreviousMessages (previousMessagesCount);
				this.TryingToLoadMessages = false;

				NotificationCenter.DefaultCenter.PostNotification (Constants.AbstractChatController_FinishedRetrievingMorePreviousMessages);
			}
		}

		#region IMediaMessagesProvider implementation
		// Save the index of the tapped media so we know what page we're on upon entering the Media Gallery.
		public Message TappedMediaMessage { get; set; }
		public int MediaMessageIndex { get; set; }

		public IList<Message> GetMediaMessages () {
			IList<Message> messages = this.viewModel;
			IList<Message> mediaMessages = new List<Message> ();

			if (messages != null) {
				for (int i = 0; i < messages.Count; i++) {
					Message message = messages [i];
					if (message.HasMedia ()) {
						mediaMessages.Add (message);
						if (message.Equals (this.TappedMediaMessage)) {
							// convert the message index to the appropriate index in the mediaMessages list.
							this.MediaMessageIndex = mediaMessages.IndexOf (message);
						}

					}
				}
			}

			return mediaMessages;
		}

		public int GetCurrentPage () {
			return this.MediaMessageIndex;
		}

		public void RequestMoreMediaMessages (Message message) {
			// Saving the last media message we're on.
			// This will help us with a number of things. 
			// It'll allow us to calculate the new Title label in Media Gallery (1 of 10..)
			// And allow us to calculate a scroll offset to scroll to the message upon returning to the chat.
			this.TappedMediaMessage = message;

			if (this.CanLoadMorePreviousMessages) {
				LoadMorePreviousMessages ();
			}
		}

		public bool HasMoreMediaMessagesToRequest () {
			return this.CanLoadMorePreviousMessages;
		}

		public void UpdateLastSeenMediaMessage (Message message) {
			this.TappedMediaMessage = message;
		}

		#endregion

		public IList<Message> viewModel { get; set; }
		public IList<Message> messages { get; set; }
		private Media stagedMedia;
		public Media StagedMedia {
			get{
				return this.stagedMedia;
			}
			set {
				this.stagedMedia = value;
			}
		}
		public float stagedMediaHeightToWidth { get; set; }

		public float stagedMediaSoundRecordingDurationSeconds { get; set; }

		public string displayedMessage; // field set by subclasses to track what they are showing
		public string typingMessage;
		private IList<ContactSpecificTimerInfo> Typers { get; set; }

		#region contact search + contact search thumbnail updating
		public abstract void ContactSearchPhotoUpdated (Contact c);
		private IList<Contact> searchContacts = new List<Contact> ();
		public IList<Contact> SearchContacts {
			get { return searchContacts; }
			set { searchContacts = value; }
		}
		#endregion

		public const int SHOW_TYPING_DELAY_MILLIS = 2000;

		bool viewVisible;
		ApplicationModel appModel;

		WeakDelegateProxy DidAddMessageAtProxy;
		WeakDelegateProxy DidRemoteModifyMessageAtProxy;
		WeakDelegateProxy DidMarkMessageHistoricalAtProxy;
		WeakDelegateProxy DidChangeStatusOfMessageProxy;
		WeakDelegateProxy DidReceiveTypingMessageProxy;
		WeakDelegateProxy DidReceiveStartUploadMessageProxy;
		WeakDelegateProxy DidReceiveUploadPercentCompleteUpdateProxy;
		WeakDelegateProxy DidReceiveCompleteUploadMessageProxy;
		WeakDelegateProxy DidReceiveFailUploadMessageProxy;
		WeakDelegateProxy DidReceiveStartDownloadMessageProxy;
		WeakDelegateProxy DidReceiveDownloadPercentCompleteUpdateProxy;
		WeakDelegateProxy DidReceiveCompleteDownloadMessageProxy;
		WeakDelegateProxy DidReceiveSoundStateChangeMessageProxy;
		WeakDelegateProxy DidFinishedStagingMediaProxy;
		WeakDelegateProxy DidChangeOwnColorThemeProxy;
		WeakDelegateProxy DidChangeDisplayNameProxy;
		WeakDelegateProxy DidChangeTotalUnreadCountProxy;
		WeakDelegateProxy WillStartBulkUpdatesProxy;
		WeakDelegateProxy DidFinishBulkUpdatesProxy;

		public bool IsNewMessage {
			get {
				return ShowAddContacts();
			}
		}

		public string Title {
			get {
				string t = string.Empty;
				ChatEntry entry = sendingChatEntry;
				if (entry != null) {
					string groupChat = appModel.platformFactory.GetTranslation ("GROUP_CHATS_TITLE");
					t = entry.contacts != null && entry.contacts.Count > 1 || this.IsGroupConversation ? groupChat : entry.ContactsLabel;
				}

				if (t.Equals (""))
					t = appModel.platformFactory.GetTranslation ("NEW_MESSAGE_TITLE");

				return t;
			}
		}

		public string ToFieldStringLabel {
			get {
				string contactString = string.Empty;
				ChatEntry chatEntry = this.sendingChatEntry;
				if (chatEntry.ContactsLabel.Length != 0) {
					contactString = chatEntry.ContactsLabel + ", ";
				}

				return contactString;
			}
		}

		#region staging media
		float stagedMediaHeight = 0;
		float stagedMediaWidth = 0;

		public float StagedMediaHeight {
			get { return stagedMediaHeight; }
			set { stagedMediaHeight = value; }
		}

		public float StagedMediaWidth {
			get { return stagedMediaWidth; }
			set { stagedMediaWidth = value; }
		}

		bool isStagingMedia;
		bool isStagingText;
		bool isStagingMediaAndText;
		public bool IsStagingMedia {
			get { return isStagingMedia; }
			set {
				if (value == false) {
					stagedMediaHeight = 0;
					stagedMediaHeight = 0;
					isStagingMediaAndText = false;
				} else if (value == true && isStagingText == true) {
					isStagingMediaAndText = true;
				}
				isStagingMedia = value;
			}

		}

		public bool IsStagingText {
			get { return isStagingText; }
			set {
				if (value == false) {
					isStagingMediaAndText = false;
				} else if (value == true && isStagingMedia == true) {
					isStagingMediaAndText = true;
				}
				isStagingText = value;
			}
		}

		public bool IsStagingMediaAndText {
			get { return isStagingMediaAndText; }
			set {
				if (value == true) {
					isStagingText = true;
					isStagingMedia = true;
				}
				isStagingMediaAndText = value;
			}
		}

        private StagingProcedurer stagingProcedurer = new StagingProcedurer ();
		protected StagingProcedurer StagingHelper { 
            get { return this.stagingProcedurer; }
            set { this.stagingProcedurer = value; }
        }
		#endregion

		public BackgroundColor backgroundColor {
			get {
				return sendingChatEntry.IsGroupChat () ? sendingChatEntry.contacts [0].colorTheme : sendingChatEntry.SenderColorTheme;
			}
		}

		public bool IsGroupConversation {
			get {
				// It's not a conversation if it's still a new message. and it's not a group chat if the sendingChatEntry is not a group chat.
				return !this.IsNewMessage && sendingChatEntry.IsGroupChat ();
			}
		}

		public bool IsDeletedGroupConversation {
			get {
				// It's not a conversation if it's still a new message. and it's not a group chat if the sendingChatEntry is not a group chat.
				return !this.IsNewMessage && sendingChatEntry.IsDeletedGroupChat ();
			}
		}

		public bool AllowsFromAliasChange {
			get {
				bool isGroupChat = sendingChatEntry.IsGroupChat ();
				return this.IsNewMessage && !isGroupChat;
			}
		}

		// should almost always be true, we mark a conversation un-editable
		// if we cannot send or receive to the convo any more (e.g. deleted alias,
		// counterparty opt out, undeliverable, etc)
        private bool editable = true;
		public bool Editable { 
            get { return this.editable; }
            set { this.editable = value; }
        }

		protected void MediaDidFailDownload (Notification notif) {
			Message message = (Message)notif.Source;
			DidReceiveFailedDownloadMessage (message);
		}

		protected void MediaDidChangeSoundState (Notification notif) {
			Message message = (Message)notif.Source;
		}

		SoundRecordingInlineController soundRecordingInlineController;
		public SoundRecordingInlineController SoundRecordingInlineController {
			get {
				if (this.soundRecordingInlineController == null) {
					this.soundRecordingInlineController = new SoundRecordingInlineController ();
				}
				return this.soundRecordingInlineController;
			}

			set {
				this.soundRecordingInlineController = value;
			}
		}

		SoundRecordingRecorderController soundRecordingRecorderController;
		public SoundRecordingRecorderController SoundRecordingRecorderController {
			get {
				if (this.soundRecordingRecorderController == null) {
					this.soundRecordingRecorderController = new SoundRecordingRecorderController ();
				}
				return this.soundRecordingRecorderController;
			}
		}	

		protected AbstractChatController (ApplicationModel applicationModel, ChatEntry ce, ChatEntry de) {
			this.Typers = new List<ContactSpecificTimerInfo> ();
			displayedMessage = null;
			typingMessage = null;
			appModel = applicationModel;

			chatList = applicationModel.chatList;

			// BEGIN Examples of NotificationCenter registering
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.Message_DownloadFailed, MediaDidFailDownload);
			// END Examples of NotificationCenter registering

			NotificationCenter.DefaultCenter.AddWeakObserver (null, ApplicationModel.IS_HANDLING_MISSED_MESSAGES_STATUS_UPDATE, MissedMessageStatusUpdate);

			DidAddMessageAtProxy = WeakDelegateProxy.CreateProxy<Message> (DidAddMessageAt);
			DidRemoteModifyMessageAtProxy = WeakDelegateProxy.CreateProxy<Message,int> (DidRemoteModifyMessageAt);
			DidMarkMessageHistoricalAtProxy = WeakDelegateProxy.CreateProxy<Message> (DidMarkMessageHistoricalAt);
			DidChangeStatusOfMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidChangeStatusOfMessage);
			DidReceiveTypingMessageProxy = WeakDelegateProxy.CreateProxy<Contact> (DidReceiveTypingMessage);

			DidReceiveStartUploadMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidReceiveStartUploadMessage);
			DidReceiveUploadPercentCompleteUpdateProxy = WeakDelegateProxy.CreateProxy<Message, double> (DidReceiveUploadPercentCompleteUpdate);
			DidReceiveCompleteUploadMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidReceiveCompleteUploadMessage);
			DidReceiveFailUploadMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidReceiveFailUploadMessage);

			DidReceiveStartDownloadMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidReceiveStartDownloadMessage);
			DidReceiveDownloadPercentCompleteUpdateProxy = WeakDelegateProxy.CreateProxy<Message, double> (DidReceiveDownloadPercentCompleteUpdate);
			DidReceiveCompleteDownloadMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidReceiveCompleteDownloadMessage);
			DidReceiveSoundStateChangeMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidReceiveSoundStateChangeMessage);

			WillStartBulkUpdatesProxy = WeakDelegateProxy.CreateProxy (WillStartReceivingBulkUpdates);
			DidFinishBulkUpdatesProxy = WeakDelegateProxy.CreateProxy (DidFinishReceivingBulkUpdates);

			DidFinishedStagingMediaProxy = WeakDelegateProxy.CreateProxy (DidFinishedStagingMedia);

			DidChangeOwnColorThemeProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (DidChangeColorTheme);

			DidChangeDisplayNameProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (AccountDidChangeDisplayName);

			DidChangeTotalUnreadCountProxy = WeakDelegateProxy.CreateProxy<int> (BackgroundChangeUnreadCount);
			chatList.DelegateTotalUnreadCountDidChange += DidChangeTotalUnreadCountProxy.HandleEvent<int>;

			chatList.DelegateWillStartBulkUpdates += WillStartBulkUpdatesProxy.HandleEvent;
			chatList.DelegateDidFinishBulkUpdates += DidFinishBulkUpdatesProxy.HandleEvent;

			sendingChatEntry = ce;

			viewVisible = false;

			displayedChatEntry = de;
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.Counterparty_DownloadCompleted, DidDownloadThumbnail);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.Counterparty_ThumbnailChanged , DidChangeThumbnail);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.Counterparty_DownloadFailed, DidFailThumbnailDownload);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ENTERING_BACKGROUND, WillEnterBackgroundEvent);

			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.ContactsManager_ProcessedDifferentContacts, ContactsManagerDidChangeContacts);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.Model_WillShowRemotePromptFromServerNotification, HandleNotificationWillShowRemotePrompt);

			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.STAGE_MEDIA_BEGIN, DidStagedMediaBegin);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.STAGE_MEDIA_DONE, DidStagedMediaEnd);

			appModel.account.accountInfo.DelegateDidChangeColorTheme += DidChangeOwnColorThemeProxy.HandleEvent<CounterParty>;
			appModel.account.accountInfo.DelegateDidChangeDisplayName += DidChangeDisplayNameProxy.HandleEvent<CounterParty>;

			InitialSetupFromAlias ();
		}

		private void HandleNotificationWillShowRemotePrompt (Notification e) {
			ClearInProgressRemoteActionMessages ();
		}

		private void InitialSetupFromAlias () {
			if (this.IsNewMessage) {
				if (sendingChatEntry != null && sendingChatEntry.contacts != null && sendingChatEntry.contacts.Count > 0) {
					Contact contact = this.sendingChatEntry.contacts [0];
					if (contact != null) {
						if (sendingChatEntry.IsGroupChat ()) {
							sendingChatEntry.fromAlias = contact.fromAlias;
						} else {
							string preferredContactToSendFromAlias = contact.LastUsedIdentifierToSendFrom;
							sendingChatEntry.fromAlias = preferredContactToSendFromAlias;
						}
					}
				}
			}
		}

		public void MissedMessageStatusUpdate (Notification n) {
			Dictionary<string, bool> extra = (Dictionary<string, bool>)n.Extra;
			bool isHandlingMissedMessages = extra [ApplicationModel.IS_HANDLING_MISSED_MESSAGES_STATUS_UPDATE_EXTRA];
			if (!isHandlingMissedMessages) {
				EMTask.DispatchMain (() => {
					GoToBottom ();
				});
			}
		}

		private void ContactsManagerDidChangeContacts (Notification n) {
			ReloadContactSearchContacts ();
		}

		~AbstractChatController() {
			// https://msdn.microsoft.com/en-us/library/b1yfkh5e%28v=VS.100%29.aspx
			Dispose (false);
		}

		public void Dispose () {
			Dispose (true);
			GC.SuppressFinalize (this);

			this.SoundRecordingInlineController.Dispose ();
			this.SoundRecordingInlineController = null;
		}

		bool hasDisposed = false;
		protected virtual void Dispose (bool disposing) {
			lock (this) {
				if (!hasDisposed) {

					if (disposing) {
						// Free other state (managed objects).
					}

					// Free your own state (unmanaged objects).
					// Set large fields to null.
					NotificationCenter.DefaultCenter.RemoveObserver (this);

					displayedChatEntry = null;
					appModel.account.accountInfo.DelegateDidChangeColorTheme -= DidChangeOwnColorThemeProxy.HandleEvent<CounterParty>;
					appModel.account.accountInfo.DelegateDidChangeDisplayName -= DidChangeDisplayNameProxy.HandleEvent<CounterParty>;

					chatList.DelegateTotalUnreadCountDidChange -= DidChangeTotalUnreadCountProxy.HandleEvent<int>;

					chatList.DelegateWillStartBulkUpdates -= WillStartBulkUpdatesProxy.HandleEvent;
					chatList.DelegateDidFinishBulkUpdates -= DidFinishBulkUpdatesProxy.HandleEvent;


					hasDisposed = true;
				}
			}
		}

		public bool HasNotDisposed() {
			lock (this) {
				return !hasDisposed;
			}
		}

		/**
		 * Called when we receive a Remote Prompt that we're about to show. We clear all progress indicators on remote message buttons.
		 */ 
		public abstract void ClearInProgressRemoteActionMessages ();

		public abstract void ReloadContactSearchContacts ();
		public abstract void PreloadImages (IList<Message> messages);

		/**
		 * Called to show the Details button in the top navigation bar. This should not be shown for new
		 * chats, only existing ones (to user, alias, group and Ad-hoc).
		 * Also, this should not be shown for chats with a deleted group
		 */
		public abstract void ShowDetailsOption (bool animated, bool forceShow);

		/**
		 * Messages may not be available immediately and are loaded in a background thread
		 * if this callback is loaded the view layer should essentially reload itself (if
		 * it already exists).  If the messages were available on view creation this may
		 * not be called.
		 */
		public abstract void DidFinishLoadingMessages ();

		public abstract void DidFinishLoadingPreviousMessages (int count);

		/**
		 * Called if this was a new conversation session where an option to add contacts
		 * is visible.  The view layer should hide this option when this is called.
		 */
		public abstract void HideAddContactsOption (bool animated);

		/**
		 * Called in response to a message getting sent.  The view should clear it's text
		 * entry area as it's now part of the conversation.
		 */
		public abstract void ClearTextEntryArea();

		/**
		 * Called before media is staged, during media file generation
		 */
		public abstract void StagedMediaBegin ();

		/**
		 * Called when media has been selected (staged)
		 */
		public abstract void StagedMediaAddedToStagingAndPreload ();

		/**
		 * Called after media has been added to the staging area, and media object is loaded
		 */
		public abstract void StagedMediaEnd ();

		/**
		 * Get aspect ratio of staged media
		 */
		public abstract float StagedMediaGetAspectRatio ();

		/**
		 * Get soudn recording duration in seconds of staged media
		 */
		public abstract float StagedMediaGetSoundRecordingDurationSeconds ();

		/**
		 * Called when media should be removed from the staging area's view.
		 */
		public abstract void StagedMediaRemovedFromStaging ();

		/**
		 * For views where the user is adding contacts, this callback indicates that
		 * it should update it's contacts view.
		 */
		public abstract void UpdateToContactsView ();

		/**
		 * For views where the user is adding a contact that is an alias
		 * it should update show the from bar.
		 */
		public abstract void UpdateFromBarVisibility (bool showFromBar);

		/**
		 * Toggling the ability for the user to select the from bar to choose a from alias.
		 * 
		 */
		public abstract void UpdateFromAliasPickerInteraction (bool shouldAllowInteraction);

		/**
		 * Handle updates to the structure and attributes of the messages
		 */
		public abstract void HandleMessageUpdates (IList<ModelStructureChange<Message>> structureChanges, IList<ModelAttributeChange<Message,object>> attributeChanges, bool animated, Action animationCompletedCallback);

		/**
		 * Indicates to the UI that it should send the send buttons
		 * state.
		 */
		public abstract void SetSendButtonMode (bool messageSendingAllowed, bool soundRecordingAllowed);

		/**
		 * Indicates the UI should show that a specific user is typing
		 */
		public abstract void ShowContactIsTyping (string typingMessage);

		/**
		 * Indicates the UI should stop showing that a specific user is typing
		 */
		public abstract void HideContactIsTyping ();

		/**
		 * Method called when a photo for a contact has downloaded (either intially or due to a change)
		 */
		public abstract void CounterpartyPhotoDownloaded (CounterParty counterparty);

		/**
		 * Method called so that the UI can confirm the take back is what the user wants to do
		 */
		public abstract void ConfirmRemoteTakeBack (int index);

		/**
		 * Method called so that the UI can confirm the moving of a message to historical status
		 */
		public abstract void ConfirmMarkHistorical(int index);

		/*
		 * Callback that the user has changed their own color scheme
		 */
		public abstract void DidChangeColorTheme ();
		/*
		 * Callback that the user has changed their display name
		 */ 
		public abstract void DidChangeDisplayName ();

		/*
		 * Callback that the total unread count has changed
		 */ 
		public abstract void DidChangeTotalUnread (int unreadCount);

		/*
		 * Function that generically handles UI changes to the chat rows.
		 */ 
		public abstract void UpdateChatRows (Message message);

		/*
		 * Function that pushes the controller view to the bottom of the chat.
		 */ 
		public abstract void GoToBottom ();

		/*
		 * Flag for controller to know if scrolling to the bottom of the chat is allowed.
		 */ 
		public abstract bool CanScrollToBottom { get; }

		/*
		 * Callback used when one of the parties in the conversation is nolonger active
		 */
		public abstract void ConversationContainsActive (bool active, InactiveConversationReason reason);

		/*
		 * Callback used when a user reenters an adhoc convseration they have left.
		 */
		public abstract void WarnLeftAdhoc();

		public abstract void UpdateTextEntryArea (string text);
		/*
		 * Callback used to pre-populate the To field to initiate an AKA search
		 */
		public abstract void PrepopulateToWithAKA (string toAka);

		public Message CreateMessageFromStagedMedia (string path, float heightToWidth) {
			string mimeType = ContentTypeHelper.GetContentTypeFromPath (path);
			DateTime sentDate = DateTime.Now.ToEMStandardTime (sendingChatEntry.appModel);
			em.Message message = em.Message.NewMessage (sendingChatEntry.appModel);
			message.chatEntry = sendingChatEntry;
			message.chatEntryID = sendingChatEntry.chatEntryID;
			message.inbound = "N";
			message.message = "<media>";
			message.sentDate = sentDate;

			string finalPath = ApplicationModel.SharedPlatform.GetFileSystemManager ()
				.MoveStagingContentsToFileStore (path, message);

			message.mediaRef = "file://" + Uri.EscapeUriString (finalPath);
			message.contentType = mimeType;
			message.heightToWidth = heightToWidth;

			return message;
		}

		public void SendMessagesFromStagedMedia (IList<Message> messages) {
			foreach (Message m in messages) {
				sendingChatEntry.AddMessageAsync (m, true);
			}
		}

		public void StageTextEntryFromString (string text) {
			UpdateUnderConstructionText (text);
			UpdateTextEntryArea(text);
			this.IsStagingText = true;
		}

		public void StageAudioFromStream (Stream s, string dest) {
			SoundRecordingRecorderController srrc = this.SoundRecordingRecorderController;
			if (srrc != null) {
				ApplicationModel.SharedPlatform.GetFileSystemManager ().CopyBytesToPath (dest, s, (double d) => { });
				Debug.WriteLine ("[StageAudioFromStream] Finished copying audio to destination " + dest);
				srrc.StageFromFile (dest);
			}
		}

		/**
		 * helper routine the view can use to update the under construction string
		 */
		public void UpdateUnderConstructionText(string text) {
			sendingChatEntry.underConstruction = text;
			sendingChatEntry.TypingAsync ();
			SetSendButtonMode (sendingChatEntry.MessageSendingAllowed, sendingChatEntry.SoundRecordingAllowed);
		}

		/**
		 * helper routine the view can use to indicate that the user has selected
		 * an item to stage.
		 */
		public void AddStagedItem(string pathToStagedValue) {
			bool success = this.StagingHelper.BeginStagingItemProcedure ();
			if (!success) {
				return;
			}

			if (!ApplicationModel.SharedPlatform.GetFileSystemManager ().FileExistsAtPath (pathToStagedValue)) {
				Debug.WriteLine ("will not stage non-existent file " + pathToStagedValue);
				if (sendingChatEntry.underConstructionMediaPath != null && sendingChatEntry.underConstructionMediaPath.Equals (pathToStagedValue)) {
					Debug.WriteLine ("clearing under construction with path  " + pathToStagedValue);
					sendingChatEntry.underConstructionMediaPath = null;
				}

				this.StagingHelper.EndStagingItemProcedure ();
				em.NotificationCenter.DefaultCenter.PostNotification (Constants.STAGE_MEDIA_DONE);
				return;
			}
				
			StagedMedia = Media.FindOrCreateMedia (new Uri ("file://" + Uri.EscapeUriString (pathToStagedValue)));
			StagedMedia.DelegateDidFinisLoadMedia += this.DidFinishedStagingMediaProxy.HandleEvent;
			sendingChatEntry.underConstructionMediaPath = pathToStagedValue;
			sendingChatEntry.SaveAsync ();

			StagedMediaAddedToStagingAndPreload ();
		}

		private void SetStagedMediaAspectRatio () {
			stagedMediaHeightToWidth = StagedMediaGetAspectRatio ();
			SetSendButtonMode (sendingChatEntry.MessageSendingAllowed, sendingChatEntry.SoundRecordingAllowed);

			stagedMedia.DelegateDidFinisLoadMedia -= this.DidFinishedStagingMediaProxy.HandleEvent;
		}

		private void SetStagedMediaSoundRecordingDurationIfApplicable () {
			this.stagedMediaSoundRecordingDurationSeconds = StagedMediaGetSoundRecordingDurationSeconds ();
		}

		/**
		 * helper routine the view can use to indicate that the user is removing a staged item
		 */
		public void RemoveStagedItem() {
			if (StagedMedia != null) {
				appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (sendingChatEntry.underConstructionMediaPath);

				sendingChatEntry.underConstructionMediaPath = null;
				sendingChatEntry.SaveAsync ();

				StagedMedia = null;
				stagedMediaHeightToWidth = 0;

				StagedMediaRemovedFromStaging ();
				SetSendButtonMode (sendingChatEntry.MessageSendingAllowed, sendingChatEntry.SoundRecordingAllowed);
			}

			sendingChatEntry.underConstruction = null;
		}

		public void ResponseToRemoteActionMessage (Message message) {
			this.appModel.UserDidRespondToRemoteActionButton (message.RemoteAction);
		}
			
		/**
		 * Helper method that sends the contents of what's in the staging area
		 */
		public void SendMessage() {
			if (this.StagingHelper.IsBusyStaging) {
				Debug.WriteLine ("AbstractChatController: Tried to SendMessage () but staging procedurer is busy staging.");
				return;
			}

			if (displayedChatEntry != sendingChatEntry) {
				// if there's no pre-existing chat entry then
				// the under construction one will become the
				// chat entry.
				if (displayedChatEntry == null)
					displayedChatEntry = sendingChatEntry;
				else {
					// otherwise there was a pre-existing conversation
					// to this list of contacts, so let's switching everything
					// over from the under construction chat entry.
					displayedChatEntry.underConstruction = sendingChatEntry.underConstruction;
					displayedChatEntry.underConstructionMediaPath = sendingChatEntry.underConstructionMediaPath;

					// if there was media associated make sure it's handed off to the existing
					// conversation, we don't want it inadvertantly getting cleaned up.
					sendingChatEntry.underConstructionMediaPath = null;
					sendingChatEntry.leftAdhoc = false;
					sendingChatEntry.SaveAsync ();

					sendingChatEntry = displayedChatEntry;
				}

				HideAddContactsOption (true);
				UpdateFromBarVisibility (false);
			}

			DateTime sentDate = DateTime.Now.ToEMStandardTime(appModel);
			if (IsNewMessage) {
				sendingChatEntry.createDate = sentDate;
				Debug.Assert (sendingChatEntry.contacts.Count > 0, "AbstractChatController:SendMessage: When sending a message, sendingChatEntry's contact count is 0.");
				chatList.underConstruction = null;
				HideAddContactsOption (true);
				UpdateFromBarVisibility (false);
			}

			if (StagedMedia != null) {
				Message message = Message.NewMessage (appModel);
				message.chatEntry = sendingChatEntry;
				message.chatEntryID = sendingChatEntry.chatEntryID;
				message.inbound = "N";
				message.message = "<media>";
				message.sentDate = sentDate;

				string finalPath = appModel.platformFactory.GetFileSystemManager ().MoveStagingContentsToFileStore (StagedMedia.uri.LocalPath, message);

				message.mediaRef = "file://" + Uri.EscapeUriString (finalPath);
				message.media.NativeThumbnail = stagedMedia.NativeThumbnail;
				message.contentType = ContentTypeHelper.GetContentTypeFromPath (StagedMedia.uri.AbsolutePath);
				message.heightToWidth = stagedMediaHeightToWidth;
				message.soundRecordingDurationSeconds = stagedMediaSoundRecordingDurationSeconds;
				sendingChatEntry.AddMessageAsync (message, true);
			}
				
			if (!string.IsNullOrEmpty (sendingChatEntry.underConstruction)) {
				Message message = Message.NewMessage (appModel);
				message.chatEntry = sendingChatEntry;
				message.chatEntryID = sendingChatEntry.chatEntryID;
				message.inbound = "N";
				message.message = sendingChatEntry.underConstruction.Trim ();
				message.sentDate = sentDate;
				sendingChatEntry.AddMessageAsync (message, true);
			}

			RemoveStagedItem ();
			ClearTextEntryArea ();
			SetSendButtonMode (sendingChatEntry.MessageSendingAllowed, sendingChatEntry.SoundRecordingAllowed);
			ShowDetailsOption (true, true);
		}

		public bool IsRemoteDeleteOkay(int indexOf) {
			Message message = viewModel[indexOf];
			return message.messageLifecycle != MessageLifecycle.deleted && !message.IsInbound () && message.messageChannel == MessageChannel.em;
		}

		public bool IsCopyTextOkay (int indexOf) {
			Message message = viewModel [indexOf];
			bool isTextMessage = !message.HasMedia ();
			return isTextMessage;
		}

		public void CopyTextToClipboard (int indexOf) {
			Message message = viewModel [indexOf];
			string text = message.message;
			appModel.platformFactory.CopyToClipboard (text);
		}

		public void InitiateRemoteTakeBack(int indexOf) {
			ConfirmRemoteTakeBack (indexOf);
		}

		public void ContinueRemoteTakeBack(int indexOf) {
			Message message = viewModel[indexOf];
			displayedChatEntry.RemoteTakeBackAsync (message);
		}

		public void InitiateMarkHistorical(int indexOf) {
			ConfirmMarkHistorical (indexOf);
		}

		public void ContinueMarkHistorical(int indexOf) {
			Message message = viewModel[indexOf];
			displayedChatEntry.MarkHistoricalAsync (message);
		}

		public bool ShowAddContacts() {
			int index = chatList.entries.IndexOf (sendingChatEntry);
			return index == -1;
		}

		public int CurrentRowForFromAliasPicker () {
			int currentRow = 0;
			IList<AliasInfo> aliases = this.appModel.account.accountInfo.ActiveAliases;
			string currentFromAlias = this.sendingChatEntry.fromAlias;
			if (currentFromAlias == null)
				currentRow = aliases.Count;
			else {
				foreach (AliasInfo aI in aliases) {
					if (aI.serverID.Equals (currentFromAlias)) {
						currentRow = aliases.IndexOf (aI);
					}
				}
			}
			return currentRow;
		}


		/**
		 * Helper routine indicating that the user
		 * has made changes to the sending chat entry's contact list.
		 * (only allowed prior to a message getting sent)
		 */
		public void ManageContactsAfterAddressBookResult (AddressBookSelectionResult result) {
			IList<Contact> contacts = result.Contacts;
			sendingChatEntry.contacts = result.Contacts;

			if (sendingChatEntry.contacts.Count == 0) {
				UpdateSendingChatEntrysFromAliasOnRemove ();
			} else {
				// Clearing the from alias on the chat entry.
				sendingChatEntry.fromAliasSilent = null;
				UpdateSendingChatEntrysFromAliasOnAdd (sendingChatEntry.contacts [0]);
			}

			RunPossibleChangesAfterContactChange ();
		}

		/**
		 * Helper routine indicating that the user
		 * has added a new contact to the chat entry
		 * (only allowed prior to a message getting sent)
		 */
		public void AddContactToReplyTo (Contact newContact) {
			sendingChatEntry.AddContact (newContact);
			UpdateSendingChatEntrysFromAliasOnAdd (newContact);
			RunPossibleChangesAfterContactChange ();
		}

		public void RemoveContactToReplyToAt (int index) {
			sendingChatEntry.RemoveContactAt (index);
			UpdateSendingChatEntrysFromAliasOnRemove ();
			RunPossibleChangesAfterContactChange ();
		}

		private void RunPossibleChangesAfterContactChange () {
			SetSendButtonMode (sendingChatEntry.MessageSendingAllowed, sendingChatEntry.SoundRecordingAllowed);
			UpdateToContactsView ();
			PossibleAllowFromAliasToBePickedChange ();
			PossibleFromBarVisibilityChange ();
			SearchForExistingChatEntry ();
		}

		private void UpdateSendingChatEntrysFromAliasOnAdd (Contact newContact) {
			// Set the initial chat entry's from alias to the first contact we get.
			if (sendingChatEntry.fromAlias == null) {
				string preferredContactToSendFromAlias = newContact.LastUsedIdentifierToSendFrom;
				if (preferredContactToSendFromAlias != null) {
					sendingChatEntry.fromAlias = preferredContactToSendFromAlias;
				} else {
					sendingChatEntry.fromAlias = newContact.fromAlias;
				}
			}
		}

		private void UpdateSendingChatEntrysFromAliasOnRemove () {
			// For cases where we are removing a contact from the list of entries to send to.
			if (sendingChatEntry.contacts == null || sendingChatEntry.contacts.Count == 0) {
				// If there are no contacts, it's fine to set the fromAlias of the chat entry back to null.
				sendingChatEntry.fromAlias = null;
			}
		}

		public void UpdateFromAlias (AliasInfo aliasInfo) {
			this.sendingChatEntry.fromAlias = aliasInfo == null ? null : aliasInfo.serverID;
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					if (self.sendingChatEntry.fromAlias == null) {
						self.UpdateAliasText (self.appModel.account.accountInfo.defaultName);
					} else {
						if (aliasInfo != null) {
							self.UpdateAliasText (aliasInfo.displayName);
						}
					}
				}
			}, HasNotDisposed);

			SearchForExistingChatEntry ();
		}

		public void PossibleAllowFromAliasToBePickedChange () {
			UpdateFromAliasPickerInteraction (this.AllowsFromAliasChange);
		}

		public void PossibleFromBarVisibilityChange () {
			bool show = false;
			if (!this.IsNewMessage || this.IsGroupConversation) {
				UpdateFromBarVisibility (show);
				return;
			}
				
			IList<AliasInfo> aliases = appModel.account.accountInfo.ActiveAliases;
			if (aliases.Count > 0)
				show = true;

			WeakReference thisRef = new WeakReference (this);

			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					self.UpdateFromBarVisibility (show);

					if (self.sendingChatEntry.fromAlias == null) {
						self.UpdateAliasText (appModel.account.accountInfo.defaultName);
					} else {
						AliasInfo aliasInfo = appModel.account.accountInfo.AliasFromServerID (this.sendingChatEntry.fromAlias);
						// AliasInfo should be non null here. The sendingChatEntry's fromAlias should always match a fromAlias that we already have.
						Debug.Assert (aliasInfo != null, "PossibleFromBarVisibilityChange: AliasInfo is null");
						if (aliasInfo != null) {
							self.UpdateAliasText (aliasInfo.displayName);
						}
					}
				}
			}, HasNotDisposed);
		}

		protected void SearchForExistingChatEntry() {
			ChatEntry existing = chatList.FindChatEntryByReplyToServerIDs (sendingChatEntry.contactIDs(), sendingChatEntry.fromAlias);
			bool shouldReload = displayedChatEntry != existing;
			displayedChatEntry = existing;
			if (shouldReload)
				DidFinishLoadingMessages ();  // TODO this method name is misleading, we are trying to just reload the UI
		}

		public void RemoveContactToReplyTo () {
			sendingChatEntry.RemoveLastContact ();
			PossibleAllowFromAliasToBePickedChange ();
			SetSendButtonMode (sendingChatEntry.MessageSendingAllowed, sendingChatEntry.SoundRecordingAllowed);
		}

		public void ClearContactsToReplyTo () {
			sendingChatEntry.ClearAllContacts ();
			PossibleAllowFromAliasToBePickedChange ();
			UpdateToContactsView ();
		}
			
		public abstract void UpdateAliasText (string text);

        private bool checkIfConversationIsValid = true;
		private bool CheckIfConversationIsValid { 
            get { return this.checkIfConversationIsValid; }
            set { this.checkIfConversationIsValid = value; }
        }

		/**
		 * Helper method indicating that the view has become
		 * visible to the user
		 */
		public void ViewBecameVisible() {
			viewVisible = true;
			if ( displayedChatEntry != null )
				displayedChatEntry.MarkAllUnreadReadAsync ();

			if (CheckIfConversationIsValid) {
				if (displayedChatEntry != null ) {
					if (displayedChatEntry.leftAdhoc) {
						WarnLeftAdhoc ();
					} else if (displayedChatEntry.FromAliasInActive) {
						SetConversationAsActive (false, InactiveConversationReason.FromAliasInActive);
					} else if (!displayedChatEntry.AreAllCounterpartiesActive ()) {
						SetConversationAsActive (false, InactiveConversationReason.Other);
					}
				}

				CheckIfConversationIsValid = false;
			}

			//Pre-populate the To Text field with an AKA that gets searched for when creating the chat conversation
			//Currently, this is used when a WH user clicks on another's EM AKA and we pop into EM
			if (sendingChatEntry.ShouldPrepopulateToField) {
				PrepopulateToWithAKA (sendingChatEntry.prePopulatedInfo.ToAKA);
			}

			PossibleAllowFromAliasToBePickedChange ();
			PossibleFromBarVisibilityChange ();
		}

		private void SetConversationAsActive (bool active, InactiveConversationReason reason) {
			Editable = active;
			ConversationContainsActive (active, reason);
		}

        private bool updateConversationIsInvalid = true;
		private bool UpdateConversationIsInvalid { 
            get { return this.updateConversationIsInvalid; }
            set { this.updateConversationIsInvalid = value; }
        }

		void ChatEntryCounterpartyDidChangeLifecycle (Notification notif) {
			WeakReference thisRef = new WeakReference (this);
			UpdateConversationIsInvalid = true;
			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					if (self.UpdateConversationIsInvalid) {
						ChatEntry displayedChatEntry = self.displayedChatEntry;

						if (displayedChatEntry.FromAliasInActive) {
							self.SetConversationAsActive (false, InactiveConversationReason.FromAliasInActive);
						} else {
							bool allCounterpartiesActive = displayedChatEntry.AreAllCounterpartiesActive ();
							self.SetConversationAsActive (allCounterpartiesActive, allCounterpartiesActive ? InactiveConversationReason.Success : InactiveConversationReason.Other);
						}

						self.UpdateConversationIsInvalid = false;
					}
				}
			}, HasNotDisposed);
		}

		/**
		 * Helper method indicating that the view is nolonger
		 * visible to the user.
		 */
		public void ViewBecameHidden() {
			viewVisible = false;
			ResetMediaDownloadState ();
		}

		private void ResetMediaDownloadState () {
			IList<Message> listOfMessages = this.messages;
			InnerResetMediaDownloadState (listOfMessages);
		}

		private const int MaxMessagesToLoopWhileResettingMediaState = 100;
		private void InnerResetMediaDownloadState (IList<Message> listOfMessages, int index = 0) {
			EMTask.DispatchMain (() => {
				if (listOfMessages == null || listOfMessages.Count == index) {
					return;
				}

				int messageCount = listOfMessages.Count;

				int offset = index;
				for (int i = 0; i < MaxMessagesToLoopWhileResettingMediaState; i++) {
					int messageIndex = i + offset;
					if (messageIndex >= messageCount) {
						break;
					} else {
						Message potentialMediaMessage = listOfMessages [messageIndex];
						if (potentialMediaMessage.HasMedia ()) {
							Media potentialFailedMedia = potentialMediaMessage.media;
							if (potentialFailedMedia.MediaState == MediaState.FailedDownload) {
								potentialFailedMedia.MediaState = MediaState.Absent;
							}
						}

						index = messageIndex + 1;
					}
				}

				InnerResetMediaDownloadState (listOfMessages, index);
			});
		}

		private void WillEnterBackgroundEvent (Notification n) {
			Debug.Assert (ApplicationModel.SharedPlatform.OnMainThread, "AbstractChatController - WillEnterBackgroundEvent called but not on main thread.");
			ResetMediaDownloadState ();
		}

		protected void DidAddMessageAt (Message message) {
			lock (this) {
				if ( messages != null ) { 
					pendingStructureChanges.Add (new ModelStructureChange<Message> (message, ModelStructureChange.added));

					if (!SuspendedUpdates)
						PostStructureAndAttributeChanges ();

					if (message.HasMedia ()) {
						EMTask.DispatchBackground (() => {
							NotificationCenter.DefaultCenter.PostNotification (Constants.AbstractChatController_NewMediaMessageAdded);
						});
					}
				}
			}
		}

		protected void DidRemoteModifyMessageAt(Message message, int modifyMask) {
			if ((modifyMask & ChatEntry.MODIFY_MASK_TAKEN_BACK) == ChatEntry.MODIFY_MASK_TAKEN_BACK) {
				lock (this) {
					pendingAttribute.Add (new ModelAttributeChange<Message,object> (message, MESSAGE_ATTRIBUTE_TAKEN_BACK, true));

					if ( !SuspendedUpdates )
						PostStructureAndAttributeChanges ();
				}
			}
		}

		protected void DidMarkMessageHistoricalAt (Message message) {
			lock (this) {
				pendingStructureChanges.Add (new ModelStructureChange<Message> (message, ModelStructureChange.deleted));

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges ();
			}
		}

		protected void PostStructureAndAttributeChanges() {
			lock (this) {
				if (pendingAttribute.Count == 0 && pendingStructureChanges.Count == 0)
					return;
				
				SuspendUpdates ();

				IList<ModelStructureChange<Message>> structureChanges = null;
				if (pendingStructureChanges.Count > 0) {
					structureChanges = new List<ModelStructureChange<Message>> ();
					foreach (ModelStructureChange<Message> m in pendingStructureChanges)
						structureChanges.Add (m);

					pendingStructureChanges.Clear ();
				}

				IList<ModelAttributeChange<Message,object>> atrributeChanges = null;
				if (pendingAttribute.Count > 0) {
					atrributeChanges = new List<ModelAttributeChange<Message,object>> ();
					foreach (ModelAttributeChange<Message,object> m in pendingAttribute)
						atrributeChanges.Add (m);

					pendingAttribute.Clear ();
				}

				WeakReference thisRef = new WeakReference (this);
				EMTask.DispatchMain (() => {
					AbstractChatController self = thisRef.Target as AbstractChatController;
					if (self != null) {
						if ( structureChanges != null ) {
							foreach (ModelStructureChange<Message> change in structureChanges) {
								if (change.Change == ModelStructureChange.added) {
									int index = messages.IndexOf (change.ModelObject);
									if (index > viewModel.Count - 1)
										viewModel.Add (change.ModelObject);
									else
										viewModel.Insert (index, change.ModelObject);
								} else if (change.Change == ModelStructureChange.deleted) {
									viewModel.Remove (change.ModelObject);
								}
							}
						}

						if (self.viewVisible)
							self.displayedChatEntry.MarkAllUnreadReadAsync ();

						self.HandleMessageUpdates (structureChanges, atrributeChanges, true, () => {
							ResumeUpdates ();
						});
					}
				}, HasNotDisposed);
			}
		}

		protected void DidChangeStatusOfMessage (Message message) {
			lock (this) {
				if (viewModel.Contains (message)) {
					pendingAttribute.Add (new ModelAttributeChange<Message,object> (message, MESSAGE_ATTRIBUTE_MESSAGE_STATUS, message.messageStatus));

					if (!SuspendedUpdates)
						PostStructureAndAttributeChanges ();
				}
			}
		}

		protected void DidReceiveTypingMessage (Contact contact) {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					ContactSpecificTimerInfo info = new ContactSpecificTimerInfo (contact);
					if ( !self.Typers.Contains(info)) {
						self.Typers.Add (info);
					} else {
						int index = self.Typers.IndexOf (info);
						info = self.Typers [index];
					}

					self.SetTypingMessage ();
					self.ScheduleHideTypingTimer (info);
					self.ShowContactIsTyping (typingMessage);
				}
			}, HasNotDisposed);
		}

		/*
		 * Called if an incoming message arrives to clear the remote typing message
		 */
		protected void ClearTypingMessage() {
			this.Typers.Clear ();
			SetTypingMessage ();
			displayedMessage = null;
			HideContactIsTyping ();
		}

		protected void SetTypingMessage() {
			switch (this.Typers.Count) {
			case 0:
				typingMessage = null;
				break;

			case 1:
				typingMessage = string.Format (appModel.platformFactory.GetTranslation ("IS_TYPING_SINGULAR"), Typers [0].contact.displayName);
				break;

			case 2:
				typingMessage = string.Format (appModel.platformFactory.GetTranslation ("IS_TYPING_DUAL"), Typers [0].contact.displayName, Typers [1].contact.displayName);
				break;

			default:
				typingMessage = appModel.platformFactory.GetTranslation ("IS_TYPING_MULTIPLE");
				break;
			}
		}

		void ScheduleHideTypingTimer (ContactSpecificTimerInfo info) {
			var thisRef = new WeakReference (this);
			info.lastTypingMessageDateTime = DateTime.Now;
			info.showTypingAnimationTimer = new Timer ((object o) => EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					var parms = (object[])o;
					var startTime = (DateTime)parms [0];
					var ct = (Contact)parms [1];
					int indexOf = self.Typers.IndexOf (new ContactSpecificTimerInfo (ct));
					ContactSpecificTimerInfo theInfo = indexOf == -1 ? null : Typers [indexOf];
					// we make sure this timer firing is the 
					if (theInfo != null && theInfo.lastTypingMessageDateTime.Equals (startTime)) {
						self.Typers.Remove (theInfo);
						self.SetTypingMessage ();
						if (self.Typers.Count == 0) {
							self.HideContactIsTyping ();
						} else {
							self.ShowContactIsTyping (typingMessage);
						}
					}
				}
			}, HasNotDisposed), new object[] { info.lastTypingMessageDateTime, info.contact }, SHOW_TYPING_DELAY_MILLIS, Timeout.Infinite);
		}

		public string ImageSearchSeedString {
			get {
				string seed = string.Empty;
				IList<Message> viewMessages = this.viewModel;

				int count = viewMessages != null ? viewMessages.Count : 0;
				while (count > 0) {
					int index = count - 1;
					Message message = viewMessages [index];
					if (!message.HasMedia ()) {
						seed = message.message;
						break;
					}

					count--;
				}

				return seed;
			}
		}

		/** delegators for updating upload progress bar **/

		protected void DidReceiveStartUploadMessage (Message message) {
			UpdateChatRowsWithMessage (message);
		}

		protected void DidReceiveUploadPercentCompleteUpdate (Message message, double compPerc) {
			UpdateChatRowsWithMessage (message);
		}

		protected void DidReceiveCompleteUploadMessage (Message message) {
			UpdateChatRowsWithMessage (message);
		}

		protected void DidReceiveFailUploadMessage (Message message) {
			UpdateChatRowsWithMessage (message);
		}

		/** delegators for updating download progress bar **/

		protected void DidReceiveStartDownloadMessage (Message message) {
			UpdateChatRowsWithMessage (message);
		}

		protected void DidReceiveDownloadPercentCompleteUpdate (Message message, double compPerc) {
			UpdateChatRowsWithMessage (message);
		}

		protected void DidReceiveCompleteDownloadMessage (Message message) {
			UpdateChatRowsWithMessage (message);
		}

		protected void DidReceiveFailedDownloadMessage (Message message) {
			UpdateChatRowsWithMessage (message);
		}

		protected void DidReceiveSoundStateChangeMessage (Message message) {
			UpdateChatRowsWithMessage (message);
		}

		private void UpdateChatRowsWithMessage (Message message) {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					self.UpdateChatRows (message);
				}
			}, HasNotDisposed);
		}

		private void DidStagedMediaBegin (Notification notif) {
			StagedMediaBegin ();
		}

		protected void DidFinishedStagingMedia () {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					try {
						self.SetStagedMediaAspectRatio();
						self.SetStagedMediaSoundRecordingDurationIfApplicable();

					} catch (Exception e) {
						Debug.WriteLine ("DidFinishedStagingMedia problem acquring staged media attributes {0}", e);
					} finally {
						self.StagingHelper.EndStagingItemProcedure ();
						em.NotificationCenter.DefaultCenter.PostNotification (Constants.STAGE_MEDIA_DONE);
					}
				}
			}, HasNotDisposed);
		}

		private void DidStagedMediaEnd (Notification notif) {
			StagedMediaEnd ();
		}

		protected void DidDownloadThumbnail (Notification notif) {
			CounterParty counterparty = notif.Source as CounterParty;
			WeakReference thisRef = new WeakReference (this);
			if (counterparty != null) {
				EMTask.DispatchMain (() => {
					AbstractChatController self = thisRef.Target as AbstractChatController;
					CounterpartyPhotoDownloaded (counterparty);
					Contact c = counterparty as Contact;
					if (self != null) {
						if (c != null && SearchContacts.Contains(c)) {
							self.ContactSearchPhotoUpdated ((Contact)c);	
						}
					}
				}, HasNotDisposed);
			}
		}

		protected void DidChangeThumbnail(Notification notif) {
			CounterParty counterparty = notif.Source as CounterParty;
			WeakReference thisRef = new WeakReference (this);

			if (counterparty != null && counterparty.media != null) {
				EMTask.DispatchMain (() => {
					AbstractChatController self = thisRef.Target as AbstractChatController;
					counterparty.media.DownloadMedia (appModel);
					Contact c = counterparty as Contact;
					if (self != null) {
						if (c != null && SearchContacts.Contains (c)) {
							self.ContactSearchPhotoUpdated ((Contact)c);
						}
					}
				}, HasNotDisposed);
			}
		}

		protected void DidFailThumbnailDownload (Notification notif) {
			DidChangeThumbnail (notif);
		}

		protected void DidChangeColorTheme (CounterParty accountInfo) {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					self.DidChangeColorTheme ();
				}
			}, HasNotDisposed);
		}

		protected void AccountDidChangeDisplayName (CounterParty accountInfo) {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					self.DidChangeDisplayName ();
				}
			}, HasNotDisposed);
		}

		protected void WillStartReceivingBulkUpdates () {
			//this is called on the main thread
			SuspendUpdates ();
		}

		protected void DidFinishReceivingBulkUpdates () {
			//this is called on the main thread
			ResumeUpdates ();
		}

		#region updating unread count
		private void BackgroundChangeUnreadCount (int unreadCount) {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractChatController self = thisRef.Target as AbstractChatController;
				if (self != null) {
					self.DidChangeTotalUnread (unreadCount);
				}
			});
		}
		#endregion

		class ContactSpecificTimerInfo {
			public DateTime lastTypingMessageDateTime;
			public Contact contact;
			public Timer showTypingAnimationTimer;

			public ContactSpecificTimerInfo(Contact c) {
				contact = c;
			}

			public override bool Equals(object obj) {
				var other = obj as ContactSpecificTimerInfo;
				return contact.Equals (other.contact);
			}

			public override int GetHashCode () {
				return contact.GetHashCode ();
			}
		}
	}
}