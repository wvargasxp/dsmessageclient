using System;
using System.Diagnostics;
using System.Timers;
using em;

namespace EMXamarin {
	public class StompClientHeartbeatScheduler : HeartbeatScheduler {

		private static readonly string TAG = "E.h.StompClientHeartbeatScheduler" + " : ";

		private Timer timeoutTimer;

		private Timer heartbeatTimer;

		StompClient heartbeater;

		private bool IsSchedulerEnabled {
			get;
			set;
		}

		private bool HasRecentActivity { get; set; }

		public StompClientHeartbeatScheduler (StompClient heartbeater) {
			this.heartbeater = heartbeater;

			var timeoutHandler = new ElapsedEventHandler ((object sender, ElapsedEventArgs e) => {
				ConnectionTimeoutCheck ();
			});

			this.timeoutTimer = new Timer (Constants.TIMER_INTERVAL_BETWEEN_RECONNECTS);
			this.timeoutTimer.AutoReset = false;
			this.timeoutTimer.Elapsed += timeoutHandler;

			var heartbeatHandler = new ElapsedEventHandler ((object sender, ElapsedEventArgs e) => {
				SendHeartbeat ();
			});

			this.heartbeatTimer = new Timer (Constants.TIMER_INTERVAL_BETWEEN_PINGS);
			this.heartbeatTimer.AutoReset = true;
			this.heartbeatTimer.Elapsed += heartbeatHandler;
		}

		public override void OnConnecting () {
//			Debug.WriteLine (DateTime.UtcNow + " - " + TAG + "OnConnecting ()");
			EMTask.Dispatch (() => {
				this.IsSchedulerEnabled = true;
				this.HasRecentActivity = false;
				this.timeoutTimer.Start ();
			}, EMTask.WEBSOCKET_PINGPONG_QUEUE);
		}

		public override void OnConnected () {
			EMTask.Dispatch (() => {
				this.HasRecentActivity = true;
				this.heartbeatTimer.Start ();
			}, EMTask.WEBSOCKET_PINGPONG_QUEUE);
		}

		public override void OnHeartbeatReceived () {
			//Debug.WriteLine (DateTime.UtcNow + " - " + TAG + "OnHeartbeatReceived ()");
			EMTask.Dispatch (() => {
				this.HasRecentActivity = true;
			}, EMTask.WEBSOCKET_PINGPONG_QUEUE);
		}

		public override void OnClosed () {
			EMTask.Dispatch (() => {
				heartbeatTimer.Stop ();
			}, EMTask.WEBSOCKET_PINGPONG_QUEUE);
		}

		public override void Stop () {
			EMTask.Dispatch (() => {
				this.IsSchedulerEnabled = false;
				this.timeoutTimer.Stop ();
				this.heartbeatTimer.Stop ();
			}, EMTask.WEBSOCKET_PINGPONG_QUEUE);
		}

		public void SendHeartbeat () {
			EMTask.Dispatch (() => {
//				Debug.WriteLine (DateTime.UtcNow + " - " + TAG + "SendHeartbeat ()");
				this.timeoutTimer.Start ();
				this.heartbeater.SendHeartbeat ();
			}, EMTask.WEBSOCKET_PINGPONG_QUEUE);
		}
			
		private void ConnectionTimeoutCheck () {
			EMTask.Dispatch (() => {
//				Debug.WriteLine (DateTime.UtcNow + " - " + TAG + "ConnectionTimeoutCheck ()");
				if (!IsSchedulerEnabled) {
//					Debug.WriteLine (DateTime.UtcNow + " - " + TAG + "ConnectionTimeoutCheck () scheduler disabled");
					return;
				}

				if (!this.HasRecentActivity) {
					OnConnectionTimeout ();
				} 

				this.HasRecentActivity = false;
			}, EMTask.WEBSOCKET_PINGPONG_QUEUE);
		}
	}
}

