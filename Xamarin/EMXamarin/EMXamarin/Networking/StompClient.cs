using System;
using EMXamarin;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace em {
	// https://stomp.github.io/stomp-specification-1.1.html
	public enum StompAckMode { StompAckModeAuto, StompAckModeClient, StompAckModeClientIndividual };

	public class StompClient {
		#region Delegate methods for calling class.
		// Quick way to write some delegate functions using Actions. Probably not the best way to implement the delegate pattern.
		public Action<StompClient> DelegateStompClientDidConnect;
		public Action<StompClient, JObject, Dictionary<string, string>> DelegateStompClientDidReceiveMessage;
		public Action<StompClient, string> DelegateServerDidSendReceipt;
		public Action<StompClient, string, string> DelegateServerDidSendError;
		public Action DelegateSocketIsDisconnected;
		#endregion


		public const string kCommandConnect = "CONNECT";
		public const string kCommandSend = "SEND";
		public const string kCommandSubscribe = "SUBSCRIBE";
		public const string kCommandUnsubscribe = "UNSUBSCRIBE";
		public const string kCommandBegin = "BEGIN";
		public const string kCommandCommit = "COMMIT";
		public const string kCommandAbort = "ABORT";
		public const string kCommandAck = "ACK";
		public const string kCommandDisconnect = "DISCONNECT";
		public const string kAckClient = "client";
		public const string kAckAuto = "auto";
		public const string kAckClientIndividual = "client-individual";
		public const string kResponseHeaderSession = "session";
		public const string kResponseHeaderReceiptId = "receipt-id";
		public const string kResponseHeaderErrorMessage = "message";
		public const string kResponseFrameConnected = "CONNECTED";
		public const string kResponseFrameMessage = "MESSAGE";
		public const string kResponseFrameReceipt = "RECEIPT";
		public const string kResponseFrameError = "ERROR";

		public bool isConnected;

		private WebsocketClient webSocket;
		private bool anonymous;
		private bool autoconnect;
		private string login;
		private string passcode;

		ApplicationModel appModel;

		HeartbeatScheduler scheduler;

		public StompClient (string _url, bool _autoconnect, ApplicationModel appModel) : this (_url, string.Empty, string.Empty, _autoconnect, appModel) {}
		public StompClient (string _url, string _login, string _passcode, ApplicationModel appModel) : this (_url, _login, _passcode, false, appModel) {}
		public StompClient (string _url, string _login, string _passcode, bool _autoconnect, ApplicationModel _appModel) {
			login = _login;
			passcode = _passcode;
			autoconnect = _autoconnect;

			appModel = _appModel;

			WebsocketConnectionFactory factory = appModel.platformFactory.GetWebSocketFactory (login, passcode);
			this.webSocket = new WebsocketClient (factory);
			isConnected = false;

			AttachWebSocket ();

			scheduler = appModel.platformFactory.GetHeartbeatScheduler (this);
			scheduler.OnConnectionTimeout += OnTimeout;

			anonymous = true;
		}

		private void AttachWebSocket () {
			webSocket.BroadcastClientOpening += OnOpening;
			webSocket.BroadcastClientOpen += OnOpened;
			webSocket.BroadcastMessageReceived += OnReceived;
			webSocket.BroadcastPongReceived += OnReceivedPong;
		}

		private void DetachWebSocket () {
			webSocket.BroadcastClientOpening -= OnOpening;
			webSocket.BroadcastClientOpen -= OnOpened;
			webSocket.BroadcastMessageReceived -= OnReceived;
			webSocket.BroadcastPongReceived -= OnReceivedPong;
		}

		private void OnOpening () {
			//Debug.WriteLine ("webSocket.OnSocketOpening");
			scheduler.OnConnecting ();
		}

		private void OnOpened () {
			//Debug.WriteLine ("webSocket.OnSocketOpened");

			this.scheduler.OnConnected ();

			if (autoconnect) {
				SendConnectFrame ();
			}
		}

		private void OnClosed () {
			//Debug.WriteLine ("webSocket.OnSocketClosed");
			isConnected = false;
			scheduler.OnClosed ();
			DelegateSocketIsDisconnected ();
		}

		private void OnReceived (string message) {
			//Debug.WriteLine ("webSocket.OnMessageReceived");
			ReceivedActivity ();
			ReceiveMessage (message);
		}

		private void OnReceivedPong () {
			ReceivedActivity ();
		}

		private void OnTimeout () {
			OnClosed ();

			if (this.appModel.NetworkReachable == false) {
				Debug.WriteLine ("stomp client timeout while network unreachable. Aborting reconnect of underlying WebSocket connection");
				return;
			}

			webSocket.Restart ();
		}

		private void ReceivedActivity () {
			scheduler.OnHeartbeatReceived ();
		}

		private void SendConnectFrame () {
			Dictionary<string, string> defaultHeaders = new Dictionary<string, string> ();
			//defaultHeaders ["heart-beat"] = "10000,10000";
			defaultHeaders ["heart-beat"] = "0,0";
			defaultHeaders ["accept-version"] = "1.1,1.0";

			if (anonymous) {
				SendFrame (kCommandConnect, defaultHeaders, null);
			} else {
				defaultHeaders ["login"] = login;
				defaultHeaders ["passcode"] = passcode;
				SendFrame (kCommandConnect, defaultHeaders, null);
			}
		}

		public void SendHeartbeat () {
			this.webSocket.SendPing ();
		}

		#region sending messages 
		public void SendMessage (string theMessage, string destination) {
			SendMessage (theMessage, destination, new Dictionary<string, string> ());
		}

		public void SendMessage (string theMessage, string destination, int receiptHeaderValue) {
			var headers = new Dictionary<string, string> ();
			headers ["receipt"] = receiptHeaderValue.ToString ();
			SendMessage (theMessage, destination, headers);
		}

		public void SendMessage (string theMessage, string destination, Dictionary<string, string> headers) {
			headers ["destination"] = destination;
			SendFrame (kCommandSend, headers, Encoding.UTF8.GetBytes (theMessage));
		}
		#endregion

		private void SendFrame (string command, Dictionary<string, string> header, byte[] body) {
			string frameString = String.Format ("{0}{1}", command, "\n");

			if (header != null) {
				foreach (string key in header.Keys) {
					frameString += key;
					frameString += ":";
					frameString += header [key];
					frameString += "\n";
				}
			} 

			frameString += "\n";
			if (body != null && body.Length > 0) {
				frameString += "\n";
				frameString += Encoding.UTF8.GetString (body, 0, body.Length);
			}

			frameString += "\0"; // Null terminator for STOMP protocol.

			//Debug.WriteLine ("sendFrame, String: {0}", frameString);
			webSocket.SendMessage (frameString);
		}

		private void SendFrame (string command) {
			SendFrame (command, null, null);
		}

		private void ReceiveMessage (string message) {
			StompFrame frame = ParseFrame (message);
			ReceiveFrame (frame.command, frame.headers, (string)frame.body);
			//Debug.WriteLine (message);
		}

		private void ReceiveFrame (string command, Dictionary<string, string> headers, string body) {
			//Debug.WriteLine ("receiveFrame {0}, {1}, {2}", command, headers.ToString (), body);
			switch (command) {
			case kResponseFrameConnected: {
					isConnected = true;
					DelegateStompClientDidConnect (this);
					break;
				}
			case kResponseFrameMessage: {
					string contentType = headers ["content-type"];
					//Debug.WriteLine ("Content-Type: {0}", contentType);
					if (contentType.Equals ("application/json;charset=UTF-8")) {
						JObject jobject = JObject.Parse (body);
						DelegateStompClientDidReceiveMessage (this, jobject, headers);
					} else {
						string destination = headers ["destination"];

						// handle disconnection signal
						if ("disconnect".Equals(destination)) {
							webSocket.Restart ();
						}

						// TODO: Handle when message should be a string and not a dictionary.
					}
					break;
				}
			case kResponseFrameReceipt: {
					string receiptId = headers [kResponseHeaderReceiptId];
					DelegateServerDidSendReceipt (this, receiptId);
					break;
				}
			case kResponseFrameError: {
					string msg = headers [kResponseHeaderErrorMessage];
					DelegateServerDidSendError (this, msg, body);
					break;
				}
			default: {
					// nothing
					break;
				}
			}
		}

		private StompFrame ParseFrame (string message) {
			StompFrame frame = new StompFrame ();
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			List<string> components = message.Split ('\n').ToList<string>();
			frame.command = components [0];

			int bodyIndex = components.Count - 1;

			for (int i = 1; i < bodyIndex; i++) {
				List<string> keyvalues = components [i].Split (':').ToList<string> ();
				if (keyvalues.Count == 2) {
					// If there's a key and a value.
					string key = keyvalues [0];
					string value = keyvalues [1];
					headers [key] = value;
				}
			}

			frame.headers = headers;

			frame.body = components [bodyIndex]; // TODO: Figure out how to trim the string like in iOS.
			//Debug.WriteLine (frame.command);
			return frame;
		}

		#region broadcast/subscriptions
		public void SubscribeToDestination (string destination) {
			SubscribeToDestination (destination, StompAckMode.StompAckModeClientIndividual);
		}

		public void SubscribeToDestination (string destination, StompAckMode ackMode) {
			string ack = null;
			switch (ackMode) {
			case StompAckMode.StompAckModeClient:
				{
					ack = kAckClient;
					break;
				}
			case StompAckMode.StompAckModeAuto:
				{
					ack = kAckAuto;
					break;
				}
			case StompAckMode.StompAckModeClientIndividual:
				{
					ack = kAckClientIndividual;
					break;
				}
			default:
				{
					break;
					// nothing
				}
			}

			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers ["destination"] = destination;
			headers ["ack"] = ack;
			headers ["id"] = StompPath.StompIdFromPath (destination).ToString (); // Converting a destination to an id (int) and then converting that to a string.
			SendFrame (kCommandSubscribe, headers, null);
		}

		public void SubscribeToDestination (string destination, Dictionary<string, string> headers) {
			headers ["destination"] = destination;
			headers ["id"] = StompPath.StompIdFromPath (destination).ToString ();
			SendFrame (kCommandSubscribe, headers, null);
		}

		public void UnsubscribeFromDestination (string destination) {
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers ["id"] = StompPath.StompIdFromPath (destination).ToString ();
			SendFrame (kCommandUnsubscribe, headers, null);
		}

		#endregion

		public void Begin (string transactionId) {
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers ["transaction"] = transactionId;
			SendFrame (kCommandBegin, headers, null);
		}

		public void Commit (string transactionId) {
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers ["transaction"] = transactionId;
			SendFrame (kCommandCommit, headers, null);
		}

		public void Abort (string transactionId) {
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers ["transaction"] = transactionId;
			SendFrame (kCommandAbort, headers, null);
		}

		public void Ack (string messageId) {
			Dictionary<string, string> headers = new Dictionary<string, string> ();
			headers ["message-id"] = messageId;
			SendFrame (kCommandAck, headers, null);
		}

		public void Start () {
			webSocket.Start ();
		}

		public void Shutdown () {
			scheduler.Stop ();
			webSocket.Stop ();
			OnClosed ();
		}
	}
}