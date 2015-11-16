namespace em {
	public enum PhoneNumberType {
		FixedLine,
		FixedLineOrMobile,
		Mobile,
		Pager,
		PersonalNumber,
		PremiumRate,
		SharedCost,
		TollFree,
		UAN,
		Unknown,
		Voicemail,
		VOIP,
		NotApplicable
	}

	public static class PhoneNumberTypeHelper {

		public static PhoneNumberType FromDatabase(string s) {
			if ( s == null )
				return PhoneNumberType.Unknown;

			if ( s.Equals("F"))
				return PhoneNumberType.FixedLine;
			if ( s.Equals("L"))
				return PhoneNumberType.FixedLineOrMobile;
			if (s.Equals ("M"))
				return PhoneNumberType.Mobile;
			if ( s.Equals("P"))
				return PhoneNumberType.Pager;
			if ( s.Equals("N"))
				return PhoneNumberType.PersonalNumber;
			if (s.Equals ("R"))
				return PhoneNumberType.PremiumRate;
			if ( s.Equals("S"))
				return PhoneNumberType.SharedCost;
			if ( s.Equals("T"))
				return PhoneNumberType.TollFree;
			if (s.Equals ("X"))
				return PhoneNumberType.UAN;
			if ( s.Equals("U"))
				return PhoneNumberType.Unknown;
			if ( s.Equals("I"))
				return PhoneNumberType.Voicemail;
			if ( s.Equals("V"))
				return PhoneNumberType.VOIP;
			if (s.Equals ("A"))
				return PhoneNumberType.NotApplicable;

			return PhoneNumberType.Unknown;
		}
	}
}