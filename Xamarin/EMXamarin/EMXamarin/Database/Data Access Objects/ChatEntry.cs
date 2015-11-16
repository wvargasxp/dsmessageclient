using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using em;
using EMXamarin;

namespace em {
	public class ChatEntry : BaseDataR {
		public static readonly string CHATENTRY_COUNTERPARTY_LIFECYCLE_HAS_CHANGED = "CHATENTRY_COUNTERPARTY_LIFECYCLE_HAS_CHANGED";

		public int chatEntryID { get; set; }

		public long entryOrder { get; set; }
		public string preview { get; set; }

		DateTime pd;
		public DateTime previewDate { 
			get { return pd; } 
			set { pd = value.ToUniversalTime ();  } 
		}

		public string previewDateString {
			get {
				var offset = new DateTimeOffset(previewDate, TimeSpan.Zero);
				return offset.ToString(Constants.ISO_DATE_FORMAT, Preference.usEnglishCulture);
			}
			set { 
				// We need to set the Assume Universal flag here because without it, the value that will be set to previewDate will be of Kind 'Unspecified'. 
				// Converting this 'Unspecified' value to UTC changes its value (in the setter function of previewDate).
				previewDate = DateTime.ParseExact (value, 
					Constants.ISO_DATE_FORMAT, 
					Preference.usEnglishCulture, 
					System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal);
			}
		}

		public string FormattedPreviewDate {
			get {

				string formattedPreviewDate = this.appModel.platformFactory.GetFormattedDate (this.previewDate, DateFormatStyle.PartialDate);
				return formattedPreviewDate;
			}
		}

		public string underConstruction { get; set; }
		public string underConstructionMediaPath { get; set; }

		string fA; // fromAlias
		public string fromAliasSilent { get { return fA; } set { fA = value; } }
		public string fromAlias { 
			get { return fA; } 
			set {
				if (fA != null) {
					AliasInfo alias = appModel.account.accountInfo.AliasFromServerID (fA);
					if (alias != null)
						alias.DelegateDidChangeLifecycle -= CounterpartyDidChangeLifecycle;
				}

				fA = value;

				if (fA != null) {
					AliasInfo alias = appModel.account.accountInfo.AliasFromServerID (fA);
					if (alias != null)
						alias.DelegateDidChangeLifecycle += CounterpartyDidChangeLifecycle;
				}
			}
		}

		bool lah;
		public bool leftAdhoc { get { return lah; } set { lah = value; } }
		public string leftAdhocString { get { return lah ? "Y" : "N"; } set { lah = value.Equals ("Y"); } }

		IList<Contact> c;
		public IList<Contact> contacts {
			get { return c; }
			set {
				IList<Contact> oldContacts = c;
				c = value;

				if (oldContacts != null) {
					// unregister from prior contacts
					foreach (Contact contact in oldContacts)
						RemoveDelegates (contact);
				}

				if ( c != null && c.Count > 0 ) {
					// register current contacts
					foreach (Contact contact in c)
						AddDelegates (contact);
				}
			}
		}

		public bool HasAKAContact {
			get {
				IList<Contact> ctcs = this.contacts;
				if (ctcs == null || ctcs.Count == 0) {
					return false;
				}

				foreach (Contact contact in ctcs) {
					if (contact.IsAKA) {
						return true;
					}
				}

				return false;
			}
		}

		DateTime cd;
		public DateTime createDate { get { return cd; } set { cd = value.ToUniversalTime (); } }
		public string createDateString {
			get {
				var offset = new DateTimeOffset(createDate,TimeSpan.Zero);
				return offset.ToString(Constants.ISO_DATE_FORMAT, Preference.usEnglishCulture);
			}
			set {
				createDate = DateTime.ParseExact (value, 
					Constants.ISO_DATE_FORMAT, 
					Preference.usEnglishCulture, 
					System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal);
			}
		}

		bool ur;
		public bool hasUnread {
			get { return ur; }
			set { ur = value; }
		}
		public string hasUnreadString {
			get { return hasUnread ? "Y" : "N"; }
			set { hasUnread = value.Equals ("Y"); }
		}

		ChatList cl;
		public ChatList chatList { 
			get { return cl; } 
			set {
				cl = value;
				appModel = cl.appModel;
			} 
		}

		PrepopulatedChatEntryInfo ppcei;
		public PrepopulatedChatEntryInfo prePopulatedInfo {
			get { return ppcei; }
			set { 
				ppcei = value;

				if (ppcei != null && ppcei.FromAKA != null) {
					var fa = appModel.account.accountInfo.AliasFromName (ppcei.FromAKA);
					if (fa != null)
						fromAlias = fa.serverID;
				}
			}
		}

		public bool UnderConstructionOccupiedWithText {
			get {
				return !string.IsNullOrWhiteSpace (underConstruction);
			}
		}

		public bool UnderConstructionOccupiedWithMedia {
			get {
				return underConstructionMediaPath != null;
			}
		}

		public bool StagingAreaOccupied {
			get {
				return this.UnderConstructionOccupiedWithText || this.UnderConstructionOccupiedWithMedia;
			}
		}

		public bool ContactsToSendToDoesExist {
			get {
				return contacts != null && contacts.Count > 0;
			}
		}

		public bool MessageSendingAllowed {
			get {
				// Function should be used to check if the send button in Chat View should be enabled or not.
				return this.ContactsToSendToDoesExist && this.StagingAreaOccupied;
			}
		}

		public bool SoundRecordingAllowed {
			get {
				return this.ContactsToSendToDoesExist && !this.StagingAreaOccupied;
			}
		}

		public bool ShouldPrepopulateToField {
			get {
				return prePopulatedInfo != null && !string.IsNullOrEmpty (prePopulatedInfo.ToAKA);
			}
		}

