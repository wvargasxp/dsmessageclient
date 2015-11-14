using System;
using System.Json;
using System.Text;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Net.Wifi;
using Android.OS;
using Android.Provider;
using Android.Telephony;
using em;
using Java.Lang;
using System.Collections.Generic;

namespace Emdroid {
	class AndroidDeviceInfo : IDeviceInfo {
		string cachedBase64;
		string cachedJson;
		string registrationId;

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

		public AndroidDeviceInfo() {
			cachedBase64 = null;
			cachedJson = null;
			registrationId = null;
		}

		public void SetPushToken(string token) {
			bool didUpdate = false;
			if (token != null && !token.Equals (registrationId)) {
				didUpdate = true;
			}

			registrationId = token;

			// clearing the cached value
			cachedBase64 = null;
			cachedJson = null;

			if (didUpdate) {
				System.Diagnostics.Debug.WriteLine ("token did update " + token);
				PushTokenDidUpdate (token);
			}
		}

		public string DeviceJSONString ()  {
			if (cachedJson == null) {

				var json = new JsonObject ();

				json.Add ("platform", "Android");
				json.Add ("androidID", Settings.Secure.AndroidId); // Probably unusable. TODO: Find an alternative.
				json.Add ("androidDeviceID", DeviceID ());
				json.Add ("macAddress", MacAddress ());
				if (registrationId != null)
					json.Add ("gcmToken", registrationId);
				json.Add ("model", Build.Model);
				json.Add ("systemVersion", Build.VERSION.Release);
				json.Add ("locale", Locale ());
				json.Add ("language", Language ());
				json.Add ("appVersion", AppVersion ());

				cachedJson = json.ToString ();
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
			return "Android_Device_Default";
		}

		public string DeviceID () {
			Context context = EMApplication.GetMainContext ();
			var telephonyManager = (TelephonyManager)context.GetSystemService (Context.TelephonyService);
			string deviceID = telephonyManager.DeviceId ?? MacAddress (); // Random numbers. TelephonyManager is unreliable.
			return deviceID;
		}

		public static string MacAddress () {
			Context context = EMApplication.GetMainContext ();
			var wifiManager = (WifiManager)context.GetSystemService (Context.WifiService);
			return wifiManager.ConnectionInfo.MacAddress;
		}

		public static string AppVersion () {
			Context context = EMApplication.GetMainContext ();
			return context.PackageManager.GetPackageInfo (context.PackageName, 0).VersionName;
		}

		public static string Locale() {
			return Resources.System.Configuration.Locale.Country; 
		}

		public static string Language() {
			return Resources.System.Configuration.Locale.Language;
		}

		public static bool IsRightLeftLanguage() {
			int directionality = Character.GetDirectionality (Resources.System.Configuration.Locale.DisplayName [0]);
			return directionality == Character.DirectionalityRightToLeft || directionality == Character.DirectionalityRightToLeftArabic;
		}

		public static bool IsPackageInstalled(string packagename, Context context) {
			PackageManager pm = context.PackageManager;
			try {
				var info = pm.GetPackageInfo(packagename, PackageInfoFlags.Activities);
				if(info != null && info.ApplicationInfo != null)
					return info.ApplicationInfo.Enabled;
			} catch (PackageManager.NameNotFoundException e) {
//				System.Diagnostics.Debug.WriteLine ("Package not found {0} {1} ", packagename, e);
			}

			return false;
		}

		public static void ListInstalledPackages (Context context) {
			IList<PackageInfo> packages;
			PackageManager pm = context.PackageManager;     
			packages = pm.GetInstalledPackages (0);
			foreach (PackageInfo packageInfo in packages) {
				System.Diagnostics.Debug.WriteLine ("Package name is  " + packageInfo.PackageName);
			}
		}
	}
}