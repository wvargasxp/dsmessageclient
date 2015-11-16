using System;
using System.Collections.Generic;

namespace em {

	public interface ISQLiteConnection : IDisposable {

		int Execute(string sql, params object[] args);
		T ExecuteScalar<T>(string sql, params object[] args);
		List<T> Query<T>(string sql, params object[] args) where T : new();
		void BeginTransaction();
		void EndTransaction();

	}
}