		public class Conversation {
			public bool CanLoadMorePreviousMessages { get; set; }

			public Conversation () {
				cachedConversationRef = new WeakReference (null);
			}

			WeakReference cachedConversationRef;
			public IList<Message> CachedMessages {
				get { return cachedConversationRef.Target as IList<Message>; }
				set { cachedConversationRef = new WeakReference (value); }
			}
		}

		Conversation cachedConversation;
		public Conversation CachedConversation {
			get { return cachedConversation; }
			set { cachedConversation = value; }
		}

		public delegate void DidAddMessageAt(Message message);
		public DidAddMessageAt DelegateDidAddMessage = delegate(Message message) { };

		public static readonly int MODIFY_MASK_TAKEN_BACK = 1;
		public static readonly int MODIFY_MASK_ATTRIBUTES = 1 << 1;
		public static readonly int MODIFY_MASK_TEXT = 1 << 2;
		public static readonly int MODIFY_MASK_MEDIA_REF = 1 << 3;

		public delegate void DidRemoteModifyMessageAt(Message message, int modifyMask);
		public DidRemoteModifyMessageAt DelegateDidRemoteModifyMessage = delegate(Message message, int modifyMask) { };

		public delegate void DidMarkMessageHistoricalAt(Message message);
		public DidMarkMessageHistoricalAt DelegateDidMarkMessageHistoricalAt = delegate(Message message) { };

		public delegate void DidChangeStatusOfMessageAt(Message message);
		public DidChangeStatusOfMessageAt DelegateDidChangeStatusOfMessage = delegate(Message message) { };

		public delegate void DidReceiveTypingMessage(Contact contact);
		public DidReceiveTypingMessage DelegateDidReceiveTypingMessage = delegate(Contact contact) { };

		public delegate void DidStartUpload(Message message);
		public DidStartUpload DelegateDidStartUpload = delegate(Message message) { };

		public delegate void DidUpdateUploadPercentComplete(Message message, double percentComplete);
		public DidUpdateUploadPercentComplete DelegateDidUpdateUploadPercentComplete = delegate(Message message, double percentComplete) { };

		public delegate void DidCompleteUpload(Message message);
		public DidCompleteUpload DelegateDidCompleteUpload = delegate(Message message) { };

		public delegate void DidFailUpload(Message message);
		public DidCompleteUpload DelegateDidFailUpload = delegate(Message message) { };

		public delegate void DidStartDownload(Message message);
		public DidStartDownload DelegateDidStartDownload = delegate(Message message) { };

		public delegate void DidUpdateDownloadPercentComplete(Message message, double percentComplete);
		public DidUpdateDownloadPercentComplete DelegateDidUpdateDownloadPercentComplete = delegate(Message message, double percentComplete) { };

		public delegate void DidCompleteDownload(Message message);
		public DidCompleteDownload DelegateDidCompleteDownload = delegate(Message message) { };

		public delegate void DidChangeSoundState(Message message);
		public DidChangeSoundState DelegateDidChangeSoundState = delegate(Message message) { };

		public delegate void DidCompleteContactThumbnailDownload(Contact contact);
		public DidCompleteContactThumbnailDownload DelegateDidCompleteContactThumbnailDownload = (Contact contact) => { };

		public delegate void ContactDidLeaveAdhoc(Contact contact);
		public ContactDidLeaveAdhoc DelegateContactDidLeaveAdhoc = (Contact contact) => { };

		public void DidReceiveMessageUpdate(Message message, MessageStatus status) {
			var conversation = this.CachedConversation.CachedMessages;
			if (conversation != null) {
				if (conversation.Contains (message)) {
					DelegateDidChangeStatusOfMessage (message);
				}
			}
		}

		public ChatEntry () {
			contacts = new List<Contact> ();
			this.CachedConversation = new Conversation ();
		}

		~ChatEntry() {
			ClearAllContacts ();
		}

		public static ChatEntry NewChatEntry(ApplicationModel _appModel, DateTime createDate) {
			var retVal = new ChatEntry ();
			retVal.isPersisted = false;
			retVal.chatList = _appModel.chatList;
			retVal.createDate = createDate;

			return retVal;
		}

		public static ChatEntry NewUnderConstructionChatEntry(ApplicationModel _appModel, DateTime createDate) {
			// this is a potential cleanup step, incase the app crashed with possibly some staged media
			// under construction.  Usually there's nothing there.
			_appModel.chatList.RemoveChatEntry (-1, false);

			ChatEntry retVal = NewChatEntry (_appModel, createDate);

			retVal.chatList.SaveUnderConstruction (retVal);

			return retVal;
		}

		public string ContactsLabel {
			get {
				var builder = new StringBuilder ();
				bool first = true;
				foreach (Contact contact in contacts) {
					if (first)
						first = false;
					else
						builder.Append (", ");

					if (contact != null && contact.displayName != null)
						builder.Append (contact.displayName);
				}

				return builder.ToString ();
			}
		}

		public IList<string> contactIDs() {
			var retVal = new List<string> ();
			foreach (Contact contact in contacts)
				retVal.Add (contact.serverID);

			return retVal;
		}

		public void BackgroundSave() {
			if (isPersisted)
				appModel.chatEntryDao.UpdateChatEntry (this);
			else
				appModel.chatEntryDao.InsertChatEntry (this);
		}

		public void SaveAsync() {
			EMTask.DispatchBackground (BackgroundSave);
		}

		public void TypingAsync () {
			EMTask.DispatchBackground (BackgroundTyping);
		}

		public void BackgroundTyping() {
			appModel.liveServerConnection.SendTypingMessageTo (this);
		}

