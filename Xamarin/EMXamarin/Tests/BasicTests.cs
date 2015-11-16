using System;
using NUnit.Framework;

using EMXamarin;
using WebSocket4Net;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using SuperSocket.ClientEngine;
using System.Threading.Tasks;
using System.Net.Http;

namespace Tests
{
	[TestFixture]
	public class BasicTests
	{
		string testUsername = "sean@whoshere.net";
		string testPassword = "Whoshere20!!";

		[Test]
		[Ignore ("ignored test")]
		public void APass ()
		{
			Assert.True (true);
		}
			

		[Test]
		public async void ALogin ()
		{
			//bool isLoggedIn = false;

			EMHttpClient httpClient = new EMHttpClient (new TestStreamingClientHandler());
			TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool> ();
			httpClient.DoLogin (testUsername, testPassword,  ((HttpResponseMessage obj) => {
				if ( obj.IsSuccessStatusCode ) {
					//isLoggedIn = true;
					tcs.SetResult(true);
				} else {
					//isLoggedIn = false;
					tcs.SetResult(false);
				}
			}));

			bool isLoggedIn = await tcs.Task;

			Assert.True (isLoggedIn);
		}

		[Test]
		[Ignore ("ignored test")]
		public void BConnect ()
		{
			StompClient stompClient = new StompClient (StompPath.kHTTPLoginPath, testUsername, testPassword, true, new TestWebSocketClient ());

			Assert.True (true);
		}

	}

	public class TestWebSocketClient : WebSocketClient {

		WebSocket websocket;

		override public WebSocketClientState SocketState {
			get {
				int num = Convert.ToInt32 (websocket.State);
				return (WebSocketClientState)num;
			}
		}

		public TestWebSocketClient () {
			webSocketAddress = ConnectionInfo.WEBSOCKET_BASE_ADDRESS + "/stomp/websocket";
			didConnect = false;
			websocket = new WebSocket (webSocketAddress, "", WebSocketVersion.Rfc6455);
		}

		override public void Close () {
			websocket.Close ();
		}

		override public void Connect (string username, string password, List<KeyValuePair<string, string>> listcookies) {
			websocket = new WebSocket (webSocketAddress, "", listcookies, null, "", ConnectionInfo.HTTP_BASE_ADDRESS, WebSocketVersion.Rfc6455);
			websocket.Opened += new EventHandler (websocket_Opened);
			websocket.Error += new EventHandler<ErrorEventArgs> (websocket_Error);
			websocket.Closed += new EventHandler (websocket_Closed);
			websocket.MessageReceived += new EventHandler<MessageReceivedEventArgs> (websocket_MessageReceived);
			websocket.Open ();
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
