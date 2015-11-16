using System;
using System.Collections.Generic;

namespace em {
	public class PreferencesDao {
		readonly DaoConnection daoConnection;

		const string dbToObj = 
			"preference_key as PreferenceKey, " +
			"preference_value as PreferenceValue"; // String used in SQL queries to convert from database columns to object properties.

		public PreferencesDao (DaoConnection conn) {
			daoConnection = conn;
		}
			
		public void CreateIfNeccessary() {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						string queryStr = String.Format ("SELECT name FROM sqlite_master WHERE type='table' AND name='preference'");
						List<SQLiteTable> query = db.Query<SQLiteTable> (queryStr);
						if (query.Count == 0) {
							CreateTable ();
							InsertDefaultValue ();
						}
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		public Preference FindPreference(string key) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					List<Preference> query = db.Query<Preference> (String.Format("select {0} from preference where preference_key=?", dbToObj), new object[] { key });
					return query.Count == 0 ? null : query [0];
				}
			}
		}

		public void UpdatePreference<T>(string key, T value) {
			lock (daoConnection) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();
					try {
						db.BeginTransaction();
						int affected = db.Execute ("update preference set preference_value=? where preference_key=?",
							new object[] {Convert.ToString(value, Preference.usEnglishCulture), key });
						
						if (affected == 0) {
							db.Execute("insert into preference (preference_key,preference_value) values (?,?)",
								new object[] { key, Convert.ToString(value, Preference.usEnglishCulture) });
						}
					}
					finally {
						db.EndTransaction ();
					}
				}
			}
		}

		void CreateTable () {
			lock ( daoConnection ) {
				using (daoConnection) {
					ISQLiteConnection db = daoConnection.connection ();

					db.Execute ("create table preference (" +
						"preference_key varchar(200) not null," +
						"preference_value varchar(200) not null," +
						"unique (preference_key)" +
						");"
					);
				}
			}
		}

		void InsertDefaultValue () {
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.LAST_MESSAGE_UPDATE + "', '" + DateTime.MinValue.ToString(Preference.usEnglishCulture) + "')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.ADDRESS_BOOK_CHECKSUM + "', '-1')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.CONTACTS_VERSION + "', '0')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.ADDRESS_BOOK_ACCESS + "', 'false')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.ADDRESS_BOOK_ACCESS_HIDE_ALERT + "', 'false')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.EM_STANDARD_TIME + "', '0.0')");

			//GA Goal Preferences
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.GA_SETUP_PROFILE + "', 'false')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.GA_SENT_MESSAGE + "', 'false')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.GA_RECEIVED_MESSAGE + "', 'false')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.GA_CREATED_AKA + "', 'false')");
			daoConnection.execute ("insert into preference (preference_key,preference_value) values ('" + Preference.GA_CREATED_GROUP + "', 'false')");
		}
	}
}