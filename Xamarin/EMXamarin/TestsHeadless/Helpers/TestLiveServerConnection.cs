using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using em;
using EMXamarin;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using emExtension;

namespace TestsHeadless
{
	public class TestLiveServerConnection : LiveServerConnection
	{
		public TestLiveServerConnection (string username, string password, ApplicationModel appModel) : base (username, password, appModel)
		{
			client.DelegateStompClientDidReceiveMessage += (StompClient stompService, JObject message, Dictionary<string, string> headers) =>  {
				StompClientMessageListener listener = StompClientMessageListener.ListenersPerAppModel [appModel.cacheIndex];
				if (listener.messageNameToMatch != null && listener.messageNameToMatch.Equals((string)message ["messageName"])) {
					if (listener.queryToMatch != null) 
						switch (listener.messageNameToMatch) {
					case "Message":
						ChatMessageInput messageInput = message.ToObject<ChatMessageInput> ();
						if (listener.queryToMatch.Equals(messageInput.messageGUID)) 
							listener.SignalMessageReceived();
						break;
					case "NotificationMessage":
						NotificationInput notification = message.ToObject<NotificationInput> ();
						if (notification != null) {
							if (notification.title.IndexOf(listener.queryToMatch) >= 0) {
								listener.SignalMessageReceived ();
								break;
							}
						}
						break;
					case "NotificationUpdate":
						NotificationStatusInput update = message.ToObject<NotificationStatusInput> ();
						if (update != null) {
							string[] matchList = listener.queryToMatch.Split(new char[]{':'});
							if (matchList.Length == 2) {
								if (update.serverID == Convert.ToInt32(matchList[0]))
								if ((update.status == NotificationStatus.Read && matchList[1].Equals("R")) ||
									update.status == NotificationStatus.Deleted && matchList[1].Equals("D"))
									listener.SignalMessageReceived();
								break;
							}
						}
						break;
					default:
						break;
					}
				}
			};
		}
	}
}