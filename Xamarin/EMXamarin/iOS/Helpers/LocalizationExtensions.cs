using Foundation;

namespace iOS {
	public static class LocalizationExtensions {
		public static string t(this string translate) {
			var translation = NSBundle.MainBundle.LocalizedString (translate, null);

			//if value is equal to the key, the translation doesn't exist. fall back to english
			if(translation.Equals(translate)) {
				var path = NSBundle.PathForResourceAbsolute("en", "lproj", NSBundle.MainBundle.ResourcePath);
				var bundle = NSBundle.FromPath(path);
				translation = bundle.LocalizedString(translate, null);
			}

			return translation;
		}
	}
}