		public void BackgroundAddMessage(Message message, bool sendToServer) {
			message.chatEntryID = chatEntryID;
			int pos = message.Save ();
			if (sendToServer) {
				if (!message.IsInbound ()) {
					EMTask.DispatchBackground (() => {
						if (string.IsNullOrEmpty (message.mediaRef))
							BackgroundSendTextMessage (message);
						else
							BackgroundSendMediaMessage (message);
					});
				}
			}

			message.DelegateDidUpdateStatus += DidReceiveMessageUpdate;

			previewDate = message.sentDate;
			SetPreviewTextForMessage (message);

			if (message.IsInbound ()) {
				if (!message.HasBeenRead ()) {
					hasUnread = true;
					NotificationCenter.DefaultCenter.PostNotification (this, Constants.AbstractInboxController_EntryWithNewInboundMessage, message);
				}
			}

			leftAdhoc = false;

			chatList.InsertOrMoveToTop (this);

			// Note: This is where the dao's version of the message is different from the cached message list's version of the message.
			// Since we're dispatching to the main thread at this point,
			// the message will have a window where it's showSentDate property is incorrect (the case where cached messages is null).
			// If we pull from the message from the database, it is possible these two properties will not be the same.
			EMTask.DispatchMain (() => {
				IList<Message> cachedMessages = this.CachedConversation.CachedMessages;

				bool shouldCallAddedMessageDelegate = true;
				if (cachedMessages != null) {
					int count = cachedMessages.Count;
					bool messageAlreadyCached = false;

					// There's a race condition here when initially sending a message.
					// When the abstract chat controller sends a message, its DisplayedChatEntry gets updated which then triggers a query for messages.
					// If the message gets saved in BackgroundAddMessage before the query finishes, the query will return with the cached conversation already containing the message we're trying to add.
					// This code tries to efficiently check if the message is already in the cached conversation before adding it.
					// Loop through backwards and check each message. If the message is the same as the message we're adding, it's in the list..
					// We break early if the last few messages are older than the message we're trying to add as a heuristic on whether or not it's a good idea to add the message to the cache.
					if (count == 0) {
						messageAlreadyCached = false;
					} else {
						int numberOfCachedMessagesOlderThanMessage = 0;
						for (int i = count-1; i >= 0; i--) {
							if (numberOfCachedMessagesOlderThanMessage >= 3 /* arbitrary */) {
								break;
							}

							Message cachedMessage = cachedMessages [i];

							if (cachedMessage.Equals (message)) {
								messageAlreadyCached = true;
								break;
							}

							if (cachedMessage.sentDate < message.sentDate) {
								numberOfCachedMessagesOlderThanMessage++;
								break;
							}
						}
					}

					// If the message is already cached, don't call the delegate as that would create a duplicate.
					if (messageAlreadyCached) {
						shouldCallAddedMessageDelegate = false;
					} else {
						cachedMessages.Add (message);
					}
				}

				if (pos != -1) {
					if (pos == 0)
						// first message shows sent date
						message.showSentDate = true;
					else if (cachedMessages != null) {
						int indexOfMessage = cachedMessages.IndexOf (message);
						Message prior = cachedMessages [indexOfMessage - 1];
						message.showSentDate = message.ShouldShowSentDateWithPriorMessage (prior);
					}
				}

				if (shouldCallAddedMessageDelegate) {
					DelegateDidAddMessage (message);
				}
				
				chatList.DelegateDidChangePreview (this, 0);
				if ( message.IsInbound() )
					chatList.UnreadCountAffected();
			});

			EMTask.DispatchBackground (() => {
				if(message.IsInbound ())
					appModel.RecordGAGoal (Preference.GA_RECEIVED_MESSAGE, AnalyticsConstants.CATEGORY_GA_GOAL, 
						AnalyticsConstants.ACTION_RECEIVED_MESSAGE, AnalyticsConstants.RECEIVED_MESSAGE, AnalyticsConstants.VALUE_RECEIVE_MESSAGE);
				else
					appModel.RecordGAGoal(Preference.GA_SENT_MESSAGE, AnalyticsConstants.CATEGORY_GA_GOAL, 
						AnalyticsConstants.ACTION_SENT_MESSAGE, AnalyticsConstants.SENT_MESSAGE, AnalyticsConstants.VALUE_SEND_MESSAGE);
			});
		}

		public void AddMessageAsync(Message message, bool sendToServer) {
			EMTask.DispatchBackground (() => {
				try {
					BackgroundAddMessage (message, sendToServer);
				}
				catch (Exception e) {
					Debug.WriteLine(string.Format("Failed to AddMessage: {0}\n{1}", e.Message, e.StackTrace));
				}
			});
		}

		#region message modification
		public void HandleMessageModification (Message message, MessageModificationOutbound incomingModification) {
			if (message != null && message.messageLifecycle != MessageLifecycle.historical) {
				int mask = 0;
				MessageLifecycle incomingModificationLifecycle = incomingModification.messageLifecycle;		
				CheckMessageAttributesDifference (message, ref mask, incomingModification.attributes);
				CheckMessageLifecycleDifference (message, ref mask, incomingModificationLifecycle);
				CheckMessageLifecycleNotDeleted (message, ref mask, incomingModification.message, incomingModification.mediaRef, incomingModification.contentType);
				CheckIfMessageModified (message, mask);
			}

		}

		public void HandleMessageModification (Message message, MessageModificationInput incomingModification) {
			if (message != null && message.messageLifecycle != MessageLifecycle.historical ) {
				int mask = 0;
				MessageLifecycle incomingModificationLifecycle = MessageLifecycleHelper.FromString (incomingModification.messageLifecycle);
				CheckMessageAttributesDifference (message, ref mask, incomingModification.attributes);
				CheckMessageLifecycleDifference (message, ref mask, incomingModificationLifecycle);
				CheckMessageLifecycleNotDeleted (message, ref mask, incomingModification.message, incomingModification.mediaRef, incomingModification.contentType);
				CheckIfMessageModified (message, mask);
			}
		}

