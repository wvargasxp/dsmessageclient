using System;

using NUnit.Framework;

namespace em {

	[TestFixture]
	public class WebSocketTest {

		WebsocketClient client = null;

		static TicksHeartbeatSchedulerMock ticksHeartbeatSchedulerMock;

		static HeartbeatListener heartbeatListener;

		/* client events */
		static bool onOpeningEventPropagated;
		static bool onOpenedEventPropagated;
		static bool onClosedEventPropagated;

		/* protocol events */
		static bool onTimeoutEventPropagated;
		static bool onHeartbeatEventPropagated;

		[SetUp]
		public void SetUp () {

			ResetEventFlags ();

			heartbeatListener = null;
		}

		private void  ResetEventFlags() {
			onOpeningEventPropagated = false;
			onOpenedEventPropagated = false;
			onClosedEventPropagated = false;

			onTimeoutEventPropagated = false;
			onHeartbeatEventPropagated = false;
		}

		private void InstrumentClientForTest () {
			client.BroadcastClientOpen += () => {
				onOpenedEventPropagated = true;
			};

			client.BroadcastClientOpening += () => {
				onOpeningEventPropagated = true;
			};
		}

		/* -------------------------------- Cases -------------------------------- */

		[Test]
		public void SuccessfulConnectShouldFireOnSocketOpenedEventToClient () {
			client = new WebsocketClient (new WebsocketConnectionFactoryMock ("AlwaysSuccess"));
			InstrumentClientForTest ();

			client.Start ();

			AssertEventsPropagated ("OOcth");
		}

		[Test]
		public void FailingToConnectShouldNotFireEventsToClient () {
			client = new WebsocketClient (new WebsocketConnectionFactoryMock ("AlwaysFail"));
			InstrumentClientForTest ();

			client.Start ();

			AssertEventsPropagated ("Oocth");
		}

		[Test]
		public void ForcedDisconnectShouldNotFireClosedEventToClient ()  {
			client = new WebsocketClient (new WebsocketConnectionFactoryMock ("AlwaysSuccess"));
			InstrumentClientForTest ();

			client.Start ();

			client.Stop ();

			AssertEventsPropagated ("OOcth");
		}

		[Test]
		public void ConnectionTimeoutShouldFireTimeoutEventFromScheduler () {
			client = new WebsocketClient (new WebsocketConnectionFactoryMock ("AlwaysFail"));
			InstrumentClientForTest ();

			long timeoutDuration = 5;

			ticksHeartbeatSchedulerMock = new TicksHeartbeatSchedulerMock (timeoutDuration);
			heartbeatListener = new HeartbeatListener (ticksHeartbeatSchedulerMock);

			ticksHeartbeatSchedulerMock.OnConnecting ();
			client.Start ();

			AssertEventsPropagated ("Oocth");

			ticksHeartbeatSchedulerMock.AttemptMoveCurrentTime (3);

			AssertEventsPropagated ("Oocth");

			ticksHeartbeatSchedulerMock.AttemptMoveCurrentTime (10);

			AssertEventsPropagated ("OocTh");
		}

		public void ConnectionSuccessShouldNotFireTimeoutEventFromScheduler () {
			client = new WebsocketClient (new WebsocketConnectionFactoryMock ("AlwaysSuccess"));
			InstrumentClientForTest ();

			long timeoutDuration = 5;

			ticksHeartbeatSchedulerMock = new TicksHeartbeatSchedulerMock (timeoutDuration);
			heartbeatListener = new HeartbeatListener (ticksHeartbeatSchedulerMock);

			client.Start ();

			AssertEventsPropagated ("Oocth");

			ticksHeartbeatSchedulerMock.AttemptMoveCurrentTime (10);

			AssertEventsPropagated ("Oocth");
			//GivenClientIsConnected ();
			//WhenHeartbeatTimesout ();

			//ThenClientRecreatesConnection ();

		}

		//HeartbeatTimeoutShouldFireTimeoutEventFromScheduler

		/* -------------------------------- DSL -------------------------------- */

		private void AssertEventsPropagated (string expected) {
			Assert.AreEqual (expected, GetCurrentEventState());
		}

		private string GetCurrentEventState () {
			string state = "";
			
			state += onOpeningEventPropagated ? "O" : "o";
			state += onOpenedEventPropagated ? "O" : "o";
			state += onClosedEventPropagated ? "C" : "c";
			state += onTimeoutEventPropagated ? "T" : "t";
			state += onHeartbeatEventPropagated ? "H" : "h";

			return state;
		}

		/* -------------------------------- Mocks -------------------------------- */

		private class WebsocketConnectionFactoryMock : WebsocketConnectionFactory {
			private string connectionType;
			public WebsocketConnectionFactoryMock (string connectionType) {
				this.connectionType = connectionType;
			}

			public WebsocketConnection BuildConnection () {
				switch (connectionType) {
				case "AlwaysSuccess" :
					return new AlwaysSuccessConnectionMock ();
				case "AlwaysFail" :
					return new AlwaysFailConnectionMock ();
				default :
					return null;
				}
			}

		}

		private abstract class ConnectionMock : WebsocketConnection {
			public override void Connect () {
				BroadcastSocketOpened ();
			}
			public override void Disconnect () {
			}
			public override void Send (string message) {
			}
			public override void Dispose () {
			}

			public override void Ping () {}
		}

		private class AlwaysSuccessConnectionMock : ConnectionMock {
		}

		private class AlwaysFailConnectionMock : ConnectionMock {
			public override void Connect () {
			}
		}

		private class TicksHeartbeatSchedulerMock : HeartbeatScheduler {
			private long timeoutMillis;

			private long currentTime;

			private long connectingTimestamp;
			private long connectedTimestamp;

			public TicksHeartbeatSchedulerMock (long timeoutMillis) {
				this.timeoutMillis = timeoutMillis;
			}

			public override void OnConnecting () {
				connectingTimestamp = currentTime;
			}

			public override void OnConnected () {
				connectedTimestamp = currentTime;
			}

			public override void OnClosed () {

			}

			public override void OnHeartbeatReceived () {

			}

			public override void Stop () {
			}

			public void SetCurrentTime (long newTime) {
				currentTime = newTime;
			}

			public void AttemptMoveCurrentTime (long newTime) {
				long timeoutDate = connectingTimestamp + timeoutMillis;

				if (newTime >= timeoutDate) {
					if (connectingTimestamp <= connectedTimestamp) {
						OnConnectionTimeout ();
					}
				}

				SetCurrentTime (newTime);
			}
		}

		private class HeartbeatListener {
			public HeartbeatListener (HeartbeatScheduler scheduler) {
				scheduler.OnConnectionTimeout += OnTimeout;
			}

			public void OnTimeout () {
				onTimeoutEventPropagated = true;
			}
			public void SendHeartbeat () {
				onHeartbeatEventPropagated = true;
			}
		}
	}
}

