using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;
using Windows10.PlatformImpl;
using Windows10.Networking;
using Windows10.Utility;
//using System.ComponentModel;
//using System.Threading;


namespace Windows10
{
    class WindowsDesktopPlatformFactory : PlatformFactory

    {
        private IUriGenerator _uriGenerator = null;

        private IUriGenerator UriGenerator
        {
            get
            {
                if (this._uriGenerator == null)
                {
                    UriPlatformResolverStrategy resolverStrategy = new WindowsDesktopUriPlatformResolverStrategy();
                    // TODO --- DS to be completed
                    //this._uriGenerator = new em.PlatformUriGenerator(resolverStrategy);
                }

                return this._uriGenerator;
            }
        }

        private IAddressBook _addressBook = null;
        private IAddressBook Addressbook
        {
            get
            {
                if (this._addressBook == null)
                {
                    this._addressBook = new WindowsDesktopAddressBook();
                }

                return this._addressBook;
            }
        }
        public bool OnMainThread
        {
            get
            {
                // TODO --- DS to be completed
                //Thread currentThread = Thread.CurrentThread;
                //Thread uiThread = App.Current.Dispatcher.Thread;
                //bool onUI = currentThread == uiThread;
                //return onUI;
                return true;
            }
        }        

        public bool CanShowUnicodeWithSkinModifier()
        {
            return false;
        }

        public void CopyToClipboard(string text)
        {
            return;
        }

        public ISQLiteConnection createSQLiteConnection(string databaseName)
        {
            string path = databaseName;

            ISQLiteConnection connection = new WindowsSqliteConnection(path, string.Empty, false);
            return connection;
        }

        public IAddressBook getAddressBook()
        {
            return this.Addressbook;
        }

        public IAdjustHelper GetAdjustHelper()
        {
            return WindowsAdjustHelper.Shared;
        }

        public IAnalyticsHelper GetAnalyticsHelper()
        {
            return new WindowsAnalyticsHelper();
        }

        public IDeviceInfo getDeviceInfo()
        {
            return new WindowsDesktopDeviceInfo();
        }

        public IFileSystemManager GetFileSystemManager()
        {
            return new WindowsDesktopFileSystemManager(this);
        }

        public string GetFormattedDate(DateTime dt, DateFormatStyle style)
        {
            throw new NotImplementedException();
        }

        public HeartbeatScheduler GetHeartbeatScheduler(StompClient heartbeater)
        {
            return new DoesNothingHeartbeatScheduler();
        }

        public IInstalledAppResolver GetInstalledAppsResolver()
        {
            return WindowsAppInstallResolver.Shared;
        }

        public IHttpInterface GetNativeHttpClient()
        {
            return new WindowsHttpClient();
        }

        public INavigationManager GetNavigationManager()
        {
            throw new NotImplementedException();
        }

        public PlatformType getPlatformType()
        {
            return PlatformType.WinDeskPlatform;
        }

        public ISecurityManager GetSecurityManager()
        {
            throw new NotImplementedException();
        }

        public SoundRecordingPlayer GetSoundRecordingPlayer()
        {
            throw new NotImplementedException();
        }

        public SoundRecordingRecorder GetSoundRecordingRecorder()
        {
            throw new NotImplementedException();
        }

        public string GetTranslation(string key)
        {
            return key;
        }

        public IUriGenerator GetUriGenerator()
        {
            return this.UriGenerator;
        }

        public IVideoConverter GetVideoConverter()
        {
            throw new NotImplementedException();
        }

        public WebsocketConnectionFactory GetWebSocketFactory(string username, string password)
        {
            return new WindowsDesktopWebsocketConnectionFactory(username, password, getDeviceInfo());
        }

        public void HideNetworkIndicator()
        {
            return; // todo
        }

        public bool NeedsToBindToWhosHere()
        {
            return false;
        }

        public bool NetworkIsConnected()
        {
            return true; // todo
        }

        public void OpenServicePoint()
        {
            // https://stackoverflow.com/questions/2960056/trying-to-run-multiple-http-requests-in-parallel-but-being-limited-by-windows
            //ServicePointManager.UseNagleAlgorithm = true;
            //ServicePointManager.Expect100Continue = true;
            //ServicePointManager.CheckCertificateRevocationList = true;
            // TODO --- DS to be completed
            //ServicePointManager.DefaultConnectionLimit = Constants.MAX_HTTP_REQUESTS;
            //ServicePoint servicePoint = ServicePointManager.FindServicePoint(MS);
        }

        public void PlayIncomingMessageSound()
        {
            return; // todo
        }

        public void ReportToXamarinInsights(string message)
        {
            return;
        }

        public void RunOnBackgroundQueue(Action action, string queueName)
        {
            // Todo: No idea if this works completely as expected.
            // Getting something up during scaffolding.
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = false;
            worker.DoWork += (object sender, DoWorkEventArgs e) => {
                action();
            };
            worker.RunWorkerAsync();
        }

        public void RunOnMainThread(Action action, Func<bool> okayToContinue)
        {
            // TODO --- DS to be completed
            //App.Current.Dispatcher.BeginInvoke(action);
        }

        public void ShowNetworkIndicator()
        {
            return; // todo
        }

        public void StartMonitoringNetworkConnectivity(Action onConnect, Action onDisconnect)
        {
            return;
            throw new NotImplementedException();
        }
    }
}
