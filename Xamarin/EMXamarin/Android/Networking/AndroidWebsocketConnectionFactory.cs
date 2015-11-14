//#define WEBSOCKET_AUTOBAHN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using em;
using EMXamarin;
using Org.Apache.Http.Message;
using Websocket;

namespace Emdroid {
	public class AndroidWebsocketConnectionFactory : WebsocketConnectionFactory {
		string webSocketAddress;

		private IDeviceInfo DeviceInfo { get; set; }

		private string AuthorizationString { get; set; }

		private string DeviceInformationString {
			get {
				return DeviceInfo.DeviceBase64String ();
			}
		}

		public AndroidWebsocketConnectionFactory (string username, string password, IDeviceInfo deviceInfo) {
			this.DeviceInfo = deviceInfo;
			this.AuthorizationString = "Basic " + Convert.ToBase64String (Encoding.UTF8.GetBytes(username + ":" + password));
			Debug.WriteLine (String.Format ("setting basic auth to websocket connection request = " + this.AuthorizationString));
		}

		public WebsocketConnection BuildConnection () {

			webSocketAddress = AppEnv.WEBSOCKET_BASE_ADDRESS + "/stomp/websocket";

			#if WEBSOCKET_AUTOBAHN
			WebSocketOptions options;
			List<BasicNameValuePair> headers = new List<BasicNameValuePair> ();
			headers.Add(new BasicNameValuePair("Authorization", "Basic " + this.AuthorizationString));
			headers.Add(new BasicNameValuePair("deviceInformation", this.DeviceInformationString));
			options = new WebSocketOptions();
			#else
			List<KeyValuePair<String, String>> headers = new List<KeyValuePair<String, String>>();
			headers.Add (new KeyValuePair<string, string> ("Authorization", this.AuthorizationString));
			headers.Add (new KeyValuePair<string, string> ("deviceInformation", this.DeviceInformationString));
			#endif

			#if WEBSOCKET_AUTOBAHN
			return new AutobahnWebsocketConnection (webSocketAddress, headers, options);
			#else
			return new AndroidAsyncWebsocketConnection (webSocketAddress, headers);
			//return new Websocket4NetConnection (webSocketAddress, headers);
			#endif
		}
	}
}

