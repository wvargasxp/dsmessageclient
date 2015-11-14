using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Android.Content;
using em;
using Emdroid.PlatformImpl;
using EMXamarin;
using Xamarin;

namespace Emdroid {
	public class AndroidPlatformFactory : PlatformFactory {

		AndroidDeviceInfo deviceInfo;
		AndroidAddressBook addressBook;
		//AndroidModalDialogs modalDialogs;
        private AbstractDateFormatter _dateFormatter = new AndroidDateFormatter ();
        private AbstractDateFormatter DateFormatter { get { return this._dateFormatter; } set { this._dateFormatter = value; } }

		System.Collections.IDictionary executors;

		IUriGenerator uriGenerator = null;
		IFileSystemManager fileSystemManager = null;

		object tempDirLock = new object ();

		public AndroidPlatformFactory() {
			executors = null;
		}

		public bool OnMainThread { 
			get { 
				return Android.OS.Looper.MyLooper () == Android.OS.Looper.MainLooper;
			} 
		}

		public void RunOnBackgroundQueue (Action action, string queueName) {
			Java.Util.Concurrent.IExecutor executor = null;
			if (queueName == null)
				queueName = EMTask.BACKGROUND_QUEUE;

			lock (this) {
				if (executors == null)
					executors = new System.Collections.Hashtable();

				executor = executors[queueName] as Java.Util.Concurrent.IExecutor;
				if (executor == null) {
					if (queueName.Equals (EMTask.BACKGROUND_QUEUE))
						executor = Java.Util.Concurrent.Executors.NewCachedThreadPool (new AndroidThreadFactory ("Background Thread {0}", -1));
					else if ( queueName.Equals (EMTask.DOWNLOAD_QUEUE))
						executor = Java.Util.Concurrent.Executors.NewCachedThreadPool (new AndroidThreadFactory ("Download Thread {0}", -1));
					else if (queueName.Equals (EMTask.VIDEO_ENCODING)) 
						executor = Java.Util.Concurrent.Executors.NewCachedThreadPool (new AndroidThreadFactory ("Encoding Thread {0}", -1));
					else
						executor = Java.Util.Concurrent.Executors.NewSingleThreadExecutor (new AndroidThreadFactory (queueName + " Thread {0}", -1));

					executors[queueName] = executor;
				}
			}
			executor.Execute (new Java.Lang.Runnable (action));
		}

		public void RunOnMainThread (Action action, Func<bool> okayToContinue) {
			EMApplication.SynchronizationContext.Post (_ => {
				if ( okayToContinue != null && !okayToContinue() )
					Debug.WriteLine("Task on main thread getting skipped as no longer able to continue");
				else
					action ();
			}, null);
		}

		public void StartMonitoringNetworkConnectivity (Action onConnect, Action onDisconnect) {
			NetworkConnectivityReceiver.networkDidConnectDelegate += onConnect;
			NetworkConnectivityReceiver.networkDidDisconnectDelegate += onDisconnect;
		}

		public bool NetworkIsConnected () { 
			return NetworkConnectivityReceiver.isCurrentlyConnected (EMApplication.GetMainContext ());
		}

		public void ShowNetworkIndicator() {
			//noop for android: Little arrows show up in the wifi symbol in the status bar when there is network activity.
			NotificationCenter.DefaultCenter.PostNotification (Constants.PlatformFactory_ShowNetworkIndicatorNotification);
		}

		public void HideNetworkIndicator() {
			//noop for android: Little arrows show up in the wifi symbol in the status bar when there is network activity.
			NotificationCenter.DefaultCenter.PostNotification (Constants.PlatformFactory_HideNetworkIndicatorNotification);
		}

		public PlatformType getPlatformType() {
			return PlatformType.AndroidPlatform;
		}

		public IDeviceInfo getDeviceInfo() {
			if ( deviceInfo == null ) {
				deviceInfo = new AndroidDeviceInfo();

				string retrievedPushToken = GetSecurityManager ().GetSecureKeyValue (Constants.NOTIFICATION_TOKEN_KEY);
				if (retrievedPushToken != null) {
					Debug.WriteLine ("retrieved push token " + retrievedPushToken);
					deviceInfo.SetPushToken (retrievedPushToken);
				}

				deviceInfo.PushTokenDidUpdate = (string updatedPushToken) => {
					EMTask.DispatchBackground (() => {
						Debug.WriteLine ("saving updated push token " + updatedPushToken);
						GetSecurityManager ().SaveSecureKeyValue (Constants.NOTIFICATION_TOKEN_KEY, updatedPushToken);
					});
				};
			}

			return deviceInfo;
		}

