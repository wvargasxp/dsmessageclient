using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace em {
	public class Message {
		public int messageID { get; set; }
		public string messageGUID { get; set; }
		public int chatEntryID { get; set; }
		public string inbound { get; set; }
		public string message { get; set; }
		public int fromContactID { get; set; }
		public string contentType { get; set; }
		public bool showSentDate { get; set; }
		DateTime sd;
		public DateTime sentDate { 
			get { return sd; } 
			set { sd = value.ToUniversalTime (); } 
		}

		public string sentDateString {
			get {
				var offset = new DateTimeOffset(sentDate,TimeSpan.Zero);
				return offset.ToString(Constants.ISO_DATE_FORMAT, Preference.usEnglishCulture);
			}
			set {
				// We need to set the Assume Universal flag here because without it, the value that will be set to previewDate will be of Kind 'Unspecified'. 
				// Converting this 'Unspecified' value to UTC changes its value (in the setter function of sentDate).
				sentDate = DateTime.ParseExact (value, 
					Constants.ISO_DATE_FORMAT, 
					Preference.usEnglishCulture, 
					System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal);
			}
		}
			
		public string FormattedSentDate {
			get {
				string formattedSentDate = this.appModel.platformFactory.GetFormattedDate (this.sentDate, DateFormatStyle.FullDate);
				return formattedSentDate;
			}
		}

		public bool isPersisted { get; set; }

		string mr;
		public string mediaRef { 
			get { return mr; }
			set {
				Media mediaToReplace = null;
				if (media != null) {
					mediaToReplace = media;

					media.BackgroundDelegateWillStartDownload -= BackgroundWillStartDownload;
					media.BackgroundDelegateDidDownloadPercentage -= BackgroundDidDownloadPercentage;
					media.BackgroundDelegateDidCompleteDownload -= BackgroundDidFinishDownload;
					media.BackgroundDelegateDidChangeSoundState -= BackgroundDidChangeSoundState;

					NotificationCenter.DefaultCenter.RemoveObserverAction (media, Constants.Media_DownloadFailed, BackgroundDidFailDownload);
				}

				mr = value;
				if (mr == null)
					media = null;
				else {
					media = Media.FindOrCreateMedia (new Uri (value));
					media.LocalPathFromUriFunc = LocalPathFromUriFunc;
					media.UploadPathFromUriFunc = UploadPathFromUriFunc;

					media.contentType = ContentTypeHelper.FromString (this.contentType);
					SetMediaDelegates ();

					if (mediaToReplace != null) {
						media.TryMigrateUploadFileToFinalPath ();
					}
				}
			}
		}

		public string MediaRefSilent {
			get { return this.mr; }
			set { 
				this.mr = value;
				if (this.mr == null) {
					this.media = null;
				} else {
					this.media = Media.FindOrCreateMedia (new Uri (this.mr));
					this.media.contentType = ContentTypeHelper.FromString (this.contentType);
					// Set Media Delegates when we're ready.
				}
			}
		}

		public void SetMediaDelegates () {
			if (media == null) return;
			media.LocalPathFromUriFunc = LocalPathFromUriFunc;
			media.UploadPathFromUriFunc = UploadPathFromUriFunc;

			media.BackgroundDelegateWillStartDownload += BackgroundWillStartDownload;
			media.BackgroundDelegateDidDownloadPercentage += BackgroundDidDownloadPercentage;
			media.BackgroundDelegateDidCompleteDownload += BackgroundDidFinishDownload;
			media.BackgroundDelegateDidChangeSoundState += BackgroundDidChangeSoundState;

			// There isn't a delegate hooked up for when media fails to download.
			// We're using NotificationCenter to pass this event around.
			NotificationCenter.DefaultCenter.AddWeakObserver (media, Constants.Media_DownloadFailed, BackgroundDidFailDownload);
		}

		public Media media { get; set; }

		public ApplicationModel appModel { get; set; }

		public MessageStatus messageStatusSilent {
			get { return ms; }
			set { ms = value; }
		}

		MessageStatus ms;
		public MessageStatus messageStatus {
			get { return ms; }
			set {
				if (ms != value) {
					if (IsInbound() && MessageStatusHelper.AffectsUnreadCount (ms, value) && chatEntry != null && chatEntry.chatList != null)
						chatEntry.chatList.UnreadCountAffected ();

					ms = value;
					if (appModel != null && ms != MessageStatus.pending)
						EMTask.DispatchMain (() => DelegateDidUpdateStatus (this, ms));
				}
			}
		}

		public bool UpdateMessageStatus (MessageStatus status) {
			if (messageStatus < status) {
				messageStatus = status;
				return true;
			}

			return false;
		}

		// used by database reading
		public string statusString {
			get { return MessageStatusHelper.toDatabase (messageStatus); }
			set { messageStatusSilent = MessageStatusHelper.fromDatabase (value); }
		}
			
		public JObject attributes { get; set; }
		public string attributesString {
			get { return attributes == null ? null : attributes.ToString(); }
			set { attributes = value == null ? null : JsonConvert.DeserializeObject<JObject> (value); }
		}

		#region remote button actions

		private const string RemoteButtonActionKey = "button";
		private bool? _hasRemoteAction;
		public bool HasRemoteAction {
			get {
				if (this._hasRemoteAction.HasValue) {
					return this._hasRemoteAction.Value;
				}

				JObject attrs = this.attributes;
				if (attrs == null) {
					return false;
				} else {
					JToken token = attrs [RemoteButtonActionKey];
					this._hasRemoteAction = token != null;
					return this._hasRemoteAction.Value;
				}
			}
		}

		public UserPromptButton RemoteAction {
			get {
				if (!this.HasRemoteAction) {
					return null;
				} else {
					JToken token = this.attributes [RemoteButtonActionKey];
					UserPromptButton input = token.ToObject<UserPromptButton> ();
					return input;
				}
			}
		}
		#endregion

		public float heightToWidth {
			get {
				if (attributes == null)
					return 0;

				JToken jtoken = attributes["heightToWidth"]; 
				return jtoken == null ? 0 : jtoken.Value<float> ();
			}

			set {
				if (attributes == null)
					attributes = new JObject ();

				JToken existing = attributes ["heightToWidth"];
				if (existing == null)
					((JObject)attributes).Add ("heightToWidth", (float) Math.Round(value,2));
				else
					attributes ["heightToWidth"] = (float) Math.Round(value,2);
			}
		}
			
		public float soundRecordingDurationSeconds {
			get {
				if (attributes == null)
					return 0;

				JToken jtoken = attributes["soundRecordingDurationSeconds"]; 
				return jtoken == null ? 0 : jtoken.Value<float> ();
			}

			set {
				if (attributes == null)
					attributes = new JObject ();

				JToken existing = attributes ["soundRecordingDurationSeconds"];
				if (existing == null)
					((JObject)attributes).Add ("soundRecordingDurationSeconds", (float) Math.Round(value,2));
				else
					attributes ["soundRecordingDurationSeconds"] = (float) Math.Round(value,2);
			}
		}

		public MessageLifecycle messageLifecycle { get; set; }

		public string lifecycleString {
			get { return MessageLifecycleHelper.toDatabase (messageLifecycle); }
			set { messageLifecycle = MessageLifecycleHelper.fromDatabase (value); }
		}

		public MessageChannel messageChannel { get; set; }

		public string channelString {
			get { return MessageChannelHelper.toDatabase (messageChannel); }
			set { messageChannel = MessageChannelHelper.fromDatabase (value); }
		}

		public Contact fromContact { get; set; }

		static List<WeakCache<string,Message>> allMessagesList = new List<WeakCache<string,Message>>();

		public static void AddCacheAtIndex(int index) {
			allMessagesList.Insert(index, new WeakCache<string,Message> ());
		}

		public delegate void DidUpdateMessageStatus(Message message, MessageStatus status);

		public DidUpdateMessageStatus DelegateDidUpdateStatus = delegate(Message message, MessageStatus status)  {
		};

		// The associated chatEntry
		public ChatEntry chatEntry { get; set; }

		public Message () {
			showSentDate = false;
			isPersisted = true;
		}

		~Message() {
			if ( mediaRef != null )
				mediaRef = null;
		}

		public static Message NewMessage(ApplicationModel _appModel) {
			var retVal = new Message ();
			retVal.appModel = _appModel;
			retVal.isPersisted = false;
			retVal.messageGUID = Guid.NewGuid ().ToString ();
			retVal.messageLifecycle = MessageLifecycle.active;
			retVal.messageStatus = MessageStatus.pending;

			return retVal;
		}

		public int Save() {
			int pos = -1;
			if (isPersisted)
				appModel.msgDao.UpdateMessage (this);
			else
				pos = appModel.msgDao.InsertMessage (this);
			string key = ToLocalID ();
			if (!allMessagesList[appModel.cacheIndex].ContainsKey (key))
				allMessagesList[appModel.cacheIndex].Put (key, this);

			return pos;
		}

		public void SaveRemoteMessageUpdate() {
			appModel.msgDao.UpdateMessageRemote (this);
		}

		public bool HasMedia() {
			return !string.IsNullOrEmpty (mediaRef);
		}

		public string FromString() {
			if (IsInbound ())
				return fromContact.displayName;

			if (chatEntry.fromAlias != null) {
				AliasInfo aliasInfo = appModel.account.accountInfo.AliasFromServerID (chatEntry.fromAlias);
				if (aliasInfo != null)
					return aliasInfo.displayName;
			}

			return appModel.account.defaultName;
		}

		public bool HasBeenRead() {
			return messageStatus == MessageStatus.read;
		}

		public bool HasBeenDelivered() {
			return messageStatus == MessageStatus.delivered || HasBeenRead ();
		}

		public bool IsInbound() {
			return inbound.Equals("Y");
		}

		public static Message FindMessageByMessageGUID(ApplicationModel _appModel, string messageGUID, string fromAlias) {
			lock (_appModel.daoConnection) {
				string key = ToLocalID (messageGUID, fromAlias);

				Message cached = allMessagesList[_appModel.cacheIndex].Get (key);
				if (cached == null) {
					cached = _appModel.msgDao.FindMessageWithMessageGUID (messageGUID, fromAlias);
					if (cached != null) {
						cached.fromContact = Contact.FindContactByContactID (_appModel, cached.fromContactID);
						cached.chatEntry = _appModel.chatList.FindChatEntryByChatEntryID (cached.chatEntryID); //need to  set appModel in this one
						allMessagesList[_appModel.cacheIndex].Put (key, cached);
					}
				}

				return cached;
			}
		}

		public static IList<Message> FindAllMessagesForChatEntry(ApplicationModel _appModel, ChatEntry chatEntry) {
			lock (_appModel.daoConnection) {
				IList<Message> fromDB = _appModel.msgDao.FindAllMessagesForChatEntry (chatEntry);
				return ProcessMessagesFromDB (_appModel, chatEntry, fromDB);
			}
		}

		public static IList<Message> FindRecentMessagesForChatEntry (ApplicationModel _appModel, ChatEntry chatEntry, int messageLimit) {
			lock (_appModel.daoConnection) {
				IList<Message> fromDB = _appModel.msgDao.FindRecentMessagesForChatEntry (chatEntry, messageLimit);
				return ProcessMessagesFromDB (_appModel, chatEntry, fromDB);
			}
		}

		public static IList<Message> FindPreviousMessagesForMessage (ApplicationModel _appModel, ChatEntry chatEntry, int messageLimit, Message message) {
			lock (_appModel.daoConnection) {
				IList<Message> fromDB = _appModel.msgDao.FindRecentMessageForChatEntryPreviousToSeedMessage (chatEntry, messageLimit, message);
				return ProcessMessagesFromDB (_appModel, chatEntry, fromDB);
			}
		}

		private static IList<Message> ProcessMessagesFromDB (ApplicationModel _appModel, ChatEntry chatEntry, IList<Message> fromDB) {
			IList<Message> retVal = new List<Message> ();
			// keep cache in sync
			foreach (Message dbMessage in fromDB) {
				string key = ToLocalID (dbMessage.messageGUID, chatEntry.fromAlias);
				dbMessage.fromContact = Contact.FindContactByContactID  (_appModel, dbMessage.fromContactID);
				Message cached = allMessagesList[_appModel.cacheIndex].Get (key);
				if (cached != null) {
					// Set the dbMessage's showSentDate on the cached version as the cached version of message's showSentDate property can have a window where it's incorrect.
					// The dbMessage's showSentDate should always be correct.
					cached.showSentDate = dbMessage.showSentDate;
					retVal.Add (cached);
				} else {
					allMessagesList[_appModel.cacheIndex].Put (key, dbMessage);
					retVal.Add (dbMessage);
				}
			}

			return retVal;
		}

		public static bool IsLatestMessage (ApplicationModel _appModel, Message message) {
			ChatEntry entry = message.chatEntry;

			bool isLatestMessage = false;

			em.ChatEntry.Conversation cachedConversation = entry.CachedConversation;
			IList<Message> cachedMessages = cachedConversation.CachedMessages;
			if (cachedMessages != null) {
				int indexOf = cachedMessages.IndexOf (message);
				if (indexOf == cachedMessages.Count - 1) {
					isLatestMessage = true;
				}
			} else {
				Message mostRecentMessage = _appModel.msgDao.FindMostRecentMessageInChatEntry (message.chatEntry);
				if (mostRecentMessage != null) {
					if (mostRecentMessage.messageID == message.messageID) {
						isLatestMessage = true;
					}
				}
			}

			return isLatestMessage;
		}

		public static Message FromChatMessageInput(ApplicationModel _appModel, ChatMessageInput messageInput, Contact from, ChatEntry chatEntry) {
			var retVal = new Message ();
			retVal.appModel = _appModel;
			retVal.messageGUID = messageInput.messageGUID;
			retVal.messageStatusSilent = MessageStatusHelper.FromString( messageInput.messageStatus );
			retVal.messageLifecycle = MessageLifecycleHelper.FromString( messageInput.messageLifecycle );
			retVal.messageChannel = MessageChannelHelper.FromString( messageInput.messageChannel );
			retVal.fromContactID = from.contactID;
			retVal.fromContact = from;
			retVal.inbound = messageInput.inbound ? "Y" : "N";
			retVal.contentType = messageInput.contentType;
			retVal.attributes = messageInput.attributes as JObject;
			retVal.message = messageInput.message;
			retVal.sentDate = messageInput.sentDate;
			retVal.isPersisted = false;
			retVal.chatEntry = chatEntry;
			retVal.mediaRef = messageInput.mediaRef;

			// any channel other than EM we automark as read
			if (!retVal.messageChannel.Equals (MessageChannel.em) && !messageInput.inbound)
				retVal.messageStatusSilent = MessageStatus.read;

			return retVal;
		}

		public MessageOutbound ToMessageOutbound() {
			var retVal = new MessageOutbound ();
			retVal.messageGUID = messageGUID;

			if (chatEntry.fromAlias != null)
				retVal.fromAlias = chatEntry.fromAlias;

			var destinations = new string[ chatEntry.contacts.Count ];
			int i = 0;
			foreach (Contact replyToContact in chatEntry.contacts)
				destinations [i++] = replyToContact.serverID;
			retVal.destination = destinations;
			retVal.mediaRef = mediaRef;
			retVal.contentType = contentType;
			retVal.attributes = attributes;
			retVal.message = message;
			DateTime utcTime = sentDate.ToUniversalTime ();
			var dto = new DateTimeOffset(utcTime);
			retVal.sentDate = String.Format("{0:yyyy-MM-ddTHH:mm:ssZ}", dto);

			return retVal;
		}

		public bool ShouldShowSentDateWithPriorMessage (Message prior) {
			if (prior == null) {
				return true;
			} else {
				TimeSpan diff = sentDate.Subtract (prior.sentDate);
				double seconds = Math.Abs (diff.TotalSeconds);
				return seconds > 15 * 60; // show sent date if greater than 15 minutes
			}
		}

		public void SetQueueEntryUploadDelegates(QueueEntry queueEntry) {
			queueEntry.DelegateWillStartUpload += WillStartUpload;
			queueEntry.DelegateDidUploadPercentage += DidUploadPercentage;
			queueEntry.DelegateDidCompleteUpload += DidFinishUpload;
			queueEntry.DelegateDidFailUpload += DidFailUpload;
		}

		public string ToLocalID() {
			return ToLocalID (messageGUID, chatEntry.fromAlias);
		}

		public static string ToLocalID(string guid, string alias) {
			string s = guid + "," + (alias ?? "");
			return s;
		}

		public static string GUIDFromLocalID (string localID) {
			return localID.Split (new [] { ',' }, StringSplitOptions.None) [0];
		}

		public static string FromAliasFromLocalID (string localID) {
			string[] split = localID.Split (new [] { ',' }, StringSplitOptions.None);
			string alias = split.Length > 1 ? split [1] : null;
			return alias == null ? null : (alias.Trim ().Length == 0 ? null : alias);
		}

		#region handling media download + uploads
		// We need to set the media state on uploads here because media uploads are driven from the QueueEntry, whereas downloads are driven from the media object itself.
		void WillStartUpload() {
			EMTask.DispatchMain (() => {
				media.MediaState = MediaState.Uploading;
				chatEntry.DelegateDidStartUpload (this);
			});
		}

		void DidUploadPercentage(double perc) {
			EMTask.DispatchMain (() => {
				media.Percentage = perc;
				chatEntry.DelegateDidUpdateUploadPercentComplete (this, perc);
			});
		}

		void DidFinishUpload() {
			media.TryMigrateUploadFileToFinalPath ();
			EMTask.DispatchMain (() => {
				media.MediaState = MediaState.Present;
				chatEntry.DelegateDidCompleteUpload (this);
			});
		}

		void DidFailUpload() {
			EMTask.DispatchMain (() => {
				media.MediaState = MediaState.FailedUpload;
				chatEntry.DelegateDidFailUpload (this);
			});
		}

		void BackgroundWillStartDownload () {
			chatEntry.DelegateDidStartDownload (this); // todo rename to BackgroundDelegate--
		}

		void BackgroundDidDownloadPercentage (double perc) {
			chatEntry.DelegateDidUpdateDownloadPercentComplete (this, perc);
		}

		void BackgroundDidFinishDownload (string localpath) {
			chatEntry.DelegateDidCompleteDownload (this);

		}

		void BackgroundDidChangeSoundState () {
			chatEntry.DelegateDidChangeSoundState (this);
		}

		protected void BackgroundDidFailDownload (Notification notif) {
			NotificationCenter.DefaultCenter.PostNotification (this, Constants.Message_DownloadFailed);
		}
		#endregion

		string LocalPathFromUriFunc(Uri uri) {
			return appModel.uriGenerator.GetFilePathForChatEntryUri (uri, chatEntry);
		}

		string UploadPathFromUriFunc () {
			Uri uri = UploadFileUriFromMediaRef ();

			return appModel.uriGenerator.GetFilePathForChatEntryUri (uri, chatEntry);
		}

		/**
		 * @returns 
		 */
		public Uri UploadFileUriFromMediaRef () {
			String mediaRefToUse = this.mediaRef;
			if (mediaRefToUse == null) {
				return null;
			}

			String extension = ExtractFileExtension (mediaRefToUse);
			if (extension == null) {
				return null;
			}

			String uploadFilename = this.messageGUID + extension;

			string spoofed = "http://foo.com/" + uploadFilename;

			return new Uri (spoofed);
		}

		/**
		 * @returns filename extension including the leading period
		 */
		private string ExtractFileExtension (String filename) {
			if (!filename.Contains (".")) {
				return null;
			}

			if (filename.EndsWith (".")) {
				return null;
			}

			return filename.Substring (filename.LastIndexOf ("."));
		}

		public string LocalPathFromMessageGUID () {
			string mediaRefToUse = this.mediaRef;

			return LocalPathFromMessageGUID (mediaRefToUse);
		}

		public string LocalPathFromMessageGUID (string mediaRefToUse) {
			string messageGUIDToUse = this.messageGUID;
			if (!mediaRefToUse.Contains (".")) {
				throw new Exception ("mediaRef must contain extension " + mediaRefToUse);
			}

			if (mediaRefToUse.EndsWith (".")) {
				throw new Exception ("mediaRef must contain extension " + mediaRefToUse);
			}

			string extension = mediaRefToUse.Substring (mediaRefToUse.LastIndexOf ('.') + 1);
			string filename = messageGUIDToUse + "." +  extension;

			string spoofed = "http://foo.com/" + filename;
			return appModel.uriGenerator.GetFilePathForChatEntryUri (new Uri (spoofed), chatEntry);
		}

		public bool ShouldEnlargeEmoji () {
			if (this.message != null) {
				return MatchOnlyEmoji (this.message);
			}
			return false;
		}
		private static Regex emojiRegex = new Regex ("^([\u2702-\u27B0]|(([\uD83C-\uD83D])([\uDC00-\uDFF0]))((\uD83C[\uDFFB-\uDFFF])?))$");
		private static Regex emojiSkinRegex = new Regex ("\ud83c[\udffb-\udfff]");

		public static bool MatchOnlyEmoji (string input) {
			return emojiRegex.IsMatch (input);
		}

		public void StripEmojiSkinModifier () {
			if (this.message != null) {
				this.message = RemoveEmojiSkinModifier (this.message);
			}
		}

		public static string RemoveEmojiSkinModifier (string str) {
			return emojiSkinRegex.Replace (str, "");
		}
	}
}