		private void CheckMessageAttributesDifference (Message message, ref int mask, JToken incomingAttributes) {
			string messageAttributes = message.attributes != null ? message.attributes.ToString () : null;
			string incomingAttributesString = incomingAttributes != null ? incomingAttributes.ToString () : null;
			if (!(new EqualsBuilder<string>(messageAttributes, incomingAttributesString)).Equals()) {
				mask |= ChatEntry.MODIFY_MASK_ATTRIBUTES;
				message.attributes = incomingAttributes as JObject;
			}
		}

		private void CheckMessageLifecycleDifference (Message message, ref int mask, MessageLifecycle lifecycle) {
			if (!(new EqualsBuilder<MessageLifecycle> (message.messageLifecycle, lifecycle)).Equals ()) {
				mask |= ChatEntry.MODIFY_MASK_TAKEN_BACK;
				message.messageLifecycle = lifecycle;
				message.message = appModel.platformFactory.GetTranslation ("DELETED_STATUS");
			
				// Clear preview if the message being taken back is the latest message.
				bool isLatestMessage = Message.IsLatestMessage (appModel, message);
				if (isLatestMessage) {
					preview = string.Empty;
					chatList.DelegateDidChangePreview (this, 0);
				}

				appModel.chatEntryDao.UpdateChatEntry (this);

				if (message.mediaRef != null) {
					string localCopyPath = appModel.uriGenerator.GetFilePathForChatEntryUri (message.media.uri, this);
					appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (localCopyPath);
					message.mediaRef = null;
					message.contentType = null;
					mask |= ChatEntry.MODIFY_MASK_MEDIA_REF;
				}
			}
		}

		private void CheckMessageLifecycleNotDeleted (Message message, ref int mask, string mmessage, string mediaRef, string contentType) {
			if (message.messageLifecycle != MessageLifecycle.deleted) {
				if (!(new EqualsBuilder<string>(message.message, mmessage)).Equals()) {
					mask |= ChatEntry.MODIFY_MASK_TEXT;
					message.message = mmessage;
				}

				if (mediaRef != null)
					message.mediaRef = mediaRef;

				if (contentType != null)
					message.contentType = contentType;
			}
		}

		private void CheckIfMessageModified (Message message, int mask) {
			message.SaveRemoteMessageUpdate ();
			if ( mask != 0 ) {
				EMTask.DispatchMain (() => {
					IList<Message> conversation = this.CachedConversation.CachedMessages;
					if (conversation != null) {
						DelegateDidRemoteModifyMessage (message, mask);
					}
				});
			}

			if (message.HasMedia ()) {
				message.media.GUID = message.messageGUID;
				message.media.DownloadMedia (appModel);
			}
		}
		#endregion

		private void SetPreviewTextForMessage (Message message) {
			bool sentByYou = !message.IsInbound ();

			if (!message.HasMedia ()) {
				string previewMessage = message.message;
				if (!appModel.platformFactory.CanShowUnicodeWithSkinModifier ()) {
					previewMessage = Message.RemoveEmojiSkinModifier (previewMessage);
				}

				this.preview = sentByYou ? string.Format (appModel.platformFactory.GetTranslation ("YOU_PREAMBLE"), previewMessage) : previewMessage;
			} else {
				ContentType type = ContentTypeHelper.FromMessage (message);

				if (ContentTypeHelper.IsVideo (type)) {
					if (sentByYou) {
						this.preview = appModel.platformFactory.GetTranslation ("MEDIA_PREVIEW_MOVIE_YOU");
					} else {
						this.preview = appModel.platformFactory.GetTranslation ("MEDIA_PREVIEW_MOVIE");
					}
				}

				if (ContentTypeHelper.IsPhoto (type)) {
					if (sentByYou) {
						this.preview = appModel.platformFactory.GetTranslation ("MEDIA_PREVIEW_PHOTO_YOU");
					} else {
						this.preview = appModel.platformFactory.GetTranslation ("MEDIA_PREVIEW_PHOTO");
					}
				}

				if (ContentTypeHelper.IsAudio (type)) {
					if (sentByYou) {
						this.preview = appModel.platformFactory.GetTranslation ("MEDIA_PREVIEW_SOUND_YOU");
					} else {
						this.preview = appModel.platformFactory.GetTranslation ("MEDIA_PREVIEW_SOUND");
					}
				}
			}
		}

		public void BackgroundRemoteTakeBack(Message message) {
			var deleteMessage = new MessageModificationOutbound ();
			deleteMessage.destination = message.chatEntry.contactIDs ();
			deleteMessage.messageGUID = message.messageGUID;
			deleteMessage.messageLifecycle = MessageLifecycle.deleted;
			deleteMessage.attributes = message.attributes;
			deleteMessage.fromAlias = message.chatEntry.fromAlias;

			HandleMessageModification (message, deleteMessage);

			var queueEntry = new QueueEntry ();
			queueEntry.destination = "/app/modifyMessage";
			queueEntry.route = QueueRoute.Websocket;
			queueEntry.methodType = QueueRestMethodType.NotApplicable;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(appModel);

			string json = JsonConvert.SerializeObject (deleteMessage);
			byte[] jsonBytes = Encoding.UTF8.GetBytes (json);
			QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (appModel, jsonBytes, "application/json", "modifyMessage.json", "modifyMessage.json");
			queueEntry.contents.Add (messageContents);

			appModel.outgoingQueue.EnqueueAndSend (queueEntry);
		}

		public void RemoteTakeBackAsync(Message message) {
			EMTask.DispatchBackground (() => {
				try {
					BackgroundRemoteTakeBack (message);
				}
				catch (Exception e) {
					Debug.WriteLine(string.Format("Failed to Delete Remotely: {0}\n{1}", e.Message, e.StackTrace));
				}
			});
		}

