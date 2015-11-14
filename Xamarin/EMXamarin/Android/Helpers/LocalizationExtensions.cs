namespace Emdroid {
	public static class LocalizationExtensions {
		public static string t(this string translate) {
			var id = EMApplication.GetInstance().ApplicationContext.Resources.GetIdentifier(translate, "string", EMApplication.GetInstance().ApplicationContext.PackageName);
			return EMApplication.GetInstance().ApplicationContext.Resources.GetString (id);
		}
	}
}