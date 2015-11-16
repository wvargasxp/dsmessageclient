using System.Collections.Generic;
using System;

namespace em {
	public abstract class AbstractGroupsController {

		readonly ApplicationModel appModel;

		WeakDelegateProxy DidAddGroupProxy;
		WeakDelegateProxy DidUpdateGroupProxy;
		WeakDelegateProxy DidDeleteGroupProxy;
		WeakDelegateProxy DidChangeGroupThumbnailSource;
		WeakDelegateProxy DidDownloadGroupThumbnail;
		WeakDelegateProxy DidChangeOwnColorThemeProxy;
		WeakDelegateProxy ChatListDidBecomeVisible;

		public IList<Group> Groups { get; set; }

		protected AbstractGroupsController (ApplicationModel applicationModel) {
			Groups = null;
			appModel = applicationModel;

			DidAddGroupProxy = WeakDelegateProxy.CreateProxy<Contact> (HandleGroupAdded);
			DidUpdateGroupProxy = WeakDelegateProxy.CreateProxy<Contact> (HandleGroupUpdated);
			DidDeleteGroupProxy = WeakDelegateProxy.CreateProxy<Contact> (HandleGroupDeleted);
			DidChangeGroupThumbnailSource = WeakDelegateProxy.CreateProxy<Contact> (HandleThumbnailSourceChange);
			DidDownloadGroupThumbnail = WeakDelegateProxy.CreateProxy<Contact> (HandleThumbnailDidLoad);
			DidChangeOwnColorThemeProxy = WeakDelegateProxy.CreateProxy<AccountInfo> (DidChangeColorTheme);

			Contact.DelegateDidAddGroup += DidAddGroupProxy.HandleEvent<Contact>;
			Contact.DelegateDidUpdateGroup += DidUpdateGroupProxy.HandleEvent<Contact>;
			Contact.DelegateDidDeleteGroup += DidDeleteGroupProxy.HandleEvent<Contact>;

			appModel.account.accountInfo.DelegateDidChangeColorTheme += DidChangeOwnColorThemeProxy.HandleEvent<CounterParty>;

			ChatListDidBecomeVisible = WeakDelegateProxy.CreateProxy (Dispose);
			appModel.chatList.DidBecomeVisible += ChatListDidBecomeVisible.HandleEvent;

			// used to trigger initial load of groups
			HandleGroupAdded (null);
		}

		public void Dispose() {
			RemoveDelegatesFromGroups ();
			Contact.DelegateDidAddGroup -= DidAddGroupProxy.HandleEvent<Contact>;
			Contact.DelegateDidUpdateGroup -= DidUpdateGroupProxy.HandleEvent<Contact>;
			Contact.DelegateDidDeleteGroup -= DidDeleteGroupProxy.HandleEvent<Contact>;

			appModel.account.accountInfo.DelegateDidChangeColorTheme -= DidChangeOwnColorThemeProxy.HandleEvent<CounterParty>;

			appModel.chatList.DidBecomeVisible -= ChatListDidBecomeVisible.HandleEvent;
		}

		public abstract void GroupsValuesDidChange();
		public abstract void ReloadGroup(Contact group);
		public abstract void TransitionToChatController (ChatEntry chatEntry);

		/*
		 * Callback that the user has changed their own color scheme
		 */
		public abstract void DidChangeColorTheme ();

		protected void HandleGroupAdded (Contact g) {
			Contact.FindAllGroupsAsync (appModel, groups => EMTask.DispatchMain (() => {
				RemoveDelegatesFromGroups ();
				Groups = groups;
				if (Groups != null) {
					foreach (Group group in Groups) {
						group.DelegateDidChangeThumbnailMedia += DidChangeGroupThumbnailSource.HandleEvent<CounterParty>;
						group.DelegateDidDownloadThumbnail += DidDownloadGroupThumbnail.HandleEvent<CounterParty>;
						NotificationCenter.DefaultCenter.AddWeakObserver (group, Constants.Counterparty_DownloadFailed, BackgroundCounterpartyDidFailToDownloadThumbnail);
					}
				}
				GroupsValuesDidChange ();
			}));
		}

