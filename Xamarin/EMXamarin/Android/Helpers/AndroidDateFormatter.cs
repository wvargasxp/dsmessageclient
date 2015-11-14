using System;
using em;
using Java.Text;

namespace Emdroid {
	public class AndroidDateFormatter : AbstractDateFormatter {
		static readonly DateTime Epoch = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public AndroidDateFormatter ()
		{
		}

		public override string ActualDateString (DateTime dt) {
			// 6/12/14
			DateFormat dateFormat = Android.Text.Format.DateFormat.GetDateFormat (EMApplication.GetMainContext());
			long millis = (long) dt.ToUniversalTime ().Subtract (Epoch).TotalMilliseconds;
			Java.Util.Date javaDate = new Java.Util.Date (millis);
			string date = dateFormat.Format (javaDate);
			return date;
		}

		public override string TimeString (DateTime dt) {
			// 10:54 PM
			long millis = (long) dt.ToUniversalTime ().Subtract (Epoch).TotalMilliseconds;
			Java.Util.Date javaDate = new Java.Util.Date (millis);
			SimpleDateFormat simpleDateFormat = new SimpleDateFormat ("h:mm a");
			string time = simpleDateFormat.Format (javaDate);
			return time;
		}

		public override string WeekDayString (DateTime dt) {
			// Tuesday
			long millis = (long) dt.ToUniversalTime ().Subtract (Epoch).TotalMilliseconds;
			Java.Util.Date javaDate = new Java.Util.Date (millis);
			SimpleDateFormat simpleDateFormat = new SimpleDateFormat ("EEEE");
			string weekday = simpleDateFormat.Format (javaDate);
			return weekday;
		}
	}
}

