using System;

// This is used to encapsulate the id required for a StompClient to properly subscribe and unsubscribe to/from a server.
// ex. headers["id"] = StompPath.FromString (kBroadcastSendPath);

namespace em {
	public enum StompPathId {
		BROADCAST_SEND = 0,
		BROADCAST_TOPIC = 1,
		RECEIVE_MESSAGE = 2,
		RECEIVE_TYPING = 3,
		SEND_MESSAGE = 4,
		SEND_MESSAGE_UPDATE = 5,
		SEND_TYPING = 6,
		SEND_DEVICE = 7,
		SEND_SAVE_OR_UPDATE_GROUP = 8,
		SEND_REGISTER_CONTACTS = 9, 
		SEND_NO_NEW_CONTACTS = 10,
		BROADCAST_TOPIC_ONLINE = 12,
		LOGIN = 13,
		LOGIN_SUCCESS = 14,
		RECEIVE_NOTIFICATIONS = 16,
		SEND_NOTIFICATION_STATUS = 17,
		RECEIVE_NOTIFICATIONS_STATUS = 18,
		UNREAD_COUNT = 19,
		REMOVE_CHAT_ENTRY = 20
	}

	public static class StompPath {
		// These constant strings are used for subscribing and unsubscribing.
		// Ex. client.SubscribeToDestination (StompPath.kBroadcastTopicPath);
		public const string kBroadcastSendPath = "/app/broadcast";
		public const string kBroadcastTopicPath = "/topic/broadcasts";
		public const string kReceiveMessagePath = "/user/topic/message";
		public const string kSendMessage = "/app/sendMessage";
		public const string kSendMessageUpdate = "/app/messageStatusUpdate";
		public const string kSendTyping = "/app/typing";
		public const string kSendEcho = "/app/echo";
		public const string kSendDevice = "/app/device";
		public const string kSendSaveOrUpdateGroup = "/app/updateGroup";
		public const string kSendRegisterContacts = "/app/registerContacts";
		public const string kSendNoNewContacts = "/app/noNewContacts";
		public const string kBroadcastTopicOnlinePath = "/topic/online";
		public const string kHTTPLoginPath       = "/login.html";
		public const string kHTTPLoginSuccessPath = "/index.html";
		public const string kReceiveNotificationsPath = "/user/topic/notifications";
		public const string kSendNotificationStatus = "/app/notifications/status";
		public const string kReceiveNotificationsStatus = "/user/topic/notificationStatus";
		public const string kUnreadCount = "/app/unreadCount";
		public const string kRemoveChatEntry = "/app/removeChatEntry";
		public const string kLeaveConversation = "/app/leaveConversation";
		public const string kInstalledApps = "/app/installedApps";
		public const string kCancelWhosHereBind = "/app/bot/whoshere/cancelBind";
		public const string kNoWhosHereAppToBindTo = "/app/whoshere/noAppToBindTo";

		public static string ToString (StompPathId id) {
			switch (id) {
			case StompPathId.BROADCAST_SEND:
				return kBroadcastSendPath;
			case StompPathId.BROADCAST_TOPIC:
				return kBroadcastTopicPath;
			case StompPathId.RECEIVE_MESSAGE:
				return kReceiveMessagePath;
			case StompPathId.SEND_MESSAGE:
				return kSendMessage;
			case StompPathId.SEND_MESSAGE_UPDATE:
				return kSendMessageUpdate;
			case StompPathId.SEND_TYPING:
				return kSendTyping;
			case StompPathId.SEND_DEVICE:
				return kSendDevice;
			case StompPathId.SEND_SAVE_OR_UPDATE_GROUP:
				return kSendSaveOrUpdateGroup;
			case StompPathId.SEND_REGISTER_CONTACTS:
				return kSendRegisterContacts;
			case StompPathId.SEND_NO_NEW_CONTACTS:
				return kSendNoNewContacts;
			case StompPathId.BROADCAST_TOPIC_ONLINE:
				return kBroadcastTopicOnlinePath;
			case StompPathId.LOGIN:
				return kHTTPLoginPath;
			case StompPathId.LOGIN_SUCCESS:
				return kHTTPLoginSuccessPath;
			case StompPathId.RECEIVE_NOTIFICATIONS:
				return kReceiveNotificationsPath;
			case StompPathId.SEND_NOTIFICATION_STATUS:
				return kSendNotificationStatus;
			case StompPathId.RECEIVE_NOTIFICATIONS_STATUS:
				return kReceiveNotificationsStatus;
			case StompPathId.UNREAD_COUNT:
				return kUnreadCount;
			case StompPathId.REMOVE_CHAT_ENTRY:
				return kRemoveChatEntry;
			default:
				return kHTTPLoginPath; // Shouldn't be reached.
			}
		}

		public static StompPathId FromString (string path) {
			switch (path) {
			case kBroadcastSendPath:
				return StompPathId.BROADCAST_SEND; 
			case kBroadcastTopicPath:
				return StompPathId.BROADCAST_TOPIC; 
			case kReceiveMessagePath:
				return StompPathId.RECEIVE_MESSAGE; 
			case kSendMessage:
				return StompPathId.SEND_MESSAGE; 
			case kSendMessageUpdate:
				return StompPathId.SEND_MESSAGE_UPDATE; 
			case kSendTyping:
				return StompPathId.SEND_TYPING; 
			case kSendDevice:
				return StompPathId.SEND_DEVICE; 
			case kSendSaveOrUpdateGroup:
				return StompPathId.SEND_SAVE_OR_UPDATE_GROUP; 
			case kSendRegisterContacts:
				return StompPathId.SEND_REGISTER_CONTACTS; 
			case kSendNoNewContacts:
				return StompPathId.SEND_NO_NEW_CONTACTS; 
			case kBroadcastTopicOnlinePath:
				return StompPathId.BROADCAST_TOPIC_ONLINE; 
			case kHTTPLoginPath:
				return StompPathId.LOGIN; 
			case kHTTPLoginSuccessPath:
				return StompPathId.LOGIN_SUCCESS;
			case kReceiveNotificationsPath:
				return StompPathId.RECEIVE_NOTIFICATIONS;
			case kSendNotificationStatus:
				return StompPathId.SEND_NOTIFICATION_STATUS;
			case kReceiveNotificationsStatus:
				return StompPathId.RECEIVE_NOTIFICATIONS_STATUS;
			case kUnreadCount:
				return StompPathId.UNREAD_COUNT;
			case kRemoveChatEntry:
				return StompPathId.REMOVE_CHAT_ENTRY;
			default:
				return StompPathId.LOGIN; // Shouldn't be reached.
			}
		}

		public static int StompIdFromPath (string path) {
			StompPathId id = StompPath.FromString (path);
			return (int)id;
		}
	}

}