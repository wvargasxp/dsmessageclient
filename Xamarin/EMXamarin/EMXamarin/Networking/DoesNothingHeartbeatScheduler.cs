using System;

namespace em {
	public class DoesNothingHeartbeatScheduler : HeartbeatScheduler {
		public override void OnConnecting () {}
		public override void OnConnected () {}
		public override void OnHeartbeatReceived () {}
		public override void OnClosed () {}
		public override void Stop () {}
	}
}