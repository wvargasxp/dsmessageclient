using System;

namespace em {
	public enum NotificationStatus {
		Delivered,
		Read,
		ActionInitiated,
		Deleted
	}

	public static class NotificationStatusHelper {
		public static NotificationStatus FromString (string ch) {
			switch (ch) {
			case "Delivered":
				return NotificationStatus.Delivered;
			case "Read":
				return NotificationStatus.Read;
			case "ActionInitiated":
				return NotificationStatus.ActionInitiated;
			default:
			case "Deleted":
				return NotificationStatus.Deleted;
			}
		}
	}
}