		public void BackgroundMarkHistorical(Message message) {
			message.messageLifecycle = MessageLifecycle.historical;
			message.message = appModel.platformFactory.GetTranslation("HISTORICAL_STATUS");
			if ( message.mediaRef != null ) {
				string localCopyPath = appModel.uriGenerator.GetFilePathForChatEntryUri(message.media.uri, this);
				appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath(localCopyPath);
				message.mediaRef = null;
				message.contentType = null;
			}

			message.SaveRemoteMessageUpdate ();

			EMTask.DispatchMain (() => {
				IList<Message> conversation = this.CachedConversation.CachedMessages;
				if (conversation != null) {
					int indexOf = conversation.IndexOf (message);
					conversation.RemoveAt(indexOf);
					DelegateDidMarkMessageHistoricalAt (message);
				}
			});
		}

		public void MarkHistoricalAsync(Message message) {
			EMTask.DispatchBackground (() => {
				try {
					BackgroundMarkHistorical (message);
				}
				catch (Exception e) {
					Debug.WriteLine(string.Format("Failed to Delete Remotely: {0}\n{1}", e.Message, e.StackTrace));
				}
			});
		}

		int lastUnreadIndex = 0;
		public int LastReadIndex {
			get { return this.lastUnreadIndex; }
			set { this.lastUnreadIndex = value; }
		}

		public void MarkAllUnreadReadAsync() {
			MarkAllUnreadWithMessageStatusAsync (MessageStatus.read, false);
		}

		public void MarkAllUnreadIgnored() {
			MarkAllUnreadWithMessageStatusAsync (MessageStatus.ignored, true);
		}

		protected void MarkAllUnreadWithMessageStatusAsync (MessageStatus updatedStatus, bool inlineInsteadOfAsync) {
			if (hasUnread)
				hasUnread = false;

			IList<Message> conversation = this.CachedConversation.CachedMessages;
			IList<Message> copy = null;

			if (conversation != null) {
				copy = new List<Message> ();
				for (int i = this.lastUnreadIndex; i < conversation.Count; i++) {
					copy.Add (conversation [i]);
				}

				lastUnreadIndex = conversation.Count;
			}

			if (copy != null && copy.Count == 0)
				return;

			Action nextSteps = () => {
				MarkAllUnreadNextSteps (copy, updatedStatus);
			};

			if (inlineInsteadOfAsync)
				nextSteps ();
			else
				EMTask.DispatchBackground (nextSteps);
		}

		private void MarkAllUnreadNextSteps (IList<Message> copy, MessageStatus updatedStatus) {
			try {
				IList<Message> unread = null;
				if (copy == null) {
					unread = appModel.msgDao.FindUnreadMessagesForChatEntry (this);
					foreach (Message dbMessage in unread) {
						dbMessage.fromContact = Contact.FindContactByContactID (appModel, dbMessage.fromContactID);
						dbMessage.messageStatus = updatedStatus;
					}

					// if convo not loaded, we may need to indicate the unread count was affected
					// if it is loaded, the message itself will indicate this when its status
					// is changed.
					if ( unread.Count > 0 )
						EMTask.DispatchMain( () => { chatList.UnreadCountAffected(); } );
				}
				else {
					unread = new List<Message> ();
					foreach (Message existing in copy) {
						// TODO maybe go in reverse order and stop
						// when we find a read message to prevent
						// scanning all messages
						if ( existing.IsInbound() && !existing.HasBeenRead ()) {
							existing.messageStatus = updatedStatus;
							unread.Add (existing);
						}
					}
				}

				if ( unread.Count > 0 ) {
					appModel.msgDao.MarkUnreadWithStatusForChatEntry (this, updatedStatus, unread);

					var array = new Message[unread.Count];
					unread.CopyTo (array, 0);

					SendStatusUpdatesAsync (array, updatedStatus);
				}

				int notifyUnreadPreviewIndex = -1;

				if ( unread.Count > 0 ) {
					BackgroundSave();

					notifyUnreadPreviewIndex = chatList.entries.IndexOf (this);
				}

				EMTask.DispatchMain (() => {
					if ( notifyUnreadPreviewIndex != -1 )
						chatList.DelegateDidChangePreview(this, notifyUnreadPreviewIndex); // in this case preview is really unread indicator.
				});
			}
			catch (Exception e) {
				Debug.WriteLine(string.Format("Failed to Mark all as Read: {0}\n{1}", e.Message, e.StackTrace));
			}
		}

		public void SendStatusUpdatesAsync (Message[] messages, MessageStatus messageStatus) {
			EMTask.DispatchBackground (() => BackgroundSendStatusUpdates (messages, messageStatus));
		}

		public void BackgroundSendStatusUpdates(Message[] messages, MessageStatus messageStatus) {
			var queueEntry = new QueueEntry ();
			queueEntry.destination = StompPath.kSendMessageUpdate;
			queueEntry.methodType = QueueRestMethodType.NotApplicable;
			queueEntry.route = QueueRoute.Websocket;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(appModel);

			var outboundMessage = new MessageStatusUpdateOutbound ();
			var messageUpdates = new List<MessageStatusOutbound> ();
			foreach (Message message in messages) {
				var status = new MessageStatusOutbound ();
				status.messageGUID = message.messageGUID;
				status.messageStatus = messageStatus;
				status.senderID = message.fromContact.serverID;
				status.fromAlias = fromAlias;
				status.destinations = message.chatEntry.contactIDs();
				messageUpdates.Add (status);
			}
			outboundMessage.messageUpdates = messageUpdates;
			string json = JsonConvert.SerializeObject (outboundMessage);
			byte[] jsonBytes = Encoding.UTF8.GetBytes (json);
			QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (appModel, jsonBytes, "application/json", "messageUpdate.json", "messageUpdate.json");
			queueEntry.contents.Add (messageContents);

			appModel.outgoingQueue.EnqueueAndSend (queueEntry);
		}

