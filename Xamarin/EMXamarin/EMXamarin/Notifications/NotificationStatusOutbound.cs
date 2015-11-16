namespace em {
	public class NotificationStatusOutbound {

		public int notification { get; set; }
		public NotificationStatus status { get; set; }

		public void add(int serverID, NotificationStatus ns) {
			notification = serverID;
			status = ns;
		}
	}
}

