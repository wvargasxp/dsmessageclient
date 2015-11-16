using System;

namespace em {
	public interface IDeviceInfo {
		void SetPushToken(string token);
		string DeviceJSONString ();
		string DeviceBase64String ();
		string DefaultName ();

		Action<string> PushTokenDidUpdate { get; set; }
	}
}