		public void LeaveConversationAsync() {
			EMTask.Dispatch (BackgroundLeaveConversation);
		}

		public void BackgroundLeaveConversation() {
			leftAdhoc = true;
			SaveAsync ();

			var queueEntry = new QueueEntry ();
			queueEntry.destination = StompPath.kLeaveConversation;
			queueEntry.methodType = QueueRestMethodType.NotApplicable;
			queueEntry.route = QueueRoute.Websocket;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(appModel);

			var outboundMessage = new LeaveConversationOutbound ();
			outboundMessage.setCreateDate (this.createDate);
			outboundMessage.destination = (this.contactIDs() as List<string>).ToArray();
			outboundMessage.fromAlias = this.fromAlias;
			string json = JsonConvert.SerializeObject (outboundMessage);
			byte[] jsonBytes = Encoding.UTF8.GetBytes (json);
			QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (appModel, jsonBytes, "application/json", "leaveConversation.json", "leaveConversation.json");
			queueEntry.contents.Add (messageContents);

			appModel.outgoingQueue.EnqueueAndSend (queueEntry);
		}

		public void LoadConversationAsync(Action<IList<Message>> completedCallback) {
			IList<Message> cachedMessages = this.CachedConversation.CachedMessages;

			if (cachedMessages != null) {
				completedCallback (cachedMessages);
			} else {
				EMTask.Dispatch (() => {
					lock (appModel.daoConnection) {
						IList<Message> messages = Message.FindAllMessagesForChatEntry (appModel, this);
						int mCount = messages.Count;
						for (int i = 0; i < mCount; i++) {
							Message message = messages [i];
							message.DelegateDidUpdateStatus += DidReceiveMessageUpdate;
						}

						EMTask.DispatchMain (() => {
							this.CachedConversation.CachedMessages = messages;
							completedCallback (messages);
						});
					}
				});
			}
		}

		public void LoadRecentMessagesAsync (Action<IList<Message>> completedCallback, int messageLimit) {
			IList<Message> cachedMessages = this.CachedConversation.CachedMessages;

			if (cachedMessages != null) {
				completedCallback (cachedMessages);
			} else {
				EMTask.Dispatch (() => {
					lock (appModel.daoConnection) {
						IList<Message> messages = Message.FindRecentMessagesForChatEntry (appModel, this, messageLimit);
						int mCount = messages.Count;
						for (int i = 0; i < mCount; i++) {
							Message message = messages [i];
							message.DelegateDidUpdateStatus += DidReceiveMessageUpdate;
						}

						EMTask.DispatchMain (() => {
							this.CachedConversation.CachedMessages = messages;
							completedCallback (messages);
						});
					}
				});
			}
		}

		public void LoadMorePreviousMessages (Action<IList<Message>> completedCallback) {
			// Cached messages shouldn't be null here. If it reaches here, it should be pinned by the AbstractChatController.
			IList<Message> cachedMessages = this.CachedConversation.CachedMessages;
			Debug.Assert (cachedMessages != null, "Expected non null cached messages.");
			EMTask.Dispatch (() => {
				lock (appModel.daoConnection) {
					Message seedMessage = cachedMessages [0];

					IList<Message> messages = Message.FindPreviousMessagesForMessage (appModel, this, Constants.NUMBER_OF_PREVIOUS_MESSAGES_TO_RETRIEVE, seedMessage);

					int mCount = messages.Count;
					for (int i = 0; i < mCount; i++) {
						Message message = messages [i];
						message.DelegateDidUpdateStatus += DidReceiveMessageUpdate;
					}

					EMTask.DispatchMain (() => {
						for (int i = mCount; i > 0; i--) {
							Message message = messages [i-1];
							cachedMessages.Insert (0, message);
						}

						// Returns only the previous messages. The cached conversation is already updated.
						completedCallback (messages);
					});
				}	
			});
		}

		public bool HasSameContacts(IList<string> replyToContactIDs) {
			if (contacts.Count != replyToContactIDs.Count)
				return false;

			foreach ( Contact contact in contacts ) {
				// TODO contact != null should not happen
				if ( contact != null && !replyToContactIDs.Contains (contact.serverID))
					return false;
			}

			return true;
		}

		public bool IsGroupChat() {
			return contacts != null && contacts.Count == 1 && contacts [0].isGroup.Equals("Y");
		}

		public bool IsDeletedGroupChat() {
			return contacts != null && contacts.Count == 1 && contacts [0].isGroup.Equals ("Y") && contacts [0].lifecycle == ContactLifecycle.Deleted;
		}

		public bool IsAdHocGroupChat() {
			if (contacts == null || contacts.Count <= 1)
				return false;

			return true;
		}

		public bool IsAdHocGroupWeCanLeave() {
			return IsAdHocGroupChat () && !leftAdhoc;
		}

		public BackgroundColor IncomingColorTheme {
			get {
				if (contacts == null || contacts.Count == 0)
					return BackgroundColor.Default;

				return contacts [0].colorTheme;
			}
		}

		public BackgroundColor SenderColorTheme {
			get {
				AccountInfo account = appModel.account.accountInfo;
				if (account != null ) {
					AliasInfo alias = account.AliasFromServerID (fromAlias);
					if (alias != null)
						return alias.colorTheme;

					return account.colorTheme;
				}

				return BackgroundColor.Default;
			}
		}

		public string SenderName {
			get {
				AccountInfo account = appModel.account.accountInfo;
				if (account != null ) {
					AliasInfo alias = account.AliasFromServerID (fromAlias);
					if (alias != null)
						return alias.displayName;

					return account.displayName;
				}

				return "";
			}
		}


