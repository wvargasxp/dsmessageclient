using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.IO;
using EMXamarin;
using System.Net;
using em;

namespace TestsHeadless
{
	public class StompClientMessageListener
	{
		public static List<StompClientMessageListener> ListenersPerAppModel = new List<StompClientMessageListener> ();
		public string messageNameToMatch;
		public string queryToMatch;

		public bool signalWasSent;
		private EventWaitHandle waitHandle; 

		public StompClientMessageListener () {
			signalWasSent = false;
			messageNameToMatch = null;
			queryToMatch = null;
			waitHandle = new AutoResetEvent (false);
		}

		public bool WaitForSignal (string messageName, string query) {
			signalWasSent = false;
			messageNameToMatch = messageName;
			queryToMatch = query;
			waitHandle.WaitOne (30000);
			return signalWasSent;
		}

		public void SignalMessageReceived () {
			messageNameToMatch = null;
			queryToMatch = null;
			signalWasSent = true;
			waitHandle.Set ();
		}
	}
}

