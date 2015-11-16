using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using EMXamarin;
using em; 
using System.Diagnostics;

namespace Tests
{
	public class TestDeviceInfo : IDeviceInfo
	{
		string cachedJson;
		string pushToken;
		TestUser user;

		public TestDeviceInfo (int userArrIndex) // should take in the user id
		{
			cachedJson = null;
			pushToken = null;
			user = TestUserDB.GetUserAtIndex(userArrIndex);
		}
	
		public void SetPushToken(string token) {
			pushToken = token;

			// clear the cached value so that it will regenerate
			cachedJson = null;
		}

		public string DeviceJSONString () {
			if (cachedJson == null) {
				Dictionary<string, object> dict = DeviceJSONDictionary (pushToken);
				cachedJson = JsonConvert.SerializeObject (dict);
				//Error error = null;
				//Data jsondict = NSJsonSerialization.Serialize (dict, 0, out error);
				//if (error != null)
				//	Debug.WriteLine ("Failed to convert NSDictionary to json " + error.LocalizedDescription);
				//cachedJson = NSString.FromData (jsondict, NSStringEncoding.UTF8).ToString();
			}

			return cachedJson;
		}

		public string DefaultName () {
			return UIDevice.CurrentDevice.Name;
		}

		private Dictionary<string, object> DeviceJSONDictionary (string pushToken) {
			// We need to cast to NSString because all "strings" are c# variables initially.
//			UIDevice device = UIDevice.CurrentDevice;

			Dictionary<string, object> dictionary = new Dictionary<string,object>();
			dictionary.Add ("platform", "iOS");
			dictionary.Add ("model", user.deviceModel );
			dictionary.Add ("name", user.deviceName);
			dictionary.Add ("identifierForVendor", user.identifierForVendor);
			dictionary.Add ("systemName", "iPhone OS");
			dictionary.Add ("systemVersion", "6.0.1");

			#region todo
			// TODO: Figure out specific Locale/Language/AppVersion to use. The below might be incorrect.

			NSLocale locale = NSLocale.CurrentLocale;
			string language = locale.LanguageCode;
			string localestr = locale.CountryCode;
			//string language = (string)NSLocale.PreferredLanguages.GetValue (0);
			NSString appversion = (NSString)NSBundle.MainBundle.InfoDictionary.ObjectForKey ((NSString)"CFBundleVersion");

			dictionary.Add ("language", language);
			dictionary.Add ("locale", localestr);
			dictionary.Add ("appVersion", appversion);
//			dictionary.SetValueForKey ((NSString)language, (NSString)"language");
//			dictionary.SetValueForKey ((NSString)localestr, (NSString)"locale");
//			dictionary.SetValueForKey (appversion, (NSString)"appVersion");
			#endregion
//
			if (pushToken == null) {
				//dictionary.Add ("pushToken", NSNull);
			} else {
				dictionary.Add ("pushToken", pushToken);
			}
			return dictionary;

		}
		//*/ 
	}
}

