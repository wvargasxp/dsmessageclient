using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Net;

namespace Emdroid
{
	[BroadcastReceiver]
	public class NetworkConnectivityReceiver : BroadcastReceiver
	{
		public static Action networkDidDisconnectDelegate;
		public static Action networkDidConnectDelegate;
		private static bool connectionIsAvailable (NetworkInfo wifi, NetworkInfo cellular) {
			if (!wifi.IsAvailable && !cellular.IsAvailable)
				return false;
			if (wifi.IsAvailable && !wifi.IsConnected && cellular.IsAvailable && !cellular.IsConnected)
				return false;
			if (wifi.IsAvailable && !wifi.IsConnected && !cellular.IsAvailable)
				return false;
			if (cellular.IsAvailable && !cellular.IsConnected && !wifi.IsAvailable)
				return false;
			return true;
		}

		public static bool isCurrentlyConnected (Context context) {
			ConnectivityManager connectivityManager = (ConnectivityManager)context.GetSystemService (Context.ConnectivityService);
			Android.Net.NetworkInfo wifi = connectivityManager.GetNetworkInfo (ConnectivityType.Wifi);
			Android.Net.NetworkInfo cellular = connectivityManager.GetNetworkInfo (ConnectivityType.Mobile);
			if (wifi == null || cellular == null)
				return false;
			return connectionIsAvailable (wifi, cellular);
		}

		public override void OnReceive (Context context, Intent intent){
			ConnectivityManager connectivityManager = (ConnectivityManager)context.GetSystemService (Context.ConnectivityService);
			Android.Net.NetworkInfo wifi = connectivityManager.GetNetworkInfo (ConnectivityType.Wifi);
			Android.Net.NetworkInfo cellular = connectivityManager.GetNetworkInfo (ConnectivityType.Mobile);

			if (wifi == null || cellular == null)
				return;
			bool isCurrentlyConnected = connectionIsAvailable (wifi, cellular);
			if (!isCurrentlyConnected) {
				networkDidDisconnectDelegate ();
			} else {
				networkDidConnectDelegate ();
			}
		}
	}
}