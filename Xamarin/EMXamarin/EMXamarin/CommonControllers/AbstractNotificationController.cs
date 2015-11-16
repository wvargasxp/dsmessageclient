using System.Collections.Generic;
using System;

namespace em {
	/**
	 * Object that encapsulates the standard behavior of a notification inbox
	 * (without having any actual view logic of it's own).  This abstract class defines
	 * several callbacks to implement to respond to various UI related events.
	 *
	 * To facilitate animating of updates this class also batches up updates
	 * and only submits them when the view is prepared to handle them.
	 */
	public abstract class AbstractNotificationController {
		public NotificationList notificationList { get; set; }

		int su;
		int suspendingUpdates {
			get { lock (this) { return su; } }
			set { lock (this) { 
					su = value;
					System.Diagnostics.Debug.Assert (su >= 0);
				} 
			}
		}

		protected bool SuspendedUpdates {
			get {
				return this.suspendingUpdates > 0;
			}
		}

		readonly IList<MoveOrInsertInstruction<NotificationEntry>> pendingMovesOrInserts;
		readonly IList<ChangeInstruction<NotificationEntry>> pendingChanges;

		WeakDelegateProxy DidAddNotificationEntryProxy;
		WeakDelegateProxy DidRemoveNotificationEntryProxy;
		WeakDelegateProxy DidMoveNotificationEntryProxy;
		WeakDelegateProxy WillStartBulkUpdatesProxy;
		WeakDelegateProxy DidFinishBulkUpdatesProxy;
		WeakDelegateProxy DidChangeThumbnailProxy;
		WeakDelegateProxy DidDownloadThumbnailProxy;
		WeakDelegateProxy DidChangeOwnColorThemeProxy;
		WeakDelegateProxy DidChangeNotificationEntryColorThemeProxy;
		WeakDelegateProxy ChatListDidBecomeVisible;

		ApplicationModel appModel;

		protected AbstractNotificationController (ApplicationModel appModel_) {
			appModel = appModel_;
			notificationList = appModel.notificationList;

			suspendingUpdates = appModel.chatList.PerformingUpdates ? 1 : 0;

			pendingMovesOrInserts = new List<MoveOrInsertInstruction<NotificationEntry>> ();
			pendingChanges = new List<ChangeInstruction<NotificationEntry>> ();

			DidAddNotificationEntryProxy = WeakDelegateProxy.CreateProxy<NotificationEntry, int> (DidAddNotificationEntry);
			DidRemoveNotificationEntryProxy = WeakDelegateProxy.CreateProxy<NotificationEntry, int> (DidRemoveNotificationEntry);
			DidMoveNotificationEntryProxy = WeakDelegateProxy.CreateProxy<NotificationEntry, int[]> (DidMoveNotificationEntry);
			WillStartBulkUpdatesProxy = WeakDelegateProxy.CreateProxy (WillStartReceivingBulkUpdates);
			DidFinishBulkUpdatesProxy = WeakDelegateProxy.CreateProxy<IList<NotificationEntry>> (DidFinishReceivingBulkUpdates);
			DidChangeThumbnailProxy = WeakDelegateProxy.CreateProxy<NotificationEntry> (DidChangeThumbnailSourceOnNotificationEntry);
			DidDownloadThumbnailProxy = WeakDelegateProxy.CreateProxy<NotificationEntry> (DidUpdateThumbnailOnNotificationEntry);
			DidChangeOwnColorThemeProxy = WeakDelegateProxy.CreateProxy<CounterParty> (DidChangeColorTheme);
			DidChangeNotificationEntryColorThemeProxy = WeakDelegateProxy.CreateProxy<NotificationEntry> (DidChangeNotificationEntryColorTheme);
			ChatListDidBecomeVisible = WeakDelegateProxy.CreateProxy (Dispose);

			notificationList.DelegateDidAdd += DidAddNotificationEntryProxy.HandleEvent<NotificationEntry, int>;
			notificationList.DelegateDidRemove += DidRemoveNotificationEntryProxy.HandleEvent<NotificationEntry, int>;
			notificationList.DelegateDidMove += DidMoveNotificationEntryProxy.HandleEvent<NotificationEntry, int[]>;
			notificationList.DelegateWillStartBulkUpdates += WillStartBulkUpdatesProxy.HandleEvent;
			notificationList.DelegateDidFinishBulkUpdates += DidFinishBulkUpdatesProxy.HandleEvent<IList<NotificationEntry>>;
			notificationList.DelegateDidChangeNotificationEntryContactThumbnailSource += DidChangeThumbnailProxy.HandleEvent<NotificationEntry>;
			notificationList.DelegateDidDownloadNotificationEntryContactThumbnail += DidDownloadThumbnailProxy.HandleEvent<NotificationEntry>;
			notificationList.appModel.account.accountInfo.DelegateDidChangeColorTheme += DidChangeOwnColorThemeProxy.HandleEvent<CounterParty>;
			notificationList.DelegateDidChangeNotificationEntryColorTheme += DidChangeNotificationEntryColorThemeProxy.HandleEvent<NotificationEntry>;
			appModel.chatList.DidBecomeVisible += ChatListDidBecomeVisible.HandleEvent;
		}

