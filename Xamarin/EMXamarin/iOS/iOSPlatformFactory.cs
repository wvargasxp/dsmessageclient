using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using AudioToolbox;
using AVFoundation;
using CoreFoundation;
using em;
using EMXamarin;
using Foundation;
using iOS.PlatformImpl;
using SystemConfiguration;
using UIDevice_Extension;
using UIKit;
using Xamarin;

namespace iOS {
	public class iOSPlatformFactory : PlatformFactory {

		IOSAddressBook addressBook;
		IOSDeviceInfo deviceInfo;
		bool networkIsConnected;

        private AbstractDateFormatter _dateFormatter = new iOSDateFormatter ();
        private AbstractDateFormatter DateFormatter { get { return this._dateFormatter; } set { this._dateFormatter = value; } } 

		IUriGenerator uriGenerator = null;

		IFileSystemManager fileSystemManager = null;

		public bool OnMainThread { 
			get { return NSThread.IsMain; } 
		}

		Dictionary<string, NSOperationQueue> operationQueues = new Dictionary<string, NSOperationQueue> ();
		object operationQueueLock = new object ();

		object tempDirLock = new object ();

		public NSOperationQueue FindOrCreateOperationQueue (string queueName) {
			NSOperationQueue executor = null;
			lock (operationQueueLock) {
				executor = operationQueues.ContainsKey (queueName) ? operationQueues [queueName] as NSOperationQueue : null;
				if (executor == null) {
					executor = new NSOperationQueue ();
					if (queueName.Equals (EMTask.BACKGROUND_QUEUE))
						executor.MaxConcurrentOperationCount = 20;
					else if (queueName.Equals (EMTask.DOWNLOAD_QUEUE))
						// we add two because non throttled media objects would be starved if we matched the
						// throttling number exactly.
						executor.MaxConcurrentOperationCount = Media.MAX_CONCONCURRENT_THROTTLED_MEDIA_DOWNLOADS + 2;
					else if (queueName.Equals (EMTask.LOGIN_QUEUE)) {
						executor.MaxConcurrentOperationCount = 1;
						if (UIDevice.CurrentDevice.IsIos8Later ()) {
							executor.QualityOfService = NSQualityOfService.UserInteractive; // highest priority
						}
					} else
						executor.MaxConcurrentOperationCount = 1;

					executor.Name = queueName;
					operationQueues [queueName] = executor;
				}
			}

			return executor;
		}


		public void RunOnBackgroundQueue (Action action, string queueName) {
			if (queueName == null)
				queueName = EMTask.BACKGROUND_QUEUE;

			NSOperationQueue executor = FindOrCreateOperationQueue (queueName);

			nint taskId = UIApplication.BackgroundTaskInvalid;
			bool keepBackgroundTaskAlive = AppDelegate.Instance.ShouldKeepTasksAlive;

			if (keepBackgroundTaskAlive) {
				taskId = UIApplication.SharedApplication.BeginBackgroundTask (() => {
					// App is going to be terminated, kill the task.
					if (taskId != UIApplication.BackgroundTaskInvalid) {
						if (UIApplication.SharedApplication != null) {
							Debug.WriteLine ("{0} : killing task with task id {1}", queueName, taskId);
							UIApplication.SharedApplication.EndBackgroundTask (taskId);
							taskId = UIApplication.BackgroundTaskInvalid;
						}
					}
				});

				Debug.WriteLine ("{0} : starting task with task id {1}", queueName, taskId);
			}
	
			executor.AddOperation( () => {
				using (var ns = new NSAutoreleasePool ()) {
					action();

					if (keepBackgroundTaskAlive) {
						// Make sure to end the task after finishing.
						if (UIApplication.SharedApplication != null) {
							if (taskId != UIApplication.BackgroundTaskInvalid) {
								Debug.WriteLine ("{0} : finishing task with task id {1}", queueName, taskId);
								UIApplication.SharedApplication.EndBackgroundTask (taskId);
								taskId = UIApplication.BackgroundTaskInvalid;
							}
						}
					}
				}
			});
		}

		public void RunOnMainThread (Action action, Func<bool> okayToContinue) {
			NSOperationQueue.MainQueue.AddOperation (() => {
				if ( okayToContinue != null && !okayToContinue() )
					Debug.WriteLine("Task on main thread getting skipped as nolonger able to continue");
				else {
					using (var ns = new NSAutoreleasePool ()) {
						action();
					}
				}
			});
		}

		const string WAIT_UNTIL_ALL_OPERATIONS_FINISHED_QUEUE = "wait.until.all.queue";

