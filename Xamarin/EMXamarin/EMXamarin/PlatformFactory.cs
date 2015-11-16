using System;

namespace em {
	public interface PlatformFactory {
		
		void RunOnBackgroundQueue (Action action, string queueName);
		void RunOnMainThread (Action action, Func<bool> okayToContinue);

		bool OnMainThread { get; }

		PlatformType getPlatformType();
		IDeviceInfo getDeviceInfo();
		IAddressBook getAddressBook();
		WebsocketConnectionFactory GetWebSocketFactory (string username, string password);
		HeartbeatScheduler GetHeartbeatScheduler (StompClient heartbeater);

		ISQLiteConnection createSQLiteConnection(string databaseName);
		void OpenServicePoint ();
		IHttpInterface GetNativeHttpClient ();
		IUriGenerator GetUriGenerator ();
		IFileSystemManager GetFileSystemManager ();

		void StartMonitoringNetworkConnectivity (Action onConnect, Action onDisconnect);
		bool NetworkIsConnected ();
		void ShowNetworkIndicator();
		void HideNetworkIndicator();

		SoundRecordingPlayer GetSoundRecordingPlayer ();
		SoundRecordingRecorder GetSoundRecordingRecorder ();

		IVideoConverter GetVideoConverter ();
		void PlayIncomingMessageSound();

		string GetTranslation(string key);
		string GetFormattedDate (DateTime dt, DateFormatStyle style);
		void CopyToClipboard (string text);

		void ReportToXamarinInsights (string message);

		IAnalyticsHelper GetAnalyticsHelper();
		IAdjustHelper GetAdjustHelper ();

		ISecurityManager GetSecurityManager ();
		INavigationManager GetNavigationManager ();

		IInstalledAppResolver GetInstalledAppsResolver ();
		bool NeedsToBindToWhosHere ();
		bool CanShowUnicodeWithSkinModifier ();
	}
}