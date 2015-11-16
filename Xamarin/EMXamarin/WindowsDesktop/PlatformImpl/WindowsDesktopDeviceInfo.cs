using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;
using EMXamarin;

namespace WindowsDesktop.PlatformImpl {
	class WindowsDesktopDeviceInfo : IDeviceInfo {
		public void SetPushToken (string token) {
			return; // todo
		}

		public string DeviceJSONString () {
			// todo:
			return "{\"platform\": \"Android\", \"androidID\": \"android_id\", \"androidDeviceID\": \"351690065590682\", \"macAddress\": \"00:ee:bd:9e:cf:67\", \"model\": \"HTC One_M8\", \"systemVersion\": \"5.0.1\", \"locale\": \"US\", \"language\": \"en\", \"appVersion\": \"1.11.3\"}";
		}

		public string DefaultName () {
			return string.Empty; // todo
		}

		void IDeviceInfo.SetPushToken (string token) {
			return; // todo
		}

		string IDeviceInfo.DeviceJSONString () {
			return string.Empty; // todo
		}

		string IDeviceInfo.DeviceBase64String () {
			return string.Empty; // todo
		}

		string IDeviceInfo.DefaultName () {
			return string.Empty;
		}

		private Action<string> _pushTokenDidUpdate = (updatedPushToken) => {};
		Action<string> IDeviceInfo.PushTokenDidUpdate {
			get { return this._pushTokenDidUpdate; }
			set {
				if (value != null) {
					this._pushTokenDidUpdate = value;
				}
			}
		}
	}
}
