using System;

using EMXamarin;
using WebSocket4Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using SuperSocket.ClientEngine;
using System.Threading.Tasks;
using System.Net.Http;
using em;

namespace TestsHeadless
{
	public class TestWebSocketClient : WebSocketClient {

		WebSocket websocket;

		override public WebSocketClientState SocketState {
			get {
				int num = Convert.ToInt32 (websocket.State);
				return (WebSocketClientState)num;
			}
		}

		private int userIndex;

		public TestWebSocketClient (int _userIndex) {
			webSocketAddress = ConnectionInfo.WEBSOCKET_BASE_ADDRESS + "/stomp/websocket";
			didConnect = false;
			websocket = new WebSocket (webSocketAddress, "", WebSocketVersion.Rfc6455);
			userIndex = _userIndex;
		}

		override public void Close () {
			websocket.Close ();
		}

		override public void Connect (string username, string password) {
			ApplicationModel appModel = TestUserDB.GetUserAtIndex (userIndex).appModel;
			appModel.account.httpClient.GetCookiesForWebSocketConnectionAsync ((List<KeyValuePair<string, string>> listcookies) => {
				websocket = new WebSocket (webSocketAddress, "", listcookies, null, "", ConnectionInfo.HTTP_BASE_ADDRESS, WebSocketVersion.Rfc6455);
				websocket.Opened += new EventHandler (websocket_Opened);
				websocket.Error += new EventHandler<ErrorEventArgs> (websocket_Error);
				websocket.Closed += new EventHandler (websocket_Closed);
				websocket.MessageReceived += new EventHandler<MessageReceivedEventArgs> (websocket_MessageReceived);
				websocket.Open ();
			});
		}

		override public void SendMessage (string frame) {
			websocket.Send (frame);
		}

		private void websocket_Opened(object sender, EventArgs e) {
			didConnect = true;
			OnSocketOpened ();
		}

		private void websocket_Closed(object sender, EventArgs e) {
			Debug.WriteLine ("websocket closed");
			OnSocketClosed ();
		}

		private void websocket_MessageReceived(object sender, MessageReceivedEventArgs e) {
			OnMessageReceived (e.Message);
		}

		private void websocket_Error(object sender, ErrorEventArgs e) {
			// Extracts the exception from the ErrorEventArgs and display it.
			Exception myReturnedException = e.Exception;
			Debug.WriteLine("The returned exception is: " + myReturnedException.Message);
			OnSocketError ();
		}
	}
}

