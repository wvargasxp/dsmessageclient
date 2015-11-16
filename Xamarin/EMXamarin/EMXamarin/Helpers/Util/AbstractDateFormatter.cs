using System;
using System.Collections.Generic;

namespace em {
	public abstract class AbstractDateFormatter {

		public string FormatDate (DateTime dt, DateFormatStyle style) {
			Dictionary<DateTime, string> cache = CacheForStyle (style);
			if (cache.ContainsKey (dt)) {
				return cache [dt];
			}

			DateTime today = DateTime.Today.ToUniversalTime ();
			DateTime localTimeOfMessage = dt.ToLocalTime ();
			DateTime localTimeToday = today.ToLocalTime ();

			string dateString = string.Empty;

			bool appendTimeToDate = (style == DateFormatStyle.FullDate);

			if (ShouldShowDate (dt)) {
				dateString = ActualDateString (dt);
			} else if (localTimeOfMessage.Day == localTimeToday.Day
				|| (dt.Month > today.Month || dt.Month == today.Month && dt.Day > today.Day)) {
				// second case accounts for if messages were sent from the 'future'
				string timeString = TimeString (dt);
				if (appendTimeToDate) {
					dateString = ApplicationModel.SharedPlatform.GetTranslation ("TODAY") + " " + timeString;
				} else {
					dateString = timeString;
				}

				appendTimeToDate = false;
			} else if ((localTimeToday - localTimeOfMessage.Date).Days == TimeSpan.FromDays (1).Days) {
				// https://stackoverflow.com/questions/3211914/elegantly-check-if-a-given-date-is-yesterday
				dateString = ApplicationModel.SharedPlatform.GetTranslation ("YESTERDAY");
			} else {
				dateString = WeekDayString (dt);
			}

			if (appendTimeToDate) {
				string timeString = TimeString (dt);
				dateString = string.Format ("{0} {1}", dateString, timeString);
			}
				
			cache.Add (dt, dateString);
			return dateString;
		}

		private Dictionary<DateTime, string> CacheForStyle (DateFormatStyle style) {
			switch (style) {
			case DateFormatStyle.FullDate:
				{
					return TimeStampCache.SharedInstance.FullDatesCache;
				}
			case DateFormatStyle.PartialDate:
				{
					return TimeStampCache.SharedInstance.PartialDatesCache;
				}
			default:
				{
					return TimeStampCache.SharedInstance.PartialDatesCache;
				}
			}
		}

		private bool ShouldShowDate (DateTime dt) {
			DateTime today = DateTime.Today.ToUniversalTime ();

			DateTime localTimeOfMessage = dt.ToLocalTime ();
			DateTime localTimeToday = today.ToLocalTime ();

			if ((localTimeToday.Date - localTimeOfMessage.Date).Days > 6 /* 1 week apart */) {
				return true;
			} else {
				return false;
			}
		}

		public abstract string ActualDateString (DateTime dt);
		public abstract string TimeString (DateTime dt);
		public abstract string WeekDayString (DateTime dt);
	}
		
	public enum DateFormatStyle {
		PartialDate,
		FullDate
	}
}