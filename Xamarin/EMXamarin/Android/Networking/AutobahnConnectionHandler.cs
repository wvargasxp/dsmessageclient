using System;
using System.Diagnostics;
using Java.Lang;
using Websocket;

namespace Emdroid {
	public class AutobahnConnectionHandler : Java.Lang.Object, IConnectionHandler {

		private static readonly string TAG = "E.h.AutobahnConnectionHandler" + " : ";

		private AutobahnWebsocketConnection client;

		public AutobahnConnectionHandler (AutobahnWebsocketConnection client) {
			this.client = client;
		}

		public bool IsDisposed {
			get;
			set;
		}

		public void OnClose(int code, string reason) {
			if (IsDisposed) {
				return;
			}

			Debug.WriteLine (TAG + "onClose code={0}; reason={1}", code, reason);

		}

		public void OnOpen() {
			if (IsDisposed) {
				return;
			}

			Debug.WriteLine (TAG + "onOpen");
			client.BroadcastSocketOpened ();
		}

		public void OnBinaryMessage(byte[] bytes) {
			if (IsDisposed) {
				return;
			}

		}

		public void OnTextMessage(string message) {
			if (IsDisposed) {
				return;
			}

			Debug.WriteLine (TAG + "onReceive {0}", message);
			client.BroadcastMessageReceived (message);
		}

		public void OnRawTextMessage(byte[] bytes) {
			if (IsDisposed) {
				return;
			}
		}
	}
}

