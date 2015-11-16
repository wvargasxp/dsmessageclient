using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading;

namespace em {
	public class ChatList {
		public ApplicationModel appModel; 
		public bool entriesInitialized;

		public void BackgroundInitEntries() {
			lock ( appModel.daoConnection ) {
				entries = appModel.chatEntryDao.FindAllChatEntries ();
				foreach (ChatEntry chatEntry in entries)
					chatEntry.chatList = this;
			}
		}

		public bool PerformingUpdates { get { return ignoringUnreadcountChanges; } }
		object entries_mutex = new object();
		IList<ChatEntry> _entries;
		public IList<ChatEntry> entries {
			get {
				lock (entries_mutex) {
					return _entries;
				}
			}

			set {
				lock (entries_mutex) {
					_entries = value;
				}
			}
		}

		public ChatEntry underConstruction;

		public ChatList (ApplicationModel _appModel) {
			appModel = _appModel;
			entries = new List<ChatEntry> ();
			entriesInitialized = false;
			underConstruction = null;

			// we dispatch loading of the entries to the background
			// we make sure that the background task has the same lock (entries_mutex)
			// that the entries themselves use so that we can effectively block
			// any access to the entries that could possibly occur before its loaded.
			//
			// the use of _appModel for the outer lock/wait/pulse was arbitrary, any
			// mutex other than entries_mutex could have been used.
			lock (_appModel) {
				EMTask.DispatchBackground (() => { 
					lock ( entries_mutex ) {
						lock ( _appModel ) {
							Monitor.PulseAll(_appModel);
						}

						BackgroundInitEntries();
					}
				});

				Monitor.Wait (_appModel);
			}
		}

        private Int64 timerSequence = 0;
		private Int64 TimerSequence { get { return this.timerSequence; } set { this.timerSequence = value; } }

		const int UNSET_UNREAD_COUNT_NUMBER = -1;
		int unreadCount = UNSET_UNREAD_COUNT_NUMBER;
		public int UnreadCount {
			get { 
				if (unreadCount == UNSET_UNREAD_COUNT_NUMBER)
					unreadCount = appModel.msgDao.GetTotalUnreadCount ();
				return unreadCount;
			}

			set {
				SetUnreadCount (value, false);
			}
		}

		protected void SetUnreadCount(int value, bool updateServer) {
			if (unreadCount != value) {
				unreadCount = value;
				DelegateTotalUnreadCountDidChange (unreadCount);
			}

			if (updateServer && appModel.liveServerConnection != null)
				appModel.liveServerConnection.SendUnreadCountAsync (unreadCount);
		}

		public delegate void ChatListVisible ();
		public ChatListVisible DidBecomeVisible = () => {};

		public delegate void DidAddChatEntryAt(ChatEntry ce, int addedPosition);
		public delegate void DidRemoveChatEntryAt(ChatEntry ce, int removePosition);
		public delegate void DidMoveChatEntryFrom(ChatEntry ce, int[] positions);
		public delegate void DidChangeChatEntryPreview(ChatEntry chatEntry, int position);
		public delegate void WillStartBulkUpdates ();
		public delegate void DidFinishBulkUpdates ();
		public delegate void TotalUnreadCountDidChange (int updateCount);
		public delegate void DidDownloadChatEntryContactThumbnail(ChatEntry chatEntry);
		public delegate void DidDownloadChatEntryAliasIcon(ChatEntry chatEntry);
		public delegate void DidChangeChatEntryContactThumbnailSource(ChatEntry chatEntry);
		public delegate void DidChangeChatEntryAliasIconSource(ChatEntry chatEntry);
		public delegate void DidChangeChatEntryName (ChatEntry chatEntry);
		public delegate void DidChangeChatEntryColorTheme(ChatEntry chatEntry);
		public delegate void ChatEntryDidHaveContactLeave (ChatEntry chatEntry, Contact leavingContact);

