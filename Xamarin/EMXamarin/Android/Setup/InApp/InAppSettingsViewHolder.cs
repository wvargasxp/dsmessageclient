using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class InAppSettingsViewHolder : RecyclerView.ViewHolder {

		private Action<int> ItemClick { get; set; }
		private Action<SettingChange<InAppSetting>> CheckBoxClick { get; set; }
		public TextView Title { get; set; }
		public SwitchCompat Switch { get; set; }

		public static InAppSettingsViewHolder NewInstance (ViewGroup parent, Action<int> itemClick, Action<SettingChange<InAppSetting>> checkBoxClick) {
			View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.reuseable_nested_setting_item, parent, false);
			FontHelper.SetFontOnAllViews (view as ViewGroup);
			InAppSettingsViewHolder holder = new InAppSettingsViewHolder (view, itemClick, checkBoxClick);
			return holder;
		}

		public InAppSettingsViewHolder (View convertView, Action<int> itemClick, Action<SettingChange<InAppSetting>> checkBoxClick) : base (convertView) {
			this.ItemClick = itemClick;
			this.CheckBoxClick = checkBoxClick;
			this.Title = convertView.FindViewById<TextView> (Resource.Id.menuLabel);

			this.Switch = convertView.FindViewById<SwitchCompat> (Resource.Id.switchy);
			this.Switch.CheckedChange += Switch_CheckedChange;
			convertView.Click += ConvertView_Click;
		}

		private void Switch_CheckedChange (object sender, CompoundButton.CheckedChangeEventArgs e) {
			SettingChange<InAppSetting> res = SettingChange<InAppSetting>.From (this.Setting, this.Switch.Checked);
			this.CheckBoxClick (res);
		}

		private void ConvertView_Click (object sender, EventArgs e) {
			this.ItemClick (base.AdapterPosition);
		}

		private InAppSetting _setting;
		public InAppSetting Setting {
			get { return this._setting; }
			set { 
				this._setting = value;

				switch (this._setting) {
				case InAppSetting.ReceiveInAppBanner: 
					{
						this.Title.Text = "IN_APP_NOTIFICATIONS".t ();
						break;
					}
				}

				this.Switch.Checked = EMApplication.Instance.appModel.account.UserSettings.EnabledForBannerSetting (this._setting);
			}
		}
	}
}