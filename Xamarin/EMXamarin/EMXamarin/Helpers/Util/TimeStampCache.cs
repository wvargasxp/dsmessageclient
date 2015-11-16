using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace em {
	public class TimeStampCache {
		private static TimeStampCache self;
		public static TimeStampCache SharedInstance {
			get {
				if (self == null) {
					self = new TimeStampCache ();
				}

				return self; 
			}
		}

		private Dictionary<DateTime, string> backingDictionary; 
		public Dictionary<DateTime, string> PartialDatesCache {
			get {
				Debug.Assert (ApplicationModel.SharedPlatform.OnMainThread, "TimeStampCache getting called from a background thread.");
				if (backingDictionary == null) {
					backingDictionary = new Dictionary<DateTime, string> ();
				}

				return backingDictionary;
			}
		}

		private Dictionary<DateTime, string> backingFullDatesDictionary;
		public Dictionary<DateTime, string> FullDatesCache {
			get {
				Debug.Assert (ApplicationModel.SharedPlatform.OnMainThread, "TimeStampCache getting called from a background thread.");
				if (backingFullDatesDictionary == null) {
					backingFullDatesDictionary = new Dictionary<DateTime, string> ();
				}

				return backingFullDatesDictionary;
			}
		}

		private TimeStampCache () {}

		public void ClearCache () {
			this.PartialDatesCache.Clear ();
			this.FullDatesCache.Clear ();
		}
	}
}

