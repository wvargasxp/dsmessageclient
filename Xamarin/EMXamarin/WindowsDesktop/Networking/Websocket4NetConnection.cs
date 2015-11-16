using System;
using System.Collections.Generic;
using System.Diagnostics;
using em;
using WebSocket4Net;

namespace WindowsDesktop.Networking {
	public class Websocket4NetConnection : WebsocketConnection {

		static readonly string TAG = "E.h.Websocket4NetConnection" + " : ";

		private WebSocket _connection;

		private object _websocketLock = new object ();

		public Websocket4NetConnection (string webSocketAddress, List<KeyValuePair<string, string>> headers) {
			_connection = new WebSocket (webSocketAddress, string.Empty, new List<KeyValuePair<string, string>> (), headers, string.Empty, AppEnv.WEBSOCKET_BASE_ADDRESS, WebSocketVersion.Rfc6455);
			_connection.Opened += WebSocketOpened;
			_connection.Error += WebsocketError;
			_connection.MessageReceived += WebsocketMessageReceived;
		}

		public override void Connect () {
			lock (_websocketLock) {
				try {
					_connection.Open ();
				} catch (Exception e) {
					Debug.WriteLine (TAG + "cannot do connect operation {0}", e);
				}
			}
		}

		public override void Disconnect () {
			lock (_websocketLock) {
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
				_connection.Send (message);
			} catch (Exception e) {
				Debug.WriteLine (TAG + "{0}", e);
			}
		}

		public override void Dispose () {
			try {
				_connection.Opened -= WebSocketOpened;
				_connection.Error -= WebsocketError;
				_connection.MessageReceived -= WebsocketMessageReceived;

				_connection.Close ();

				_connection.Dispose ();

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