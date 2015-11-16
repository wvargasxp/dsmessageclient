using System;

namespace em {
	public abstract class AbstractPhotoVideoItemController {

		private ChatEntry _chatEntry;
		public ChatEntry ChatEntry {
			get {
				return _chatEntry;
			}

			set {
				_chatEntry = value;
			}
		}

		ApplicationModel appModel;

		WeakDelegateProxy DidReceiveStartDownloadMessageProxy;
		WeakDelegateProxy DidReceiveDownloadPercentCompleteUpdateProxy;
		WeakDelegateProxy DidReceiveCompleteDownloadMessageProxy;

		public AbstractPhotoVideoItemController (ApplicationModel applicationModel, ChatEntry chatEntry) {
			appModel = applicationModel;
			this.ChatEntry = chatEntry;

			DidReceiveStartDownloadMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidReceiveStartDownloadMessage);
			DidReceiveDownloadPercentCompleteUpdateProxy = WeakDelegateProxy.CreateProxy<Message, double> (DidReceiveDownloadPercentCompleteUpdate);
			DidReceiveCompleteDownloadMessageProxy = WeakDelegateProxy.CreateProxy<Message> (DidReceiveCompleteDownloadMessage);

			this.ChatEntry.DelegateDidStartDownload += DidReceiveStartDownloadMessageProxy.HandleEvent<Message>;
			this.ChatEntry.DelegateDidUpdateDownloadPercentComplete += DidReceiveDownloadPercentCompleteUpdateProxy.HandleEvent<Message,double>;
			this.ChatEntry.DelegateDidCompleteDownload += DidReceiveCompleteDownloadMessageProxy.HandleEvent<Message>;
		}

		public void Dispose () {
			this.ChatEntry.DelegateDidStartDownload -= DidReceiveStartDownloadMessageProxy.HandleEvent<Message>;
			this.ChatEntry.DelegateDidUpdateDownloadPercentComplete -= DidReceiveDownloadPercentCompleteUpdateProxy.HandleEvent<Message,double>;
			this.ChatEntry.DelegateDidCompleteDownload -= DidReceiveCompleteDownloadMessageProxy.HandleEvent<Message>;
		}

		protected void DidReceiveStartDownloadMessage (Message message) {
			EMTask.DispatchMain (() => {
				MediaStartedDownload (message);
			});
		}

		protected void DidReceiveDownloadPercentCompleteUpdate (Message message, double compPerc) {
			EMTask.DispatchMain (() => {
				MediaPercentDownloadUpdated (message, compPerc);
			});
		}

		protected void DidReceiveCompleteDownloadMessage (Message message) {
			EMTask.DispatchMain (() => {
				MediaCompletedDownload (message);
			});
		}

		/**
		 * The media associated with a message is being downloaded remotely.
		 */
		public abstract void MediaStartedDownload (Message message);

		/**
		 * The media associated with a message has been incrementally downloaded total
		 * percent of download is supplied
		 */
		public abstract void MediaPercentDownloadUpdated (Message message, double percentComplete);

		/**
		 * The media associated with a message has finished it's download.
		 */
		public abstract void MediaCompletedDownload (Message message);
	}
}

