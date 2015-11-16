namespace em {
	public enum GroupMemberLifecycle {
		Active,
		Removed,
		Owner
	}

	public static class GroupMemberLifecycleHelper {
		const string ActiveCase = "A";
		const string RemoveCase = "R";
		const string OwnerCase = "O";

		public static GroupMemberLifecycle FromDatabase (string s) {
			switch (s) {
			case ActiveCase:
				return GroupMemberLifecycle.Active;

			case RemoveCase:
				return GroupMemberLifecycle.Removed;

			case OwnerCase:
				return GroupMemberLifecycle.Owner;

			default:
				return GroupMemberLifecycle.Active;
			}
		}

		public static string ToDatabase (GroupMemberLifecycle c) {
			switch (c) {
			case GroupMemberLifecycle.Active:
				return ActiveCase;

			case GroupMemberLifecycle.Owner:
				return OwnerCase;

			case GroupMemberLifecycle.Removed:
				return RemoveCase;

			default:
				return ActiveCase;
			}
		}

		public static bool EMCanSendTo(string lifecycleStr) {
			return EMCanSendTo (GroupMemberLifecycleHelper.FromDatabase (lifecycleStr));
		}

		public static bool EMCanSendTo(GroupMemberLifecycle lifecycle) {
			return lifecycle.Equals (GroupMemberLifecycle.Active) || lifecycle.Equals (GroupMemberLifecycle.Owner);
		}
	}
}