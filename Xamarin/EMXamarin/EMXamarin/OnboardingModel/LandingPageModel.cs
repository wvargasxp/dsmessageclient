using System;

namespace em {
	public class LandingPageModel {
		public string PreviewViewURL;
		public Action ContinueSelected;  // In Objc this would be void(^continueSelected)().

		public bool didClickGetStarted;

		public LandingPageModel () {
			PreviewViewURL = AppEnv.HTTP_BASE_ADDRESS + "/web/onboarding/start";
			didClickGetStarted = false;
		}

	}
}