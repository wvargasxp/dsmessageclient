using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace em {
	/**
	 * Object that encapsulates the standard behavior of an inbox
	 * (without having any actual view logic of it's own).  This abstract class defines
	 * several callbacks to implement to respond to various UI related events.
	 *
	 * To facilitate animating of updates this class also batches up updates
	 * and only submits them when the view is prepared to handle them.
	 */
	public abstract class AbstractInBoxController {
		public static readonly string CHATENTRY_THUMBNAIL = "thumbnail";
		public static readonly string CHATENTRY_PREVIEW = "preview";
		public static readonly string CHATENTRY_NAME = "name";
		public static readonly string CHATENTRY_COLOR_THEME = "colorTheme";

		public ChatList chatList { get; set; }
		public List<ChatEntry> viewModel { get; set; }

		readonly IList<ModelStructureChange<ChatEntry>> pendingMovesOrInserts;
		readonly IList<ModelAttributeChange<ChatEntry,object>> pendingChanges;

		WeakDelegateProxy DidAddChatEntryProxy;
		WeakDelegateProxy DidRemoveChatEntryProxy;
		WeakDelegateProxy DidMoveChatEntryProxy;
		WeakDelegateProxy DidChangePreviewAtProxy;
		WeakDelegateProxy WillStartBulkUpdatesProxy;
		WeakDelegateProxy DidFinishBulkUpdatesProxy;
		WeakDelegateProxy DidChangeThumbnailProxy;
		WeakDelegateProxy DidDownloadThumbnailProxy;
		WeakDelegateProxy DidDownloadAliasIconProxy;
		WeakDelegateProxy DidChangeChatEntryAliasIconSourceProxy;
		WeakDelegateProxy DidChangeOwnColorThemeProxy;
		WeakDelegateProxy DidChangeChatEntryNameProxy;
		WeakDelegateProxy DidChangeChatEntryColorThemeProxy;
		WeakDelegateProxy ContactDidLeaveAdhocProxy;

		private bool showingProgressIndicator = false;
		public bool ShowingProgressIndicator { 
			get {
				return showingProgressIndicator;
			}

			set {
				showingProgressIndicator = value;
				UpdateTitleProgressIndicatorVisibility ();
			}
		}

		protected AbstractInBoxController (ChatList cl) {
			chatList = cl;
			viewModel = new List<ChatEntry> ();
			foreach (ChatEntry chatEntry in cl.entries )
				viewModel.Add(chatEntry);

			lock (this) {
				su = chatList.PerformingUpdates ? 1 : 0;
			}

			NotificationCenter.DefaultCenter.AddWeakObserver (null, ApplicationModel.IS_HANDLING_MISSED_MESSAGES_STATUS_UPDATE, MissedMessageStatusUpdate);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.PlatformFactory_ShowNetworkIndicatorNotification, HandleShowNetworkIndicator);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.PlatformFactory_HideNetworkIndicatorNotification, HandleHideNetworkIndicator);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.NotificationEntryDao_UnreadCountChanged, HandleNotificationNotificationEntryCountChanged);

			pendingMovesOrInserts = new List<ModelStructureChange<ChatEntry>> ();
			pendingChanges = new List<ModelAttributeChange<ChatEntry,object>> ();

			DidAddChatEntryProxy = WeakDelegateProxy.CreateProxy<ChatEntry, int> (DidAddChatEntry);
			DidRemoveChatEntryProxy = WeakDelegateProxy.CreateProxy<ChatEntry, int> (DidRemoveChatEntry);
			DidMoveChatEntryProxy = WeakDelegateProxy.CreateProxy<ChatEntry, int[]> (DidMoveChatEntry);
			DidChangePreviewAtProxy = WeakDelegateProxy.CreateProxy<ChatEntry, int> (DidChangePreviewAt);
			WillStartBulkUpdatesProxy = WeakDelegateProxy.CreateProxy (WillStartReceivingBulkUpdates);
			DidFinishBulkUpdatesProxy = WeakDelegateProxy.CreateProxy (DidFinishReceivingBulkUpdates);
			DidChangeThumbnailProxy = WeakDelegateProxy.CreateProxy<ChatEntry> (DidChangeThumbnailSourceOnChatEntry);
			DidDownloadThumbnailProxy = WeakDelegateProxy.CreateProxy<ChatEntry> (DidUpdateThumbnailOnChatEntry);
			DidDownloadAliasIconProxy = WeakDelegateProxy.CreateProxy<ChatEntry> (DidUpdateAliasIconOnChatEntry);
			DidChangeChatEntryAliasIconSourceProxy = WeakDelegateProxy.CreateProxy<ChatEntry> (DidChangeChatEntryAliasIconSource);
			DidChangeOwnColorThemeProxy = WeakDelegateProxy.CreateProxy<CounterParty> (DidChangeColorTheme);
			DidChangeChatEntryNameProxy = WeakDelegateProxy.CreateProxy<ChatEntry> (DidChangeChatEntryName);
			DidChangeChatEntryColorThemeProxy = WeakDelegateProxy.CreateProxy<ChatEntry> (DidChangeChatEntryColorTheme);
			ContactDidLeaveAdhocProxy = WeakDelegateProxy.CreateProxy<ChatEntry,Contact> (ContactDidLeaveAdhoc);

			chatList.DelegateDidAdd += DidAddChatEntryProxy.HandleEvent<ChatEntry, int>;
			chatList.DelegateDidRemove += DidRemoveChatEntryProxy.HandleEvent<ChatEntry, int>;
			chatList.DelegateDidMove += DidMoveChatEntryProxy.HandleEvent<ChatEntry, int[]>;
			chatList.DelegateDidChangePreview += DidChangePreviewAtProxy.HandleEvent<ChatEntry, int>;
			chatList.DelegateWillStartBulkUpdates += WillStartBulkUpdatesProxy.HandleEvent;
			chatList.DelegateDidFinishBulkUpdates += DidFinishBulkUpdatesProxy.HandleEvent;
			chatList.DelegateDidChangeChatEntryContactThumbnailSource += DidChangeThumbnailProxy.HandleEvent<ChatEntry>;
			chatList.DelegateDidDownloadChatEntryContactThumbnail += DidDownloadThumbnailProxy.HandleEvent<ChatEntry>;
			chatList.DelegateDidDownloadChatEntryAliasIcon += DidDownloadAliasIconProxy.HandleEvent<ChatEntry>;
			chatList.DelegateDidChangeChatEntryAliasIconSource += DidChangeChatEntryAliasIconSourceProxy.HandleEvent<ChatEntry>;
			chatList.appModel.account.accountInfo.DelegateDidChangeColorTheme += DidChangeOwnColorThemeProxy.HandleEvent<CounterParty>;
			chatList.DelegateDidChangeChatEntryName += DidChangeChatEntryNameProxy.HandleEvent<ChatEntry>;
			chatList.DelegateDidChangeChatEntryColorTheme += DidChangeChatEntryColorThemeProxy.HandleEvent<ChatEntry>;
			chatList.DelegateChatEntryDidHaveContactLeave += ContactDidLeaveAdhocProxy.HandleEvent<ChatEntry,Contact>;
		}

		/**
		 * Must be called when the inbox is being destroyed.
		 */
		public void Dispose () {
			chatList.DelegateDidAdd -= DidAddChatEntryProxy.HandleEvent<ChatEntry, int>;
			chatList.DelegateDidRemove -=  DidRemoveChatEntryProxy.HandleEvent<ChatEntry, int>;
			chatList.DelegateDidMove -= DidMoveChatEntryProxy.HandleEvent<ChatEntry, int[]>;
			chatList.DelegateDidChangePreview -= DidChangePreviewAtProxy.HandleEvent<ChatEntry, int>;
			chatList.DelegateWillStartBulkUpdates -= WillStartBulkUpdatesProxy.HandleEvent;
			chatList.DelegateDidFinishBulkUpdates -= DidFinishBulkUpdatesProxy.HandleEvent;
			chatList.DelegateDidChangeChatEntryContactThumbnailSource -= DidChangeThumbnailProxy.HandleEvent<ChatEntry>;
			chatList.DelegateDidDownloadChatEntryContactThumbnail -= DidDownloadThumbnailProxy.HandleEvent<ChatEntry>;
			chatList.DelegateDidDownloadChatEntryAliasIcon -= DidDownloadAliasIconProxy.HandleEvent<ChatEntry>;
			chatList.DelegateDidChangeChatEntryAliasIconSource -= DidChangeChatEntryAliasIconSourceProxy.HandleEvent<ChatEntry>;
			chatList.appModel.account.accountInfo.DelegateDidChangeColorTheme -= DidChangeOwnColorThemeProxy.HandleEvent<CounterParty>;
			chatList.DelegateDidChangeChatEntryName -= DidChangeChatEntryNameProxy.HandleEvent<ChatEntry>;
			chatList.DelegateDidChangeChatEntryColorTheme -= DidChangeChatEntryColorThemeProxy.HandleEvent<ChatEntry>;
			chatList.DelegateChatEntryDidHaveContactLeave -= ContactDidLeaveAdhocProxy.HandleEvent<ChatEntry,Contact>;
		}
			
		int su;
		public void SuspendUpdates() {
			lock(this) {
				su++;
			}
		}

		public void ResumeUpdates(bool animate) {
			lock(this) {
				su--;

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges (animate);
			}
		}

		protected bool SuspendedUpdates {
			get {
				lock (this) {
					return su > 0;
				}
			}
		}

		protected void WillStartReceivingBulkUpdates () {
			//this is called on the main thread
			SuspendUpdates ();
		}

		protected void DidFinishReceivingBulkUpdates () {
			//this is called on the main thread
			ResumeUpdates(false);
		}

		/**
		 * Callback that is used to tell the inbox it has either new chat entries or
		 * the status of some chat entries has changed.
		 * The callback parameter is used to call ResumeUpdates() so it's clear to see when we call Suspend & Resume
		 */
		public abstract void HandleUpdatesToChatList (IList<ModelStructureChange<ChatEntry>> repositionChatItems, IList<ModelAttributeChange<ChatEntry,object>> previewUpdates, bool animated, Action callback);

		/*
		 * Callback that the user has changed their own color scheme
		 */
		public abstract void DidChangeColorTheme ();

		public abstract void ShowNotificationBanner (ChatEntry entry);

		/*
		 * Callback for a missed message update. (Started processing / Finished processing).
		 */
		public abstract void UpdateTitleProgressIndicatorVisibility ();

		protected void DidAddChatEntry (ChatEntry ce, int addedPosition) {
			lock (this) {
				pendingMovesOrInserts.Add (new ModelStructureChange<ChatEntry> (ce, ModelStructureChange.added));

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges (true);
			}
		}

		protected void DidRemoveChatEntry (ChatEntry ce, int removePosition) {
			lock (this) {
				pendingMovesOrInserts.Add (new ModelStructureChange<ChatEntry> (ce, ModelStructureChange.deleted));

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges (true);
			}
		}

		protected void DidMoveChatEntry (ChatEntry ce, int[] positions) {
			lock (this) {
				pendingMovesOrInserts.Add (new ModelStructureChange<ChatEntry> (ce, ModelStructureChange.moved));

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges (true);
			}
		}

		protected void DidChangeColorTheme (CounterParty accountInfo) {
			EMTask.DispatchMain (DidChangeColorTheme);
		}

		protected void DidChangeThumbnailSourceOnChatEntry (ChatEntry ce) {
			//TODO: maybe something better could be done here
			// but this reload should kick off the download.
			//this method runs on the main thread
			DidUpdateThumbnailOnChatEntry (ce);
		}

		protected void DidUpdateAliasIconOnChatEntry (ChatEntry ce) {
			//this method runs on the main thread
			DidUpdateThumbnailOnChatEntry (ce);
		}

		protected void DidChangeChatEntryAliasIconSource (ChatEntry ce) {
			//this method runs on the main thread
			DidUpdateThumbnailOnChatEntry (ce);
		}

		protected void DidUpdateThumbnailOnChatEntry (ChatEntry ce) {
			lock (this) {
				pendingChanges.Add (new ModelAttributeChange<ChatEntry,object> (ce, CHATENTRY_THUMBNAIL, true));

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges (true);
			}
		}

		protected void DidChangePreviewAt (ChatEntry ce, int position) {
			lock (this) {
				pendingChanges.Add (new ModelAttributeChange<ChatEntry,object> (ce, CHATENTRY_PREVIEW, true));

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges (true);
			}
		}

		protected void DidChangeChatEntryName (ChatEntry ce) {
			lock (this) {
				pendingChanges.Add (new ModelAttributeChange<ChatEntry,object> (ce, CHATENTRY_NAME, true));

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges (true);
			}
		}

		protected void DidChangeChatEntryColorTheme (ChatEntry ce) {
			lock (this) {
				pendingChanges.Add (new ModelAttributeChange<ChatEntry,object> (ce, CHATENTRY_COLOR_THEME, true));

				if ( !SuspendedUpdates )
					PostStructureAndAttributeChanges (true);
			}
		}

		protected void ContactDidLeaveAdhoc (ChatEntry ce, Contact leavingContact) {
			// treat as a name and thumbnail change
			DidChangeChatEntryName (ce);
			DidUpdateThumbnailOnChatEntry (ce);
		}

		protected void PostStructureAndAttributeChanges(bool animated) {
			lock (this) {
				if (pendingChanges.Count == 0 && pendingMovesOrInserts.Count == 0)
					return;
				
				SuspendUpdates ();

				IList<ModelStructureChange<ChatEntry>> structureChanges = null;
				if (pendingMovesOrInserts.Count > 0) {
					structureChanges = new List<ModelStructureChange<ChatEntry>> ();
					foreach (ModelStructureChange<ChatEntry> m in pendingMovesOrInserts)
						structureChanges.Add (m);

					pendingMovesOrInserts.Clear ();
				}

				IList<ModelAttributeChange<ChatEntry,object>> atrributeChanges = null;
				if (pendingChanges.Count > 0) {
					atrributeChanges = new List<ModelAttributeChange<ChatEntry,object>> ();
					foreach (ModelAttributeChange<ChatEntry,object> m in pendingChanges)
						atrributeChanges.Add (m);

					pendingChanges.Clear ();
				}

				if ( animated )
					animated = ((structureChanges != null ? structureChanges.Count : 0) +
				                (atrributeChanges != null ? atrributeChanges.Count : 0)) < 3;

				WeakReference thisRef = new WeakReference (this);
				EMTask.DispatchMain (() => {
					AbstractInBoxController self = thisRef.Target as AbstractInBoxController;
					if (self != null) {
						if ( structureChanges != null ) {
							foreach (ModelStructureChange<ChatEntry> change in structureChanges) {
								if (change.Change == ModelStructureChange.added) {
									viewModel.Insert (0, change.ModelObject);
								}
								else if (change.Change == ModelStructureChange.deleted) {
									change.Index = viewModel.IndexOf(change.ModelObject);
									viewModel.Remove (change.ModelObject);
								}
								else if (change.Change == ModelStructureChange.moved) {
									int indexOf = viewModel.IndexOf(change.ModelObject);
									if ( indexOf != -1 ) {
										change.Index = indexOf;
										viewModel.RemoveAt(indexOf);
										// not sure if assuming zero is correct, possibly look
										// at final position?
										viewModel.Insert(0, change.ModelObject);
									}
								}
							}
						}

						self.HandleUpdatesToChatList(structureChanges, atrributeChanges, animated, () => {
							ResumeUpdates(true);
						});
					}
				}, null);
			}
		}

		public abstract void GoToChatEntry (ChatEntry chatEntry);

		public abstract void UpdateBurgerUnreadCount (int unreadCount);

		public void MissedMessageStatusUpdate (Notification n) {
			Dictionary<string, bool> extra = (Dictionary<string, bool>)n.Extra;
			bool isHandlingMissedMessages = extra [ApplicationModel.IS_HANDLING_MISSED_MESSAGES_STATUS_UPDATE_EXTRA];
			this.ShowingProgressIndicator = isHandlingMissedMessages;
			EMTask.DispatchBackground (() => {
				string guid64 = chatList.appModel.GuidFromNotification;
				if (guid64 != null) {
					ChatEntry chatEntry = chatList.appModel.FindChatEntryThatMatchesGUID (guid64);
					if (chatEntry != null) {
						EMTask.DispatchMain ( () => {
							GoToChatEntry (chatEntry);
						});
						chatList.appModel.GuidFromNotification = null;
					}
				}
			});
		}

		public void HandleShowNetworkIndicator (Notification n) {
			this.ShowingProgressIndicator = true;
		}

		public void HandleHideNetworkIndicator (Notification n) {
			this.ShowingProgressIndicator = false;
		}

		private void HandleNotificationNotificationEntryCountChanged (em.Notification notif) {
			int newCount = Convert.ToInt32 (notif.Extra);
			UpdateBurgerUnreadCount (newCount);
		}
	}
}