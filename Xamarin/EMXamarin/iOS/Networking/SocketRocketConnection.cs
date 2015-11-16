using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Timers;
using EMXamarin;
using em;
using SocketRocket;
using UIKit;
using Foundation;

namespace iOS {
	public class SocketRocketConnection : WebsocketConnection {
		protected SRWebSocket Connection { get; set; }
		protected string Username { get; set; }
		protected string Password { get; set; }
		protected IDeviceInfo DeviceInfo { get; set; }

		object websocketLock = new object ();
		protected object WebsocketLock { get { return websocketLock; } }

		public SocketRocketConnection (NSMutableUrlRequest request) {
			this.Connection = new SRWebSocket (request);
		}

		#region delegates
		private void WebSocketOpened (object sender, EventArgs e) {
			Debug.WriteLine ("websocket opened");
			BroadcastSocketOpened ();
		}

		private void WebsocketMessageReceived (object sender, SRMessageReceivedEventArgs e) {
			string message = e.Message.ToString ();
			BroadcastMessageReceived (message);
		}
			
		private void WebsocketError (object sender, SRErrorEventArgs e) {
			Debug.WriteLine ("WebsocketError is " + e.Err.Description);
		}

		private void WebSocketPongReceived (object sender, SRPongEventArgs e) {
			if (e == null) {
				Debug.WriteLine ("Received pong but e is null");
			} else {
				NSData payload = e.PongPayload;
				try {
					NSString payloadStr = NSString.FromData (payload, NSStringEncoding.UTF8);
					if (payloadStr != null) {
						string payloadString = payloadStr.ToString ();
						if (payloadString.Equals (WebsocketConnection.PingMessage)) {
							BroadcastPongReceived ();
						} else {
							Debug.WriteLine ("Received pong but payload does not match");
						}
					}
				} catch (Exception ex) {
					Debug.WriteLine ("Received pong but exception was thrown. {0}", ex);
				}
			}
		}

		#endregion

		public override void Connect () {
			try {
				this.Connection.Opened += WebSocketOpened;
				this.Connection.MessageReceived += WebsocketMessageReceived;
				this.Connection.Error += WebsocketError;
				this.Connection.DidReceivePong += WebSocketPongReceived;

				this.Connection.Open ();
			} catch (Exception e) {
				Debug.WriteLine ("cannot open SocketRocket connection {0}", e);
			}
		}

		public override void Ping () {
			try {
				if (this.Connection.ReadyState == SRReadyState.SR_OPEN) {
					this.Connection.SendPing (NSData.FromString (WebsocketConnection.PingMessage));
				}
			} catch (Exception e) {
				Debug.WriteLine ("cannot send SocketRocket frame {0}", e);
			}
		}
			
		public override void Send (string frame) {
			try {
				if (this.Connection.ReadyState == SRReadyState.SR_OPEN) {
					this.Connection.Send (new NSString (frame));
				}
			} catch (Exception e) {
				Debug.WriteLine ("cannot send SocketRocket frame {0}", e);
			}
		}

		public override void Disconnect () {
			Dispose ();
		}

		public override void Dispose () {
			try {
				this.Connection.Opened -= WebSocketOpened;
				this.Connection.MessageReceived -= WebsocketMessageReceived;
				this.Connection.Error -= WebsocketError;
				this.Connection.DidReceivePong -= WebSocketPongReceived;

				this.Connection.Close ();
				this.Connection.Dispose ();
			} catch (Exception e) {
				Debug.WriteLine ("cannot close and/or dispose SocketRocket {0}", e);
			} finally {
				Debug.WriteLine ("SocketRocket : disposed");
			}
		}
	}
}