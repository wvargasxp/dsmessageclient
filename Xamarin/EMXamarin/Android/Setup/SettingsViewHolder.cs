using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class SettingsViewHolder : RecyclerView.ViewHolder {
		private Action<int> ItemClick { get; set; }
		public TextView Title { get; set; }

		public static SettingsViewHolder NewInstance (ViewGroup parent, Action<int> itemClick) {
			View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.settings_menu_item, parent, false);
			FontHelper.SetFontOnAllViews (view as ViewGroup);
			SettingsViewHolder holder = new SettingsViewHolder (view, itemClick);
			return holder;
		}

		public SettingsViewHolder (View convertView, Action<int> itemClick) : base (convertView) {
			this.ItemClick = itemClick;
			this.Title = convertView.FindViewById<TextView> (Resource.Id.menuLabel);
			convertView.Click += ConvertView_Click;
		}

		private void ConvertView_Click (object sender, EventArgs e) {
			this.ItemClick (base.AdapterPosition);
		}

		SettingMenuItem _menuItem;
		public SettingMenuItem Menu {
			get { return this._menuItem; }
			set {
				this._menuItem = value;

				switch (this._menuItem) {
				case SettingMenuItem.Push:
					{
						this.Title.Text = "OFFLINE_NOTIFICATIONS_TITLE".t ();
						break;
					}
				case SettingMenuItem.InAppSounds:
					{
						this.Title.Text = "SOUNDS_TITLE".t ();
						break;
					}
				case SettingMenuItem.InAppSettings:
					{
						this.Title.Text = "IN_APP_SETTINGS_TITLE".t ();
						break;
					}
				}
			}
		}
	}
}