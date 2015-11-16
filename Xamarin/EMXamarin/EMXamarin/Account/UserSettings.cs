using System;
using System.Diagnostics;

namespace em {
	public class UserSettings {
		private const string PushEnabledPreferenceKey = "push_enabled_prefernece_key";
		private const string PushSoundEnabledPreferenceKey = "push_sound_enabled_preference_key";
		private const string ReceiveIncomingSoundKey = "in_app_sound_receive_incoming_key";
		private const string ReceiveIncomingBannerKey = "in_app_settings_receive_incoming_banner";
		private const string LastTimeTrackedInstalledAppsKey = "last_time_tracked_installed_apps_key";

		public ApplicationModel AppModel { get; set; }

		public UserSettings () {}

		public bool EnabledForSoundSetting (SoundSetting item) {
			switch (item) {
			default:
			case SoundSetting.ReceiveIncomingMessagesSound:
				{
					return this.IncomingSoundEnabled;
				}
			}
		}

		public bool EnabledForBannerSetting (InAppSetting item) {
			switch (item) {
			default:
			case InAppSetting.ReceiveInAppBanner:
				{
					return this.IncomingBannerEnabled;
				}
			}
		}

		public bool EnabledForSetting (PushSetting item) {
			switch (item) {
			default:
			case PushSetting.EnableNotifications:
				{
					return this.PushShowInNotificationCenterEnabled;
				}
			case PushSetting.EnableSounds:
				{
					return this.PushWithSoundEnabled;
				}
			}
		}

		public void HandlePushSettingChange<T> (SettingChange<T> change) {
			if (change is SettingChange<PushSetting>) {
				SettingChange<PushSetting> changed = change as SettingChange<PushSetting>;
				PushSetting item = changed.Item;
				bool enabled = changed.Enabled;
				switch (item) {
				default:
				case PushSetting.EnableNotifications:
					{
						this.PushShowInNotificationCenterEnabled = enabled;
						break;
					}
				case PushSetting.EnableSounds:
					{
						this.PushWithSoundEnabled = enabled;
						break;
					}
				}
			}

			if (change is SettingChange<SoundSetting>) {
				SettingChange<SoundSetting> changed = change as SettingChange<SoundSetting>;
				SoundSetting item = changed.Item;
				bool enabled = changed.Enabled;
				switch (item) {
				case SoundSetting.ReceiveIncomingMessagesSound:
					{
						this.IncomingSoundEnabled = enabled;
						break;
					}
				}
			}

			if (change is SettingChange<InAppSetting>) {
				SettingChange<InAppSetting> changed = change as SettingChange<InAppSetting>;
				InAppSetting item = changed.Item;
				bool enabled = changed.Enabled;
				switch (item) {
				case InAppSetting.ReceiveInAppBanner:
					{
						this.IncomingBannerEnabled = enabled;
						break;
					}
				}
			}
		}

		private bool? _incomingBannerEnabled = null;
		public bool IncomingBannerEnabled {
			set { SetPreferenceWithKey (ref this._incomingBannerEnabled, ReceiveIncomingBannerKey, value); }
			get { return GetPreferenceWithKey (ref this._incomingBannerEnabled, ReceiveIncomingBannerKey); }
		}

		private bool? _incomingSoundEnabled = null;
		public bool IncomingSoundEnabled {
			set { SetPreferenceWithKey (ref this._incomingSoundEnabled, ReceiveIncomingSoundKey, value); }
			get { return GetPreferenceWithKey (ref this._incomingSoundEnabled, ReceiveIncomingSoundKey); }
		}
			
		private bool? _pushShowInNotificationCenterEnabled = null;
		public bool PushShowInNotificationCenterEnabled {
			set { SetPreferenceWithKey (ref this._pushShowInNotificationCenterEnabled, PushEnabledPreferenceKey, value); }
			get { return GetPreferenceWithKey (ref this._pushShowInNotificationCenterEnabled, PushEnabledPreferenceKey); }
		}

		private bool? _pushWithSoundEnabled = null;
		public bool PushWithSoundEnabled {
			set { SetPreferenceWithKey (ref this._pushWithSoundEnabled, PushSoundEnabledPreferenceKey, value); }
			get { return GetPreferenceWithKey (ref this._pushWithSoundEnabled, PushSoundEnabledPreferenceKey); }
		}

		private void SetPreferenceWithKey (ref bool? val, string key, bool newValue) {
			Debug.Assert (this.AppModel != null, "Using UserSettings object before setting AppModel.");
			if (!val.HasValue) {
				val = newValue;
				EMTask.DispatchBackground (() => {
					Preference.UpdatePreference<bool> (this.AppModel, key, newValue);
				});
			} else {
				bool oldValue = val.Value;
				val = newValue;

				if (oldValue != val.Value) {
					EMTask.DispatchBackground (() => {
						Preference.UpdatePreference<bool> (this.AppModel, key, newValue);
					});
				}
			}
		} 

		private bool GetPreferenceWithKey (ref bool? val, string key) {
			Debug.Assert (this.AppModel != null, "Using UserSettings object before setting AppModel.");
			if (!val.HasValue) {
				bool prefExist = Preference.DoesPreferenceExist (this.AppModel, key);
				if (prefExist) {
					val = Preference.GetPreference<bool> (this.AppModel, key);
				} else {
					bool newValue = true; // default to true
					val = newValue; 
					EMTask.DispatchBackground (() => {
						Preference.UpdatePreference<bool> (this.AppModel, key, newValue);
					});
				}
			}

			return val.Value;
		}

		private long? _lastTimeTrackedInstalledApps = null;
		public long LastTimeTrackedInstalledApps {
			set { SetLongWithKey (ref this._lastTimeTrackedInstalledApps, LastTimeTrackedInstalledAppsKey, value); }
			get { return GetLongWithKey (ref this._lastTimeTrackedInstalledApps, LastTimeTrackedInstalledAppsKey); }
		}

		private void SetLongWithKey (ref long? val, string key, long newValue) {
			Debug.Assert (this.AppModel != null, "Using UserSettings object before setting AppModel.");
			if (!val.HasValue) {
				val = newValue;
				EMTask.DispatchBackground (() => {
					Preference.UpdatePreference<long> (this.AppModel, key, newValue);
				});
			} else {
				long oldValue = val.Value;
				val = newValue;

				if (oldValue != val.Value) {
					EMTask.DispatchBackground (() => {
						Preference.UpdatePreference<long> (this.AppModel, key, newValue);
					});
				}
			}
		} 

		private long GetLongWithKey (ref long? val, string key) {
			Debug.Assert (this.AppModel != null, "Using UserSettings object before setting AppModel.");
			if (!val.HasValue) {
				bool prefExist = Preference.DoesPreferenceExist (this.AppModel, key);
				if (prefExist) {
					val = Preference.GetPreference<long> (this.AppModel, key);
				} else {
					long newValue = 0; // default to 0
					val = newValue; 
					EMTask.DispatchBackground (() => {
						Preference.UpdatePreference<long> (this.AppModel, key, newValue);
					});
				}
			}

			return val.Value;
		}
	}
}

