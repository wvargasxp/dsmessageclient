namespace em {
	public enum ContactIdentifierType {
		Email,
		Phone,
		NotApplicable,
		Unknown
	}

	public static class ContactIdentifierTypeHelper {

		public static ContactIdentifierType FromDatabase(string s) {
			if ( s == null )
				return ContactIdentifierType.Unknown;
			
			if ( s.Equals("E"))
				return ContactIdentifierType.Email;
			if ( s.Equals("P"))
				return ContactIdentifierType.Phone;
			if (s.Equals ("N"))
				return ContactIdentifierType.NotApplicable;

			return ContactIdentifierType.Unknown;
		}
	}
}