using System;

namespace em {
	public enum AddressBookContactLifeCycle {
		Active,
		Orphaned,
		Duplicate,
		Deleted,
		Unknown
	}

	public static class AddressBookContactLifeCycleHelper {
		public static AddressBookContactLifeCycle FromDatabase (string s) {
			if (s == null) {
				return AddressBookContactLifeCycle.Unknown;
			}

			if (s.Equals ("A")) {
				return AddressBookContactLifeCycle.Active;
			}

			if (s.Equals ("O")) {
				return AddressBookContactLifeCycle.Orphaned;
			}

			if (s.Equals ("P")) {
				return AddressBookContactLifeCycle.Duplicate;
			}

			if (s.Equals ("D")) {
				return AddressBookContactLifeCycle.Deleted;
			}

			return AddressBookContactLifeCycle.Unknown;
		}

		public static string ToDatabase (AddressBookContactLifeCycle lifecycle) {
			switch (lifecycle) {
			case AddressBookContactLifeCycle.Active:
				{
					return "A";
				}
			case AddressBookContactLifeCycle.Deleted:
				{
					return "D";
				}
			case AddressBookContactLifeCycle.Duplicate:
				{
					return "P";
				}

			case AddressBookContactLifeCycle.Orphaned:
				{
					return "O";
				}
			case AddressBookContactLifeCycle.Unknown:
				{
					return "U";
				}
			default:
				{
					return "A";
				}
			}
		}
	}
}