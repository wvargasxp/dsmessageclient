using System;
using System.Diagnostics;
using Android.App;
using Android.Content;
using Google.Analytics.Tracking;
using Java.IO;

namespace Emdroid {

	[BroadcastReceiver(Exported = true)]
	[IntentFilter(new []{ "com.android.vending.INSTALL_REFERRER" })]
	public class GoogleInterceptor : BroadcastReceiver {

		public override void OnReceive(Context context, Intent intent) {
			try {
				Android.OS.Bundle extras = intent.Extras;

				string referrerString = extras.GetString ("referrer");
				if (!string.IsNullOrEmpty (referrerString)) {
					Debug.WriteLine ("GATRACKING Custom Receiver. Referrer: " + referrerString);

					File ourFile = context.GetDir ("em", FileCreationMode.Private);

					string fullFilePath = ourFile.Path + "/referrer.txt";
					using (var w = new System.IO.StreamWriter (fullFilePath)) {
						w.WriteLine (referrerString);
					}

					Debug.WriteLine("GATRACKING wrote file.");
				}

				// Pass along to google
				var receiver = new CampaignTrackingReceiver ();
				receiver.OnReceive (context, intent);
			} catch(Exception e) {
				Debug.WriteLine ("GATRACKING exception saving referrer: " + e.Message);
			}
		}

		public static string GetReferrer(File ourFile) {
			string retVal = null;

			try {
				string fullFilePath = ourFile.Path + "/referrer.txt";

				Debug.WriteLine ("GATRACKING referrer file path: " + fullFilePath);

				if (System.IO.File.Exists (fullFilePath)) {
					Debug.WriteLine ("GATRACKING referrer file found");
					using (var r = new System.IO.StreamReader (fullFilePath)) {
						retVal = r.ReadLine ();
					}
				} else {
					Debug.WriteLine ("GATRACKING No referrer found.");
				}
			} catch(Exception e) {
				Debug.WriteLine ("GATRACKING exception getting referrer: " + e.Message);
			}

			return retVal;
		}

		public static void DeleteReferrer(File ourFile) {
			try {
				string fullFilePath = ourFile.Path + "/referrer.txt";

				if (System.IO.File.Exists (fullFilePath)) {
					System.IO.File.Delete (fullFilePath);
				}
			} catch(Exception e) {
				Debug.WriteLine ("GATRACKING exception deleting referrer: " + e.Message);
			}
		}
	}
}