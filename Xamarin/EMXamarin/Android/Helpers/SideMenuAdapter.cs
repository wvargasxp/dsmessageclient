using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class SideMenuAdapter<T> : ArrayAdapter<T> {

		readonly LayoutInflater layoutInflator;

		readonly IList<string> mItems;
		ViewHolder holder;
		TextView unreadNotificationCount;

		public SideMenuAdapter (Context context, int textViewResourceId, IList<T> objects) : base (context, textViewResourceId, objects) {
			mItems = (IList<string>)objects;
			layoutInflator = (LayoutInflater)context.GetSystemService (Context.LayoutInflaterService);
			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, em.Constants.NotificationEntryDao_UnreadCountChanged, HandleNotificationNotificationEntryCountChanged);
		}

		private void HandleNotificationNotificationEntryCountChanged (em.Notification notif) {
			UpdateUnreadNotificationCount ();
		}

		public void UpdateUnreadNotificationCount() {
			EMApplication.Instance.appModel.notificationList.ObtainUnreadCountAsync ((int unread) => {
				EMTask.DispatchMain (() => {
					if (unreadNotificationCount != null) {
						unreadNotificationCount.Text = unread.ToString ();
					}
				});
			});
		}

		protected override void Dispose (bool disposing) {
			em.NotificationCenter.DefaultCenter.RemoveObserver (this);
			base.Dispose (disposing);
		}

		public override View GetView (int position, View convertView, ViewGroup parent) {
			RelativeLayout sideMenuView;

			if (convertView == null) {
				sideMenuView = (RelativeLayout)layoutInflator.Inflate (Resource.Layout.side_menu_item, parent, false);
				FontHelper.SetFontOnAllViews (sideMenuView);
				holder = new ViewHolder ();
				holder.TopLine = sideMenuView.FindViewById<View> (Resource.Id.topLine);
				holder.Title = sideMenuView.FindViewById<TextView> (Resource.Id.sideMenuLabel);
				holder.Icon = sideMenuView.FindViewById<ImageView> (Resource.Id.sideMenuIcon);
				holder.AccessoryIcon = sideMenuView.FindViewById<ImageView> (Resource.Id.accessoryIcon);
				holder.AccessoryLabel = sideMenuView.FindViewById<TextView> (Resource.Id.accessoryLabel);
				holder.AccessoryIconWrapper = sideMenuView.FindViewById<RelativeLayout> (Resource.Id.sideMenuAccessory);
				// save reference to unread count textview in a class variable so I can update it (and only it) when needed
				if ((SideMenuItems)position == SideMenuItems.Notifications)
					unreadNotificationCount = holder.AccessoryLabel;

				holder.BottomLine = sideMenuView.FindViewById<View> (Resource.Id.bottomLine);
				sideMenuView.Tag = holder;
			} else {
				sideMenuView = (RelativeLayout)convertView;
				holder = (ViewHolder)convertView.Tag;
			}

			if (position < mItems.Count) {
				var sideMenuItem = (SideMenuItems)position;
				switch (sideMenuItem) {
				case SideMenuItems.Account:
					holder.Title.Text = "MY_ACCOUNT_TITLE".t ();
					holder.Icon.SetImageResource (Resource.Drawable.iconAccount);
					holder.AccessoryIconWrapper.Visibility = ViewStates.Gone;
					break;
				case SideMenuItems.Alias:
					holder.Title.Text = "ALIAS_TITLE".t ();
					holder.Icon.SetImageResource (Resource.Drawable.iconAlias);
					holder.AccessoryIconWrapper.Visibility = ViewStates.Gone;
					break;
				case SideMenuItems.Notifications:
					holder.Title.Text = "NOTIFICATIONS_TITLE".t ();
					holder.Icon.SetImageResource (Resource.Drawable.iconNotify);
					holder.AccessoryIconWrapper.Visibility = ViewStates.Visible;
					holder.AccessoryIcon.SetImageResource (Resource.Drawable.iconNotifyCounter);
					UpdateUnreadNotificationCount ();
					break;
				case SideMenuItems.Groups:
					holder.Title.Text = "GROUPS_TITLE".t ();
					holder.Icon.SetImageResource (Resource.Drawable.iconGroup);
					holder.AccessoryIconWrapper.Visibility = ViewStates.Gone;
					break;
				case SideMenuItems.Invite:
					{
						holder.Title.Text = "INVITE_FRIENDS_TITLE".t ();
						holder.Icon.SetImageResource (Resource.Drawable.iconInviteFriend);
						holder.AccessoryIconWrapper.Visibility = ViewStates.Gone;
						break;
					}
				/*
				case SideMenuItems.Search:
					holder.title.Text = "SEARCH_TITLE".t ();
					holder.icon.SetImageResource (Resource.Drawable.iconFind);
					break;
				*/
				case SideMenuItems.Help:
					holder.Title.Text = "HELP_TITLE".t ();
					holder.Icon.SetImageResource (Resource.Drawable.iconHelp);
					holder.AccessoryIconWrapper.Visibility = ViewStates.Gone;
					break;
				case SideMenuItems.Settings:
					holder.Title.Text = "SETTINGS_TITLE".t ();
					holder.Icon.SetImageResource (Resource.Drawable.iconSettings);
					holder.AccessoryIconWrapper.Visibility = ViewStates.Gone;
					break;
				case SideMenuItems.About:
					holder.Title.Text = "ABOUT_TITLE".t ();
					holder.Icon.SetImageResource (Resource.Drawable.iconInfo);
					holder.AccessoryIconWrapper.Visibility = ViewStates.Gone;
					break;
				default:
					holder.Icon.SetImageResource (Resource.Drawable.iconUser); // not used yet
					holder.AccessoryIconWrapper.Visibility = ViewStates.Gone;
					break;
				}
			}

			if (position != 0 || position != 0 && position < mItems.Count-1)
				holder.TopLine.Visibility = ViewStates.Invisible;

			return sideMenuView;
		}

		class ViewHolder : Java.Lang.Object {
			public TextView Title { get; set; }
			public View TopLine { get; set; }
			public ImageView Icon { get; set; }
			public ImageView AccessoryIcon { get; set; }
			public TextView AccessoryLabel { get; set; }
			public View BottomLine { get; set; }
			public RelativeLayout AccessoryIconWrapper { get; set; }
		}
	}
}