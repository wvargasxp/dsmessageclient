using System;
using Foundation;

namespace iOS {
	public static class NSDateExtension {
		public static DateTime NSDateToDateTime(this NSDate date) {
			DateTime reference = TimeZone.CurrentTimeZone.ToLocalTime( 
				new DateTime(2001, 1, 1, 0, 0, 0) );
			return reference.AddSeconds(date.SecondsSinceReferenceDate);
		}
	}
}

