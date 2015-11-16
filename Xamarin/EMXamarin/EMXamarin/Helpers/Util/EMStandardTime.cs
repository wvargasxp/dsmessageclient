using System;

namespace em {
	
	public static class EMStandardTime {

		public static DateTime ToEMStandardTime(this DateTime now, ApplicationModel appModel) {
			var diff = appModel.EMStandardTimeDiff;
			var updatedDateTime = now.AddSeconds (diff);
			return updatedDateTime;
		}

	}
}