using System;
using em;
using Foundation;

namespace iOS {
	public class iOSDateFormatter : AbstractDateFormatter {
		static NSDateFormatter formatter = null;
		static NSDateFormatter timeFormatter = null;
		static NSDateFormatter weekDayFormatter = null;

		public iOSDateFormatter ()
		{
		}

		public override string ActualDateString (DateTime dt) {
			// 6/23/11
			NSDate nsDate = dt.ToLocalTime ().DateTimeToNSDate ();
			if (formatter == null) {
				formatter = new NSDateFormatter ();
				formatter.DateStyle = NSDateFormatterStyle.Short;
				formatter.TimeStyle = NSDateFormatterStyle.None;
				formatter.TimeZone = NSTimeZone.LocalTimeZone;
			}

			string date = formatter.StringFor (nsDate);
			return date;
		}

		public override string TimeString (DateTime dt) {
			// 10:40 PM
			NSDate nsDate = dt.ToLocalTime ().DateTimeToNSDate ();
			if (timeFormatter == null) {
				timeFormatter = new NSDateFormatter ();
				timeFormatter.TimeStyle = NSDateFormatterStyle.Short;
				timeFormatter.TimeZone = NSTimeZone.LocalTimeZone;
			}

			string timeString = timeFormatter.StringFor (nsDate);
			return timeString;
		}

		public override string WeekDayString (DateTime dt) {
			// Monday
			NSDate nsDate = dt.ToLocalTime ().DateTimeToNSDate ();
			if (weekDayFormatter == null) {
				weekDayFormatter = new NSDateFormatter ();
				weekDayFormatter.DateFormat = "EEEE";
				weekDayFormatter.TimeZone = NSTimeZone.LocalTimeZone;
			}

			string date = weekDayFormatter.StringFor (nsDate);
			return date;
		}
	}
}