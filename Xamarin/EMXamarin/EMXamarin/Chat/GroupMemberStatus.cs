namespace em {

	public enum GroupMemberStatus {
		Joined,
		Abandoned
	}

	public static class GroupMemberStatusHelper {
		public static GroupMemberStatus FromDatabase(string s) {
			switch (s) {
			default:
			case "J":
				return GroupMemberStatus.Joined;

			case "A":
				return GroupMemberStatus.Abandoned;
			}
		}

		public static string ToDatabase(GroupMemberStatus status) {
			switch (status) {
			default:
			case GroupMemberStatus.Joined:
				return "J";

			case GroupMemberStatus.Abandoned:
				return "A";
			}
		}

		public static GroupMemberStatus FromString(string s) {
			switch (s) {
			default:
			case "Joined":
				return GroupMemberStatus.Joined;

			case "Abandoned":
				return GroupMemberStatus.Abandoned;
			}
		}

	}
}