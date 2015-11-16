using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace em {
	public class ChatEntryDao {
		DaoConnection daoConnection;
		ApplicationModel appModel;

		private string chat_entry_columns = "ce.chat_entry_id as chatEntryID, " +
		                                  "ce.entry_order as entryOrder, " +
		                                  "ce.has_unread as hasUnreadString, " +
		                                  "ce.create_date as createDateString, " +
		                                  "ce.preview as preview, " +
										  "ce.preview_date as previewDateString, " +
		                                  "ce.under_construction as underConstruction, " +
		                                  "ce.under_construction_media_path as underConstructionMediaPath, " +
		                                  "ce.from_alias as fromAliasSilent, " +
										  "ce.left_adhoc as leftAdhocString";

		public ChatEntryDao (ApplicationModel appModel) {
			this.appModel = appModel;
			daoConnection = appModel.daoConnection;
		}

		public void CreateIfNeccessary() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					string queryStr = String.Format ("SELECT name FROM sqlite_master WHERE type='table' AND name='chat_entry'");
					List<SQLiteTable> query = db.Query<SQLiteTable> (queryStr);
					if (query.Count == 0) {
						CreateTable ();
					}

					// TODO After launch this can be rolled into the create table
					try {
						int count = db.ExecuteScalar<int>("select count(*) from chat_entry where left_adhoc='Y';");
						// if this succeeeds we don't need to upgrade
					}
					catch (Exception e) {
						db.Execute ("alter table chat_entry add left_adhoc char(1) default 'N';");
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
						db.Execute ("create table chat_entry (" +
							"chat_entry_id integer not null, " + // comment 'The ID for this chat list entry'
							"entry_order int not null default 0, " + //comment 'The order (largest first) of this chat entry'
							"has_unread char(1) not null default 'N', " + // if this chat entry has any unread
							"create_date varchar(32), " + // The sent date of the message we created this entry in response to
							"preview varchar(100), " +
							"preview_date varchar(32), " +
							"under_construction varchar(2000), " +
							"under_construction_media_path varchar(200), " +
							"from_alias varchar(100), " +
							"primary key (chat_entry_id)" +
							");"
						);

						db.Execute ("create index chat_entry_order on chat_entry (entry_order desc);");

						db.Execute ("CREATE TABLE chat_entry_contacts (" +
							"chat_entry_id int NOT NULL," +
							"contact_id int NOT NULL," +
							"FOREIGN KEY (chat_entry_id) REFERENCES chat_entry (chat_entry_id)," +
							"FOREIGN KEY (contact_id) REFERENCES contact (contact_id)" +
							");"
						);

						// only allow a contact to appear once in a chat entry's contacts
						db.Execute ("CREATE UNIQUE INDEX unique_per_ce_ind ON chat_entry_contacts (chat_entry_id, contact_id);");
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}
			
		public IList<ChatEntry> FindAllChatEntries() {
			lock ( daoConnection ) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<ChatEntry> result = db.Query<ChatEntry>(string.Format("select {0} from chat_entry ce where entry_order > 0 order by entry_order desc", chat_entry_columns));
					foreach (ChatEntry chatEntry in result) {
						IList<Contact> contacts = Contact.FindAllContactsForChatEntry (appModel, chatEntry.chatEntryID);
						chatEntry.contacts = contacts;
					}

					return result;
				}
			}
		}

		public ChatEntry FindUnderConstructionChatEntry() {
			lock ( daoConnection ) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<ChatEntry> result = db.Query<ChatEntry>(string.Format("select {0} from chat_entry ce where entry_order = -1", chat_entry_columns));
					if (result == null || result.Count == 0)
						return null;

					return result [0];
				}
			}
		}

		public void UpdateChatEntry(ChatEntry chatEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					try {
						db.BeginTransaction ();
						db.Execute ("update chat_entry set preview=?, preview_date=?, has_unread=?, under_construction=?, under_construction_media_path=?, from_alias=?, entry_order=?, left_adhoc=? where chat_entry_id=?",
							new object[] { chatEntry.preview, chatEntry.previewDateString, chatEntry.hasUnreadString, chatEntry.underConstruction, chatEntry.underConstructionMediaPath, chatEntry.fromAlias, chatEntry.entryOrder, chatEntry.leftAdhocString, chatEntry.chatEntryID});

						// because a chat entry is now saved even when under construction
						// we may have to add the contacts in.  The 'insert or ignore' syntax
						// basically lets not error if it's already been added.
						foreach ( Contact contact in chatEntry.contacts ) {
							object[] args = new object[] { chatEntry.chatEntryID, contact.contactID };
							db.Execute ("insert or ignore into chat_entry_contacts (chat_entry_id, contact_id) values (?, ?);", args);
						}
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public void InsertChatEntry(ChatEntry chatEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					try {
						db.BeginTransaction ();

						object[] args = new object[] { chatEntry.entryOrder, chatEntry.createDateString, chatEntry.preview, chatEntry.previewDateString, chatEntry.hasUnreadString, chatEntry.underConstruction, chatEntry.underConstructionMediaPath, chatEntry.fromAlias, chatEntry.leftAdhocString };
						db.Execute("insert into chat_entry (entry_order, create_date, preview, preview_date, has_unread, under_construction, under_construction_media_path, from_alias, left_adhoc) values (?, ?, ?, ?, ?, ?, ?, ?, ?);", args);

						int id = db.ExecuteScalar<int>("select last_insert_rowid();");
						chatEntry.chatEntryID = id;

						foreach ( Contact contact in chatEntry.contacts ) {
							args = new object[] { chatEntry.chatEntryID, contact.contactID };
							db.Execute ("insert into chat_entry_contacts (chat_entry_id, contact_id) values (?, ?);", args);
						}
					}
					finally {
						db.EndTransaction ();
					}

					chatEntry.isPersisted = true;
				}
			}
		}

		public IList<Message> RemoveChatEntry(ChatEntry chatEntry) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					object[] parms = { chatEntry.chatEntryID };

					//commented out because result wasn't being used anywhere
					//IList<Message> result = appModel.msgDao.FindMessagesWithAttachedMedia (chatEntry);

					try {
						appModel.platformFactory.GetFileSystemManager ().RemoveFilesForChatEntry(chatEntry);
					}
					catch (Exception e) {
						Debug.WriteLine ("Exception removing chat entry (RemoveFilesForChatEntry): " + e);
					}

					if (chatEntry.underConstructionMediaPath != null) {
						try {
							appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (chatEntry.underConstructionMediaPath); 
						}
						catch (Exception e) {
							Debug.WriteLine ("Exception removing chat entry (RemoveFileAtPath): " + e);
						}
					}

					IList<Message> guids = appModel.msgDao.FindAllMessagesForChatEntry (chatEntry);

					try {
						db.BeginTransaction ();
						db.Execute ("delete from message_status where message_id in (select message_id from message where chat_entry_id=?)", parms);
						db.Execute ("delete from message where chat_entry_id=?", parms);
						db.Execute ("delete from chat_entry_contacts where chat_entry_id=?", parms);
						db.Execute ("delete from chat_entry where chat_entry_id=?", parms);
					}
					finally {
						db.EndTransaction ();
						db.Execute ("vacuum");
					}

					return guids;
				}
			}
		}

		public void RemoveContactFromChatEntry(ChatEntry chatEntry, Contact contact) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					object[] parms = { chatEntry.chatEntryID, contact.contactID };
					db.Execute("delete from chat_entry_contacts where chat_entry_id=? and contact_id=?", parms );
				}
			}
		}
	}
}

