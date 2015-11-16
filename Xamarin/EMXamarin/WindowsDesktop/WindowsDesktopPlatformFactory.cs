using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;
using EMXamarin;
using WindowsDesktop.Networking;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading;
using System.Net;
using WindowsDesktop.PlatformImpl;

namespace WindowsDesktop {
	class WindowsDesktopPlatformFactory : PlatformFactory {

		private IUriGenerator _uriGenerator = null;
		private IUriGenerator UriGenerator {
			get {
				if (this._uriGenerator == null) {
					UriPlatformResolverStrategy resolverStrategy = new WindowsDesktopUriPlatformResolverStrategy ();
					this._uriGenerator = new PlatformUriGenerator (resolverStrategy);
				}

				return this._uriGenerator;
			}
		}

		private IAddressBook _addressBook = null;
		private IAddressBook Addressbook {
			get {
				if (this._addressBook == null) {
					this._addressBook = new WindowsDesktopAddressBook ();
				}

				return this._addressBook;
			}
		}

		public void RunOnBackgroundQueue (Action action, string queueName) {
			// Todo: No idea if this works completely as expected.
			// Getting something up during scaffolding.
			BackgroundWorker worker = new BackgroundWorker ();
			worker.WorkerReportsProgress = false;
			worker.DoWork += (object sender, DoWorkEventArgs e) => {
				action ();
			};
			worker.RunWorkerAsync ();
		}

		public void RunOnMainThread (Action action, Func<bool> okayToContinue) {
			App.Current.Dispatcher.BeginInvoke (action);
		}

		public bool OnMainThread {
			get {
				Thread currentThread = Thread.CurrentThread;
				Thread uiThread = App.Current.Dispatcher.Thread;
				bool onUI = currentThread == uiThread;
				return onUI;
			}
		}

		public PlatformType getPlatformType () {
			return PlatformType.WinDeskPlatform;
		}

		public IDeviceInfo getDeviceInfo () {
			return new WindowsDesktopDeviceInfo ();
		}

		public IAddressBook getAddressBook () {
			return this.Addressbook;
		}

		public WebsocketConnectionFactory GetWebSocketFactory (string username, string password) {
			return new WindowsDesktopWebsocketConnectionFactory (username, password, getDeviceInfo ());
		}

		public HeartbeatScheduler GetHeartbeatScheduler (StompClient heartbeater) {
			return new DoesNothingHeartbeatScheduler ();
		}

		public ISQLiteConnection createSQLiteConnection (string databaseName) {
			string path = databaseName;

			ISQLiteConnection connection = new WindowsSqliteConnection (path, string.Empty, false);
			return connection;
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
			return new WindowsHttpClient ();
		}

		public IUriGenerator GetUriGenerator () {
			return this.UriGenerator;
		}

		public IFileSystemManager GetFileSystemManager () {
			return new WindowsDesktopFileSystemManager (this);
		}

		public void StartMonitoringNetworkConnectivity (Action onConnect, Action onDisconnect) {
			return;
			throw new NotImplementedException ();
		}

		public bool NetworkIsConnected () {
			return true; // todo
		}

		public void ShowNetworkIndicator () {
			return; // todo
		}

		public void HideNetworkIndicator () {
			return; // todo
		}

		public SoundRecordingPlayer GetSoundRecordingPlayer () {
			throw new NotImplementedException ();
		}

		public SoundRecordingRecorder GetSoundRecordingRecorder () {
			throw new NotImplementedException ();
		}

		public IVideoConverter GetVideoConverter () {
			throw new NotImplementedException ();
		}

		public void PlayIncomingMessageSound () {
			return; // todo
		}

		public string GetTranslation (string key) {
			return key; // todo
		}

		public string GetFormattedDate (DateTime dt, DateFormatStyle style) {
			throw new NotImplementedException ();
		}

		public void CopyToClipboard (string text) {
			return;
		}

		public void ReportToXamarinInsights (string message) {
			return;
		}

		public IAnalyticsHelper GetAnalyticsHelper () {
			return new WindowsAnalyticsHelper ();
		}

		public IAdjustHelper GetAdjustHelper () {
			return WindowsAdjustHelper.Shared;
		}

		public ISecurityManager GetSecurityManager () {
			return WindowsSecurityManager.Shared;
		}

		public INavigationManager GetNavigationManager () {
			throw new NotImplementedException ();
		}

		public IInstalledAppResolver GetInstalledAppsResolver () {
			return WindowsAppInstallResolver.Shared;
		}

		public bool NeedsToBindToWhosHere () {
			return false;
		}

		public bool CanShowUnicodeWithSkinModifier () {
			return false;
		}
	}
}