		public void WaitUntilAllOperationsAreFinished () {
			EMTask.Dispatch (() => {
				List<NSOperationQueue> operationQueueValues;
				lock (operationQueueLock) {
					operationQueueValues = new List<NSOperationQueue>(operationQueues.Values);
				}

				foreach (NSOperationQueue queue in operationQueueValues) {
					if (!queue.Name.Equals (WAIT_UNTIL_ALL_OPERATIONS_FINISHED_QUEUE)) {
						Debug.WriteLine ("{0} : Waiting for queue - pending {1}", queue.Name, queue.OperationCount);
						queue.WaitUntilAllOperationsAreFinished ();
					}
				}

				Debug.WriteLine ("Finished WaitUntilAllOperationsAreFinished");
				EMTask.DispatchMain (() => {
					if (UIApplication.SharedApplication != null) {
						nint keepAliveTaskId = AppDelegate.Instance.KeepAliveTaskId;
						if (keepAliveTaskId != UIApplication.BackgroundTaskInvalid) {
							UIApplication.SharedApplication.EndBackgroundTask (keepAliveTaskId);
						}
					}
				});
			}, WAIT_UNTIL_ALL_OPERATIONS_FINISHED_QUEUE);
		}

		public bool IsReachableWithoutRequiringConnection(NetworkReachabilityFlags flags) {
			bool isReachable = ((flags & NetworkReachabilityFlags.Reachable) != 0);
			bool needsConnection = ((flags & NetworkReachabilityFlags.ConnectionRequired) != 0);
			return (isReachable && !needsConnection);
		}
			
		public void StartMonitoringNetworkConnectivity (Action onConnect, Action onDisconnect) {
			var reachability = new NetworkReachability ("www.google.com");
			NetworkReachabilityFlags initialFlags;
			bool isReachable = reachability.TryGetFlags(out initialFlags);
			networkIsConnected = isReachable ? IsReachableWithoutRequiringConnection (initialFlags) : false;
			reachability.SetNotification ((NetworkReachabilityFlags flags) => {
				// Is it reachable with the current network configuration?
				if (reachability.TryGetFlags(out flags)) {
					if (IsReachableWithoutRequiringConnection(flags)) {
						networkIsConnected = true;
						if (onConnect != null)
							onConnect ();
					} else {
						networkIsConnected = false;
						if (onDisconnect != null)
							onDisconnect ();
					}
				}
			});
			reachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
		}

		public bool NetworkIsConnected () { 
			return networkIsConnected;
		}

		public void ShowNetworkIndicator() {
			EMTask.DispatchMain (NetworkIndicator.ShowNetworkIndicator);
			em.NotificationCenter.DefaultCenter.PostNotification (Constants.PlatformFactory_ShowNetworkIndicatorNotification);
		}

		public void HideNetworkIndicator() {
			EMTask.DispatchMain (NetworkIndicator.HideNetworkIndicator);
			em.NotificationCenter.DefaultCenter.PostNotification (Constants.PlatformFactory_HideNetworkIndicatorNotification);
		}

		public PlatformType getPlatformType() {
			return PlatformType.IOSPlatform;
		}

		public IDeviceInfo getDeviceInfo() {
			if (deviceInfo == null) {
				deviceInfo = new IOSDeviceInfo ();

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
				addressBook = new IOSAddressBook();

			return addressBook;
		}

		public WebsocketConnectionFactory GetWebSocketFactory (string username, string password) {
			return new iOSWebsocketConnectionFactory (username, password, getDeviceInfo ());
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
			return new iOSHttpClient ();
		}

		public IUriGenerator GetUriGenerator () {
			if (this.uriGenerator == null) {
				UriPlatformResolverStrategy resolverStrategy = new iOSUriPlatformResolverStrategy ();
				this.uriGenerator = new PlatformUriGenerator (resolverStrategy);
			}

			return uriGenerator;
		}

		public IFileSystemManager GetFileSystemManager () {
			if (this.fileSystemManager == null) {
				this.fileSystemManager = new iOSFileSystemManager (this);
			}
			return this.fileSystemManager;
		}

		public ISQLiteConnection createSQLiteConnection(string databaseName) {
			// we need to put in /Library/ on iOS5.1 to meet Apple's iCloud terms
			// (they don't want non-user-generated data in Documents)
			string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.Personal); // Documents folder
			string libraryPath = Path.Combine (documentsPath, "..", "Library"); // Library folder

			var path = Path.Combine (libraryPath, databaseName);
			if (!File.Exists (path)) {
				// first create an empty file with default file protection attributes, to take advantage of iOS' data protection feature
				CreateFileWithDefaultProtection (path);
			}

			string sqliteNewFilename = databaseName + ".tmp";
			var exportDBPath = Path.Combine(libraryPath, sqliteNewFilename);
			if (!File.Exists (exportDBPath)) {
				// first create an empty file with default file protection attributes, to take advantage of iOS' data protection feature
				CreateFileWithDefaultProtection (exportDBPath);
			}

			string tmpDbName = databaseName.Substring (0, 6);

			SQLiteConnectionWrapper conn = null;

			try {
				conn = new SQLiteConnectionWrapper (path, CleanupTempDir(), false, exportDBPath, tmpDbName, GetFileSystemManager());
			} catch(SQLite.SQLiteException e) {
				ReportToXamarinInsights ("Wiping iOS DB and creating a new one! SQLiteException Message: " + e.Message);

				//TODO: notifiy user we have to get all missed messages again

				String backupDBPath = path + ".bak-" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day + "-" + DateTime.Now.Hour;
				GetFileSystemManager ().MoveFileAtPath (path, backupDBPath);

				// first create an empty file with default file protection attributes, to take advantage of iOS' data protection feature
				CreateFileWithDefaultProtection (path);

				conn = new SQLiteConnectionWrapper (path, CleanupTempDir(), false, exportDBPath, tmpDbName, GetFileSystemManager());
			}

			return conn;
		}