		public IAddressBook getAddressBook() {
			if ( addressBook == null )
				addressBook = new AndroidAddressBook();

			return addressBook;
		}

		public WebsocketConnectionFactory GetWebSocketFactory (string username, string password) {
			return new AndroidWebsocketConnectionFactory (username, password, getDeviceInfo());
		}

		public HeartbeatScheduler GetHeartbeatScheduler (StompClient heartbeater) {
			return new StompClientHeartbeatScheduler (heartbeater);
		}

		public void OpenServicePoint () {
			// https://stackoverflow.com/questions/2960056/trying-to-run-multiple-http-requests-in-parallel-but-being-limited-by-windows
			//ServicePointManager.UseNagleAlgorithm = true;
			//ServicePointManager.Expect100Continue = true;
			//ServicePointManager.CheckCertificateRevocationList = true;
			ServicePointManager.DefaultConnectionLimit = Constants.MAX_HTTP_REQUESTS;
			//ServicePoint servicePoint = ServicePointManager.FindServicePoint(MS);
		}

		public IHttpInterface GetNativeHttpClient () {
			return new AndroidHttpClient ();
		}

		public IUriGenerator GetUriGenerator () {
			if (this.uriGenerator == null) {
				UriPlatformResolverStrategy resolverStrategy = new AndroidUriPlatformResolverStrategy ();
				this.uriGenerator = new PlatformUriGenerator (resolverStrategy);
			}

			return this.uriGenerator;
		}

		public IFileSystemManager GetFileSystemManager () {
			if (this.fileSystemManager == null) {
				this.fileSystemManager = new AndroidFileSystemManager (this);
			}
			return this.fileSystemManager;
		}

		public ISQLiteConnection createSQLiteConnection(string databaseName) {
			string libraryPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
			var path = Path.Combine(libraryPath, databaseName);

			string sqliteNewFilename = databaseName + ".tmp";
			var exportDBPath = Path.Combine(libraryPath, sqliteNewFilename);

			string tmpDbName = databaseName.Substring (0, 6);

			SQLiteConnectionWrapper conn = null;

			try {
				conn = new SQLiteConnectionWrapper (path, CleanupTempDir(), false, exportDBPath, tmpDbName, GetFileSystemManager());
			} catch(SQLite.SQLiteException e) {
				ReportToXamarinInsights ("Wiping Android DB and creating a new one! SQLiteException Message: " + e.Message);

				//TODO: notifiy user we have to get all missed messages again

				String backupDBPath = path + ".bak-" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Hour;
				GetFileSystemManager ().MoveFileAtPath (path, backupDBPath);

				conn = new SQLiteConnectionWrapper (path, CleanupTempDir(), false, exportDBPath, tmpDbName, GetFileSystemManager());
			}

			return conn;
		}

		private string CleanupTempDir () {
			ISecurityManager securityManager = GetSecurityManager ();

			lock (tempDirLock) {
				string android_id = Android.Provider.Settings.Secure.GetString (EMApplication.GetMainContext ().ContentResolver, Android.Provider.Settings.Secure.AndroidId);

				string key = "EM-" + android_id;
				var p = securityManager.GetSecureKeyValue (key);
				if (p != null)
					return p;

				if (p == null) {
					p = securityManager.retrieveSecureField (key);
					if (p != null) {
						securityManager.SaveSecureKeyValue (key, p);
						securityManager.removeSecureField (key);
						return p;
					}
				}

				var tManager = (Android.Telephony.TelephonyManager)EMApplication.GetMainContext ().GetSystemService (Context.TelephonyService);
				string device_id = tManager.DeviceId;

				var sb = new StringBuilder ();
				sb.Append (Android.OS.Build.Board).Append (Android.OS.Build.Brand).Append (Android.OS.Build.CpuAbi).Append (Android.OS.Build.Device).Append (Android.OS.Build.Display)
					.Append (Android.OS.Build.Fingerprint).Append (Android.OS.Build.Host).Append (Android.OS.Build.Id).Append (Android.OS.Build.Manufacturer).Append (Android.OS.Build.Model)
					.Append (Android.OS.Build.Product).Append (Android.OS.Build.Tags).Append (Android.OS.Build.Type).Append (Android.OS.Build.User);

				p = securityManager.CalculateMD5Hash (sb + device_id + android_id);

				securityManager.SaveSecureKeyValue (key, p);
				return p;
			}
		}

		public SoundRecordingPlayer GetSoundRecordingPlayer () {
			return new AndroidSoundRecordingPlayer ();
		}