		public Media SenderThumbnailMedia {
			get {
				CounterParty senderCounterParty = this.SenderCounterParty;
				return senderCounterParty != null ? senderCounterParty.media : null;
			}
		}

		public CounterParty SenderCounterParty {
			get {
				AccountInfo account = appModel.account.accountInfo;
				if (account != null ) {
					AliasInfo alias = account.AliasFromServerID (fromAlias);
					if (alias != null)
						return alias;

					return account;
				}

				return null;
			}
		}

		public Media CounterThumbnailMedia {
			get {
				CounterParty c = this.FirstContactCounterParty;
				return c != null ? c.media : null;
			}
		}

		public CounterParty FirstContactCounterParty {
			get {
				if (contacts.Count > 0)
					return contacts [0];
				return null;
			}
		}

		public bool ShowMessageStatusForSentMessage() {
			return contacts.Count <= 1;
		}

		public void AddContact(Contact contact) {
			contacts.Add (contact);

			AddDelegates (contact);
		}

		public void RemoveContactAt (int index) {
			if (contacts != null && contacts.Count > index) {
				Contact removed = contacts [index];
				RemoveDelegates (removed);

				contacts.RemoveAt (index);
			}
		}

		public void RemoveContactFromDB(Contact contact) {
			appModel.chatEntryDao.RemoveContactFromChatEntry (this, contact);	
		}

		public void RemoveLastContact () {
			if (contacts != null && contacts.Count > 0) {
				var index = contacts.Count - 1;
				Contact removed = contacts [index];
				RemoveDelegates (removed);

				contacts.RemoveAt (index);
			}
		}

		public void ClearAllContacts () {
			foreach (Contact contact in contacts)
				RemoveDelegates (contact);

			contacts.Clear ();
		}

		public bool ShowFromLabelAndPhoto() {
			return contacts.Count <= 1;
		}

		// This function only returns true if the current fromAlias is an alias. (Not a user)
		// And its lifecycle deems it inactive.
		public bool FromAliasInActive {
			get {
				AliasInfo alias = appModel.account.accountInfo.AliasFromServerID (this.fromAlias);
				if ( alias != null && alias.lifecycle != ContactLifecycle.Active ) {
					return true;
				}

				return false;
			}
		}

		public bool AreAllCounterpartiesActive() {
			// TODO: Can we return early if allActive is set to false at the beginning?
			bool allActive = true, ranBlockedOnce = false;

			// Note usage of this function will usually be preceded by calling FromAliasInActive.
			// So if it hits this function, this will probably be false.
			// Kept in this function for correctness.
			if (this.FromAliasInActive) {
				allActive = false;
			}

			if (contacts != null) {
				int numberOfInactiveContacts = 0;
				for (int i = 0; allActive && i < contacts.Count; i++) {
					Contact contact = contacts [i];
					if (contact.lifecycle != ContactLifecycle.Active) {
						numberOfInactiveContacts++;
					}

					// we don't allow conversations to groups we've abandoned.
					if (contact.IsAGroup && contact.GroupStatus == GroupMemberStatus.Abandoned) {
						allActive = false;
						break;
					}

					// we don't allow conversations to groups we've been removed from.
					if (contact.IsAGroup && contact.GroupMemberLifeCycle == GroupMemberLifecycle.Removed) {
						allActive = false;
						break;
					}

					// we don't continue conversations if we blocked the person(s) we are communicating with
					// check to see if all members (1 or more) of a conversation or an adhoc group are blocked
					if(!ranBlockedOnce) {
						int numBlocked = 0;
						foreach (var adhocContact in contacts) {
							if (!BlockStatusHelper.CanSend (adhocContact.BlockStatus))
								numBlocked++;
						}

						ranBlockedOnce = true;

						if (numBlocked == contacts.Count) {
							allActive = false;
							break;
						}
					}
				}

				if (allActive) {
					// Adhoc group case, as long as one contact is active, we can send to it.
					// If the number of inactive contacts match the number of contacts, every contact being sent to is inactive.
					// Works for the 1 to 1 case too.
					allActive = numberOfInactiveContacts != contacts.Count;
				}
			}

			return allActive;
		}

		protected void BackgroundSendTextMessage(Message message) {
			var queueEntry = new QueueEntry ();
			queueEntry.destination = StompPath.kSendMessage;
			queueEntry.methodType = QueueRestMethodType.NotApplicable;
			queueEntry.route = QueueRoute.Websocket;
			queueEntry.sentDate = message.sentDate;

			MessageOutbound messageOutbound = message.ToMessageOutbound ();
			string json = JsonConvert.SerializeObject (messageOutbound);
			byte[] jsonBytes = Encoding.UTF8.GetBytes (json);
			QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (appModel, jsonBytes, "application/json", "message.json", "message.json");
			messageContents.localID = message.ToLocalID();
			queueEntry.contents.Add (messageContents);

			appModel.outgoingQueue.EnqueueAndSend (queueEntry);
		}

		protected void BackgroundSendMediaMessage(Message message) {
			var queueEntry = new QueueEntry ();

			queueEntry.destination = "/uploadFiles/sendMessage";
			queueEntry.methodType = QueueRestMethodType.MultiPartPost;

			// If the media message is a video, encode it before sending it.
			if (ContentTypeHelper.IsVideo (ContentTypeHelper.FromString (message.contentType))) {
				message.media.MediaState = MediaState.Encoding;
				queueEntry.route = QueueRoute.VideoEncoding;
			} else {
				queueEntry.route = QueueRoute.Rest;
			}

			queueEntry.sentDate = message.sentDate;

			MessageOutbound outbound = message.ToMessageOutbound ();
			string json = JsonConvert.SerializeObject (outbound);
			byte[] jsonBytes = Encoding.UTF8.GetBytes (json);
			QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (appModel, jsonBytes, "application/json", "message.json", "message.json");
			messageContents.localID = message.ToLocalID();
			queueEntry.contents.Add (messageContents);

			QueueEntryContents attachmentContents = QueueEntryContents.CreateTemporaryContentsFromLocalPath (message.media.uri.LocalPath, message.contentType, "attachment", "attachment"); 
			queueEntry.contents.Add (attachmentContents);

			appModel.outgoingQueue.EnqueueAndSend (queueEntry, null);
		}

