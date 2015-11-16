using System;
using System.Threading;

namespace em {
	public class DaoConnection : IDisposable {
		ISQLiteConnection conn;
		int count;
		int sequence;

		readonly int IDLE_CONNECTION_CLOSE_TIMEOUT = 300000; // 5 minutes in milliseconds
		readonly int SHUTDOWN_CONNECTION_CLOSE_TIMEOUT = 5000; // 5 seconds in milliseconds

		ApplicationModel appModel;
		string databaseName;
		public DaoConnection (ApplicationModel appModel, string databaseName) {
			conn = null;
			count = 0;
			sequence = 1;

			this.appModel = appModel;
			this.databaseName = databaseName;
		}

		public ISQLiteConnection connection() {
			lock (this) {
				count++;
				sequence++;
				if (conn == null)
					conn = appModel.platformFactory.createSQLiteConnection (databaseName);

				return conn;
			}
		}

		public void Dispose() {
			lock (this) {
				count--;
				innerCloseIfPossible (IDLE_CONNECTION_CLOSE_TIMEOUT);
			}
		}

		public void EnteringBackground() {
			innerCloseIfPossible (SHUTDOWN_CONNECTION_CLOSE_TIMEOUT);
		}

		protected void innerCloseIfPossible(int timeout) {
			lock (this) {
				if (count == 0 && conn != null) {
					DaoConnection self = this;
					new Timer ((object o) => {
						int state = (int)o;
						lock (self) {
							if (state == sequence && conn != null) {
								conn.Dispose ();
								conn = null;
							}
						}
					}, sequence, timeout, Timeout.Infinite);
				}
			}
		}

		public void execute(string sql, params object[] values) {
			lock (this) {
				using (this) {
					ISQLiteConnection db = connection ();
					db.Execute (sql, values);
				}
			}
		}
	}
}