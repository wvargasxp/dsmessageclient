using em;
using Android.OS;
using Android.App;

namespace Emdroid {
	
	public class AndroidNavigationManager : INavigationManager {

		static AndroidNavigationManager _shared = null;
		public static AndroidNavigationManager Shared {
			get {
				if (_shared == null) {
					_shared = new AndroidNavigationManager ();
				}

				return _shared;
			}
		}

		public void StartNewChat (ChatEntry ce) {
			EMTask.DispatchMain (() => {
				var appModel = EMApplication.Instance.appModel;
				appModel.chatList.underConstruction = ce;

				var chatFragment = ChatFragment.NewInstance (ce);

				var args = new Bundle ();
				var index = appModel.chatList.entries.IndexOf (ce);
				args.PutInt ("Position", index >= 0 ? index : ChatFragment.NEW_MESSAGE_INITIATED_FROM_NOTIFICATION_POSITION);
				chatFragment.Arguments = args;

				Activity activity = EMApplication.GetCurrentActivity ();

				activity.FragmentManager.BeginTransaction ()
					.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
					.Replace (Resource.Id.content_frame, chatFragment, "chatEntry" + ce.chatEntryID)
					.AddToBackStack ("chatEntry" + ce.chatEntryID)
					.Commit ();
			});
		}

	}
}