		public RemoveChatEntryOutbound ToRemoveChatEntryOutbound() {
			var retVal = new RemoveChatEntryOutbound ();
			retVal.fromAlias = fromAlias;
			retVal.setCreateDate( createDate );
			retVal.replyTo = contactIDs ();

			return retVal;
		}

		void ContactDidChangeThumbnail(CounterParty cp) {
			EMTask.DispatchMain (() => {
				if (chatList != null)
					chatList.DelegateDidChangeChatEntryContactThumbnailSource (this);
			});
		}

		void ContactDidLoadThumbnail(CounterParty cp) {
			EMTask.DispatchMain (() => {
				DelegateDidCompleteContactThumbnailDownload (cp as Contact);
				if (chatList != null)
					chatList.DelegateDidDownloadChatEntryContactThumbnail (this);
			});
		}

		void BackgroundContactDidFailToDownloadThumbnail (Notification notification) {
			var cp = notification.Source as CounterParty;
			if (cp != null) {
				ContactDidChangeThumbnail (cp);
			}
		}

		void CounterpartyDidChangeLifecycle(CounterParty cp) {
			// not posting to main thread here because current implementation of
			// notification center is kinda slow.
			NotificationCenter.DefaultCenter.PostNotification (this, CHATENTRY_COUNTERPARTY_LIFECYCLE_HAS_CHANGED);
		}

		void CounterpartyDidChangeBlockStatus(Contact c) {
			// this will have the same affect as a user being deleted, so currently just
			// calling the lifecycle change code.
			CounterpartyDidChangeLifecycle(c);
		}

		void CounterpartyDidChangeGroupMemberStatus(Contact c) {
			// this will have the same affect as a user being deleted, so currently just
			// calling the lifecycle change code.
			CounterpartyDidChangeLifecycle(c);
		}

		void CounterpartyDidChangeGroupMemberLifecycle(Contact c) {
			// this will have the same affect as a user being deleted, so currently just
			// calling the lifecycle change code.
			CounterpartyDidChangeLifecycle(c);
		}

		void ContactDidChangeColorTheme(CounterParty cp) {
			EMTask.DispatchMain (() => {
				if (chatList != null)
					chatList.DelegateDidChangeChatEntryColorTheme (this);
			});
		}

		void ContactDidChangeDisplayName(CounterParty cp) {
			EMTask.DispatchMain (() => {
				if (chatList != null)
					chatList.DelegateDidChangeChatEntryName (this);
			});
		}

		protected void AddDelegates(Contact contact) {
			contact.DelegateDidChangeDisplayName += ContactDidChangeDisplayName;
			contact.DelegateDidChangeColorTheme += ContactDidChangeColorTheme;
			contact.DelegateDidChangeThumbnailMedia += ContactDidChangeThumbnail;
			contact.DelegateDidDownloadThumbnail += ContactDidLoadThumbnail;
			contact.DelegateDidChangeLifecycle += CounterpartyDidChangeLifecycle;
			contact.DelegateDidChangeBlockStatus += CounterpartyDidChangeBlockStatus;
			contact.DelegateDidChangeGroupMemberStatus += CounterpartyDidChangeGroupMemberStatus;
			contact.DelegateDidChangeGroupMemberLifecycle += CounterpartyDidChangeGroupMemberLifecycle;

			NotificationCenter.DefaultCenter.AddWeakObserver (contact, Constants.Counterparty_DownloadFailed, BackgroundContactDidFailToDownloadThumbnail);
		}

		public void RemoveDelegates(Contact contact) {
			contact.DelegateDidChangeDisplayName -= ContactDidChangeDisplayName;
			contact.DelegateDidChangeColorTheme -= ContactDidChangeColorTheme;
			contact.DelegateDidChangeThumbnailMedia -= ContactDidChangeThumbnail;
			contact.DelegateDidDownloadThumbnail -= ContactDidLoadThumbnail;
			contact.DelegateDidChangeLifecycle -= CounterpartyDidChangeLifecycle;
			contact.DelegateDidChangeBlockStatus -= CounterpartyDidChangeBlockStatus;
			contact.DelegateDidChangeGroupMemberStatus -= CounterpartyDidChangeGroupMemberStatus;
			contact.DelegateDidChangeGroupMemberLifecycle -= CounterpartyDidChangeGroupMemberLifecycle;

			NotificationCenter.DefaultCenter.RemoveObserverAction (contact, Constants.Counterparty_DownloadFailed, BackgroundContactDidFailToDownloadThumbnail);
		}

		public void ContactLeftChatEntry(Contact c) {
			if (c != null) {
				int indexOf = contacts.IndexOf (c);
				if (indexOf >= 0) {
					RemoveContactAt (indexOf);
					RemoveContactFromDB (c);

					DelegateContactDidLeaveAdhoc (c);
					chatList.DelegateChatEntryDidHaveContactLeave (this, c);
				}
			}
		}
	}

	public class PrepopulatedChatEntryInfo {
		public string ToAKA { get; set; }

		string faka;
		public string FromAKA {
			get { return faka; }
			set {
				if (string.IsNullOrEmpty (value))
					faka = null;
				else
					faka = value;
			}
		}

		public PrepopulatedChatEntryInfo(string to, string from) {
			ToAKA = to;
			FromAKA = from;
		}
	}
}