		public SoundRecordingRecorder GetSoundRecordingRecorder () {
			return new AndroidSoundRecordingRecorder ();
		}

		public IVideoConverter GetVideoConverter () {
			//return VideoConverter.Shared;
			return NativeVideoConverter.Shared;
		}

		#region playing sound
		private Android.OS.Vibrator vibrate;
		private Android.OS.Vibrator Vibrate {
			get {
				if (this.vibrate == null) {
					this.vibrate = (Android.OS.Vibrator)EMApplication.Instance.GetSystemService (Context.VibratorService);
				}

				return this.vibrate;
			}
		}

		public void PlayIncomingMessageSound () {
			Android.Media.RingerMode ringerMode = EMApplication.Instance.RingerMode;
			switch (ringerMode) {
			case Android.Media.RingerMode.Normal:
				{
					Android.Media.MediaPlayer mp = Android.Media.MediaPlayer.Create (EMApplication.Instance.ApplicationContext, Resource.Raw.InAppIncomingMessage);
					mp.Start ();
					mp.Completion += delegate {
						mp.Reset ();
						mp.Release ();
					};
					break;
				}
			case Android.Media.RingerMode.Silent:
				{
					break;
				}
			case Android.Media.RingerMode.Vibrate:
				{
					this.Vibrate.Vibrate (Android_Constants.VIBRATE_DURATION_MILLIS); // milliseconds
					break;
				}
			}
		}
		#endregion

		public string GetTranslation(string key) {
			return key.t ();
		}

		public string GetFormattedDate (DateTime dt, DateFormatStyle style) {
			return this.DateFormatter.FormatDate (dt, style);
		}

		public void CopyToClipboard (string text) {
			ClipboardManager clipboard = (ClipboardManager) EMApplication.GetCurrentActivity ().GetSystemService (Context.ClipboardService);
			ClipData clip = ClipData.NewPlainText ("EM", text);
			clipboard.PrimaryClip = clip;
		}

		public void ReportToXamarinInsights (string message) {
			try {
				Insights.Report(null, new Dictionary<string,string>
					{
						{ "Message", message }
					}
				);
			} catch(Exception e) {
				Debug.WriteLine ("Exception thrown trying to report to xamarin insignts", e);
			}
		}

		public IAnalyticsHelper GetAnalyticsHelper() {
			return new AnalyticsHelper();
		}

		public IAdjustHelper GetAdjustHelper () {
			return AndroidAdjustHelper.Shared;
		}

		public ISecurityManager GetSecurityManager () {
			return AndroidSecurityManager.Shared;
		}

		public INavigationManager GetNavigationManager () {
			return AndroidNavigationManager.Shared;
		}

		public IInstalledAppResolver GetInstalledAppsResolver () {
			return AndroidAppInstallResolver.Shared;
		}

		public bool NeedsToBindToWhosHere () {
			return false;
		}

		public bool CanShowUnicodeWithSkinModifier () {
			return false;
		}
	}

	class AndroidThreadFactory : Java.Lang.Object, Java.Util.Concurrent.IThreadFactory {
		int num;
		readonly Java.Text.MessageFormat nameFormat;
		readonly int priortyAdjustmentFromNormal;
		readonly Java.Lang.ThreadGroup group;

		public AndroidThreadFactory(String nameTemplate, int priortyAdjustment) {
			num = 1;
			nameFormat = new Java.Text.MessageFormat (nameTemplate);
			priortyAdjustmentFromNormal = priortyAdjustment;
			group = new Java.Lang.ThreadGroup (nameFormat.Format (new Java.Lang.Object[] { "" }));
		}

		public Java.Lang.Thread NewThread (Java.Lang.IRunnable r) {
			var t = new Java.Lang.Thread (
				group,
				()=> {
					try {
						r.Run();
					}
					catch (Java.Lang.RuntimeException e) {
						Debug.WriteLine("Uncaught Exception " + e.Message + "\n" + e.StackTrace);
						throw e;
					}
					catch (Java.Lang.Error e) {
						Debug.WriteLine("Uncaught Error " + e.Message + "\n" + e.StackTrace);
						throw e;
					}
				},
				nameFormat.Format(new Java.Lang.Object[] { num++ }));
			t.Priority = Java.Lang.Thread.NormPriority + priortyAdjustmentFromNormal;
			t.Daemon = true;
			t.UncaughtExceptionHandler = Java.Lang.Thread.CurrentThread().UncaughtExceptionHandler;

			return t;
		}
	}
}