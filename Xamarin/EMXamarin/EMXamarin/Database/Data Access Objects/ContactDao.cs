using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace em {
	public class ContactDao {
		readonly DaoConnection daoConnection;

		const string dbToObj = "c.contact_id as contactID, " +
		                 "c.address_book_id as addressBookID, " +
		                 "c.server_id as serverID, " +
						 "c.server_contact_id as serverContactID, " +
						 "c.preferred as preferredString, " +
						 "c.label as label, " +
						 "c.description as description, " +
		                 "c.address_book_first_name as addressBookFirstName, " +
	                     "c.address_book_last_name as addressBookLastName, " +
	                     "c.display_name as displayName, " +
						 "c.from_alias as fromAlias, " +
						 "c.is_group as isGroup, " +
						 "c.attributes as attributesString, " +
						 "c.lifecycle as lifecycleStringSilent, " +
						 "c.address_book_lifecycle as AddressBookLifeCycleString, " +
						 "c.group_member_lifecycle as GroupMemberLifeCycleString, " +
						 "c.group_status as GroupStatusString, " +
						 "c.block_status as BlockStatusString, " +
						 "c.identifier_type as identifierTypeString, " +
						 "c.phone_number_type as phoneNumberTypeString, " +
						 "c.me as meString, " +
						 "c.temp_contact as tempContactString, " +
						 "c.last_used_identifier_to_send_from as LastUsedIdentifierToSendFrom, " + 
						 "c.thumbnail_url as thumbnailURLSilent"; // String used in SQL queries to convert from database columns to object properties.

		public ContactDao (ApplicationModel appModel) {
			daoConnection = appModel.daoConnection;
		}

		public void CreateIfNeccessary() {
			lock ( daoConnection ) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					string queryStr = String.Format ("SELECT name FROM sqlite_master WHERE type='table' AND name='contact'");
					List<SQLiteTable> query = db.Query<SQLiteTable> (queryStr);
					if (query.Count == 0)
						CreateTable ();

				    // TODO After launch this can be rolled into the create table
					try {
						int count = db.ExecuteScalar<int>("select count(*) from contact where group_status is not null and block_status = ''");
						// if this succeeeds we don't need to upgrade
					}
					catch (Exception e) {
						db.Execute ("alter table contact add group_status char(1)"); //, block_status char(1) not null default 'N') after lifecycle");
						db.Execute ("alter table contact add block_status char(1) not null default 'N'");
					}

					// TODO After launch this can be rolled into the create table
					try {
						int count = db.ExecuteScalar<int>("select count(*) from contact where phone_number_type is not null");
						// if this succeeeds we don't need to upgrade
					}
					catch (Exception e) {
						db.Execute ("alter table contact add phone_number_type char(1) not null default 'A'");
					}

					// TODO After launch this can be rolled into the create table
					try {
						int count = db.ExecuteScalar<int>("select count(*) from contact where address_book_lifecycle is not null");
						// if this succeeeds we don't need to upgrade
					} catch (Exception e) {
						db.Execute ("alter table contact add address_book_lifecycle char(1) default 'A'");
					}

					// TODO After launch this can be rolled into the create table
					try {
						int count = db.ExecuteScalar<int>("select count(*) from contact where group_member_lifecycle is not null");
						// if this succeeeds we don't need to upgrade
					} catch (Exception e) {
						db.Execute ("alter table contact add group_member_lifecycle char(1) default 'A'");
					}

					// TODO
					try {
						int count = db.ExecuteScalar<int>("select count(*) from contact where last_used_identifier_to_send_from is not null");
					} catch (Exception e) {
						db.Execute ("alter table contact add last_used_identifier_to_send_from varchar(100)");
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
						db.Execute("create table contact (" +
							"contact_id integer not null, " + // integer instead of int for autoincrement
							"address_book_id varchar(100), " +
							"server_id varchar(100), " +
							"server_contact_id integer, " +
							"preferred char(1) default 'N', " +
							"label varchar(50), " +
							"description varchar(100), " +
							"address_book_first_name varchar(100), " +
							"address_book_last_name varchar(100), " +
							"display_name varchar(100), " +
							"from_alias varchar(100), " +
							"is_group char(1) default 'N', " +
							"attributes varchar(2000), " +
							"lifecycle char(1) default 'A', " +
							"identifier_type char(1) default 'U', " +
							"me char(1) default 'N', " +
							"temp_contact char(1) default 'N', " +
							"thumbnail_url varchar(200), " +
							"primary key (contact_id)" +
							");"
						);

						db.Execute("create index contact_address_book_id_ind on contact (address_book_id);");
						db.Execute("create index contact_server_id_ind on contact (server_id);");
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}
			
		public int InsertContact (Contact contact) {
			object [] args = {contact.addressBookID, contact.serverID, contact.serverContactID, contact.preferredString, contact.label, contact.description, contact.addressBookFirstName, contact.addressBookLastName, contact.displayName, contact.fromAlias, contact.isGroup, contact.attributesString, contact.lifecycleString, contact.AddressBookLifeCycleString, contact.GroupMemberLifeCycleString, contact.GroupStatusString, contact.BlockStatusString, contact.identifierTypeString, contact.phoneNumberTypeString, contact.meString, contact.tempContactString, contact.thumbnailURL, contact.LastUsedIdentifierToSendFrom};
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					try {
						db.BeginTransaction ();
						db.Execute ("insert into contact (address_book_id, server_id, server_contact_id, preferred, label, description, address_book_first_name, address_book_last_name, display_name, from_alias, is_group, attributes, lifecycle, address_book_lifecycle, group_member_lifecycle, group_status, block_status, identifier_type, phone_number_type, me, temp_contact, thumbnail_url, last_used_identifier_to_send_from) " +
							"values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);", args);

						int id = db.ExecuteScalar<int>("select last_insert_rowid();");
						contact.contactID = id;

						contact.isPersisted = true;
						return id; // Returns the id of the the object being inserted.
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public void UpdateContact (Contact contact) {
			object [] args = {contact.addressBookID, contact.serverID, contact.serverContactID, contact.preferredString, contact.label, contact.description, contact.addressBookFirstName, contact.addressBookLastName, contact.displayName, contact.fromAlias, contact.isGroup, contact.attributesString, contact.lifecycleString, contact.AddressBookLifeCycleString, contact.GroupMemberLifeCycleString, contact.GroupStatusString, contact.BlockStatusString, contact.identifierTypeString, contact.phoneNumberTypeString, contact.meString, contact.tempContactString, contact.thumbnailURL, contact.LastUsedIdentifierToSendFrom, contact.contactID};
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update contact set address_book_id=?, server_id=?, server_contact_id=?, preferred=?, label=?, description=?, address_book_first_name=?, address_book_last_name=?, display_name=?, from_alias=?, is_group=?, attributes=?, lifecycle=?, address_book_lifecycle=?, group_member_lifecycle=?, group_status=?, block_status=?, identifier_type=?, phone_number_type=?, me=?, temp_contact=?, thumbnail_url=?, last_used_identifier_to_send_from=? where contact_id=?", args);
				}
			}
		}

		public void DeleteContactWithContactId (Contact contact) {
			object[] args = { contact.contactID };
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("delete from contact where contact_id=?", args);
				}
			}
		}

		public void MarkContactTemp(Contact contact) {
			object [] args = {contact.contactID};
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					db.Execute ("update contact set temp_contact='Y' where contact_id=?", args);
				}
			}
		}

		#region Querying for a Contact
		public Contact ContactWithAddressBookID(string addressBookId, string description) {
			return ContactFromQuery (String.Format("select {0} from contact c where c.address_book_id = ? and c.description=?", dbToObj), new object[] { addressBookId, description });
		}

		public Contact ContactWithServerID(string serverId) {
			return ContactFromQuery (String.Format("select {0} from contact c where c.server_id = ?", dbToObj), new object[] { serverId });
		}

		public Contact ContactWithContactID(int contactId) {
			return ContactFromQuery (String.Format("select {0} from contact c where c.contact_id = ?", dbToObj), new object[] { contactId });
		}

		public IList<Contact> FindContactsWithChatEntryID(int chatEntryId) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					IList<Contact> t = db.Query<Contact> (String.Format("select {0} from contact c, chat_entry_contacts cec where c.contact_id=cec.contact_id and cec.chat_entry_id=?", dbToObj), chatEntryId);
					return t;
				}
			}
		}

		public IList<Group> FindAllGroups() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					IList<Group> t = db.Query<Group> (String.Format ("select {0} from contact c where c.is_group='Y' and c.temp_contact='N'", dbToObj));
					return t;
				}
			}
		}

		public Group FindGroupByServerID(string serverId) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					List<Group> g = db.Query<Group> (String.Format ("select {0} from contact c where c.server_id = ? and c.is_group='Y'", dbToObj), new object[] { serverId });
					return g.Count == 0 ? null : g [0];
				}
			}
		}

		public IList<Contact> FindAllContactsWithServerIDs() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					IList<Contact> t = db.Query<Contact> (String.Format("select {0} from contact c where server_id is not null and me='N'", dbToObj));
					return t;
				}
			}
		}

		public IList<Contact> FindAllPermanentContacts() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					IList<Contact> t = db.Query<Contact> (String.Format("select {0} from contact c where temp_contact='N'", dbToObj));
					return t;
				}
			}
		}

		public IList<Contact> RemoveUnusedTemporaryContacts() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					try {
						db.BeginTransaction ();

						IList<Contact> t = db.Query<Contact> (String.Format("select {0} from contact c where temp_contact='Y' and contact_id not in (select distinct(contact_id) from chat_entry_contacts) and contact_id not in (select distinct(from_contact_id) from message)", dbToObj));
						if (t.Count > 0) {
							//foreach (Contact c in t)
							//	Debug.WriteLine (" ************************* About to delete " + c.displayName + " " + c.contactID);

							db.Execute ("delete from contact where temp_contact='Y' and contact_id not in (select distinct(contact_id) from chat_entry_contacts) and contact_id not in (select distinct(from_contact_id) from message)");
						}


						return t;
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		Contact ContactFromQuery(string query, params object[] parms) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					List<Contact> t = db.Query<Contact> (query, parms);
					return t.Count == 0 ? null : t [0];
				}
			}
		}

		#endregion
	}
}