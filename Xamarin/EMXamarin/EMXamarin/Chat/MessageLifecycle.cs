using System;

namespace em {
	public enum MessageLifecycle {
		active,
		deleted,
		timed,
		expired,
		historical // client side only
	}

	public static class MessageLifecycleHelper {
		public static string toDatabase(MessageLifecycle ms) {
			switch (ms) {
			default:
			case MessageLifecycle.active:
				return "A";

			case MessageLifecycle.deleted:
				return "D";

			case MessageLifecycle.timed:
				return "T";

			case MessageLifecycle.expired:
				return "X";

			case MessageLifecycle.historical:
				return "H";
			}
		}

		public static MessageLifecycle fromDatabase(string ch) {
			switch (ch) {
			default:
			case "A":
				return MessageLifecycle.active;

			case "D":
				return MessageLifecycle.deleted;

			case "T":
				return MessageLifecycle.timed;

			case "X":
				return MessageLifecycle.expired;

			case "H":
				return MessageLifecycle.historical;
			}
		}

		public static MessageLifecycle FromString(string ch) {
			switch (ch) {
			default:
			case "active":
				return MessageLifecycle.active;

			case "deleted":
				return MessageLifecycle.deleted;

			case "timed":
				return MessageLifecycle.timed;

			case "expired":
				return MessageLifecycle.expired;

			case "historical":
				return MessageLifecycle.historical;
			}
		}
	}
}

