using System;
using System.Collections.Generic;

namespace em {
	public class AbstractInAppSettingsController {
		private ApplicationModel AppModel { get; set; }

		private IList<InAppSetting> _settings = null;
		public IList<InAppSetting> Settings {
			get {
				if (this._settings == null) {
					this._settings = new List<InAppSetting> ();
					Array settingMenuNames = Enum.GetValues (typeof(InAppSetting));
					int length = settingMenuNames.Length;
					for (int i = 0; i < length; i++) {
						InAppSetting x = (InAppSetting)settingMenuNames.GetValue (i);
						this._settings.Add (x);
					}
				}

				return this._settings;
			}
		}

		public AbstractInAppSettingsController (ApplicationModel appModel) {
			this.AppModel = appModel;
		}

		public void HandlePushSettingChangeResult (SettingChange<InAppSetting> change) {
			this.AppModel.account.UserSettings.HandlePushSettingChange<InAppSetting> (change);
		}
	}
}

