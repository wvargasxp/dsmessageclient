using System;
using Android.Views;
using Com.Nhaarman.Listviewanimations.Itemmanipulation.Swipedismiss;

namespace Emdroid {

	public class NotificationsDismissCallback : Java.Lang.Object, IOnDismissCallback {

		readonly NotificationsFragment frag;

		public NotificationsDismissCallback (NotificationsFragment fragment) {
			frag = fragment;
		}

		public void OnDismiss (ViewGroup listView, int[] reverseSortedPositions) {
			if (reverseSortedPositions.Length > 0) {
				//get the last position, as this is the position of the latest action
				var position = reverseSortedPositions [reverseSortedPositions.Length - 1];
				//frag.notificationsList.RemoveNotificationEntryAtAsync (position);
			}
		}
	}
}