		/**
		 * Must be called when the notifications is being destroyed.
		 */
		public void Dispose() {
			notificationList.DelegateDidAdd -= DidAddNotificationEntryProxy.HandleEvent<NotificationEntry, int>;
			notificationList.DelegateDidRemove -=  DidRemoveNotificationEntryProxy.HandleEvent<NotificationEntry, int>;
			notificationList.DelegateDidMove -= DidMoveNotificationEntryProxy.HandleEvent<NotificationEntry, int[]>;
			notificationList.DelegateWillStartBulkUpdates -= WillStartBulkUpdatesProxy.HandleEvent;
			notificationList.DelegateDidFinishBulkUpdates -= DidFinishBulkUpdatesProxy.HandleEvent<IList<NotificationEntry>>;
			notificationList.DelegateDidChangeNotificationEntryContactThumbnailSource -= DidChangeThumbnailProxy.HandleEvent<NotificationEntry>;
			notificationList.DelegateDidDownloadNotificationEntryContactThumbnail -= DidDownloadThumbnailProxy.HandleEvent<NotificationEntry>;
			notificationList.appModel.account.accountInfo.DelegateDidChangeColorTheme -= DidChangeOwnColorThemeProxy.HandleEvent<CounterParty>;
			notificationList.DelegateDidChangeNotificationEntryColorTheme -= DidChangeNotificationEntryColorThemeProxy.HandleEvent<NotificationEntry>;
			appModel.chatList.DidBecomeVisible -= ChatListDidBecomeVisible.HandleEvent;
		}

		/**
		 * Called during animation so new updates won't be forwarded
		 * to the view layer.
		 */
		public void SuspendUpdates() {
			lock (this) {
				suspendingUpdates++;
			}
		}

		/**
		 * Called after animations complete to allow the resumptions of
		 * events being sent to the view layer.
		 */
		public void ResumeUpdates(bool animate) {
			lock (this) {
				suspendingUpdates--;

				if (suspendingUpdates <= 0 && (pendingMovesOrInserts.Count > 0 || pendingChanges.Count > 0)) {
					//don't try to animate too many inserts, moves and deletes
					if (pendingMovesOrInserts.Count > 2)
						animate = false;

					HandleUpdatesToNotificationList (pendingMovesOrInserts, pendingChanges, animate, (bool animateFlag) => {
						//noop here
					});

					pendingMovesOrInserts.Clear ();
					pendingChanges.Clear ();

					suspendingUpdates = 0;
				}
			}
		}

		protected void WillStartReceivingBulkUpdates() {
			//this is called on the main thread
			SuspendUpdates ();
		}

		protected void DidFinishReceivingBulkUpdates(IList<NotificationEntry> old) {
			//this is called on the main thread
			FinalizeInstructionPositions (old);
			ResumeUpdates (true);
		}

		public enum NotificationChangeType {
			Photo,
			Theme
		}

		/**
		 * Callback that is used to tell the inbox it has either new chat entries or
		 * the status of some chat entries has changed.
		 * The callback parameter is used to call ResumeUpdates() so it's clear to see when we call Suspend & Resume
		 */
		public abstract void HandleUpdatesToNotificationList(IList<MoveOrInsertInstruction<NotificationEntry>> repositionItems, IList<ChangeInstruction<NotificationEntry>> previewUpdates, bool animated, Action<bool> callback);

		/*
		 * Callback that the user has changed their own color scheme
		 */
		public abstract void DidChangeColorTheme ();

		protected void DidAddNotificationEntry(NotificationEntry ne, int addedPosition) {
			var instruction = new MoveOrInsertInstruction<NotificationEntry> (-1, addedPosition);
			instruction.Entry = ne;
			HandleInsertMoveOrRemoveInstruction (ne, instruction);
		}

