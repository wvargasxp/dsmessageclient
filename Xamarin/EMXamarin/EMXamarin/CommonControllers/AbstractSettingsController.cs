using System;
using System.Collections.Generic;

namespace em {
	public class AbstractSettingsController {

		private IList<SettingMenuItem> _settings = null;
		public IList<SettingMenuItem> Settings {
			get {
				if (this._settings == null) {
					this._settings = new List<SettingMenuItem> ();
					Array settingMenuNames = Enum.GetValues (typeof(SettingMenuItem));
					int length = settingMenuNames.Length;
					for (int i = 0; i < length; i++) {
						SettingMenuItem settingName = (SettingMenuItem)settingMenuNames.GetValue (i);
						this._settings.Add (settingName);
					}
				}

				return this._settings;
			}
		}

		// iOS doesn't have Push setting (yet?) so we use a separate list from Android's.
		public IList<SettingMenuItem> Settings2 {
			get {
				IList<SettingMenuItem> settings = this.Settings;
				if (settings.Contains (SettingMenuItem.Push)) {
					settings.Remove (SettingMenuItem.Push);
				}

				return settings;
			}
		}

		private ApplicationModel AppModel { get; set; }

		public AbstractSettingsController (ApplicationModel model) {
			this.AppModel = model;
		}
	}
}

