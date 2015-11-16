using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Threading;

namespace em {
	public class WebsocketClient {
		private const string Tag = "WebsocketClient: ";

		private ClientState State {
			get;
			set;
		}
			
		private WebsocketConnectionFactory connFactory;
		private WebsocketConnection connection;

		// These are implemented/used by the calling class.
		public Action BroadcastClientOpening = () => {};
		public Action BroadcastClientOpen = () => {};
		public Action<string> BroadcastMessageReceived = (message) => {};
		public Action BroadcastPongReceived = () => {};

		public WebsocketClient (WebsocketConnectionFactory connFactory) {
			this.connFactory = connFactory;
			this.State = ClientState.Disconnected;
			this.connection = connFactory.BuildConnection ();
		}

		public WebsocketClient () {}

		public void Start () {
			EMTask.Dispatch (InnerStart, EMTask.WEBSOCKET_CLIENT_QUEUE);
		}

		private void InnerStart () {
			if (this.State == ClientState.Connected || this.State == ClientState.Connecting) {
				return;
			}

			this.State = ClientState.Connecting;
			connection = connFactory.BuildConnection ();
			AttachConnection ();
			connection.Connect ();

			EMTask.DispatchBackground (() => {
				BroadcastClientOpening ();
			});
		}

		public void Stop () {
			EMTask.Dispatch (InnerStop, EMTask.WEBSOCKET_CLIENT_QUEUE);
		}

		private void InnerStop () {
			this.State = ClientState.Disconnected;
			DetachConnection ();
			connection.Disconnect ();
		}

		public void Restart () {
			EMTask.Dispatch (InnerRestart, EMTask.WEBSOCKET_CLIENT_QUEUE);
		}

		/*
		 *  InnerRestart is dispatched inside and added to WEBSOCKET_CLIENT_QUEUE.
		 *  We call Stop, now Stop is dispatched to the queue.
		 *  We then call Start, Start is dispatched to the queue.
		 *  These do not happen synchronously but since they happen on one queue, they happen one right after the other. 
		 */
		private void InnerRestart () {
			Stop ();
			Start ();
		}
			
		public void SendPing () {
			EMTask.Dispatch (InnerSendPing, EMTask.WEBSOCKET_CLIENT_QUEUE);
		}

		/* can fail silently */
		private void InnerSendPing () {
			if (this.State != ClientState.Connected) {
				Debug.WriteLine ("{0} Websocket not connected while trying to ping server.", Tag);
				return;
			}

			this.connection.Ping ();
		}
			
		public void SendMessage (string message) {
			EMTask.Dispatch (() => { InnerSendMessage (message); }, EMTask.WEBSOCKET_CLIENT_QUEUE);
		}

		/* can fail silently */
		private void InnerSendMessage (string message) {
			if (this.State != ClientState.Connected) {
				Debug.WriteLine ("{0} Websocket not connected while trying to send message: {1}", Tag, message);
				return;
			}

			this.connection.Send (message);
		}

		private void AttachConnection () {
			this.connection.BroadcastSocketOpened += ReceiveEventSocketOpen;
			this.connection.BroadcastMessageReceived += ReceiveEventMessageReceived;
			this.connection.BroadcastPongReceived += ReceiveEventPongReceived;
		}

		private void DetachConnection () {
			this.connection.BroadcastSocketOpened -= ReceiveEventSocketOpen;
			this.connection.BroadcastMessageReceived -= ReceiveEventMessageReceived;
			this.connection.BroadcastPongReceived -= ReceiveEventPongReceived;
		}

		public void ReceiveEventSocketOpen () {
			EMTask.Dispatch (() => {
				this.State = ClientState.Connected;
				EMTask.DispatchBackground (() => {
					BroadcastClientOpen ();
				});
			}, EMTask.WEBSOCKET_CLIENT_QUEUE);
		}

		public void ReceiveEventMessageReceived (string message) {
			EMTask.Dispatch (() => {
				BroadcastMessageReceived (message);
			}, EMTask.WEBSOCKET_RECEIVE_MESSAGE_QUEUE);
		}

		public void ReceiveEventPongReceived () {
			EMTask.DispatchBackground (() => {
				BroadcastPongReceived ();
			});
		}

		private enum ClientState {
			Disconnected,
			Connecting,
			Connected
		}
	}
}