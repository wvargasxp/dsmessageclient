using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;
using System.Diagnostics;

namespace em {
	public class NotificationList {
		
		public ApplicationModel appModel;

		object entries_mutex = new object ();
		IList<NotificationEntry> _Entries;
		public IList<NotificationEntry> Entries {
			get {
				lock (entries_mutex) {
					return _Entries;
				}
			}

			set {
				lock (entries_mutex) {
					_Entries = value;
				}
			}
		}

		public void BackgroundInitEntries() {
			lock ( appModel.daoConnection ) {
				Entries = appModel.notificationEntryDao.FindAllNotificationEntries ();

				InitializeEntries ();
			}
		}

		public bool PerformingUpdates { get { return _entriesSnapShot != null; } }
		IList<NotificationEntry> _entriesSnapShot = null;
		public IList<NotificationEntry> UIEntries {
			get {
				if (_entriesSnapShot != null)
					return _entriesSnapShot;

				return Entries;
			}
		}

		public NotificationList (ApplicationModel _appModel) {
			appModel = _appModel;
			Entries = new List<NotificationEntry> ();

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

		#region unread count for notificiations - ported from chatlist
		private bool ignoringUnreadcountChanges = false;
		private bool shouldScheduleUnreadCountChangeAfterBulkUpdates = false;
		private Int64 timerSequence = 0;
		private Int64 TimerSequence { get { return this.timerSequence; } set { this.timerSequence = value; } }

		const int UNSET_UNREAD_COUNT_NUMBER = -1;
		int unreadCount = UNSET_UNREAD_COUNT_NUMBER;
		public int UnreadCount {
			get { 
				if (unreadCount == UNSET_UNREAD_COUNT_NUMBER)
					unreadCount = appModel.notificationEntryDao.GetTotalUnreadCount ();
				return unreadCount;
			}

			set {
				SetUnreadCount (value);
			}
		}

		protected void SetUnreadCount (int value) {
			if (this.unreadCount != value) {
				this.unreadCount = value;
				NotificationCenter.DefaultCenter.PostNotification (null, Constants.NotificationEntryDao_UnreadCountChanged, this.unreadCount);
			}
		}

		public void BackgroundUpdateUnreadCount () {
			int totalUnread = appModel.notificationEntryDao.GetTotalUnreadCount(); // might be slow
			SetUnreadCount (totalUnread);
		}

		public void ObtainUnreadCountAsync (Action<int> callback) {
			EMTask.Dispatch (() => {
				int unread = this.UnreadCount;
				EMTask.DispatchMain (() => {
					callback (unread);
				});
			});
		}

		public void UnreadCountAffected () {
			if (this.ignoringUnreadcountChanges) {
				this.shouldScheduleUnreadCountChangeAfterBulkUpdates = true;
				return;
			}

			ScheduleUnreadCountChangeTimer ();
		}

		private readonly int DISPLAY_UNREAD_COUNT_TIMER = 300; // .3 second in milliseconds
		protected void ScheduleUnreadCountChangeTimer () {
			Int64 seq = ++this.TimerSequence;
			WeakReference thisRef = new WeakReference (this);
			new Timer ((object o) => {
				Debug.Assert (Thread.CurrentThread.ManagedThreadId != 1, "On main thread when assumed to be on background thread.");
				Int64 state = (Int64)o;
				if (state == this.TimerSequence) {
					NotificationList self = thisRef.Target as NotificationList;
					if (self != null) {
						self.BackgroundUpdateUnreadCount ();
					}
				}
			}, seq, DISPLAY_UNREAD_COUNT_TIMER, Timeout.Infinite);
		}
		#endregion

		public void ShowAllNotifications() {
			CleanupEntries ();

			Entries = null;

			Entries = appModel.notificationEntryDao.FindAllNotificationEntries ();

			InitializeEntries ();
		}

		public void ShowUnreadNotifications() {
			CleanupEntries ();

			Entries = null;

			Entries = appModel.notificationEntryDao.FindUnreadNotificationEntries ();

			InitializeEntries ();
		}

		public void ShowReadNotifications() {
			CleanupEntries ();

			Entries = null;

			Entries = appModel.notificationEntryDao.FindReadNotificationEntries ();

			InitializeEntries ();
		}

		void CleanupEntries() {
			foreach (NotificationEntry notifEntry in Entries) {
				if (notifEntry != null) {
					notifEntry.appModel = null;
					notifEntry.NotificationList = null;
					notifEntry.RemoveDelegates ();
				}
			}
		}

		void InitializeEntries() {
			foreach (NotificationEntry notifEntry in Entries) {
				if (notifEntry != null) {
					notifEntry.appModel = appModel;
					notifEntry.NotificationList = this;
					notifEntry.AddDelegates ();
				}
			}
		}

		public delegate void DidAddNotificationEntryAt(NotificationEntry ce, int addedPosition);
		public delegate void DidRemoveNotificationEntryAt(NotificationEntry ce, int removePosition);
		public delegate void DidMoveNotificationEntryFrom(NotificationEntry ce, int[] positions);
		public delegate void WillStartBulkUpdates ();
		public delegate void DidFinishBulkUpdates (IList<NotificationEntry> old);
		public delegate void DidDownloadNotificationEntryContactThumbnail(NotificationEntry notificationEntry);
		public delegate void DidChangeNotificationEntryContactThumbnailSource(NotificationEntry notificationEntry);
		public delegate void DidChangeNotificationEntryColorTheme(NotificationEntry notificationEntry);

		public DidAddNotificationEntryAt DelegateDidAdd = delegate(NotificationEntry ce, int addedPosition) { };
		public DidRemoveNotificationEntryAt DelegateDidRemove = delegate(NotificationEntry ce, int removePosition) { };
		public DidMoveNotificationEntryFrom DelegateDidMove = delegate(NotificationEntry ce, int[] positions) { };
		public WillStartBulkUpdates DelegateWillStartBulkUpdates = delegate() { };
		public DidFinishBulkUpdates DelegateDidFinishBulkUpdates = delegate(IList<NotificationEntry> old) { };
		public DidDownloadNotificationEntryContactThumbnail DelegateDidDownloadNotificationEntryContactThumbnail = delegate(NotificationEntry notificationEntry) { };
		public DidChangeNotificationEntryContactThumbnailSource DelegateDidChangeNotificationEntryContactThumbnailSource = delegate(NotificationEntry notificationEntry) { };
		public DidChangeNotificationEntryColorTheme DelegateDidChangeNotificationEntryColorTheme = delegate(NotificationEntry notificationEntry) { };

		public NotificationEntry FindNotificationEntry(int notificationEntryID) {
			lock (appModel.daoConnection) {
				if (Entries != null) {
					foreach (NotificationEntry entry in Entries) {
						if (entry.NotificationEntryID == notificationEntryID) {
							return entry;
						}
					}
				}
			}

			return null;
		}

		public void SaveNotificationEntry(NotificationEntry entry) {
			NotificationEntry existing = FindNotificationEntry(entry.NotificationEntryID);
			if (existing == null) {
				Entries.Insert (0, entry);

				entry.AddDelegates ();

				EMTask.DispatchMain (() => DelegateDidAdd (entry, 0));
			}
		}

		public void MarkNotificationEntryReadAsync(NotificationEntry entry) {
			if(entry.Read)
				return;
			
			EMTask.DispatchBackground (() => {
				lock (appModel.daoConnection) {
					entry.Read = true;
					appModel.notificationEntryDao.UpdateNotificationEntry (entry);

					appModel.liveServerConnection.SendNotificationStatusUpdateAsync (entry.NotificationEntryID, NotificationStatus.Read);
				}
			});
		}

		public void MarkNotificationEntryActionInitiatedAsync(NotificationEntry entry) {
			EMTask.DispatchBackground (() => {
				lock (appModel.daoConnection) {
					appModel.liveServerConnection.SendNotificationStatusUpdateAsync (entry.NotificationEntryID, NotificationStatus.ActionInitiated);
				}
			});
		}

		public void RemoveNotificationEntryAtAsync(int position) {
			if (position >= 0) {
				EMTask.DispatchBackground (() => {
					lock (appModel.daoConnection) {
						NotificationEntry toRemove = Entries[position];

						if(toRemove != null) {
							Entries.RemoveAt(position);
							appModel.notificationEntryDao.RemoveNotificationEntry(toRemove);

							appModel.liveServerConnection.SendNotificationStatusUpdateAsync (toRemove.NotificationEntryID, NotificationStatus.Deleted);

							toRemove.RemoveDelegates();

							EMTask.DispatchMain (() => {
								DelegateDidRemove (toRemove, position);
							});
						}
					}
				});
			} else
				Debug.WriteLine ("Trying to remove a notification at false position: " + position);
		}

		public void RemoveNotificationEntryAtAsync(NotificationEntry toRemove) {
			if (toRemove != null) {
				int position = Entries.IndexOf(toRemove);
				RemoveNotificationEntryAtAsync (position);
			}
		}

		//don't need to set notification list or app model because notification entry is either already in entries or was created anew and passed in an appmodel; 
		//the thing that calls this requires that it already has an appmodel
		public void InsertOrMoveToTop(NotificationEntry notificationEntry) {
			lock (appModel.daoConnection) {
				int indexOf = Entries.IndexOf (notificationEntry);
				if (indexOf != -1)
					Entries.Remove (notificationEntry);
				Entries.Insert (0, notificationEntry);

				if (notificationEntry.isPersisted)
					appModel.notificationEntryDao.UpdateNotificationEntry (notificationEntry);
				else
					appModel.notificationEntryDao.InsertNotificationEntry (notificationEntry);

				notificationEntry.AddDelegates ();

				if (indexOf == -1)
					EMTask.DispatchMain (() => DelegateDidAdd (notificationEntry, 0));
				else if ( indexOf != 0 ) // only send out a move notice if the item actually moved!
					EMTask.DispatchMain (() => DelegateDidMove (notificationEntry, new [] {indexOf, 0}));
			}
		}

		public void BackgroundStartingBulkUpdates() {
			EMTask.DispatchMain (() => {
				System.Diagnostics.Debug.Assert(_entriesSnapShot == null);

				this.ignoringUnreadcountChanges = true;

				if(_entriesSnapShot == null) {
					_entriesSnapShot = new List<NotificationEntry>( Entries );

					DelegateWillStartBulkUpdates ();
				}
			});
		}

		public void BackgroundStoppingBulkUpdates() {
			EMTask.DispatchMain (() => {
				System.Diagnostics.Debug.Assert(_entriesSnapShot != null);

				if (this.shouldScheduleUnreadCountChangeAfterBulkUpdates) {
					ScheduleUnreadCountChangeTimer ();
					this.shouldScheduleUnreadCountChangeAfterBulkUpdates = false;
				}

				if(_entriesSnapShot != null) {
					//IList<NotificationEntry> old = new List<NotificationEntry>(_entriesSnapShot);
					IList<NotificationEntry> old = new List<NotificationEntry>();
					if(_entriesSnapShot != null && _entriesSnapShot.Count > 0) {
						foreach(NotificationEntry ne in _entriesSnapShot)
							old.Add(ne);
					}

					_entriesSnapShot = null;

					DelegateDidFinishBulkUpdates(old);
				}
			});
		}
	}
}