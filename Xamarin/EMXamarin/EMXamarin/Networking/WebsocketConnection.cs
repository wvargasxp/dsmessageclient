using System;

namespace em {
	public abstract class WebsocketConnection {
		public Action BroadcastSocketOpened = () => {};
		public Action<string> BroadcastMessageReceived = (message) => {};
		public Action BroadcastPongReceived = () => {};

		public abstract void Connect ();
		public abstract void Disconnect ();

		public const string PingMessage = "em_ping"; // No idea if this string is supposed to have meaning.

		public abstract void Send (string message);
		public abstract void Ping ();

		public abstract void Dispose ();
	}
}