		protected void HandleGroupUpdated(Contact g) {
			if (Groups != null) {
				for(int i=0; i < Groups.Count; i++) {
					if(Groups[i].serverID.Equals(g.serverID)) {
						Group oldGroup = Groups [i];
						oldGroup.DelegateDidChangeThumbnailMedia -= DidChangeGroupThumbnailSource.HandleEvent<CounterParty>;
						oldGroup.DelegateDidDownloadThumbnail -= DidDownloadGroupThumbnail.HandleEvent<CounterParty>;
						NotificationCenter.DefaultCenter.RemoveObserverAction (oldGroup, Constants.Counterparty_DownloadFailed, BackgroundCounterpartyDidFailToDownloadThumbnail);

						Group group = Contact.FindGroupByServerID (appModel, g.serverID);
						group.DelegateDidChangeThumbnailMedia += DidChangeGroupThumbnailSource.HandleEvent<CounterParty>;
						group.DelegateDidDownloadThumbnail += DidDownloadGroupThumbnail.HandleEvent<CounterParty>;
						NotificationCenter.DefaultCenter.AddWeakObserver (group, Constants.Counterparty_DownloadFailed, BackgroundCounterpartyDidFailToDownloadThumbnail);

						Groups [i] = group;

						EMTask.DispatchMain (() => ReloadGroup (group));

						break;
					}
				}
			}
		}

		protected void HandleGroupDeleted(Contact g) {
			Contact.FindAllGroupsAsync (appModel, groups => EMTask.DispatchMain (() => {
				RemoveDelegatesFromGroups ();
				Groups = groups;
				if (Groups != null) {
					foreach (Group group in Groups) {
						group.DelegateDidChangeThumbnailMedia += DidChangeGroupThumbnailSource.HandleEvent<CounterParty>;
						group.DelegateDidDownloadThumbnail += DidDownloadGroupThumbnail.HandleEvent<CounterParty>;
						NotificationCenter.DefaultCenter.AddWeakObserver (group, Constants.Counterparty_DownloadFailed, BackgroundCounterpartyDidFailToDownloadThumbnail);
					}
				}
				GroupsValuesDidChange ();
			}));
		}

		protected void RemoveDelegatesFromGroups() {
			if ( Groups != null ) {
				foreach ( Group group in Groups ) {
					group.DelegateDidChangeThumbnailMedia -= DidChangeGroupThumbnailSource.HandleEvent<CounterParty>;
					group.DelegateDidDownloadThumbnail -= DidDownloadGroupThumbnail.HandleEvent<CounterParty>;
					NotificationCenter.DefaultCenter.RemoveObserverAction (group, Constants.Counterparty_DownloadFailed, BackgroundCounterpartyDidFailToDownloadThumbnail);
				}
			}
		}

		protected void HandleThumbnailSourceChange(CounterParty group) {
			EMTask.DispatchMain (() => ReloadGroup (group as Contact));
		}

		protected void HandleThumbnailDidLoad(CounterParty group) {
			EMTask.DispatchMain (() => ReloadGroup (group as Contact));
		}

		protected void DidChangeColorTheme(CounterParty accountInfo) {
			EMTask.DispatchMain (DidChangeColorTheme);
		}

		protected void BackgroundCounterpartyDidFailToDownloadThumbnail (Notification notification) {
			var counterparty = notification.Source as CounterParty;
			if (counterparty != null) {
				EMTask.DispatchMain (() => ReloadGroup (counterparty as Contact));
			}
		}

		public void GoToNewOrExistingChatEntry (Contact contact) {
			ChatList chatList = this.appModel.chatList;
			ChatEntry ce = chatList.FindChatEntryByReplyToServerIDs( new List<string> { contact.serverID }, contact.fromAlias);
			if ( ce == null ) {
				ce = ChatEntry.NewUnderConstructionChatEntry (this.appModel, DateTime.Now.ToEMStandardTime (this.appModel));
				chatList.underConstruction = ce;
				ce.contacts = new List<Contact> { contact };
			}

			TransitionToChatController (ce);
		}
	}
}