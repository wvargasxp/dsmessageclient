using System;
using System.Collections.Generic;
using System.Diagnostics;
using em;
using UIKit;
using Xamarin;

namespace iOS {
	public class Application {

		// This is the main entry point of the application.
		static void Main (string[] args) {
			try {
				// if you want to use a different Application Delegate class from "AppDelegate"
				// you can specify it here.
				UIApplication.Main (args, null, "AppDelegate");
			}
			catch (Exception e) {
				HandleException (e);
			}
		}

		static void HandleException (Exception e) {
			Debug.WriteLine( string.Format("Application Failure: {0}\n{1}", e.Message, e.StackTrace));

			try {
				if(Insights.IsInitialized) {
					var extraData = new Dictionary<string, string>();
					extraData.Add("Build Mode", AppEnv.EnvType.ToString ());
					extraData.Add("Stack Trace", e.StackTrace);
					Insights.Report(e, extraData, Insights.Severity.Error);
				} else
					Console.WriteLine (e);
			} catch (Exception g) {
				Console.WriteLine (g);
			}

			Exception baseException = e.GetBaseException ();
			if (baseException != null && baseException != e)
				HandleException (baseException);
		}
	}
}