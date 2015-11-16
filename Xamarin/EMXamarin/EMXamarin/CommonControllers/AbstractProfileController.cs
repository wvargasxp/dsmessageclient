using System;
using System.Collections.Generic;

namespace em {
	public abstract class AbstractProfileController {

		readonly ApplicationModel appModel;

		public CounterParty Profile { get; set; }
		public ChatEntry chatEntry { get; set; }
		public IList<Contact> Profiles { get; set; }

		WeakDelegateProxy didChangeTempPropertyProxy;
		WeakDelegateProxy ChatListDidBecomeVisible;
		WeakDelegateProxy BlockStatusDidChange;

		protected AbstractProfileController (ApplicationModel theAppModel, CounterParty p) {
			appModel = theAppModel;
			Profile = p;

			var contact = Profile as Contact;
			if (contact != null) {
				didChangeTempPropertyProxy = WeakDelegateProxy.CreateProxy<Property<bool>,bool> (TempPropertyDidChange);
				contact.tempContact.DelegateDidChangePropertyValue += didChangeTempPropertyProxy.HandleEvent<Property<bool>,bool>;

				BlockStatusDidChange = WeakDelegateProxy.CreateProxy<Contact> (HandleDidChangeBlockStatus);
				contact.DelegateDidChangeBlockStatus += BlockStatusDidChange.HandleEvent<Contact>;
			}

			ChatListDidBecomeVisible = WeakDelegateProxy.CreateProxy (Dispose);
			appModel.chatList.DidBecomeVisible += ChatListDidBecomeVisible.HandleEvent;
		}

		~AbstractProfileController() {
			Dispose ();
		}

		protected AbstractProfileController (ApplicationModel theAppModel, ChatEntry ce) {
			appModel = theAppModel;
			chatEntry = ce;
			Profiles = ce.contacts;
		}

		bool HasDisposed = false;
		public void Dispose() {
			if (!HasDisposed) {
				HasDisposed = true;

				var contact = Profile as Contact;
				if (contact != null) {
					contact.tempContact.DelegateDidChangePropertyValue -= didChangeTempPropertyProxy.HandleEvent<Property<bool>,bool>;
					contact.DelegateDidChangeBlockStatus -= BlockStatusDidChange.HandleEvent<Contact>;
				}

				if (ChatListDidBecomeVisible != null)
					appModel.chatList.DidBecomeVisible -= ChatListDidBecomeVisible.HandleEvent;
			}
		}

		public void AddContactAsync(Action<ResultInput> completionHandler) {
			EMTask.DispatchBackground (() => AddContact (completionHandler));
		}

		void AddContact(Action<ResultInput> completionHandler) {
			appModel.account.AddContactToContactList (Profile as Contact, completionHandler);
		}

		public void DidTapBlockButton(Action<ResultInput> completionHandler) {
			var contact = Profile as Contact;
			switch (contact.BlockStatus) {
			case BlockStatus.Blocked:
			case BlockStatus.BothBlocked:
				EMTask.DispatchBackground (() => BackgroundUnblockContact (completionHandler));
				break;

			case BlockStatus.HasBlockedUs:
			case BlockStatus.NotBlocked:
				EMTask.DispatchBackground (() => BackgroundBlockContact (completionHandler));
				break;
			}
		}
			
		void BackgroundBlockContact(Action<ResultInput> completionHandler) {
			appModel.account.BlockContact(Profile as Contact, completionHandler);
		}

		void BackgroundUnblockContact(Action<ResultInput> completionHandler) {
			appModel.account.UnblockContact(Profile as Contact, completionHandler);
		}

		public void RemoveFromAdHocGroupAsync() {
			chatEntry.LeaveConversationAsync ();
		}

		public abstract void DidChangeTempProperty ();
		public abstract void DidChangeBlockStatus (Contact c);
		public abstract void TransitionToChatController (ChatEntry chatEntry);

		protected void TempPropertyDidChange(Property<bool> prop, bool previous) {
			EMTask.DispatchMain (DidChangeTempProperty);
		}

		protected void HandleDidChangeBlockStatus(Contact c) {
			EMTask.DispatchMain (() => DidChangeBlockStatus (c));
		}

		public void SendMessage () {
			EMTask.DispatchBackground (() => {
				var c = Profile as Contact;
				System.Diagnostics.Debug.Assert(c != null, "Expecting the contact to exist!");

				if(c != null) {
					ChatEntry ce = appModel.chatList.FindChatEntryByReplyToServerIDs (new List<string> () { c.serverID }, c.fromAlias);

					if (ce == null) {
						ce = ChatEntry.NewUnderConstructionChatEntry (appModel, DateTime.Now.ToEMStandardTime(appModel));
						appModel.chatList.underConstruction = ce;
						ce.contacts = new List<Contact> () { c };
					}

					TransitionToChatController (ce);	
				}
			});
		}
	}
}