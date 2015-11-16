using System;

namespace em {
	public enum MessageStatus {
		pending,
		failed,
		sent,
		delivered,
		ignored,
		read
	}

	public static class MessageStatusHelper {
		public static string toDatabase(MessageStatus ms) {
			switch (ms) {
			case MessageStatus.delivered:
				return "D";

			default:
			case MessageStatus.pending:
				return "P";

			case MessageStatus.read:
				return "R";

			case MessageStatus.ignored:
				return "I";

			case MessageStatus.sent:
				return "S";

			case MessageStatus.failed:
				return "F";
			}
		}

		public static MessageStatus fromDatabase(string ch) {
			switch (ch) {
			case "D":
				return MessageStatus.delivered;

			default:
			case "P":
				return MessageStatus.pending;

			case "I":
				return MessageStatus.ignored;

			case "R":
				return MessageStatus.read;

			case "S":
				return MessageStatus.sent;

			case "F":
				return MessageStatus.failed;
			}
		}

		public static MessageStatus FromString(string ch) {
			switch (ch) {
			case "delivered":
				return MessageStatus.delivered;

			case "pending":
				return MessageStatus.pending;

			case "ignored":
				return MessageStatus.ignored;

			default:
			case "read":
				return MessageStatus.read;

			case "sent":
				return MessageStatus.sent;

			case "failed":
				return MessageStatus.failed;
			}
		}

		public static bool AffectsUnreadCount(MessageStatus from, MessageStatus to) {
			switch (from) {
			// if already in a read state it won't affect
			case MessageStatus.read:
			case MessageStatus.ignored:
				return false;

			default:
				// if it's one of the unread states, then we only return true
				// if we are going to a read status
				switch (to) {
				case MessageStatus.read:
				case MessageStatus.ignored:
					return true;

				default:
					break;
				}
				break;
			}

			// otherwise going to 'to' from 'from' won't affect unread
			return false;
		}

		public static bool CanTransitionFromStatusToStatus(MessageStatus from, MessageStatus to) {
			switch (from) {
			case MessageStatus.delivered:
				switch (to) {
				case MessageStatus.pending:
				case MessageStatus.failed:
				case MessageStatus.sent:
				case MessageStatus.delivered:
					return false;

				default:
				case MessageStatus.ignored:
				case MessageStatus.read:
					return true;
				}

			default:
			case MessageStatus.pending:
				switch (to) {
				case MessageStatus.pending:
					return false;

				default:
				case MessageStatus.failed:
				case MessageStatus.sent:
				case MessageStatus.delivered:
				case MessageStatus.ignored:
				case MessageStatus.read:
					return true;
				}

			case MessageStatus.read:
				return false;

			case MessageStatus.ignored:
				switch (to) {
				case MessageStatus.pending:
				case MessageStatus.failed:
				case MessageStatus.sent:
				case MessageStatus.delivered:
				case MessageStatus.ignored:
					return false;

				default:
				case MessageStatus.read:
					return true;
				}

			case MessageStatus.sent:
				switch (to) {
				case MessageStatus.pending:
				case MessageStatus.failed:
				case MessageStatus.sent:
					return false;

				default:
				case MessageStatus.delivered:
				case MessageStatus.ignored:
				case MessageStatus.read:
					return true;
				}

			case MessageStatus.failed:
				return true;
			}
		}
	}
}

