using System;
using System.Globalization;

namespace em {
	public class Preference {
		public static readonly string LAST_MESSAGE_UPDATE = "LAST_MESSAGE_UPDATE";
		public static readonly string ADDRESS_BOOK_CHECKSUM = "ADDRESS_BOOK_CHECKSUM";
		public static readonly string CONTACTS_VERSION = "CONTACTS_VERSION";
		public static readonly string ADDRESS_BOOK_ACCESS = "ADDRESS_BOOK_ACCESS";
		public static readonly string ADDRESS_BOOK_ACCESS_HIDE_ALERT = "ADDRESS_BOOK_ACCESS_HIDE_ALERT";
		public static readonly string EM_STANDARD_TIME = "EM_STANDARD_TIME";

		//GA Goal Preferences
		public static readonly string GA_SETUP_PROFILE = "GA_SETUP_PROFILE";
		public static readonly string GA_SENT_MESSAGE = "GA_SENT_MESSAGE";
		public static readonly string GA_RECEIVED_MESSAGE = "GA_RECEIVED_MESSAGE";
		public static readonly string GA_CREATED_AKA = "GA_CREATED_AKA";
		public static readonly string GA_CREATED_GROUP = "GA_CREATED_GROUP";

		public static readonly CultureInfo usEnglishCulture = new CultureInfo("en-US");

		public string PreferenceKey { get; set; }
		public string PreferenceValue { get; set; }

		public static T GetPreference<T>(ApplicationModel appModel, string key) {
			Preference pref = appModel.preferenceDao.FindPreference (key);

			if (pref == null)
				return default(T);

			if (typeof (T) == typeof (DateTime))
				return (T)(object) Convert.ToDateTime(pref.PreferenceValue, usEnglishCulture);

			if (typeof(T) == typeof(int))
				return (T)(object) Convert.ToInt32(pref.PreferenceValue, usEnglishCulture);

			if (typeof(T) == typeof(double))
				return (T)(object) Convert.ToDouble(pref.PreferenceValue, usEnglishCulture);

			if (typeof(T) == typeof(bool))
				return (T)(object) Convert.ToBoolean (pref.PreferenceValue, usEnglishCulture);

			if (typeof(T) == typeof(long)) {
				return (T)(object)Convert.ToInt64 (pref.PreferenceValue, usEnglishCulture);
			}
			
			return (T)(object)pref.PreferenceValue;
		}

		public static void UpdatePreference<T>(ApplicationModel appModel, string key, T value) {
			appModel.preferenceDao.UpdatePreference<T> (key, value);
		}

		public static bool DoesPreferenceExist(ApplicationModel appModel, string key) {
			Preference pref = appModel.preferenceDao.FindPreference (key);

			return pref != null;
		}
	}
}