namespace em {
	public enum ContactLifecycle {
		Active,
		Orphaned,
		Deleted,
		OptOut,
		Unreachable,
		Unknown
	}

	public static class ContactLifecycleHelper {

		public static ContactLifecycle FromDatabase(string s) {
			if ( s == null )
				return ContactLifecycle.Unknown;
			
			if ( s.Equals("A"))
				return ContactLifecycle.Active;
			if ( s.Equals("O"))
				return ContactLifecycle.Orphaned;
			if ( s.Equals("D"))
				return ContactLifecycle.Deleted;
			if ( s.Equals("U"))
				return ContactLifecycle.Unreachable;

			return ContactLifecycle.Unknown;
		}

		public static bool EMCanSendTo(string lifecycleStr) {
			return EMCanSendTo (ContactLifecycleHelper.FromDatabase (lifecycleStr));
		}

		public static bool EMCanSendTo(ContactLifecycle lifecycle) {
			return lifecycle.Equals(ContactLifecycle.Active);
		}
	}
}