		public DidAddChatEntryAt DelegateDidAdd = delegate(ChatEntry ce, int addedPosition) { };
		public DidRemoveChatEntryAt DelegateDidRemove = delegate(ChatEntry ce, int removePosition) { };
		public DidMoveChatEntryFrom DelegateDidMove = delegate(ChatEntry ce, int[] positions) { };
		public DidChangeChatEntryPreview DelegateDidChangePreview = delegate(ChatEntry chatEntry, int position) { };
		public WillStartBulkUpdates DelegateWillStartBulkUpdates = delegate() { };
		public DidFinishBulkUpdates DelegateDidFinishBulkUpdates = delegate() { };
		public TotalUnreadCountDidChange DelegateTotalUnreadCountDidChange = delegate(int updateCount) { };
		public DidDownloadChatEntryContactThumbnail DelegateDidDownloadChatEntryContactThumbnail = delegate(ChatEntry chatEntry) { };
		public DidDownloadChatEntryAliasIcon DelegateDidDownloadChatEntryAliasIcon = delegate(ChatEntry chatEntry) { };
		public DidChangeChatEntryContactThumbnailSource DelegateDidChangeChatEntryContactThumbnailSource = delegate(ChatEntry chatEntry) { };
		public DidChangeChatEntryAliasIconSource DelegateDidChangeChatEntryAliasIconSource = delegate(ChatEntry chatEntry) { };
		public DidChangeChatEntryName DelegateDidChangeChatEntryName = delegate(ChatEntry chatEntry) { };
		public DidChangeChatEntryColorTheme DelegateDidChangeChatEntryColorTheme = delegate(ChatEntry chatEntry) { };
		public ChatEntryDidHaveContactLeave DelegateChatEntryDidHaveContactLeave = delegate(ChatEntry chatEntry, Contact leavingContact) { };

		public void ResetCachedUnreadCountValue () {
			unreadCount = UNSET_UNREAD_COUNT_NUMBER;
		}

		public void BackgroundUpdateUnreadCount () {
			int totalUnread = appModel.msgDao.GetTotalUnreadCount(); // might be slow
			SetUnreadCount (totalUnread, true);
		}

		public void ObtainUnreadCountAsync (Action<int> callback) {
			EMTask.Dispatch (() => {
				int unread = this.UnreadCount;
				EMTask.DispatchMain(() => {
					callback (unread);
				});
			});
		}

		public void UnreadCountAffected() {
			if (ignoringUnreadcountChanges) {
				shouldScheduleUnreadCountChangeAfterBulkUpdates = true;
				return;
			}

			ScheduleUnreadCountChangeTimer ();
		}

		private readonly int DISPLAY_UNREAD_COUNT_TIMER = 300; // .3 second in milliseconds
		protected void ScheduleUnreadCountChangeTimer () {
			Int64 seq = ++this.TimerSequence;
			WeakReference thisRef = new WeakReference (this);
			new Timer ((object o) => {
				Int64 state = (Int64)o;
				if (state == this.TimerSequence) {
					ChatList self = thisRef.Target as ChatList;
					if (self != null)
						self.BackgroundUpdateUnreadCount();
				}
			}, seq, DISPLAY_UNREAD_COUNT_TIMER, Timeout.Infinite);
		}

		public ChatEntry FindChatEntryByReplyToServerIDs(IList<string> replyToServerIDs, string alias) {
			lock (appModel.daoConnection) {
				foreach (ChatEntry entry in entries) {
					if (!(new EqualsBuilder<string> (alias, entry.fromAlias).Equals ()))
						continue;

					if (entry.HasSameContacts (replyToServerIDs))
						return entry; //inside entries so should already have appModel and chatList
				}
			}

			return null;
		}

		public ChatEntry FindChatEntryByChatEntryID(int chatEntryID) {
			lock (appModel.daoConnection) {
				foreach (ChatEntry entry in entries) {
					if (entry.chatEntryID == chatEntryID )
						return entry; //inside entries so should already have appModel and chatList
				}
			}

			return null;
		}

		public ChatEntry FindOrCreateChatEntryForReplyTo(IList<Contact> replyTo, string alias, DateTime createDateIfNeeded) {
			lock (appModel.daoConnection) {
				foreach (ChatEntry entry in entries) {
					IList<Contact> entryContacts = entry.contacts;

					if (entryContacts.Count != replyTo.Count)
						continue;

					if (!(new EqualsBuilder<string> (alias, entry.fromAlias).Equals ()))
						continue;

					bool continueSearch = false;
					foreach (Contact replyContact in replyTo) {
						bool contactIsInEntry = false;
						foreach (Contact entryContact in entryContacts) {
							if (entryContact.MatchesByServerId (replyContact)) {
								contactIsInEntry = true;
								break;
							}
						}

						if (!contactIsInEntry) {
							continueSearch = true;
							break;
						}
					}

					if (continueSearch)
						continue;

					return entry; //inside entries so should already have appModel and chatList
				}

				ChatEntry chatEntry = ChatEntry.NewChatEntry (appModel, createDateIfNeeded); //need to set appModel/chatlist here
				chatEntry.contacts = new List<Contact>(replyTo);
				chatEntry.fromAlias = alias;
				chatEntry.BackgroundSave ();
				return chatEntry;
			}
		}

