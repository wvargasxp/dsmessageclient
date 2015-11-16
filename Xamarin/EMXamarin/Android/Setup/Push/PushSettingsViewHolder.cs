using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class PushSettingsViewHolder : RecyclerView.ViewHolder {
		private Action<int> ItemClick { get; set; }
		private Action<SettingChange<PushSetting>> CheckBoxClick { get; set; }
		public TextView Title { get; set; }
		public SwitchCompat Switch { get; set; }

		public static PushSettingsViewHolder NewInstance (ViewGroup parent, Action<int> itemClick, Action<SettingChange<PushSetting>> checkBoxClick) {
			View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.reuseable_nested_setting_item, parent, false);
			FontHelper.SetFontOnAllViews (view as ViewGroup);
			PushSettingsViewHolder holder = new PushSettingsViewHolder (view, itemClick, checkBoxClick);
			return holder;
		}

		public PushSettingsViewHolder (View convertView, Action<int> itemClick, Action<SettingChange<PushSetting>> checkBoxClick) : base (convertView) {
			this.ItemClick = itemClick;
			this.CheckBoxClick = checkBoxClick;
			this.Title = convertView.FindViewById<TextView> (Resource.Id.menuLabel);
			this.Switch = convertView.FindViewById<SwitchCompat> (Resource.Id.switchy);
			this.Switch.CheckedChange += Switch_CheckedChange;
			convertView.Click += ConvertView_Click;
		}

		private void Switch_CheckedChange (object sender, CompoundButton.CheckedChangeEventArgs e) {
			SettingChange<PushSetting> res = SettingChange<PushSetting>.From (this.Setting, this.Switch.Checked);
			this.CheckBoxClick (res);
		}

		private void ConvertView_Click (object sender, EventArgs e) {
			this.ItemClick (base.AdapterPosition);
		}

		PushSetting _setting;
		public PushSetting Setting {
			get { return this._setting; }
			set { 
				this._setting = value; 

				switch (this._setting) {
				case PushSetting.EnableNotifications: 
					{
						this.Title.Text = "OFFLINE_NOTIFICATIONS_SHOW".t ();
						break;
					}
				case PushSetting.EnableSounds:
					{
						this.Title.Text = "PLAY_SOUNDS".t ();
						break;
					}
				}

				this.Switch.Checked = EMApplication.Instance.appModel.account.UserSettings.EnabledForSetting (this._setting);
			}
		}
	}
}