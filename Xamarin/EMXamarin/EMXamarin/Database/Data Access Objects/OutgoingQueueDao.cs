using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace em {
	public class OutgoingQueueDao {
		DaoConnection daoConnection;
		public ApplicationModel ApplicationModel { get; set; }

		private string outgoing_queue_columns = "message_id as messageID, " +
											"method_type as methodTypeString, " +
		                                    "route as routeString, " +
		                                    "send_date as sendDateString, " +
		                                    "destination, " +
											"retry as retryCount, " +
		                                    "entry_state as entryStateString";

		private string outgoing_queue_contents_columns = "local_path as localPath, " +
		                                                 "delete_on_removal as deleteOnRemovalString, " +
		                                                 "mime_type as mimeType, " +
		                                                 "name, " +
														 "file_name as fileName, " +
														 "local_id as localID";

		public OutgoingQueueDao (ApplicationModel appModel) {
			daoConnection = appModel.outgoingQueueDaoConnection;
			this.ApplicationModel = appModel;
		}

		public void CreateIfNeccessary() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					string queryStr = String.Format ("SELECT name FROM sqlite_master WHERE type='table' AND name='outgoing_queue'");
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
					try {
						db.BeginTransaction();
						db.Execute ("create table outgoing_queue (" +
							"message_id integer primary key autoincrement not null, " + // The ID for this queue entry
							"method_type char(1) not null default 'N', " + // G for Get, R for Post, M for MultiPart, P for Put, D for Delete, N for Not Applicable
							"route char(1) not null default 'W', " + // W for websocket, R for HTTP RESTian
							"send_date varchar(32)," + // The date this message was added to the queue
							"destination varchar(100) not null, " + // the destination (stomp path or URI)
							"retry integer not null default 0, " + // count of how many times this message has been retried
							"entry_state char(1) default 'P'" + // Whether this entry is pending or in the process of being sent
							");"
						);

						db.Execute ("create index outgoing_queue_send_order on outgoing_queue (send_date)");

						db.Execute ("create table outgoing_queue_contents (" +
							"message_id integer not null, " + // comment 'The message this entry applies to'
							"local_path varchar(200) not null, " + // where the contents are stored on the file system
							"delete_on_removal char(1) not null default 'Y', " + // whether or not to delete this file once message is removed
							"mime_type varchar(64) not null, " + // application/json, image/jpeg, etc
							"name varchar(64) not null, " + // The attachment field name
							"file_name varchar(64) not null, " + // The files local name
							"local_id varchar(100), " + // the local ID if needed for updating status
							"FOREIGN KEY (message_id) REFERENCES outgoing_queue (message_id)" +
							");"
						);
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public void InsertQueueEntry(QueueEntry queueEntry) {
			object [] args = { queueEntry.methodTypeString, queueEntry.routeString, queueEntry.sendDateString, queueEntry.destination, queueEntry.retryCount, queueEntry.entryStateString };
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					try {
						db.BeginTransaction();
						db.Execute ("insert into outgoing_queue (method_type, route, send_date, destination, retry, entry_state) " +
							"values (?, ?, ?, ?, ?, ?);", args);

						int id = db.ExecuteScalar<int>("select last_insert_rowid();");
						queueEntry.messageID = id;

						foreach (QueueEntryContents contents in queueEntry.contents) {
							object[] contentsArgs = { queueEntry.messageID, contents.localPath, contents.deleteOnRemovalString, contents.mimeType, contents.name, contents.fileName, contents.localID };
							db.Execute ("insert into outgoing_queue_contents (message_id, local_path, delete_on_removal, mime_type, name, file_name, local_id) " +
								"values (?, ?, ?, ?, ?, ?, ?);", contentsArgs);
						}
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public void DeleteQueueEntry(QueueEntry queueEntry) {
			object [] args = { queueEntry.messageID };
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						db.Execute ("delete from outgoing_queue_contents where message_id=?", args);
						db.Execute ("delete from outgoing_queue where message_id=?", args);
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		/*
		 * Useful on app relaunch, when no possible request can be pending.  If we crashed
		 * or otherwise messages remain marked as (S)ending we clear them hear so that they
		 * can be retried.
		 */
		public void MarkAllQueueEntriesPending() {
			MarkAllWebsocketQueueEntriesAsPending ();
			MarkAllUploadsAsUploadPending ();
			MarkAllVideoEncodingsAsPending ();
		}

		public void MarkAllWebsocketQueueEntriesAsPending () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set entry_state='P' where entry_state <> 'P' and route = 'W'");
				}
			}
		}

		public void MarkReuploadingRestQueueEntriesAsUploadPending () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set entry_state='U' where entry_state = 'R' and route = 'R'");
				}
			}
		}

		public void MarkAllUploadsAsUploadPending () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set entry_state='U' where entry_state <> 'U' and entry_state <> 'D' and route = 'R'");
				}
			}
		}

		public void MarkAllVideoEncodingsAsPending () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set entry_state='P' where entry_state = 'E' and route = 'V'");
				}
			}
		}

		public void MarkAsSending(QueueEntry queueEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set entry_state='S' where message_id=?", new object[] { queueEntry.messageID });
				}
			}
		}

		public void UpdateRouteAndStatus (QueueEntry queueEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set route=?, entry_state=? where message_id=?", new object[] {
						queueEntry.routeString,
						queueEntry.entryStateString,
						queueEntry.messageID
					});
				}
			}
		}

		public void UpdateRoute (QueueEntry queueEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set route=? where message_id=?", new object[] { queueEntry.routeString, queueEntry.messageID });
				}
			}
		}

		public void UpdateStatus(QueueEntry queueEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set entry_state=? where message_id=?", new object[] { queueEntry.entryStateString, queueEntry.messageID });
				}
			}
		}

		public void UpdateStatusAndRetryCount(QueueEntry queueEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update outgoing_queue set entry_state=?, retry=? where message_id=?", new object[] { queueEntry.entryStateString, queueEntry.retryCount, queueEntry.messageID });
				}
			}
		}

		public IList<QueueEntry> FindPendingQueueEntries() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where entry_state='P' order by send_date", outgoing_queue_columns));

						foreach (QueueEntry queueEntry in t) {
							object[] args = { queueEntry.messageID };
							IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
							queueEntry.contents = c;
						}
						return t;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public IList<QueueEntry> FindUploadPendingQueueEntries() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where route='R' and entry_state='U' order by send_date", outgoing_queue_columns));

						foreach (QueueEntry queueEntry in t) {
							object[] args = { queueEntry.messageID };
							IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
							queueEntry.contents = c;
						}
						return t;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}
			

		public IList<QueueEntry> FindRetryingQueueEntries() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where entry_state='R' order by send_date", outgoing_queue_columns));

						foreach (QueueEntry queueEntry in t) {
							object[] args = { queueEntry.messageID };
							IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
							queueEntry.contents = c;
						}
						return t;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public IList<QueueEntry> FindPendingVideoEncodingQueueEntries () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where route='V' and entry_state='P' order by send_date", outgoing_queue_columns));
						foreach (QueueEntry entry in t) {
							object[] args = { entry.messageID };
							IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
							entry.contents = c;
						}

						return t;
					} finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public QueueEntry FindOldestPendingVideoEncodingQueueEntry () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where route='V' order by send_date limit 1", outgoing_queue_columns));

						if (t.Count == 0) return null;

						QueueEntry queueEntry = t[0];
						object[] args = { queueEntry.messageID };

						IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
						queueEntry.contents = c;

						return queueEntry;
					} finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public IList<QueueEntry> FindPendingDeletions() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where entry_state='D' order by send_date", outgoing_queue_columns));

						foreach (QueueEntry queueEntry in t) {
							object[] args = { queueEntry.messageID };
							IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
							queueEntry.contents = c;
						}
						return t;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public QueueEntry FindOldestPendingNonEncodingQueueEntry () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where entry_state='P' and route <> 'V' order by send_date limit 1", outgoing_queue_columns));

						if (t.Count == 0) return null;

						QueueEntry queueEntry = t[0];
						object[] args = { queueEntry.messageID };

						IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
						queueEntry.contents = c;

						return queueEntry;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public QueueEntry FindOldestPendingQueueEntry () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where entry_state='P' order by send_date limit 1", outgoing_queue_columns));

						if (t.Count == 0) return null;

						QueueEntry queueEntry = t[0];
						object[] args = { queueEntry.messageID };
						
						IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
						queueEntry.contents = c;

						return queueEntry;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public QueueEntry FindQueueEntry(int messageID) {
			lock (daoConnection) {
				using (daoConnection) {
					object[] args = { messageID };
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						IList<QueueEntry> t = db.Query<QueueEntry> (String.Format("select {0} from outgoing_queue where message_id=?", outgoing_queue_columns), args);

						foreach (QueueEntry queueEntry in t) {
							IList<QueueEntryContents> c = db.Query<QueueEntryContents> (String.Format("select {0} from outgoing_queue_contents where message_id=?", outgoing_queue_contents_columns), args);
							queueEntry.contents = c;
						}
						return t.Count > 0 ? t[0] : null;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}
	}
}