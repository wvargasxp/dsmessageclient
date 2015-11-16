using System;
using System.Collections.Generic;

namespace em {
	public abstract class AbstractMediaGalleryController {

		private int _currentPage;
		public int CurrentPage {
			get { return this._currentPage; }
			set {
				this._currentPage = value;

				IList<Message> messages = this.MediaMessages;
				if (messages != null && messages.Count > this._currentPage) {
					Message message = messages [this._currentPage];
					IMediaMessagesProvider provider = this.Provider;
					if (provider == null) return;
					provider.UpdateLastSeenMediaMessage (message);
				}
			}
		}

		private WeakReference _r = null;
		private IMediaMessagesProvider Provider { 
			get { return this._r != null ? this._r.Target as IMediaMessagesProvider : null; }
			set { this._r = new WeakReference (value); }
		}

		private IList<Message> mediaMessages;
		public IList<Message> MediaMessages {
			get { return mediaMessages; }
			set { mediaMessages = value; }
		}

		public AbstractMediaGalleryController (IMediaMessagesProvider provider, ApplicationModel model) {
			this.Provider = provider;
			SetMediaMessagesAndCurrentPage ();
		}

		private void SetMediaMessagesAndCurrentPage () {
			IMediaMessagesProvider provider = this.Provider;
			if (provider == null) return;

			this.MediaMessages = provider.GetMediaMessages ();
			this.CurrentPage = provider.GetCurrentPage ();
		}

		private void HandleProviderFinishedRetrievingPreviousMessages (em.Notification notif) {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractMediaGalleryController self = thisRef.Target as AbstractMediaGalleryController;
				if (self == null) return;
				self.Update ();
			});
		}

		private void HandleMoreMediaMessagesAdded (em.Notification n) {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractMediaGalleryController self = thisRef.Target as AbstractMediaGalleryController;
				if (self == null) return;
				self.Update ();
			});
		}

		private void Update () {
			SetMediaMessagesAndCurrentPage ();
			UpdateDatasourceAndDelegates ();
			SetInitialController ();
			UpdateTitleBar ();
		}

		public void AddObservers () {
			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.AbstractChatController_FinishedRetrievingMorePreviousMessages, HandleProviderFinishedRetrievingPreviousMessages);
			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.AbstractChatController_NewMediaMessageAdded, HandleMoreMediaMessagesAdded);
		}

		public void RemoveObservers () {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
		}

		public void CheckIfNeedsToRequestMoreMessages () {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractMediaGalleryController self = thisRef.Target as AbstractMediaGalleryController;
				if (self == null) return;
				IMediaMessagesProvider provider = self.Provider;
				if (provider == null) return;

				if (provider.HasMoreMediaMessagesToRequest ()) {
					if (self.CurrentPage < 5) {
						Message currentMessage = self.MediaMessages [self.CurrentPage];
						provider.RequestMoreMediaMessages (currentMessage);
					}
				}
			});
		}

		public Message MessageForCurrentPage {
			get {
				return this.MediaMessages [this.CurrentPage];
			}
		}

		public void Dispose () {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
		}

		public abstract void UpdateDatasourceAndDelegates ();
		public abstract void SetInitialController ();
		public abstract void UpdateTitleBar ();
	}
}

