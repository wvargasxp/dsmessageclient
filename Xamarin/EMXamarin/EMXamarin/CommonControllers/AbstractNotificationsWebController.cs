using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace em {
	public abstract class AbstractNotificationsWebController {
		readonly ApplicationModel appModel;

		WeakDelegateProxy didChangeColorThemeProxy;

		public AbstractNotificationsWebController (ApplicationModel theAppModel) {
			appModel = theAppModel;

			didChangeColorThemeProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (DidChangeColorTheme);

			appModel.account.accountInfo.DelegateDidChangeColorTheme += didChangeColorThemeProxy.HandleEvent<CounterParty>;
		}

		public void Dispose() {
			appModel.account.accountInfo.DelegateDidChangeColorTheme -= didChangeColorThemeProxy.HandleEvent<CounterParty>;
		}

		public abstract void DidChangeColorTheme ();

		public abstract void GoToChatControllerUsingChatEntry (ChatEntry chatEntry);

		protected void DidChangeColorTheme(CounterParty accountInfo) {
			EMTask.DispatchMain (() => {
				DidChangeColorTheme ();
			});
		}

		public void GoToNewOrExistingChatEntry (Contact contact) {
			Debug.Assert (contact != null, "Expected contact to be non null here.");
			if (contact != null) {
				ChatList chatList = this.appModel.chatList;
				ChatEntry ce = chatList.FindChatEntryByReplyToServerIDs (new List<string> { contact.serverID }, contact.LastUsedIdentifierToSendFrom);
				if (ce == null) {
					ce = ChatEntry.NewUnderConstructionChatEntry (this.appModel, DateTime.Now.ToEMStandardTime (this.appModel));
					chatList.underConstruction = ce;
					ce.contacts = new List<Contact> () { contact };
				}

				GoToChatControllerUsingChatEntry (ce);
			}
		}
	}
}

