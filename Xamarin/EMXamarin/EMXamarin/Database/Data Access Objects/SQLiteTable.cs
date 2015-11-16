using System;

namespace em {
	/**
	 * When testing for the existence of a table we creating we search the
	 * sqlite system tables for a specific name.  This type reprsents the
	 * result set from that query.
	 */
	public class SQLiteTable {
		public string name { get; set; }

		public SQLiteTable () {
		}
	}
}

