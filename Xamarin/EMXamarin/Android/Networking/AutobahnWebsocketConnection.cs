using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Timers;
using EMXamarin;
using WebSocket4Net;
using Websocket;
using Org.Apache.Http.Message;
using em;

namespace Emdroid {
	public class AutobahnWebsocketConnection : WebsocketConnection {

		private static readonly string TAG = "E.h.AutobahnWebsocketConnection" + " : ";

		WebSocketConnection connection;

		object websocketLock = new object ();

		string webSocketAddress;
		WebSocketOptions options;
		List<BasicNameValuePair> headers;

		AutobahnConnectionHandler connectionHandler;

		public AutobahnWebsocketConnection (string webSocketAddress, List<BasicNameValuePair> headers, WebSocketOptions options) {
			this.webSocketAddress = webSocketAddress;
			this.headers = headers;
			this.options = options;

			this.connectionHandler = new AutobahnConnectionHandler (this);
		}

		public override void Connect () {
			lock (websocketLock) {
				try {
					EMTask.DispatchMain (() => {
						connection = new WebSocketConnection ();

						connection.Connect(webSocketAddress, new String[] {"Rfc6455"}, connectionHandler, options, headers);
					});
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
					// Debug.WriteLine (TAG + "cannot do disconnect operation {0}", e);
				}
			}
		}

		public override void Ping () {
			throw new NotImplementedException ();
		}

		public override void Send (string frame) {
			try {
				connection.SendTextMessage (frame);
			} catch (Exception e) {
				// Debug.WriteLine (TAG + "{0}", e);
			}
		}

		public override void Dispose () {
			try {
				connection.Disconnect ();

				connection.Dispose ();

				Debug.WriteLine (TAG + "disposed");
			} catch (Exception e) {

			}
		}


	}
}