using em;
using UIKit;

namespace iOS {
	
	public class iOSNavigationManager : INavigationManager {

		static iOSNavigationManager _shared = null;
		public static iOSNavigationManager Shared {
			get {
				if (_shared == null) {
					_shared = new iOSNavigationManager ();
				}

				return _shared;
			}
		}

		public void StartNewChat (ChatEntry ce) {
			System.Diagnostics.Debug.WriteLine (ce.chatEntryID);
			EMTask.DispatchMain (() => {
				var appModel = AppDelegate.Instance.applicationModel;
				appModel.chatList.underConstruction = ce;

				MainController mainController = AppDelegate.Instance.MainController;
				UINavigationController navController = mainController.ContentController as UINavigationController;
				bool didFindChatEntry = AppDelegate.Instance.PopToChatEntry (ce);
				if (!didFindChatEntry) {
					ChatViewController controller = new ChatViewController (ce);
					navController.PushViewController(controller, true);
				}
			});
		}

	}
}