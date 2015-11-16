using System;
using System.Collections.Generic;

namespace em {
	public class AbstractInAppSoundSettingsController {
		private ApplicationModel AppModel { get; set; }

		private IList<SoundSetting> _settings = null;
		public IList<SoundSetting> Settings {
			get {
				if (this._settings == null) {
					this._settings = new List<SoundSetting> ();
					Array settingMenuNames = Enum.GetValues (typeof(SoundSetting));
					int length = settingMenuNames.Length;
					for (int i = 0; i < length; i++) {
						SoundSetting x = (SoundSetting)settingMenuNames.GetValue (i);
						this._settings.Add (x);
					}
				}

				return this._settings;
			}
		}

		public AbstractInAppSoundSettingsController (ApplicationModel appModel) {
			this.AppModel = appModel;
		}

		public void HandlePushSettingChangeResult (SettingChange<SoundSetting> change) {
			this.AppModel.account.UserSettings.HandlePushSettingChange<SoundSetting> (change);
		}
	}
}

