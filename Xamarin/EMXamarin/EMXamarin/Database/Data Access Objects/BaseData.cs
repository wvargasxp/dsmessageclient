using System;

namespace em {
	public class BaseData {
		public static ApplicationModel appModel { get; set; }
		public bool isPersisted { get; set; }

		public BaseData () {
			isPersisted = true;
		}
	}
}

