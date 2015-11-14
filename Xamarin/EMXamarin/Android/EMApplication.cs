using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Runtime;
using em;
using Gcm.Client;
using Xamarin;

namespace Emdroid {

	[Application (Label="EMwithME")]
	public class EMApplication : Application {

		#region model
		ApplicationModel applicationModel;
		public ApplicationModel appModel {
			get { return applicationModel; }
			set { applicationModel = value; }
		}
		#endregion

		static EMApplication instance;
		static Context mainContext;

		public static BuildVersionCodes SDK_VERSION {
			get { return Build.VERSION.SdkInt; }
		}

		// The activity that is on screen.
		static Activity currentActivity;

		public static EMApplication Instance {
			get { return instance; }
		}

		#region detecting 3rd party specific OS
		bool isSenseUI = false;
		bool hasSetSenseUIFlag = false;
		public bool IsSenseUI {
			get {
				// https://stackoverflow.com/questions/3676959/how-to-detect-htc-sense
				if (!hasSetSenseUIFlag) {
					PackageManager m = mainContext.PackageManager;
					Intent i = new Intent (Intent.ActionMain);
					i.AddCategory (Intent.CategoryHome);
					IList<ResolveInfo> list = m.QueryIntentActivities (i, PackageInfoFlags.MatchDefaultOnly);
					foreach (ResolveInfo info in list) {
						if (info.ActivityInfo != null && "com.htc.launcher.Launcher".Equals (info.ActivityInfo.Name))
							isSenseUI = true;
					}

					hasSetSenseUIFlag = true;
				}

				return isSenseUI;
			}
		}
		#endregion

		#region audio related
		AudioManager am;
		public AudioManager AudioManager {
			get { 
				if (am == null)
					am = (AudioManager)this.GetSystemService (Context.AudioService);
				return am; 
			}
		}

		public RingerMode RingerMode {
			get {
				return this.AudioManager.RingerMode;
			}
		}
		#endregion

		#region google analytics
		public static string TrackingId {
			get {
				if (AppEnv.EnvType == EnvType.Release) //prod
					return "UA-52238710-2";

				return "UA-52238710-1"; //dev & staging
			}
		}
		#endregion

		public EMApplication (IntPtr handle, JniHandleOwnership transfer) : base (handle, transfer) {}

		public override void OnCreate () {
			base.OnCreate ();
			instance = this;
			appModel = new ApplicationModel (new AndroidPlatformFactory ());

			foreach (BackgroundColor color in BackgroundColor.AllColors) {
				color.GetBackgroundResource ( (string file) => { });
			}

			mainContext = Application.Context;

			appModel.CompletedOnboardingPhase += () => EMTask.DispatchMain (() => {
				GcmClient.CheckDevice (this);
				GcmClient.CheckManifest (this);
				GcmClient.Register (this, GcmBroadcastReceiver.SENDER_IDS);
			});

			AndroidEnvironment.UnhandledExceptionRaiser += HandleAndroidException;

			EMTask.DispatchBackground (() => {
				Insights.HasPendingCrashReport += (sender, isStartupCrash) => {
					if (isStartupCrash)
						Insights.PurgePendingCrashReports().Wait();
				};

				// Initialize Xamarin Insights
				Insights.Initialize("09dd9f3bf52ea28e15514c70234c59d5578d7826", mainContext);
				
				if (applicationModel.account.accountInfo.username != null)
					Insights.Identify (applicationModel.account.accountInfo.username, Insights.Traits.Name, applicationModel.account.accountInfo.defaultName);
			});

			appModel.platformFactory.GetAdjustHelper ().Init ();

			NotificationCenter.DefaultCenter.AddWeakObserver (null, LiveServerConnection.NOTIFICATION_DID_CONNECT_WEBSOCKET, HandleDidConnectWebsocketNotification);
		}

		protected override void Dispose(bool disposing) {
			AndroidEnvironment.UnhandledExceptionRaiser -= HandleAndroidException;

			base.Dispose (disposing);
		}

		public static EMApplication GetInstance () {
			return instance;
		}

		public static Context GetMainContext () {
			if (mainContext == null) {
				mainContext = Application.Context;
			}
			return mainContext;
		}

		void HandleAndroidException(object sender, RaiseThrowableEventArgs e) {
			try {
				if(Insights.IsInitialized) {
					var extraData = new Dictionary<string, string>();
					extraData.Add("Build Mode", AppEnv.EnvType.ToString ());
					extraData.Add("Stack Trace", e.Exception.StackTrace);
					Insights.Report(e.Exception, extraData, Insights.Severity.Error);
				} else
					Console.WriteLine (e);
			} catch (Exception g) {
				Console.WriteLine (g);
			}

			e.Handled = false;
			System.Diagnostics.Debug.WriteLine( string.Format("Application Failure: {0}\n{1}", e.Exception.Message, e.Exception.StackTrace));
		}

		#region Keeping track of the current on screen activity.
		public static Activity GetCurrentActivity () {
			return currentActivity;
		}

		public static void SetCurrentActivity (Activity curActivity) {
			currentActivity = curActivity;
		}
		#endregion

		public void HandleDidConnectWebsocketNotification(em.Notification n) {}
	}
}