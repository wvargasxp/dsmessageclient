namespace em {
	public static class Constants {
		public static readonly int LARGE_COPY_BUFFER = 32768;

		public static readonly string ISO_DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:ss'Z'";

		public static readonly int TIMEOUT_REGISTER_CONTACTS = 600; // 60 seconds * 10 => 10 minutes
	
		#region LiveServerConnection Reconnects
		public static readonly int TIMER_INTERVAL_BETWEEN_RECONNECTS = 5000; // Five seconds.
		public static readonly int TIMER_INTERVAL_BETWEEN_TIMEOUT_CHECKS = 10000; // Ten seconds.
		public static readonly int TIMER_INTERVAL_BETWEEN_PINGS = 2000; // Two seconds.
		public static readonly int TIMER_INTERVAL_BETWEEN_PLAYING_SOUNDS = 7000; // Seven seconds.
		public static readonly string SOCKET_CONNECTION_STATUS_NOTIF = "doeswebsocketshaveaconnection";
		public static readonly string LIVE_SERVER_HAS_CONNECTION_KEY = "keyforliveserverconnection"; // Key for dictionary.
		#endregion
		public static readonly int TIMER_INTERVAL_BEFORE_RETRIEVING_OLD_MESSAGES = 500;
		public static readonly int INITIAL_NUMBER_OF_MESSAGES_TO_RETRIEVE_IN_CHAT = 25;
		public static readonly int NUMBER_OF_PREVIOUS_MESSAGES_TO_RETRIEVE = 100;

        public static readonly int WEBSOCKET_PING_INTERVAL_SECONDS = 10;

		public static readonly string DID_BECOME_ACTIVE = "applicationdidbecomeactive";
		public static readonly string DID_ENTER_FOREGROUND = "applicationdidenterforeground";
		public static readonly string DID_ENTER_BACKGROUND = "applicationdidenterbackground";

		public static readonly string ENTERING_FOREGROUND = "application.entering.foreground";
		public static readonly string ENTERING_BACKGROUND = "application.entering.background";

		public static readonly int TIMER_INTERVAL_BEFORE_HIDING_UI_IN_PHOTO_GALLERY = 2000;

		public static readonly int TIMER_SEND_CHAT_MESSAGE_VIA_WEBSOCKET_TIMEOUT = 5000;

		public static readonly int[] FIXED_DURATIONS_FOR_CHAT_MESSAGE_RETRY = {0, 5 * 1000, 10 * 1000, 25 * 1000, 30 * 1000, 2 * 60 * 1000};

		public static readonly int TIMER_INTERVAL_BEFORE_MOVING_MEDIAREF_FOR_OUTGOING_CHAT_MESSAGE = 5000;

		public static readonly float BASE_PROGRESS_ON_PROGRESS_VIEW = .01f;

		public static readonly int[] RGB_ODD_INBOX_ROW = { 251, 251, 251 };
		public static readonly int[] RGB_EVEN_INBOX_ROW = { 237, 237, 237 };

		public static readonly int[] RGB_PURPLE_COLOR = { 99, 47, 109 }; // #632F6D
		public static readonly int[] RGB_BLUE_COLOR = { 19, 129, 208 }; // #1381D0
		public static readonly int[] RGB_ORANGE_COLOR = { 252, 153, 56 }; // #FC9938
		public static readonly int[] RGB_PINK_COLOR = { 189, 24, 123 }; // #BD187B
		public static readonly int[] RGB_GREEN_COLOR = { 35, 195, 3 }; // #23C303
		public static readonly int[] RGB_GRAY_COLOR = { 60, 60, 60 }; // #3C3C3C
		public static readonly int[] RGB_WHITE_COLOR = { 250, 250, 250 }; // #FAFAFA
		public static readonly int[] RGB_BLACK_COLOR = { 40, 40, 40 }; // #282828
		public static readonly int[] RGB_NEUTRAL_COLOR = {237, 237, 237 };

		public static readonly int[] RGB_INBOX_ROW_SEPERATOR_COLOR = { 153, 153, 153 };

		public static readonly int[] RGB_TOOLBAR_COLOR = { 237, 237, 237 }; // #ededed

		public static readonly int PORTRAIT_CHAT_THUMBNAIL_HEIGHT = 144;
		public static readonly int LANDSCAPE_CHAT_THUMBNAIL_WIDTH = 168;

		public static readonly double LONG_PRESS_DURATION_SECONDS = 0.75;

		public static readonly string FONT_FOR_TITLES = "Ubuntu-Light";
		public static readonly string FONT_FOR_LABELS = "Ubuntu-Light";
		public static readonly string FONT_FOR_DEFAULT = "Ubuntu-Light";
		public static readonly string FONT_FOR_TEXTFIELDS = "Ubuntu-Light";
		public static readonly string FONT_FOR_TITLES_BOLD = "Ubuntu-Bold";
		public static readonly string FONT_FOR_LABELS_BOLD = "Ubuntu-Bold";
		public static readonly string FONT_FOR_DEFAULT_BOLD = "Ubuntu-Bold";
		public static readonly string FONT_FOR_TEXTFIELDS_BOLD = "Ubuntu-Bold";

		public static readonly string FONT_LIGHT_ITALIC = "Ubuntu-LightItalic";
		public static readonly string FONT_BOLD_ITALIC = "Ubuntu-BoldItalic";

		public static readonly int MAX_HTTP_REQUESTS = Media.MAX_CONCONCURRENT_THROTTLED_MEDIA_DOWNLOADS + 10;

		#region NOTIFICATION CENTER STRINGS
		// As a quick mental note, I've prepended the class that would usually post the notification.
		public static string Message_DownloadFailed = "message_download_failed";
		public static string Media_DownloadFailed = "media_did_fail_download";
		public static string Counterparty_DownloadFailed = "counterparty_did_fail_download";

		public static string Counterparty_DownloadCompleted = "COUNTERPARTY_MEDIA_DID_COMPLETE_DOWNLOAD";
		public static string Counterparty_ThumbnailChanged = "COUNTERPARTY_MEDIA_DID_CHANGE_THUMBNAIL";
		public static string Counterparty_CounterpartyKey = "COUNTERPARTY_KEY_KEY";

		public static string ApplicationModel_DidRegisterContactsNotification = "contacts_did_register_notification";
		public static string AppDelegate_DidRegisterPushNotification = "appdelegate_did_register_pushnotifications";
		public static string ContactsManager_StartAccessedDifferentContacts = "contactsmanager_start_accessed_new_contact_list";
		public static string ContactsManager_AccessedDifferentContacts = "contactsmanager_accessed_new_contact_list";
		public static string ContactsManager_ProcessedDifferentContacts = "contactsmanager_processing_new_contact_list";
		public static string ContactsManager_FailedProcessedDifferentContacts = "contactsmanager_failed_processing_new_contact_list";
		public static string ApplicationModel_LiveServerConnectionChange = "appmodel_liveserverconnectoin_changed";
		public static string MediaGallery_PageChangedNotification = "mediagallery_changed_page_notification";
		public static string MediaGallery_Paused = "mediagallery_paused";
		public static string PlatformFactory_ShowNetworkIndicatorNotification = "platformfactory_show_network_indicator";
		public static string PlatformFactory_HideNetworkIndicatorNotification = "platformfactory_hide_network_indicator";

		public static string Model_WillShowRemotePromptFromServerNotification = "appdelegate_or_mainactivity_will_show_rempot_";
		public static string NotificationEntryDao_UnreadCountChanged = "notificationentrydao_unread_count_changed";
		public static string AbstractChatController_FinishedRetrievingMorePreviousMessages = "abstractchatcontroller_finished_retrieving_more_previous_messages";
		public static string EMAccount_LoginAndHasConfigurationNotification = "emaccount_login_notification";
		public static string EMAccount_EMHttpUnauthorizedResponseNotification = "emaccount_http_unauthorized_notification";
		public static string EMAccount_EMHttpAuthorizedResponseNotification = "emaccount_http_authorized_notification";

		public static string ApplicationModel_LoggedInAndWebsocketConnectedNotification ="applicationmodel_is_logged_in_and_websocket_connectd";
		public static string AbstractChatController_NewMediaMessageAdded = "abstractchatcontroller_added_new_media_message";

		public static string AbstractInboxController_EntryWithNewInboundMessage = "abstractinboxcontroller_entry_with_inbound_message";
		#endregion

		public static readonly long FADE_ANIMATION_DURATION_MILLIS = 200;

		public static readonly int MAX_DIMENSION_SENT_PHOTO = 1600;
		public static readonly float JPEG_CONVERSION_QUALITY = 0.75f;

		public static readonly int NUM_SECONDS_RANGE_TO_COMPARE_CREATEDATES = 3;

		#region stage media
		public static string STAGE_MEDIA_BEGIN = "stage_media_begin"; 
		public static string STAGE_MEDIA_DONE = "stage_media_done";
		#endregion

		public const double SOUND_RECORDING_MIN_RECORDING_DURATION_SECONDS = 0.25d;

		public static readonly long ODD_EVEN_BACKGROUND_COLOR_CORRECTION_PAUSE = 200;

		#region db
		public static readonly int new_kdf_iter = 4000;
		public static readonly int old_default_kdf_iter = 64000;
		public static readonly string DB_MAIN = "EMDatabase.db3";
		public static readonly string DB_OUTGOING_QUEUE = "EMOutgoingQueue.db3";
		#endregion

		#region secure
		public static readonly string ENCRYPTION_TYPE = "AES";
		public static readonly string SECURE_FILE_NAME = "_cache.tmp";
		public static readonly string USERNAME_KEY = "un";
		public static readonly string VERIFICATION_CODE_KEY = "vc";
		public static readonly string DB_KEY = "sc";
		public static readonly string URL_QUERY_VERIFICATION_CODE_KEY = "urlvc";
		public static readonly string KEY_1 = "Z7k_4l";
		public static readonly string KEY_2 = "_$&@;b-P";
		public static readonly string KEY_3 = "EM";
		public static readonly string NOTIFICATION_TOKEN_KEY = "notiftoken";			// base64-encoded, push token (iOS) or gcm token (Android)
		#endregion

		#region social
		public static readonly string EM_FACEBOOK_ID_DEFAULT = "251874468342857";
		public static readonly string EM_FACEBOOK_ID_ARABIA = "630356707064879";
		public static readonly uint EM_TWITTER_ID_DEFAULT = 2691282907;
		public static readonly uint EM_TWITTER_ID_ARABIA = 3166273053;
		public static readonly string EM_TWITTER_NAME_DEFAULT = "emtheapp";
		public static readonly string EM_TWITTER_NAME_ARABIA = "emtheapparabia";
		#endregion
	}
}