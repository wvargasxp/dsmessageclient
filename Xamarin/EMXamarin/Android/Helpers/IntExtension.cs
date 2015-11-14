using System;
using Android.Util;
using Android.Content.Res;

namespace Emdroid {
	public static class IntExtension {
		public static int PixelToDpUnit (this int value) {

			// https://stackoverflow.com/questions/8309354/formula-px-to-dp-dp-to-px-android

			DisplayMetrics displayMetrics = EMApplication.Instance.Resources.DisplayMetrics;
			int dp = (int) ((value / displayMetrics.Density) + 0.5);
			return dp;
		}

		public static int DpToPixelUnit (this int value) {
			DisplayMetrics displayMetrics = EMApplication.Instance.Resources.DisplayMetrics;
			int px = (int) ((value * displayMetrics.Density) + 0.5);
			return px;
		}

	}
}

