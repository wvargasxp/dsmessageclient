using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using em;
namespace Windows10.PlatformImpl
{
    class WindowsSqliteConnection : ISQLiteConnection    
    {

        private SQLiteConnection Connection { get; set; }
        private int OpenTransactionCount { get; set; }

        public WindowsSqliteConnection(string p0, string p1, bool p2)
        {
            this.Connection = new SQLiteConnection(p0, p2);
            this.OpenTransactionCount = 0;
        }

        #region ISQLiteConnection implementation
        public int Execute(string sql, params object[] args)
        {
            return this.Connection.Execute(sql, args);
        }

        public T ExecuteScalar<T>(string sql, params object[] args)
        {
            return this.Connection.ExecuteScalar<T>(sql, args);
        }

        public List<T> Query<T>(string sql, params object[] args) where T : new()
        {
            SQLiteCommand cmd = this.Connection.CreateCommand(sql, args);
            return cmd.ExecuteQuery<T>();
        }

        public void BeginTransaction()
        {
            if (this.OpenTransactionCount == 0)
            {
                Execute("begin transaction");
            }

            this.OpenTransactionCount++;
        }

        public void EndTransaction()
        {
            this.OpenTransactionCount--;
            if (this.OpenTransactionCount == 0)
            {
                Execute("commit transaction");
            }
        }

        public void Dispose()
        {
            this.Connection.Dispose();
        }
        #endregion
    }
}
