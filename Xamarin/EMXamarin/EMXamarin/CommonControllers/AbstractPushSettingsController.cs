using System;
using System.Collections.Generic;

namespace em {
	public class AbstractPushSettingsController {

		private IList<PushSetting> _settings = null;
		public IList<PushSetting> Settings {
			get {
				if (this._settings == null) {
					this._settings = new List<PushSetting> ();
					Array settingMenuNames = Enum.GetValues (typeof(PushSetting));
					int length = settingMenuNames.Length;
					for (int i = 0; i < length; i++) {
						PushSetting push = (PushSetting)settingMenuNames.GetValue (i);
						this._settings.Add (push);
					}
				}

				return this._settings;
			}
		}

		private ApplicationModel AppModel { get; set; }
		public AbstractPushSettingsController (ApplicationModel appModel) {
			this.AppModel = appModel;
		}

		public void HandlePushSettingChangeResult (SettingChange<PushSetting> change) {
			this.AppModel.account.UserSettings.HandlePushSettingChange<PushSetting> (change);
		}
	}
}