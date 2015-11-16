namespace em {
	public enum MessageChannel {
		em,
		sms,
		email,
		unknown
	}

	public static class MessageChannelHelper {
		public static string toDatabase(MessageChannel ms) {
			switch (ms) {
			case MessageChannel.em:
				return "A";
			
			case MessageChannel.sms:
				return "S";

			case MessageChannel.email:
				return "E";

			default:
			case MessageChannel.unknown:
				return "U";
			}
		}

		public static MessageChannel fromDatabase(string ch) {
			switch (ch) {
			case "A":
				return MessageChannel.em;

			case "S":
				return MessageChannel.sms;

			case "E":
				return MessageChannel.email;

			default:
			case "U":
				return MessageChannel.unknown;
			}
		}

		public static MessageChannel FromString(string ch) {
			switch (ch) {
			case "em":
				return MessageChannel.em;

			case "sms":
				return MessageChannel.sms;

			case "email":
				return MessageChannel.email;

			default:
			case "unknown":
				return MessageChannel.unknown;
			}
		}
	}
}