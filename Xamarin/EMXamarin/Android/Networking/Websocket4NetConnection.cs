using System;
using System.Collections.Generic;
using System.Diagnostics;
using em;
using WebSocket4Net;

namespace Emdroid {
	public class Websocket4NetConnection : WebsocketConnection {

		static readonly string TAG = "E.h.Websocket4NetConnection" + " : ";

		WebSocket connection;

		object websocketLock = new object ();

		//AutobahnConnectionHandler connectionHandler;

		public Websocket4NetConnection (string webSocketAddress, List<KeyValuePair<String, String>> headers) {
			connection = new WebSocket (webSocketAddress, "", null, headers, "", AppEnv.WEBSOCKET_BASE_ADDRESS, WebSocketVersion.Rfc6455);
			connection.Opened += WebSocketOpened;
			connection.Error += WebsocketError;
			connection.MessageReceived += WebsocketMessageReceived;
		}

		public override void Connect () {
			lock (websocketLock) {
				try {
					connection.Open ();
				} catch (Exception e) {
					Debug.WriteLine (TAG + "cannot do connect operation {0}", e);
				}
			}
		}

		public override void Disconnect () {
			lock (websocketLock) {
				try {
					Dispose ();
				} catch (Exception e) {
					Debug.WriteLine (TAG + "cannot do disconnect operation {0}", e);
				}
			}
		}

		public override void Ping () {
			throw new NotImplementedException ();
		}

		public override void Send (string message) {
			try {
				connection.Send (message);
			} catch (Exception e) {
				Debug.WriteLine (TAG + "{0}", e);
			}
		}

		public override void Dispose () {
			try {
				connection.Opened -= WebSocketOpened;
				connection.Error -= WebsocketError;
				connection.MessageReceived -= WebsocketMessageReceived;

				connection.Close ();

				connection.Dispose ();

				Debug.WriteLine (TAG + "disposed");
			} catch (Exception e) {
				Debug.WriteLine (TAG + "{0}", e);
			}
		}

		void WebSocketOpened (object sender, EventArgs e) {
			BroadcastSocketOpened ();
		}

		void WebsocketError (object sender, SuperSocket.ClientEngine.ErrorEventArgs e) {
			Debug.WriteLine ("Websocket4NetConnection WebsocketError " + e.Exception.Message);
		}

		void WebsocketMessageReceived (object sender, MessageReceivedEventArgs e) {
			BroadcastMessageReceived (e.Message);
		}
	}
}