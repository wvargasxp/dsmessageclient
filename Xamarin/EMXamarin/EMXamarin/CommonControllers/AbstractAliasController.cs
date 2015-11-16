using System.Collections.Generic;

namespace em {
	public abstract class AbstractAliasController {

		readonly ApplicationModel appModel;

		public static readonly int MAX_NUMBER_ALIASES = 3;

		WeakDelegateProxy DidUpdateAliasListProxy;

		WeakDelegateProxy DidChangeAliasColorThemeProxy;
		WeakDelegateProxy DidChangeAliasThumbnailSourceProxy;
		WeakDelegateProxy DidDownloadAliasThumbnailProxy;
		WeakDelegateProxy DidChangeAliasIconSourceProxy;
		WeakDelegateProxy DidDownloadAliasIconProxy;
		WeakDelegateProxy DidChangeLifecycleProxy;

		IList<AliasInfo> a;
		public IList<AliasInfo> Aliases {
			get {
				return a;
			}

			set {
				if (a != null)
					RemoveDelegatesFromAliases ();

				a = value;

				if (a != null)
					AddDelegatesToAliases ();
			}
		}

		protected AbstractAliasController (ApplicationModel _appModel) {
			appModel = _appModel;

			DidUpdateAliasListProxy = WeakDelegateProxy.CreateProxy (AliasListUpdated);
			AccountInfo.DelegateDidUpdateAliasList += DidUpdateAliasListProxy.HandleEvent;

			DidChangeAliasColorThemeProxy = WeakDelegateProxy.CreateProxy<CounterParty> (AliasDidChangeColorTheme);
			DidChangeAliasThumbnailSourceProxy = WeakDelegateProxy.CreateProxy<CounterParty> (AliasDidChangeThumbnailMedia);
			DidDownloadAliasThumbnailProxy = WeakDelegateProxy.CreateProxy<CounterParty> (AliasDidDownloadThumbnail);
			DidChangeAliasIconSourceProxy = WeakDelegateProxy.CreateProxy<AliasInfo> (AliasDidChangeIconMedia);
			DidDownloadAliasIconProxy = WeakDelegateProxy.CreateProxy<AliasInfo> (AliasDidDownloadIcon);
			DidChangeLifecycleProxy = WeakDelegateProxy.CreateProxy<CounterParty> (AliasDidUpdateLifecycle);

			Aliases = appModel.account.accountInfo.VisibleAliases;
		}

		~AbstractAliasController() {
			Dispose ();
		}

		bool hasDisposed = false;
		public void Dispose() {
			if (!hasDisposed) {
				AccountInfo.DelegateDidUpdateAliasList -= DidUpdateAliasListProxy.HandleEvent;

				RemoveDelegatesFromAliases ();

				hasDisposed = true;
			}
		}

		public bool EnableAddNewAliasButton() {
			return appModel.account.accountInfo.VisibleAliases.Count < MAX_NUMBER_ALIASES;
		}

		public void ReactivateAlias(AliasInfo alias) {
			appModel.account.ReactivateAlias(alias.serverID, (ResultInput obj) =>  {
			});
		}

		public abstract void DidChangeAliasList();
		public abstract void DidChangeColorTheme ();
		public abstract void DidChangeThumbnailMedia ();
		public abstract void DidDownloadThumbnail ();
		public abstract void DidChangeIconMedia ();
		public abstract void DidDownloadIcon ();
		public abstract void DidUpdateLifecycle ();

		protected void AddDelegatesToAliases() {
			if (Aliases != null) {
				foreach (AliasInfo alias in Aliases) {
					alias.DelegateDidChangeColorTheme += DidChangeAliasColorThemeProxy.HandleEvent<CounterParty>;
					alias.DelegateDidChangeThumbnailMedia += DidChangeAliasThumbnailSourceProxy.HandleEvent<CounterParty>;
					alias.DelegateDidDownloadThumbnail += DidDownloadAliasThumbnailProxy.HandleEvent<CounterParty>;
					alias.DelegateChangeAliasIcon += DidChangeAliasIconSourceProxy.HandleEvent<AliasInfo>;
					alias.DelegateDownloadAliasIcon += DidDownloadAliasIconProxy.HandleEvent<AliasInfo>;
					alias.DelegateDidChangeLifecycle += DidChangeLifecycleProxy.HandleEvent<CounterParty>;

					NotificationCenter.DefaultCenter.AddWeakObserver (alias, Constants.Counterparty_DownloadFailed, BackgroundAliasDidFailDownloadThumbnail);
				}
			}
		}

		protected void RemoveDelegatesFromAliases() {
			if ( Aliases != null ) {
				foreach ( AliasInfo alias in Aliases ) {
					alias.DelegateDidChangeColorTheme -= DidChangeAliasColorThemeProxy.HandleEvent<CounterParty>;
					alias.DelegateDidChangeThumbnailMedia -= DidChangeAliasThumbnailSourceProxy.HandleEvent<CounterParty>;
					alias.DelegateDidDownloadThumbnail -= DidDownloadAliasThumbnailProxy.HandleEvent<CounterParty>;
					alias.DelegateChangeAliasIcon -= DidChangeAliasIconSourceProxy.HandleEvent<AliasInfo>;
					alias.DelegateDownloadAliasIcon -= DidDownloadAliasIconProxy.HandleEvent<AliasInfo>;
					alias.DelegateDidChangeLifecycle -= DidChangeLifecycleProxy.HandleEvent<CounterParty>;

					NotificationCenter.DefaultCenter.RemoveObserverAction (alias, Constants.Counterparty_DownloadFailed, BackgroundAliasDidFailDownloadThumbnail);
				}
			}
		}

		protected void AliasListUpdated() {
			EMTask.DispatchMain (() => {
				Aliases = appModel.account.accountInfo.VisibleAliases;
				DidChangeAliasList ();
			});
		}

		protected void AliasDidChangeColorTheme(CounterParty cp) {
			EMTask.DispatchMain (DidChangeColorTheme);
		}

		protected void AliasDidChangeThumbnailMedia(CounterParty cp) {
			EMTask.DispatchMain (DidChangeThumbnailMedia);
		}

		protected void AliasDidDownloadThumbnail(CounterParty cp) {
			EMTask.DispatchMain (DidDownloadThumbnail);
		}

		protected void BackgroundAliasDidFailDownloadThumbnail (Notification notification) {
			EMTask.DispatchMain (DidChangeThumbnailMedia);
		}

		protected void AliasDidChangeIconMedia(AliasInfo alias) {
			EMTask.DispatchMain (DidChangeIconMedia);
		}

		protected void AliasDidDownloadIcon(AliasInfo alias) {
			EMTask.DispatchMain (DidDownloadIcon);
		}

		protected void AliasDidUpdateLifecycle(CounterParty alias) {
			EMTask.DispatchMain (() => {
				Aliases = appModel.account.accountInfo.VisibleAliases;
				DidUpdateLifecycle ();
			});
		}

	}
}