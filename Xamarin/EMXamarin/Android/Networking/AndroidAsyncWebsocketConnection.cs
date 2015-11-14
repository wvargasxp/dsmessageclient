using System;
using em;
using System.Collections.Generic;
using System.Diagnostics;
using Com.Koushikdutta.Async.Http;
using Com.Koushikdutta.Async.Callback;

namespace Emdroid {
	public class AndroidAsyncWebsocketConnection : WebsocketConnection {
		private static readonly string TAG = "E.h.AndroidAsyncWebsocketConnection" + " : ";

        private string wSA = string.Empty;
		private string WebSocketAddress { get { return this.wSA; } set { this.wSA = value; } }

		private List<KeyValuePair<string, string>> Headers { get; set; }

        private object _lock = new object ();
        private object Lock { get { return this._lock; } set { this._lock = value; } }

		private IWebSocket zz;
		protected IWebSocket Socket { 
			get { return zz; }
			set {
				if (zz != null) {
					RemoveCallbacksOnSocket (zz);
				}

				zz = value;
				if (zz != null) {
					zz.ClosedCallback = CloseCallbackImpl.From (this);
					zz.StringCallback = StringCallbackImpl.From (this);
					zz.PongCallback = PongCallbackImpl.From (this);
				}
			}
		}

		public AndroidAsyncWebsocketConnection (string webSocketAddress, 
			List<KeyValuePair<string, string>> headers) {
			this.WebSocketAddress = webSocketAddress;
			this.Headers = headers;
		}

		public override void Connect () {
			lock (this.Lock) {
				try {
					// Workaround
					// https://github.com/koush/AndroidAsync/issues/340
					// TODO: Improve this.
					if (this.WebSocketAddress.Contains ("wss")) {
						this.WebSocketAddress = this.WebSocketAddress.Replace ("wss", "https");
					} else {
						this.WebSocketAddress = this.WebSocketAddress.Replace ("ws", "http");
					}

					//this.WebSocketAddress = "https://ws.emwith.me/stomp/websocket";
					AsyncHttpGet g = new AsyncHttpGet (this.WebSocketAddress);
					foreach (KeyValuePair<string, string> pair in this.Headers) {
						g.AddHeader (pair.Key, pair.Value);
					}

					AsyncHttpClient.DefaultInstance.Websocket (g, null, ConnectCallbackImpl.From (this));

				} catch (Exception e) {
					Debug.WriteLine (TAG + "cannot do connect operation {0}", e);
				}
			}
		}

		public override void Disconnect () {
			lock (this.Lock) {
				try {
					Dispose ();
				} catch (Exception e) {
					Debug.WriteLine (TAG + "cannot do disconnect operation {0}", e);
				}
			}
		}

		public override void Ping () {
			try {
				IWebSocket socket =  this.Socket;
				if (socket != null) {
					socket.Ping (WebsocketConnection.PingMessage);
				} else {
					Debug.WriteLine (TAG + "{0} Socket is null during Ping ()");
				}
			} catch (Exception e) {
				Debug.WriteLine (TAG + "{0}", e);
			}
		}

		public override void Send (string message) {
			try {
				IWebSocket socket =  this.Socket;
				if (socket != null) {
					socket.Send (message);
				} else {
					Debug.WriteLine ("{0} Socket is null during Send ({1})", TAG, message);
				}
			} catch (Exception e) {
				Debug.WriteLine (TAG + "{0}", e);
			}
		}

		public override void Dispose () {
			try {
				IWebSocket socket = this.Socket;
				if (socket != null) {
					RemoveCallbacksOnSocket (socket);
					socket.Close ();
				}

				Debug.WriteLine (TAG + "disposed");
			} catch (Exception e) {
				Debug.WriteLine (TAG + "{0}", e);
			}
		}

		public void WebSocketOpened () {
			BroadcastSocketOpened ();
		}

		private void WebsocketError (Java.Lang.Exception p0) {
			if (p0 != null) {
				Debug.WriteLine (TAG + " WebsocketError " + p0);
			}
		}

		private void WebsocketMessageReceived (string message) {
			BroadcastMessageReceived (message);
		}

		private void WebsocketPongReceived () {
			BroadcastPongReceived ();
		}

		private void RemoveCallbacksOnSocket (IWebSocket socket) {
			if (socket != null) {
				socket.ClosedCallback = null;
				socket.StringCallback = null;
				socket.PongCallback = null;
			}
		}

		private class PongCallbackImpl : Java.Lang.Object, IWebSocketPongCallback {
			AndroidAsyncWebsocketConnection This { get; set; }

			public static PongCallbackImpl From (AndroidAsyncWebsocketConnection connection) {
				PongCallbackImpl self = new PongCallbackImpl ();
				self.This = connection;
				return self;
			}

			public void OnPongReceived (string p0) {
				if (p0 == null) {
					Debug.WriteLine ("AndroidAsyncWebsocketConnection : Received Pong but string was null.");
				} else {
					if (p0.Equals (WebsocketConnection.PingMessage)) {
						//Debug.WriteLine ("AndroidAsyncWebsocketConnection : PongReceived"); // Commented by default, in the good (working) case we'd see too many of these.
						this.This.WebsocketPongReceived ();
					} else {
						Debug.WriteLine ("AndroidAsyncWebsocketConnection : PongReceived but message {0} does not equal PingMessage {1}", p0, WebsocketConnection.PingMessage);
					}
				}
			}
		}

		private class StringCallbackImpl : Java.Lang.Object, IWebSocketStringCallback {
			AndroidAsyncWebsocketConnection This { get; set; }

			public static StringCallbackImpl From (AndroidAsyncWebsocketConnection connection) {
				StringCallbackImpl self = new StringCallbackImpl ();
				self.This = connection;
				return self;
			}

			public void OnStringAvailable (string p0) {
				this.This.WebsocketMessageReceived (p0);
			}
		}

		private class CloseCallbackImpl : Java.Lang.Object, ICompletedCallback {
			AndroidAsyncWebsocketConnection This { get; set; }
			public static CloseCallbackImpl From (AndroidAsyncWebsocketConnection connection) {
				CloseCallbackImpl self = new CloseCallbackImpl ();
				self.This = connection;
				return self;
			}

			public void OnCompleted (Java.Lang.Exception p0) {
				this.This.WebsocketError (p0);
			}
		}

		private class ConnectCallbackImpl : Java.Lang.Object, AsyncHttpClient.IWebSocketConnectCallback {
			AndroidAsyncWebsocketConnection This { get; set; }

			public static ConnectCallbackImpl From (AndroidAsyncWebsocketConnection connection) {
				ConnectCallbackImpl self = new ConnectCallbackImpl ();
				self.This = connection;
				return self;
			}

			public void OnCompleted (Java.Lang.Exception p0, IWebSocket p1) {
				if (p0 == null && p1 != null) {
					this.This.Socket = p1;
					this.This.WebSocketOpened ();
				} else {
					this.This.WebsocketError (p0);
				}
			}
		}

	}
}