		public SoundRecordingPlayer GetSoundRecordingPlayer () {
			return new iOSSoundRecordingPlayer ();
		}

		public SoundRecordingRecorder GetSoundRecordingRecorder () {
			return new iOSSoundRecordingRecorder ();
		}

		public IVideoConverter GetVideoConverter () {
			return VideoConverter.Shared;
		}
	
		AVAudioPlayer player;
		public void PlayIncomingMessageSound() {
			try {
				if (player == null) {
					var file = Path.Combine("sounds", "InAppIncomingMessage.mp3");
					var Soundurl = NSUrl.FromFilename(file);
					player = AVAudioPlayer.FromUrl(Soundurl);
					player.FinishedPlaying += DidFinishPlaying;
					player.PrepareToPlay();
				}

				player.Play();
				SystemSound.Vibrate.PlaySystemSound ();
			}
			catch (Exception e) {
				Debug.WriteLine("PlaySound: Error: " + e.Message);
			}
		}
			
		protected void DidFinishPlaying(object sender , AVStatusEventArgs e) {
			if (e.Status) {
				// your code
			}
		}

		public string GetTranslation(string key) {
			return key.t ();
		}

		void CreateFileWithDefaultProtection(string systemPath) {
			bool succeeded = NSFileManager.DefaultManager.CreateFile (systemPath, new NSData(), NSDictionary.FromObjectAndKey(NSFileManager.FileProtectionCompleteUntilFirstUserAuthentication, NSFileManager.FileProtectionKey));
			if (!succeeded) {
				throw new IOException (String.Format ("cannot create protected file at path {0}", systemPath));
			}
		}

		public string GetFormattedDate (DateTime dt, DateFormatStyle style) {
			return this.DateFormatter.FormatDate (dt, style);
		}

		public void CopyToClipboard (string text) {
			UIPasteboard pasteBoard = UIPasteboard.General;
			pasteBoard.String = text;
		}

		string CleanupTempDir() {
			var key = NSBundle.MainBundle.BundleIdentifier;
			ISecurityManager securityManager = GetSecurityManager ();

			lock (tempDirLock) {
				var p = securityManager.GetSecureKeyValue (key);
				if (p != null)
					return p;

				if(p == null) {
					p = securityManager.retrieveSecureField (key);
					if (p != null) {
						securityManager.SaveSecureKeyValue (key, p);
						securityManager.removeSecureField (key);
						return p;
					}
				}

				p = Guid.NewGuid ().ToString ();
				securityManager.SaveSecureKeyValue (key, p);
				return p;
			}
		}

		public ISecurityManager GetSecurityManager () {
			return iOSSecurityManager.Shared;
		}

		public INavigationManager GetNavigationManager () {
			return iOSNavigationManager.Shared;
		}

		public void ReportToXamarinInsights (string message) {
			try {
				Insights.Report(null, new Dictionary<string,string>
					{
						{ "Message", message }
					}
				);
			} catch(Exception e) {
				//this is called currently because Insights.Initialize hasn't been called yet.
				//TODO: figure out how to fix this. maybe put in a queue?
				Debug.WriteLine ("Exception thrown trying to report to xamarin insignts", e.Message);
			}
		}

		public IAnalyticsHelper GetAnalyticsHelper() {
			return new AnalyticsHelper();
		}

		public IAdjustHelper GetAdjustHelper () {
			return iOSAdjustHelper.Shared;
		}

		public IInstalledAppResolver GetInstalledAppsResolver () {
			return iOSAppInstallResolver.Shared;
		}

		public bool NeedsToBindToWhosHere () {
			return true;
		}

		public bool CanShowUnicodeWithSkinModifier () {
			return UIDevice.CurrentDevice.IsIos8v3Later ();
		}
	}
}