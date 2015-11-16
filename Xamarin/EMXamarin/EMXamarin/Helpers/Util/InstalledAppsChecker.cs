using System;

namespace em {
	public static class InstalledAppsChecker {
		public static InstalledAppsOutbound ListOfInstalledApps {
			get {
				PlatformFactory factory = ApplicationModel.SharedPlatform;
				IInstalledAppResolver appResolver = factory.GetInstalledAppsResolver ();

				InstalledAppsOutbound retVal = new InstalledAppsOutbound ();

				Array values = Enum.GetValues (typeof(OtherApp));
				int length = values.Length;
				for (int i = 0; i < length; i++) {
					OtherApp app = (OtherApp)values.GetValue (i);
					bool appInstalled = appResolver.AppInstalled (app);

					AppDescriptionOutbound appDesc = new AppDescriptionOutbound ();
					appDesc.app = AppDescriptionOutbound.DescriptionOf (app);
					appDesc.installed = appInstalled;
					retVal.appDescriptions.Add (appDesc);
				}

				return retVal;
			}
		}

		public static bool WhosHereInstalled {
			get {
				PlatformFactory factory = ApplicationModel.SharedPlatform;
				IInstalledAppResolver appResolver = factory.GetInstalledAppsResolver ();
				bool whosHereInstalled = appResolver.AppInstalled (OtherApp.WhosHere);
				return whosHereInstalled;
			}
		}
	}
}

