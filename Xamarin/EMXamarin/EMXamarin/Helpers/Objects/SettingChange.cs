using System;

namespace em {
	public class SettingChange<T> {

		public T Item { get; set; }
		public bool Enabled { get; set; }

		public static SettingChange<T> From (T item, bool enabled) {
			SettingChange<T> res = new SettingChange<T> ();
			res.Item = item;
			res.Enabled = enabled;
			return res;
		}

		public SettingChange ()
		{
		}
	}
}