		//don't need to set chat list or app model because chat entry is either already in entries or was created anew and passed in an appmodel; 
		//the thing that calls this requires that it already has an appmodel
		public void InsertOrMoveToTop(ChatEntry chatEntry) {
			lock (appModel.daoConnection) {
				chatEntry.entryOrder = DateTime.Now.ToEMStandardTime(appModel).Ticks / TimeSpan.TicksPerMillisecond;

				int indexOf = entries.IndexOf (chatEntry);
				if (indexOf != -1)
					entries.Remove (chatEntry);
				entries.Insert (0, chatEntry);

				if (chatEntry.isPersisted)
					appModel.chatEntryDao.UpdateChatEntry (chatEntry);
				else
					appModel.chatEntryDao.InsertChatEntry (chatEntry);

				if (indexOf == -1)
					EMTask.DispatchMain (() => DelegateDidAdd (chatEntry, 0));
				else if ( indexOf != 0 ) // only send out a move notice if the item actually moved!
					EMTask.DispatchMain (() => DelegateDidMove (chatEntry, new [] {indexOf, 0}));
			}
		}

		public void SaveUnderConstruction(ChatEntry chatEntry) {
			lock (appModel.daoConnection) {
				chatEntry.entryOrder = -1;

				if (chatEntry.isPersisted)
					appModel.chatEntryDao.UpdateChatEntry (chatEntry);
				else
					appModel.chatEntryDao.InsertChatEntry (chatEntry);
			}
		}

		public void RemoveChatEntry(int position, bool updateServer) {
			InnerRemoveChatEntry (position, updateServer, true);
		}

		protected void InnerRemoveChatEntry(int position, bool updateServer, bool useDelegate) {
			lock (appModel.daoConnection) {
				if (position == -1) {
					// removing any under construction chat entry.
					ChatEntry toRemove = appModel.chatEntryDao.FindUnderConstructionChatEntry();
					if ( toRemove != null )
						appModel.chatEntryDao.RemoveChatEntry (toRemove);
				}
				else {
					// removing chat entries that actually exist in the list.
					ChatEntry toRemove = entries [position];
					entries.RemoveAt (position);
					toRemove.MarkAllUnreadIgnored ();
					appModel.chatEntryDao.RemoveChatEntry (toRemove);
					Contact.RemoveUnusedTemporaryContacts (appModel);

					if (updateServer) {
						var queueEntry = new QueueEntry ();
						queueEntry.destination = StompPath.kRemoveChatEntry;
						queueEntry.methodType = QueueRestMethodType.NotApplicable;
						queueEntry.route = QueueRoute.Websocket;
						queueEntry.sentDate = DateTime.Now.ToEMStandardTime (appModel);

						string json = JsonConvert.SerializeObject (toRemove.ToRemoveChatEntryOutbound ());
						byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes (json);
						QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (appModel, jsonBytes, "application/json", "removeMessage.json", "removeMessage.json");
						queueEntry.contents.Add (messageContents);

						appModel.outgoingQueue.EnqueueAndSend (queueEntry);
					}

					if ( useDelegate )
						EMTask.DispatchMain (() => DelegateDidRemove (toRemove, position));
				}
			}
		}

		public void RemoveChatEntryAtAsync(int position, bool updateServer) {
			EMTask.DispatchBackground (() => InnerRemoveChatEntry (position, updateServer, true));
		}

		public void RemoveChatEntryAtAsyncSilent(int position, bool updateServer) {
			EMTask.DispatchBackground (() => InnerRemoveChatEntry (position, updateServer, false));
		}

		public void DidUpdateAliases() {
			EMTask.DispatchMain (() => {
				foreach (ChatEntry chatEntry in entries) {
					if (chatEntry.fromAlias != null)
						// TODO, would like stronger detection of changes so
						// that we aren't forcing these reloads all the time.
						DelegateDidChangeChatEntryAliasIconSource (chatEntry);
				}
			});
		}

		public void DidDownloadAliasIconMedia(AliasInfo alias) {
			EMTask.DispatchMain (() => {
				// look for any chat entries that use this alias
				foreach (ChatEntry chatEntry in entries) {
					if (chatEntry.fromAlias != null && chatEntry.fromAlias.Equals (alias.serverID))
						DelegateDidDownloadChatEntryAliasIcon (chatEntry);
				}
			});
		}

		bool ignoringUnreadcountChanges = false;
		bool shouldScheduleUnreadCountChangeAfterBulkUpdates = false;
		public void BackgroundStartingBulkUpdates() {
			EMTask.PerformOnMain (() => {
				ignoringUnreadcountChanges = true;
				DelegateWillStartBulkUpdates ();
			});
		}

		public void BackgroundStoppingBulkUpdates() {
			EMTask.PerformOnMain (() => {
				ignoringUnreadcountChanges = false;
				if ( shouldScheduleUnreadCountChangeAfterBulkUpdates ) {
					ScheduleUnreadCountChangeTimer();

					shouldScheduleUnreadCountChangeAfterBulkUpdates = false;
				}
					
				DelegateDidFinishBulkUpdates();
			});
		}
	}
}