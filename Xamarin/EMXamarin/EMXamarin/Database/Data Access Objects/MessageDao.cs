using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace em {
	public class MessageDao {
		
		readonly ApplicationModel appModel;
		readonly DaoConnection daoConnection;

		static readonly string PARAMETER_TEMPLATE = "{0}message_id as messageID, " +
										"{0}chat_entry_id as chatEntryID, " +
										"{0}message_guid as messageGUID, " +
										"{0}inbound as inbound, " +
										"{0}message as message, " +
										"{0}sent_date as sentDateString, " +
										"{0}from_contact_id as fromContactID, " +
										"{0}content_type as contentType, " +
										"{0}media_ref as MediaRefSilent, " +
										"{0}attributes as attributesString, " +
										"{0}status as statusString, " +
										"{0}lifecycle as lifecycleString, " +
										"{0}channel as channelString"; // String used in SQL queries to convert from database columns to object properties.

		static readonly string dbToObj = PARAMETER_TEMPLATE.Replace ("{0}", "");
		static readonly string dbJoins = PARAMETER_TEMPLATE.Replace ("{0}", "m.");

		public MessageDao (ApplicationModel _appModel) {
			appModel = _appModel;
			daoConnection = appModel.daoConnection;
		}

		public void CreateIfNeccessary() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						string queryStr = String.Format ("SELECT name FROM sqlite_master WHERE type='table' AND name='message'");
						List<SQLiteTable> query = db.Query<SQLiteTable> (queryStr);
						if (query.Count == 0)
							CreateTable ();

						// message status is added separately for upgrade purposes for 1.0 beta users.
						queryStr = String.Format ("SELECT name FROM sqlite_master WHERE type='table' AND name='message_status'");
						query = db.Query<SQLiteTable> (queryStr);
						if (query.Count == 0)
							CreateStatusTable ();
					}
					finally {
						db.EndTransaction();
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
						db.Execute("create table message (" +
							"message_id integer primary key autoincrement not null," +
							"chat_entry_id int not null," +
							"message_guid varchar(100)," +
							"inbound char(1) not null default 'Y'," +
							"message varchar(2000)," +
							"sent_date varchar(32)," +
							"from_contact_id int not null," + // comment 'From contact'
							"content_type varchar(100)," + // comment 'Set when theres a media ref attached to this message'
							"media_ref varchar(200)," + // comment 'Optional URI to some attachment media'
							"attributes varchar(2000), " +
							"status char(1) not null default 'S'," +
							"lifecycle char(1) not null default 'A'," +
							"channel char(1) not null default 'A'," +
							"unique (chat_entry_id,message_guid)," +
							"foreign key (chat_entry_id) references chat_entry (chat_entry_id)" +
							"foreign key (from_contact_id) references contact (contact_id)" +
							");"
						);
						db.Execute("create index message_guid_ind on message (message_guid);");
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public void CreateStatusTable () {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						db.Execute(
							"create table message_status (" +
								"message_id integer not null," +
								"contact_id integer not null," +
								"status char(1) not null default 'S'," +
								"unique (message_id, contact_id)," +
								"foreign key (message_id) references message (message_id)," +
								"foreign key (contact_id) references contact (contact_id)" +
							");"
						);
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}


		public Message FindMessageWithMessageGUID(string guid, string fromAlias) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<Message> query;
					if (fromAlias != null)
						query = db.Query<Message> (String.Format("select {0} from message m, chat_entry ce where m.chat_entry_id=ce.chat_entry_id and ce.from_alias=? and message_guid=?", dbJoins), new object[] { fromAlias, guid } );
					else 
						query = db.Query<Message> (String.Format("select {0} from message m, chat_entry ce where m.chat_entry_id=ce.chat_entry_id and ce.from_alias is null and message_guid=?", dbJoins), new object[] { guid } );

					if (query.Count > 0) {
						query [0].appModel = appModel;
						return query [0];
					}

					return null;
				}
			}
		}

		public IList<Message> FindAllMessagesForChatEntry(ChatEntry chatEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<Message> query = db.Query<Message> (String.Format("select {0} from message where chat_entry_id=? and lifecycle <> 'H' order by message_id", dbToObj), new object[] { chatEntry.chatEntryID } );

					SetMessageProperties (query, chatEntry);

					return query;
				}
			}
		}

		public IList<Message> FindRecentMessagesForChatEntry (ChatEntry chatEntry, int messageLimit) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<Message> query = db.Query<Message> (String.Format("select {0} from (select * from message where chat_entry_id=? and lifecycle <> 'H' order by message_id desc limit {1}) order by message_id asc", dbToObj, messageLimit), new object[] { chatEntry.chatEntryID } );

					SetMessageProperties (query, chatEntry);

					// Check if the first message in the list of recent messages we received is read.
					if (query != null && query.Count > 0) {
						Message message = query [0];
						MessageStatus messageStatus = MessageStatusHelper.fromDatabase (message.statusString);

						// If the message is not read, it means previous messages could have also been unread.
						if (messageStatus != MessageStatus.read) {
							// Retrieve previous messages until we hit a message that is read to get all unread messages.
							while (messageStatus != MessageStatus.read) {
								List<Message> previousMessages = FindRecentMessageForChatEntryPreviousToSeedMessage (chatEntry, messageLimit, message);
								if (previousMessages == null || previousMessages.Count == 0) {
									break;
								} else {
									message = previousMessages [0];
									messageStatus = MessageStatusHelper.fromDatabase (message.statusString);

									// Previous messages comes before the latest query so we add the query to previous messages to get a sorted list.
									previousMessages.AddRange (query);
									query = previousMessages;
								}
							}
						}
					}

					return query;
				}
			}
		}

		public List<Message> FindRecentMessageForChatEntryPreviousToSeedMessage (ChatEntry chatEntry, int messageLimit, Message seedMessage) {
			Debug.Assert (seedMessage != null, "Passing in a null Message to function that expects a non null Message.");
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<Message> query = db.Query<Message> (String.Format("select {0} from (select * from message where chat_entry_id=? and lifecycle <> 'H' and message_id < ? order by message_id desc limit {1}) order by message_id asc", dbToObj, messageLimit), new object[] { chatEntry.chatEntryID, seedMessage.messageID } );

					SetMessageProperties (query, chatEntry);

					// The last message in this query will be the prior message to the seed message.
					if (query.Count > 0) {
						Message lastMesssage = query [query.Count - 1];
						seedMessage.showSentDate = seedMessage.ShouldShowSentDateWithPriorMessage (lastMesssage);
					}

					return query;
				}
			}
		}

		public Message FindMostRecentMessageInChatEntry (ChatEntry chatEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<Message> query = db.Query<Message> (String.Format("select {0} from message where chat_entry_id=? and lifecycle <> 'H' order by message_id desc limit 1", dbToObj), new object[] { chatEntry.chatEntryID } );

					SetMessageProperties (query, chatEntry);

					if (query != null && query.Count > 0) {
						return query [0];
					} else {
						return null;
					}
				}
			}
		}

		private void SetMessageProperties (IList<Message> query, ChatEntry chatEntry) {
			Message prior = null;

			foreach (Message message in query) {
				message.showSentDate = message.ShouldShowSentDateWithPriorMessage (prior);
				message.appModel = appModel;
				message.chatEntry = chatEntry;
				message.SetMediaDelegates ();
				prior = message;
			}
		}

		public IList<Message> FindUnreadMessagesForChatEntry(ChatEntry chatEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					string queryString = String.Format("select {0} from message where chat_entry_id=? and inbound='Y' and status <> 'R' and lifecycle in ('A', 'T')", dbToObj);
				
					List<Message> query = db.Query<Message> (queryString, new object[] { chatEntry.chatEntryID });

					foreach (Message message in query) {
						message.chatEntry = chatEntry;
						message.appModel = appModel;
						message.SetMediaDelegates ();
					}

					return query;
				}
			}
		}

		public int GetTotalUnreadCount() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					int count = db.ExecuteScalar<int>("select count(*) from message where status <> 'R' and lifecycle in ('A', 'T') and inbound='Y'");
					return count;
				}
			}
		}

		public void MarkAllReadForChatEntry(ChatEntry chatEntry, IList<Message> unread) {
			MarkUnreadWithStatusForChatEntry (chatEntry, MessageStatus.read, unread);
		}

		public void MarkUnreadWithStatusForChatEntry(ChatEntry chatEntry, MessageStatus status, IList<Message> unread) {
			IList<int> ids = new List<int> ();

			foreach (Message m in unread) {
				ids.Add (m.messageID);
			}
				
			String unreads = "(" + String.Join (",", ids) + ")";

			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					db.Execute (String.Format ("update message set status='" + MessageStatusHelper.toDatabase(status) + "' where chat_entry_id=? and inbound='Y' and status <> 'R' and message_id in {0}", unreads), new object[] { chatEntry.chatEntryID });
				}
			}
		}

		public int InsertMessage(Message message) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					try {
						db.BeginTransaction ();

						db.Execute ("insert into message (chat_entry_id, message_guid, inbound, message, sent_date, from_contact_id, content_type, media_ref, attributes, status, lifecycle, channel) values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);",
							new object[] { message.chatEntryID, message.messageGUID, message.inbound, message.message, message.sentDateString, message.fromContactID, message.contentType, message.mediaRef, message.attributesString, message.statusString, message.lifecycleString, message.channelString });

						int id = db.ExecuteScalar<int>("select last_insert_rowid();");
						message.messageID = id;

						message.isPersisted = true;

						int count = db.ExecuteScalar<int>("select count(*) from message where chat_entry_id=? and lifecycle <> 'H'", new object[] { message.chatEntryID });

						return count - 1;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public void UpdateMessage(Message message) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update message set status=?, lifecycle=?, channel=?, media_ref=?, attributes=? where message_id=?;",
						new object[] { message.statusString, message.lifecycleString, message.channelString, message.mediaRef, message.attributesString, message.messageID });
				}
			}
		}

		public void UpdateMessageStatus(Message message, Contact contact, MessageStatus status) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction ();
						// really just grabbing the status string, but our DB wrapper can't pull
						// a string directly... it needs an object, so we populate a message's status field
						List<Message> query = db.Query<Message>("select status as statusString from message_status where message_id=? and contact_id=?;", new object[] { message.messageID, contact.contactID});
						if ( query.Count == 0 ) {
							db.Execute ("insert into message_status (message_id, contact_id, status) values (?, ?, ?);",
								new object[] { message.messageID, contact.contactID, MessageStatusHelper.toDatabase(status) });
						}
						else {
							MessageStatus currentStatus = query[0].messageStatus;
							if ( MessageStatusHelper.CanTransitionFromStatusToStatus(currentStatus, status))
								db.Execute ("update message_status set status=? where message_id=? and contact_id=?;",
									new object[] { MessageStatusHelper.toDatabase(status), message.messageID, contact.contactID });
						}
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public void UpdateMessageRemote(Message message) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update message set lifecycle=?, media_ref=?, message=?, attributes=? where message_id=? ",
						new object[] { message.lifecycleString, message.mediaRef, message.message, message.attributesString, message.messageID });
				}
			}
		}

		public IList<Message> FindMessagesWithAttachedMedia(ChatEntry chatEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					List<Message> result = db.Query<Message>(string.Format("select {0} from message where chat_entry_id=? and media_ref is not null", dbToObj), new object[] { chatEntry.chatEntryID });
					return result;
				}
			}
		}
	}
}