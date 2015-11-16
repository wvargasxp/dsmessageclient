using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;
using System.Diagnostics;
using WindowsDesktop.Networking;

namespace WindowsDesktop.PlatformImpl {
	class WindowsDesktopWebsocketConnectionFactory : WebsocketConnectionFactory {

		private List<KeyValuePair<String, String>> Headers { get; set; }
		private string WebsocketAddress { get; set; }

		public WindowsDesktopWebsocketConnectionFactory (string username, string password, IDeviceInfo deviceInfo) {
			this.Headers = new List<KeyValuePair<string, string>> ();

			var authBase64Token = Convert.ToBase64String (Encoding.UTF8.GetBytes(username + ":" + password));
			string deviceInformationJson = deviceInfo.DeviceJSONString ();
			string base64DeviceInformation = Convert.ToBase64String (Encoding.UTF8.GetBytes (deviceInformationJson));

			Debug.WriteLine (String.Format ("adding basic auth to websocket connection request = " + authBase64Token));
			this.WebsocketAddress = AppEnv.WEBSOCKET_BASE_ADDRESS + "/stomp/websocket";

			this.Headers.Add (new KeyValuePair<string, string> ("Authorization", "Basic " + authBase64Token));
			this.Headers.Add (new KeyValuePair<string, string> ("deviceInformation", base64DeviceInformation));
		}

		public WebsocketConnection BuildConnection () {
			return new Websocket4NetConnection (this.WebsocketAddress, this.Headers);
		}
	}
}
