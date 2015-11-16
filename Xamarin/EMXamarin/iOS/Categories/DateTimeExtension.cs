using System;
using Foundation;

namespace iOS {
	public static class DateTimeExtension {
		public static NSDate DateTimeToNSDate(this DateTime date) {
			if (date.Kind == DateTimeKind.Unspecified)
				date = DateTime.SpecifyKind (date, DateTimeKind.Local);
			return (NSDate) date;
		}

		public static NSDate DateTimeToNSDateUtc(this DateTime date) {
			if (date.Kind == DateTimeKind.Unspecified)
				date = DateTime.SpecifyKind (date, DateTimeKind.Utc);
			return (NSDate) date;
		}
	}
}

