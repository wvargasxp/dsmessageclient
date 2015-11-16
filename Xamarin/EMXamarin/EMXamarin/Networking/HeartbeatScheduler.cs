using System;

namespace em {
	public abstract class HeartbeatScheduler {
		public Action OnConnectionTimeout = () => {};

		public abstract void OnConnecting ();
		public abstract void OnConnected ();
		public abstract void OnHeartbeatReceived ();
		public abstract void OnClosed ();
		public abstract void Stop ();
	}
}