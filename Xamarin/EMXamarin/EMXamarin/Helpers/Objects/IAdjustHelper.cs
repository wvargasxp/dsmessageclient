using System;
using System.Collections.Generic;

namespace em {
	public interface IAdjustHelper {
		void Init ();
		void SendEvent (EmAdjustEvent adjustEvent);
		void SendEvent (EmAdjustEvent adjustEvent, Dictionary<string, string> parameters);
	}

	public enum EmAdjustEvent {
		Verified
	}

	public static class EmAdjustParamKey {
		public static string AccountKey = "account_id";
	}
}