		protected void DidRemoveNotificationEntry(NotificationEntry ne, int removePosition) {
			var instruction = new MoveOrInsertInstruction<NotificationEntry> (removePosition, -1);
			instruction.Entry = ne;
			HandleInsertMoveOrRemoveInstruction (ne, instruction);
		}

		protected void DidMoveNotificationEntry(NotificationEntry ne, int[] positions) {
			var instruction = new MoveOrInsertInstruction<NotificationEntry> (positions [0], positions [1]);
			instruction.Entry = ne;
			HandleInsertMoveOrRemoveInstruction (ne, instruction);
		}

		protected ChangeInstruction<NotificationEntry> ExistingNotificationEntryChangeInstruction (NotificationEntry ne) {
			foreach ( ChangeInstruction<NotificationEntry> ins in pendingChanges )
				if ( ins.Entry.Equals(ne) )
					return ins;

			return null;
		}

		protected void DidChangeColorTheme (CounterParty accountInfo) {
			EMTask.DispatchMain (DidChangeColorTheme);
		}

		protected void DidChangeThumbnailSourceOnNotificationEntry (NotificationEntry ne) {
			//TODO: maybe something better could be done here
			// but this reload should kick off the download.
			//this method runs on the main thread
			DidUpdateThumbnailOnNotificationEntry (ne);
		}

		protected void DidUpdateThumbnailOnNotificationEntry (NotificationEntry ne) {
			var instruction = new ChangeInstruction<NotificationEntry> (ne, true, false, false, false);
			HandleChangeInstruction (ne, instruction, NotificationChangeType.Photo);
		}

		protected void DidChangeNotificationEntryColorTheme (NotificationEntry ne) {
			var instruction = new ChangeInstruction<NotificationEntry> (ne, false, false, false, true);
			HandleChangeInstruction (ne, instruction, NotificationChangeType.Theme);
		}

		void FinalizeInstructionPositions (IList<NotificationEntry> old) {
			foreach (MoveOrInsertInstruction<NotificationEntry> i in pendingMovesOrInserts) {
				var oldIndex = old.IndexOf (i.Entry);
				var newIndex = notificationList.Entries.IndexOf (i.Entry);

				i.FromPosition = oldIndex;
				i.ToPosition = newIndex;
			}
		}

		void HandleInsertMoveOrRemoveInstruction(NotificationEntry ne, MoveOrInsertInstruction<NotificationEntry> instruction) {
			EMTask.DispatchMain (() => {
				if (this.SuspendedUpdates)
					pendingMovesOrInserts.Add (new MoveOrInsertInstruction<NotificationEntry> (ne));
				else {
					SuspendUpdates();
					var movesInsertsOrDeletes = new List<MoveOrInsertInstruction<NotificationEntry>> ();
					movesInsertsOrDeletes.Add(instruction);
					HandleUpdatesToNotificationList (movesInsertsOrDeletes, new List<ChangeInstruction<NotificationEntry>>(), true, (bool animateFlag) => {
						ResumeUpdates (animateFlag);
					});
				}
			});
		}

		void HandleChangeInstruction(NotificationEntry ne, ChangeInstruction<NotificationEntry> instruction, NotificationChangeType type) {
			EMTask.DispatchMain (() => {
				if (this.SuspendedUpdates) {
					AddOrUpdatePendingChangesWithInstruction (ne, instruction, type);
				} else {
					SuspendUpdates();
					List<ChangeInstruction<NotificationEntry>> changes = new List<ChangeInstruction<NotificationEntry>> ();
					changes.Add(instruction);
					HandleUpdatesToNotificationList (new List<MoveOrInsertInstruction<NotificationEntry>>(), changes, true, (bool animateFlag) => {
						ResumeUpdates (animateFlag);
					});
				}
			});
		}

		private void AddOrUpdatePendingChangesWithInstruction (NotificationEntry ne, ChangeInstruction<NotificationEntry> instruction, NotificationChangeType type) {
			ChangeInstruction<NotificationEntry> existing = ExistingNotificationEntryChangeInstruction(ne);
			if (existing == null) {
				existing = instruction;
				this.pendingChanges.Add (existing);
			} else {
				switch(type) {
				case NotificationChangeType.Photo:
					existing.PhotoChanged = true;
					break;

				case NotificationChangeType.Theme:
					existing.ColorThemeChanged = true;
					break;
				}
			}
		}
	}
}