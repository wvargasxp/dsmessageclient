using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace em {
	using emExtension;

	public class LiveServerConnection {
		public static readonly String NOTIFICATION_DID_CONNECT_WEBSOCKET = "NOTIFICATION_DID_CONNECT_WEBSOCKET";

		public const int TYPING_ANTI_SPAM_DELAY_MILLIS = 700;
		ApplicationModel appModel;

		// AppModel can use this as a callback for when the live server connection either obtains a connection or fails to get a connection.
		public Action<bool> DelegateLiveServerHasConnection;

		// callback for when the live server re-establishes a connection.
		public Action DelegateLiveServerDidReconnect;

		static object liveServerLock = new object ();

		protected StompClient client;

		private static LiveServerConnection singleton = null;
		private static object singletonLock = new object();

		private object lossLastConnectionLock = new object ();
		private bool lossLastConnection;

		public bool stompClientIsConnected() {
			return client.isConnected;
		}

		private LiveServerConnection (string username, string password, ApplicationModel appModel, Action<bool> liveServerHasConnectionCallback, Action liveServiceDidReconnectCallback) {
			this.appModel = appModel;
			this.lossLastConnection = false;
			this.DelegateLiveServerHasConnection = liveServerHasConnectionCallback;
			this.DelegateLiveServerDidReconnect = liveServiceDidReconnectCallback;
			client = new StompClient (StompPath.kHTTPLoginPath, username, password, true, appModel);

			client.DelegateStompClientDidConnect += (StompClient stompService) => {
				SubscribeBroadcasts ();

				lock (lossLastConnectionLock) {
					if (lossLastConnection) {
						lossLastConnection = false;
						EMTask.DispatchMain (() => {
							DelegateLiveServerDidReconnect ();
						});
					}
				}

				EMTask.DispatchMain (() => {
					DelegateLiveServerHasConnection (true);
				});

				NotificationCenter.DefaultCenter.PostNotification(this, NOTIFICATION_DID_CONNECT_WEBSOCKET);
			};

			client.DelegateStompClientDidReceiveMessage += (StompClient stompService, JObject message, Dictionary<string, string> headers) =>  {
				//Debug.WriteLine ("LiveServerConnection-DelegateStompClientDidReceiveMessage-Thread id is " + Thread.CurrentThread.ManagedThreadId);
				EMTask.Dispatch (() => {
					appModel.HandleMessage(message);
				}, EMTask.HANDLE_MESSAGE_QUEUE);

				EMTask.Dispatch (() => {
					if (stompService != null)
						stompService.Ack (headers["message-id"]);
				}, EMTask.LIVE_SERVER_QUEUE);
			};

			client.DelegateServerDidSendReceipt += (StompClient stompService, string receiptId) => {
				EMTask.Dispatch (() => {
					appModel.outgoingQueue.AckQueueEntry (receiptId);
				}, EMTask.OUTGOING_QUEUE_QUEUE);
			};

			client.DelegateServerDidSendError += (StompClient stompService, string shortError, string fullError) => {
				Debug.WriteLine("DidSendError received");
			};
				
			client.DelegateSocketIsDisconnected += () => {
				lossLastConnection = true;

				EMTask.DispatchMain (() => {
					DelegateLiveServerHasConnection (false);
				});
			};
		}

		public static LiveServerConnection GetInstance (string username, string password, ApplicationModel appModel, Action<bool> liveServerHasConnectionCallback, Action liveServiceDidReconnectCallback) {
			lock (singletonLock) {
				if (singleton == null)
					singleton = new LiveServerConnection (username, password, appModel, liveServerHasConnectionCallback, liveServiceDidReconnectCallback);
			}

			return singleton;
		}

		public void SubscribeBroadcasts () {
			lock (liveServerLock) {
				if (stompClientIsConnected()) {
					client.SubscribeToDestination (StompPath.kBroadcastTopicPath);
					client.SubscribeToDestination (StompPath.kReceiveMessagePath);
					client.SubscribeToDestination (StompPath.kBroadcastTopicOnlinePath);
					client.SubscribeToDestination (StompPath.kReceiveNotificationsPath);
					client.SubscribeToDestination (StompPath.kReceiveNotificationsStatus);
				}
			}
		}

		public void SendDeviceDetails (Dictionary<string, string> details) {
			string json = JsonConvert.SerializeObject (details);
			SendDeviceDetails (json);
		}

		public void SendDeviceDetailsAsync (string details) {
			EMTask.Dispatch (() => SendDeviceDetails (details), EMTask.LIVE_SERVER_QUEUE);
		}

		private void SendDeviceDetails (string details) {
			ClientSendMessage (details, StompPath.kSendDevice);
		}

		public void SendToDestinationAsync (string message, string destination, int messageID) {
			EMTask.Dispatch (() => SendToDestination (message, destination, messageID), EMTask.LIVE_SERVER_QUEUE);
		}

		public void SendInstalledAppsAsync(InstalledAppsOutbound installedApps) {
			EMTask.Dispatch (() => SendInstalledApps (installedApps), EMTask.LIVE_SERVER_QUEUE);
		}

		protected void SendInstalledApps(InstalledAppsOutbound installedApps) {
			string json = JsonConvert.SerializeObject (installedApps);
			ClientSendMessage(json, StompPath.kInstalledApps);
		}

		public void SendNoWhosHereAppToBindToAsync() {
			EMTask.Dispatch (() => SendNoWhosHereAppToBindTo (), EMTask.LIVE_SERVER_QUEUE);
		}

		protected void SendNoWhosHereAppToBindTo() {
			string json = "{ }";
			ClientSendMessage(json, StompPath.kNoWhosHereAppToBindTo);
		}

		public void SendCancelWhoshereBind() {
			// message is not important just including a payload
			string json = "{ \"action\" : \"stop\" }";
			ClientSendMessage(json, StompPath.kCancelWhosHereBind);
		}

		private void SendToDestination (string message, string destination, int messageID) {
			ClientSendMessage (message, destination, messageID);
		}

		public void SendUnreadCountAsync (int unreadCount) {
			EMTask.Dispatch (() => SendUnreadCount (unreadCount), EMTask.LIVE_SERVER_QUEUE);
		}

		private void SendUnreadCount (int unreadCount) {
			UnreadCountOutbound uco = new UnreadCountOutbound ();
			uco.unreadCount = unreadCount;
			string json = JsonConvert.SerializeObject (uco);
			ClientSendMessage (json, StompPath.kUnreadCount);
		}

		public void ClientSendMessage (string json, string stompPath) {
			lock (liveServerLock) {
				if (stompClientIsConnected()) {
					client.SendMessage (json, stompPath);
				}
			}
		}

		public void ClientSendMessage (string json, string stompPath, int headerValue) {
			lock (liveServerLock) {
				if (stompClientIsConnected()) {
					client.SendMessage (json, stompPath, headerValue);
				}
			}
		}

		Timer noSpamWithTypingMessagesTimer;
		object typingLock = new object ();
		public void SendTypingMessageTo (ChatEntry to) {
			lock (typingLock) {
				if (noSpamWithTypingMessagesTimer == null) {
					if (to.contacts == null || to.contacts.Count < 1)
						return;

					var destination = new List<string> ();
					foreach (Contact contact in to.contacts)
						destination.Add (contact.serverID);

					TypingOutbound typingOutbound = new TypingOutbound ();
					typingOutbound.destination = destination;
					typingOutbound.fromAlias = to.fromAlias;

					string json = JsonConvert.SerializeObject (typingOutbound);

					EMTask.Dispatch (() => {
						ClientSendMessage (json, StompPath.kSendTyping);
					}, EMTask.LIVE_SERVER_QUEUE);

					ScheduleNoSpamTimer ();
				}
			}
		}

		protected void ScheduleNoSpamTimer() {
			noSpamWithTypingMessagesTimer = new Timer ((object o) => {
				lock (typingLock) {
					noSpamWithTypingMessagesTimer = null;
				}
			}, null, TYPING_ANTI_SPAM_DELAY_MILLIS, Timeout.Infinite);
		}

		public void SendNotificationStatusUpdateAsync (int notification, NotificationStatus status) {
			EMTask.Dispatch (() => SendNotificationStatusUpdate (notification, status), EMTask.LIVE_SERVER_QUEUE);
		}

		private void SendNotificationStatusUpdate (int notification, NotificationStatus status) {
			var outbound = new NotificationStatusOutbound ();
			outbound.add (notification, status);
			string json = JsonConvert.SerializeObject (outbound);
			ClientSendMessage (json, StompPath.kSendNotificationStatus);
		}

		public void Start () {
			client.Start ();
		}

		public void Shutdown () {
			client.Shutdown ();
		}
	}
}