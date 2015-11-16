using System;

namespace em {
	public abstract class AbstractHelpController {
		readonly ApplicationModel appModel;

		WeakDelegateProxy didChangeColorThemeProxy;

		public AbstractHelpController (ApplicationModel theAppModel) {
			appModel = theAppModel;

			didChangeColorThemeProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (DidChangeColorTheme);

			appModel.account.accountInfo.DelegateDidChangeColorTheme += didChangeColorThemeProxy.HandleEvent<CounterParty>;
		}

		public void Dispose() {
			appModel.account.accountInfo.DelegateDidChangeColorTheme -= didChangeColorThemeProxy.HandleEvent<CounterParty>;
		}

		public abstract void DidChangeColorTheme ();

		protected void DidChangeColorTheme(CounterParty accountInfo) {
			EMTask.DispatchMain (() => {
				DidChangeColorTheme ();
			});
		}
	}
}

