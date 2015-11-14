using System;
using Android.OS;

namespace Emdroid
{
	public static class VersionHelper
	{
		public static bool SupportsListItemViewsWithTransientState() {
			return (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.JellyBean );
		}
	}
}

