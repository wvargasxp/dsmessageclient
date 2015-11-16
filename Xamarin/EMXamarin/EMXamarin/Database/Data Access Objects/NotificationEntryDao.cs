using System;
using System.Collections.Generic;

namespace em {
	public class NotificationEntryDao {

		readonly DaoConnection daoConnection;
		readonly ApplicationModel _appModel;

		public DaoConnection DaoConnection {
			get { return daoConnection; }
		}

		const string notification_entry_columns = "ne.notification_entry_id as NotificationEntryID, " +
												  "ne.action_contact_id as ActionID, " +
												  "ne.timestamp as NotificationDate, " +
		                                          "ne.title as Title, " +
		                                          "ne.url as Url, " +
		                                          "ne.read as ReadString";

		public NotificationEntryDao (ApplicationModel appModel) {
			_appModel = appModel;
			daoConnection = appModel.daoConnection;
		}

		public void CreateIfNeccessary() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					string queryStr = String.Format ("SELECT name FROM sqlite_master WHERE type='table' AND name='notification_entry'");
					List<SQLiteTable> query = db.Query<SQLiteTable> (queryStr);
					if (query.Count == 0) {
						CreateTable ();
					}
				}
			}
		}

		public void CreateTable () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("create table notification_entry (" +
						"notification_entry_id integer not null, " + // comment 'The ID for this notification list entry'
						"action_contact_id varchar(32) null, " +
						"timestamp varchar(32) not null, " +
						"title varchar(255) not null, " +
						"url varchar(50) not null, " + 
						"read char(1) not null default 'N', " + 
						"primary key (notification_entry_id) " +
						");"
					);

					//db.Execute ("create index timestamp on notification_entry (timestamp desc)");
				}
			}
		}

		public int GetTotalUnreadCount() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					int unread =  db.ExecuteScalar<int> ("select count(*) from notification_entry where read = ?", new [] { "N" });
					return unread;
				}
			}
		}

		public IList<NotificationEntry> FindAllNotificationEntries() {
			lock ( daoConnection ) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<NotificationEntry> result = db.Query<NotificationEntry>(string.Format("select {0} from notification_entry ne order by notification_entry_id desc", notification_entry_columns));
					foreach (NotificationEntry entry in result)
						entry.counterparty = Contact.FindContactByServerID (_appModel, entry.ActionID);

					return result;
				}
			}
		}

		public IList<NotificationEntry> FindUnreadNotificationEntries() {
			lock ( daoConnection ) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<NotificationEntry> result = db.Query<NotificationEntry>(string.Format("select {0} from notification_entry ne where read = ? order by notification_entry_id desc", notification_entry_columns), new [] { "N" });
					foreach (NotificationEntry entry in result)
						entry.counterparty = Contact.FindContactByServerID (_appModel, entry.ActionID);

					return result;
				}
			}
		}

		public IList<NotificationEntry> FindReadNotificationEntries() {
			lock ( daoConnection ) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<NotificationEntry> result = db.Query<NotificationEntry>(string.Format("select {0} from notification_entry ne where read = ? order by notification_entry_id desc", notification_entry_columns), new [] { "Y" });
					foreach (NotificationEntry entry in result)
						entry.counterparty = Contact.FindContactByServerID (_appModel, entry.ActionID);

					return result;
				}
			}
		}

		public NotificationEntry FindNotificationEntry(int notificationEntryID) {
			lock ( daoConnection ) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<NotificationEntry> result = db.Query<NotificationEntry>(string.Format("select {0} from notification_entry ne where notification_entry_id=?", notification_entry_columns), 
						new object[] { notificationEntryID });

					if (result == null || result.Count < 1)
						return null;

					foreach (NotificationEntry entry in result)
						entry.counterparty = Contact.FindContactByServerID (_appModel, entry.ActionID);

					return result[0];
				}
			}
		}

		public void UpdateNotificationEntry(NotificationEntry entry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					int affected = db.Execute ("update notification_entry set read=? where notification_entry_id=? and read !=?", 
						new object[] { entry.ReadString, entry.NotificationEntryID, entry.ReadString } );

					if (affected > 0 && entry.Read) {
						this._appModel.notificationList.UnreadCountAffected ();
					}

					entry.counterparty = Contact.FindContactByServerID (_appModel, entry.ActionID);
				}
			}
		}

		public void InsertNotificationEntry(NotificationEntry entry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					object[] args = { entry.NotificationEntryID, entry.ActionID, entry.NotificationDateString, entry.Title, entry.Url, entry.ReadString };
					db.Execute("insert into notification_entry (notification_entry_id, action_contact_id, timestamp, title, url, read) values (?, ?, ?, ?, ?, ?);", args);

					if (!entry.Read) {
						this._appModel.notificationList.UnreadCountAffected ();
					}

					entry.isPersisted = true;

					entry.counterparty = Contact.FindContactByServerID (_appModel, entry.ActionID);
				}
			}
		}

		public void RemoveNotificationEntry(NotificationEntry notificationEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					object[] parms = { notificationEntry.NotificationEntryID };

					int affected = db.Execute ("delete from notification_entry where notification_entry_id=?", parms);
					db.Execute ("vacuum");

					if (affected > 0 && !notificationEntry.Read) {
						this._appModel.notificationList.UnreadCountAffected ();
					}
				}
			}
		}
	}
}