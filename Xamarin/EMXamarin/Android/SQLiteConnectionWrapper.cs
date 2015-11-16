using System.Collections.Generic;
using System.IO;
using em;
using SQLite;

namespace Emdroid {

	public class SQLiteConnectionWrapper : ISQLiteConnection {

		readonly SQLiteConnection c;
		int openTransactionCount = 0;

		public SQLiteConnectionWrapper (string p0, string p1, bool p2, string p3, string p4, IFileSystemManager fileSystemManager) {
			c = new SQLiteConnection (":memory:");
			var cmd = c.CreateCommand (string.Format("PRAGMA cipher_default_kdf_iter = {0};", Constants.new_kdf_iter), new object[]{ });
			cmd.ExecuteNonQuery ();
			c.Close ();

			c = new SQLiteConnection (p0, p1, p2);

			try {
				//if this query succeeds, we're at our custom kdf iter (4000), we're good an can just return
				c.ExecuteScalar<string> ("SELECT name FROM sqlite_master WHERE type='table' AND name='contact'");
			} catch(SQLiteException e) {
				System.Diagnostics.Debug.WriteLine ("Exception! " + e.Message);

				//create the new db file
				var stream = File.Create (p3);
				stream.Close ();

				//if we're not, we have to do a migration
				c = new SQLiteConnection (":memory:");
				cmd = c.CreateCommand (string.Format("PRAGMA cipher_default_kdf_iter = {0};", Constants.old_default_kdf_iter), new object[]{ });
				cmd.ExecuteNonQuery ();
				c.Close ();

				c = new SQLiteConnection (p0, p1, p2);

				c.ExecuteScalar<string> ("SELECT name FROM sqlite_master WHERE type='table' AND name='contact'");

				cmd = c.CreateCommand (string.Format ("ATTACH DATABASE '{0}' AS {1} KEY '{2}';", p3, p4, p1));
				cmd.ExecuteNonQuery ();
				cmd = c.CreateCommand (string.Format ("PRAGMA {0}.kdf_iter = {1};", p4, Constants.new_kdf_iter));
				cmd.ExecuteNonQuery ();
				cmd = c.CreateCommand (string.Format("SELECT sqlcipher_export('{0}');", p4));
				cmd.ExecuteQuery<object> ();
				cmd = c.CreateCommand (string.Format ("DETACH DATABASE {0};", p4));
				cmd.ExecuteNonQuery ();
				c.Close ();

				//move new db to existing db
				fileSystemManager.MoveFileAtPath (p3, p0);

				//create new connection
				c = new SQLiteConnection (":memory:");
				cmd = c.CreateCommand (string.Format("PRAGMA cipher_default_kdf_iter = {0};", Constants.new_kdf_iter), new object[]{ });
				cmd.ExecuteNonQuery ();
				c.Close ();

				c = new SQLiteConnection (p0, p1, p2);
			}
		}

		#region ISQLiteConnection implementation

		public int Execute (string sql, params object[] args) {
			return c.Execute (sql, args);
		}

		public T ExecuteScalar<T> (string sql, params object[] args) {
			return c.ExecuteScalar<T> (sql, args);
		}

		public List<T> Query<T> (string sql, params object[] args) where T : new() {
			var cmd = c.CreateCommand (sql, args);
			return cmd.ExecuteQuery<T> ();
		}

		public void BeginTransaction() {
			if (openTransactionCount == 0)
				Execute ("begin transaction;");
			openTransactionCount++;
		}

		public void EndTransaction() {
			--openTransactionCount;
			if (openTransactionCount == 0)
				Execute ("commit transaction;");
		}

		#endregion

		#region IDisposable implementation

		public void Dispose () {
			c.Dispose ();
		}

		#endregion

	}
}