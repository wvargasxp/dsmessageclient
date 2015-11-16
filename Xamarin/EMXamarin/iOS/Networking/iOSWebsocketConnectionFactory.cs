using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using em;
using Foundation;

namespace iOS {
	public class iOSWebsocketConnectionFactory : WebsocketConnectionFactory {

		private IDeviceInfo DeviceInfo { get; set; }

		private string AuthorizationString { get; set; }

		private string DeviceInformationString {
			get {
				return DeviceInfo.DeviceBase64String ();
			}
		}

		public iOSWebsocketConnectionFactory (string username, string password, IDeviceInfo deviceInfo) {
			this.DeviceInfo = deviceInfo;
			this.AuthorizationString = "Basic " + Convert.ToBase64String (Encoding.UTF8.GetBytes(username + ":" + password));
			Debug.WriteLine (String.Format ("setting basic auth to websocket connection request = " + this.AuthorizationString));
		}

		public WebsocketConnection BuildConnection () {
			string webSocketAddress = AppEnv.WEBSOCKET_BASE_ADDRESS + "/stomp/websocket";
			NSMutableUrlRequest request = new NSMutableUrlRequest (new NSUrl (webSocketAddress));

			NSMutableDictionary d = new NSMutableDictionary ();
			d.Add (new NSString ("Authorization"), new NSString (this.AuthorizationString));
			d.Add (new NSString ("deviceInformation"), new NSString (this.DeviceInformationString));

			request.Headers = new NSDictionary (d);

			return new SocketRocketConnection (request);
		}
	}
}