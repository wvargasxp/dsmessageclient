namespace em {
	
	public interface IAnalyticsHelper {

		void SendEvent(string category, string action, string label, int value);

	}

	public static class AnalyticsConstants {
		//categories
		public static readonly string CATEGORY_UI_ACTION = "UI Action";
		public static readonly string CATEGORY_GA_GOAL = "Goal";

		//actions
		public static readonly string ACTION_BUTTON_PRESS = "Button Press";
		public static readonly string ACTION_SETUP_PROFILE = "Setup Profile";
		public static readonly string ACTION_SENT_MESSAGE = "Sent Message";
		public static readonly string ACTION_RECEIVED_MESSAGE = "Received Message";
		public static readonly string ACTION_CREATE_AKA = "Created AKA";
		public static readonly string ACTION_CREATE_GROUP = "Created Group";

		//labels
		public static readonly string SHARE_PROFILE = "Share Profile";
		public static readonly string PROFILE_CUSTOM_NAME = "Custom Profile Name";
		public static readonly string PROFILE_DEFAULT_NAME = "Default Profile Name";
		public static readonly string SENT_MESSAGE = "Sent Message";
		public static readonly string RECEIVED_MESSAGE = "Received Message";
		public static readonly string CREATED_AKA = "Created AKA";
		public static readonly string CREATED_GROUP = "Created Group";
		public static readonly string ADDRESS_BOOK_ALLOW = "Allow Address Book Access";
		public static readonly string ADDRESS_BOOK_CANCEL = "Cancel Address Book Access";
		public static readonly string ADDRESS_BOOK_DONT_SHOW = "Don't Show Address Book Access";
		public static readonly string SUPPORT_LINK = "Support - {0}";

		//values
		public static readonly int VALUE_SETUP_PROFILE = 3;
		public static readonly int VALUE_SEND_MESSAGE = 5;
		public static readonly int VALUE_RECEIVE_MESSAGE = 5;
		public static readonly int VALUE_CREATE_AKA = 1;
		public static readonly int VALUE_CREATE_GROUP = 1;
	}
}