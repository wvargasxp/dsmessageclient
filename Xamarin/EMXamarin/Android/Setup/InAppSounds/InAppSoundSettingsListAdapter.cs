using System;
using System.Collections.Generic;
using em;

namespace Emdroid {
	public class InAppSoundSettingsListAdapter : EmRecyclerViewAdapter {
		private WeakReference _r = null;
		private InAppSoundSettingsFragment Fragment {
			get { return this._r != null ? this._r.Target as InAppSoundSettingsFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		/*
		 * The int that this returns is the position of the element in the model.
		 * Not the position of the UI element. If a header is showing, the UI position would be incremented by one.
		 */ 
		public event EventHandler<SettingChange<SoundSetting>> CheckBoxClick;

		/*
		 * @param uiPosition - A position that would be used in conjunction with our view, including a header if present.
		 * Calls the ItemClick event and returns back a model position.
		 */ 
		protected void OnCheckBoxClick (SettingChange<SoundSetting> result) {
			if (CheckBoxClick != null) {
				CheckBoxClick (this, result);
			}
		}

		public InAppSoundSettingsListAdapter (InAppSoundSettingsFragment f) {
			this.Fragment = f;
		}

		#region implemented abstract members of Adapter

		public override void OnBindViewHolder (Android.Support.V7.Widget.RecyclerView.ViewHolder holder, int position) {
			InAppSoundSettingsFragment fragment = this.Fragment;
			if (fragment == null) return;

			SharedInAppSoundSettingsController shared = fragment.Shared;
			IList<SoundSetting> settings = shared.Settings;

			SoundSetting menuSetting = settings [position];
			InAppSoundSettingsViewHolder sHolder = holder as InAppSoundSettingsViewHolder;
			System.Diagnostics.Debug.Assert (sHolder != null, "Holder is null after a cast.");
			sHolder.Setting = menuSetting;
		}

		public override Android.Support.V7.Widget.RecyclerView.ViewHolder OnCreateViewHolder (Android.Views.ViewGroup parent, int viewType) {
			return InAppSoundSettingsViewHolder.NewInstance (parent, OnClick, OnCheckBoxClick);
		}

		public override int ItemCount {
			get {
				InAppSoundSettingsFragment fragment = this.Fragment;
				if (fragment == null) return 0;

				SharedInAppSoundSettingsController shared = fragment.Shared;
				IList<SoundSetting> settings = shared.Settings;
				return settings.Count;
			}
		}

		#endregion
	}
}