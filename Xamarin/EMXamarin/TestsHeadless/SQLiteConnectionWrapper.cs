using System;
using EMXamarin;
using SQLite.Net;
using System.Collections.Generic;
using SQLite.Net.Platform.Generic;

namespace TestsHeadless {

	public class SQLiteConnectionWrapper : ISQLiteConnection {

		readonly SQLiteConnection c;

		const string password = "EMPRIVATEKEY";

		public SQLiteConnectionWrapper (string path, bool storeDateTimeAsTicks) {
			//var bytes = new byte[password.Length * sizeof(char)];
			//System.Buffer.BlockCopy(password.ToCharArray(), 0, bytes, 0, bytes.Length);

			c = new SQLiteConnection (new SQLitePlatformGeneric(), path, storeDateTimeAsTicks);
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

		#endregion

		#region IDisposable implementation

		public void Dispose () {
			c.Dispose ();
		}

		#endregion
	}
}

