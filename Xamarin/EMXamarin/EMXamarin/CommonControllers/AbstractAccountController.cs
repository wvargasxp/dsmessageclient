using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace em {
	public abstract class AbstractAccountController {

		readonly ApplicationModel appModel;

		BackgroundColor initialBackgroundColor;

		WeakDelegateProxy changeThumbnailProxy;
		WeakDelegateProxy didDownloadThumbnailProxy;
		WeakDelegateProxy didChangeColorThemeProxy;
		WeakDelegateProxy didChangeDisplayNameProxy;
		WeakDelegateProxy ChatListDidBecomeVisible;

		public bool IsOnboarding { get; set; }

		byte[] updatedThumbnail;
		public byte[] UpdatedThumbnail {
			get { return updatedThumbnail; }
			set { updatedThumbnail = value; }
		}

		public AccountInfo AccountInfo {
			get { return appModel.account.accountInfo; }
		}

		public BackgroundColor ColorTheme {
			get { return this.AccountInfo.colorTheme; }
			set { this.AccountInfo.colorTheme = value; } // this will trigger a delegate call in the colorTheme property setter if it's different
		}

		string textInDisplayNameField; // keep track of the text in the display name field when changing
		public string TextInDisplayNameField {
			get { return textInDisplayNameField; }
			set { textInDisplayNameField = value; }
		}

		public string DisplayName {
			get { return this.AccountInfo.displayName; }
			set { this.AccountInfo.displayName = value; } // this will trigger a delegate call in the colorTheme property setter if it's different
		}

		protected AbstractAccountController (ApplicationModel theAppModel) {
			appModel = theAppModel;

			initialBackgroundColor = appModel.account.accountInfo.colorTheme;

			changeThumbnailProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (AccountDidChangeThumbnailMedia);
			didDownloadThumbnailProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (AccountThumbnailDidLoad);
			didChangeColorThemeProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (DidChangeColorTheme);
			didChangeDisplayNameProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (AccountDidChangeDisplayName);

			this.AccountInfo.DelegateDidChangeThumbnailMedia += changeThumbnailProxy.HandleEvent<CounterParty>;
			this.AccountInfo.DelegateDidDownloadThumbnail += didDownloadThumbnailProxy.HandleEvent<CounterParty>;
			this.AccountInfo.DelegateDidChangeColorTheme += didChangeColorThemeProxy.HandleEvent<CounterParty>;
			this.AccountInfo.DelegateDidChangeDisplayName += didChangeDisplayNameProxy.HandleEvent<CounterParty>;

			NotificationCenter.DefaultCenter.AddWeakObserver (this.AccountInfo, Constants.Counterparty_DownloadFailed, BackgroundAccountFailedToDownloadMedia);

			ChatListDidBecomeVisible = WeakDelegateProxy.CreateProxy (Dispose);
			appModel.chatList.DidBecomeVisible += ChatListDidBecomeVisible.HandleEvent;
		}

		~AbstractAccountController() {
			Dispose();
		}
	
		bool hasDisposed = false;
		public void Dispose() {
			if (!hasDisposed) {
				this.AccountInfo.DelegateDidChangeThumbnailMedia -= changeThumbnailProxy.HandleEvent<CounterParty>;
				this.AccountInfo.DelegateDidDownloadThumbnail -= didDownloadThumbnailProxy.HandleEvent<CounterParty>;
				this.AccountInfo.DelegateDidChangeColorTheme -= didChangeColorThemeProxy.HandleEvent<CounterParty>;
				this.AccountInfo.DelegateDidChangeDisplayName -= didChangeDisplayNameProxy.HandleEvent<CounterParty>;
				appModel.chatList.DidBecomeVisible -= ChatListDidBecomeVisible.HandleEvent;
				NotificationCenter.DefaultCenter.RemoveObserver (this);

				hasDisposed = true;
			}
		}

		public abstract void DidChangeThumbnailMedia ();
		public abstract void DidDownloadThumbnail ();
		public abstract void DidChangeColorTheme ();
		public abstract void DidChangeDisplayName ();
		public abstract void DismissAccountController ();
		public abstract void DisplayBlankTextInDisplayAlert ();

		public void TrySaveAccount () {
			// if a blank name, prompt user we will user their username (email or phone number) as their display name
			if (string.IsNullOrWhiteSpace (this.TextInDisplayNameField)) {
				DisplayBlankTextInDisplayAlert ();
			} else {
				SaveAccountAsync ();
			}
		}

		public void SaveAccountAsync () {
			EMTask.DispatchBackground (() => {
				if (ShouldSaveOrUpdateAccountInfo()) {
					SaveAccount ();
				}
			});

			DismissAccountController ();
		}
		
		private void SaveAccount () {
			try {
				this.AccountInfo.defaultName = string.IsNullOrEmpty(this.TextInDisplayNameField) ? this.AccountInfo.username.Replace(" ", "") : this.TextInDisplayNameField;
				this.AccountInfo.colorTheme = this.ColorTheme;
				this.AccountInfo.lastUpdated = Convert.ToInt64(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds);

				var parms = new Dictionary<string, object> ();
				var attrs = new Dictionary<string, string> ();

				parms.Add ("defaultName", this.AccountInfo.defaultName);
				attrs.Add ("color", this.AccountInfo.colorTheme.ToHexString());
				parms.Add ("attributes", attrs);
				parms.Add ("lastUpdated", this.AccountInfo.lastUpdated);
				string attributes = JsonConvert.SerializeObject (parms);

				appModel.account.UpdateAccountInfo (this.UpdatedThumbnail, null, attributes, (ResultInput ri) => EMTask.DispatchMain (() => {
					this.UpdatedThumbnail = null;
				}));

				appModel.RecordGAGoal(Preference.GA_SETUP_PROFILE, AnalyticsConstants.CATEGORY_GA_GOAL, 
					AnalyticsConstants.ACTION_SETUP_PROFILE, AnalyticsConstants.PROFILE_CUSTOM_NAME, AnalyticsConstants.VALUE_SETUP_PROFILE);

			} catch (Exception e) {
				Debug.WriteLine(string.Format("Failed to Save Account: {0}\n{1}", e.Message, e.StackTrace));
			}
		}

		bool ShouldSaveOrUpdateAccountInfo() {
			bool same = true;

			if (this.AccountInfo.defaultName != this.TextInDisplayNameField)
				same = false;

			if (this.AccountInfo.colorTheme != this.initialBackgroundColor)
				same = false;

			if (UpdatedThumbnail != null && UpdatedThumbnail.Length > 0)
				same = false;

			return !same;
		}

		protected void AccountDidChangeThumbnailMedia(CounterParty accountInfo) {
			EMTask.DispatchMain (DidChangeThumbnailMedia);
		}

		protected void AccountThumbnailDidLoad(CounterParty accountInfo) {
			EMTask.DispatchMain (DidDownloadThumbnail);
		}

		protected void DidChangeColorTheme(CounterParty accountInfo) {
			EMTask.DispatchMain (DidChangeColorTheme);
		}

		protected void AccountDidChangeDisplayName(CounterParty accountInfo) {
			EMTask.DispatchMain (DidChangeDisplayName);
		}

		protected void BackgroundAccountFailedToDownloadMedia (Notification notification) {
			CounterParty acctInfo = notification.Source as CounterParty;
			if (acctInfo != null) {
				AccountDidChangeThumbnailMedia (acctInfo);
			}
		}
	}
}