using System;
using System.Collections.Generic;
using Android.Support.V7.Widget;
using em;

namespace Emdroid {
	public class SettingsListAdapter : EmRecyclerViewAdapter {
		private WeakReference _r = null;
		private SettingsFragment Fragment {
			get { return this._r != null ? this._r.Target as SettingsFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SettingsListAdapter (SettingsFragment f) {
			this.Fragment = f;
		}

		#region implemented abstract members of Adapter

		public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position) {
			SettingsFragment fragment = this.Fragment;
			if (fragment == null) return;

			SharedSettingsController shared = fragment.Shared;
			IList<SettingMenuItem> settings = shared.Settings;

			SettingMenuItem menuSetting = settings [position];

			SettingsViewHolder sHolder = holder as SettingsViewHolder;
			System.Diagnostics.Debug.Assert (sHolder != null, "Holder is null after a cast.");
			sHolder.Menu = menuSetting;
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder (Android.Views.ViewGroup parent, int viewType) {
			return SettingsViewHolder.NewInstance (parent, OnClick);
		}

		public override int ItemCount {
			get {
				SettingsFragment fragment = this.Fragment;
				if (fragment == null) return 0;

				SharedSettingsController shared = fragment.Shared;
				IList<SettingMenuItem> settings = shared.Settings;
				return settings.Count;
			}
		}

		#endregion
	}
}