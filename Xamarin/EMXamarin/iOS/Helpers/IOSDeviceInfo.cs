using System;
using System.Diagnostics;
using System.Text;
using em;
using Foundation;
using UIKit;

namespace iOS {
	public class IOSDeviceInfo : IDeviceInfo {
		string cachedBase64;
		string cachedJson;
		string pushToken;

		Action<string> pushTokenDidUpdate = (updatedPushToken) => {};

		public Action<string> PushTokenDidUpdate {
			get {
				return pushTokenDidUpdate;
			}
			set {
				if (value != null) {
					pushTokenDidUpdate = value;
				}
			}
		}

		public IOSDeviceInfo () {
			cachedBase64 = null;
			cachedJson = null;
			pushToken = null;
		}

		public void SetPushToken(string token) {
			bool didUpdate = false;
			if (token != null && !token.Equals (pushToken)) {
				didUpdate = true;
			}

			pushToken = token;

			// clear the cached value so that it will regenerate
			cachedBase64 = null;
			cachedJson = null;

			if (didUpdate) {
				Debug.WriteLine ("token did update " + token);
				PushTokenDidUpdate (token);
			}
		}

		public string DeviceJSONString () {
			if (cachedJson == null) {
				NSDictionary dict = DeviceJSONDictionary (pushToken);
				NSError error = null;
				NSData jsondict = NSJsonSerialization.Serialize (dict, 0, out error);
				if (error != null)
					Debug.WriteLine ("Failed to convert NSDictionary to json " + error.LocalizedDescription);
				cachedJson = NSString.FromData (jsondict, NSStringEncoding.UTF8).ToString();
			}

			return cachedJson;
		}

		public string DeviceBase64String () {
			if (cachedBase64 == null) {
				string deviceInformationJson = DeviceJSONString ();
				cachedBase64 = Convert.ToBase64String (Encoding.UTF8.GetBytes (deviceInformationJson));
			}

			return cachedBase64;
		}

		public string DefaultName () {
			return UIDevice.CurrentDevice.Name;
		}

		static NSMutableDictionary DeviceJSONDictionary (string pushToken) {
			// We need to cast to NSString because all "strings" are c# variables initially.
			UIDevice device = UIDevice.CurrentDevice;

			var dictionary = new NSMutableDictionary ();
			dictionary.SetValueForKey ((NSString)"iOS", (NSString)"platform");
			dictionary.SetValueForKey ((NSString)device.Model, (NSString)"model");
			dictionary.SetValueForKey ((NSString)device.Name, (NSString)"name");
			dictionary.SetValueForKey ((NSString)device.IdentifierForVendor.AsString (), (NSString)"identifierForVendor");
			dictionary.SetValueForKey ((NSString)device.SystemName, (NSString)"systemName");
			dictionary.SetValueForKey ((NSString)device.SystemVersion, (NSString)"systemVersion");

			NSLocale locale = NSLocale.CurrentLocale;
			string language = locale.LanguageCode;
			string localestr = locale.CountryCode;
			//string language = (string)NSLocale.PreferredLanguages.GetValue (0);
			var appversion = (NSString)NSBundle.MainBundle.InfoDictionary.ObjectForKey ((NSString)"CFBundleVersion");

			dictionary.SetValueForKey ((NSString)language, (NSString)"language");
			dictionary.SetValueForKey ((NSString)localestr, (NSString)"locale");
			dictionary.SetValueForKey (appversion, (NSString)"appVersion");

			if (pushToken == null)
				dictionary.SetValueForKey (NSNull.Null, (NSString)"pushToken");
			else
				dictionary.SetValueForKey ((NSString)pushToken, (NSString)"pushToken");
			
			return dictionary;
		}
	}
}