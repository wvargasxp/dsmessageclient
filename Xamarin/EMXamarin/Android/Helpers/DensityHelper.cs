using System;

namespace Emdroid {
	public static class DensityHelper {
		// https://stackoverflow.com/questions/3166501/getting-the-screen-density-programmatically-in-android
		public static DensityName Density {
			get { 
				float density = EMApplication.Instance.Resources.DisplayMetrics.Density;
				if (density >= 4.0) {
					return DensityName.Xxxhdpi;
				} 

				if (density >= 3.0) {
					return DensityName.Xxhdpi;
				}

				if (density >= 2.0) {
					return DensityName.Xhdpi;
				}

				if (density >= 1.5) {
					return DensityName.Hdpi;
				}

				if (density >= 1.0) {
					return DensityName.Mdpi;
				}

				return DensityName.Ldpi;
			}
		}

		public static int MaxLargeNotificationIconSizeFromDensity () {
			/* 
    		ldpi: 48x48 px *0.75
    		mdpi: 64x64 px *1.00
   		 	hdpi: 96x96 px *1.50
    		xhdpi: 128x128 px *2.00
    		xxhdpi: 192x192 px *3.00
			*/

			DensityName density = DensityHelper.Density;

			// https://stackoverflow.com/questions/25030710/gcm-push-notification-large-icon-size
			switch (density) {
			case DensityName.Ldpi:
				return 48;
			case DensityName.Mdpi:
				return 64;
			case DensityName.Hdpi:
				return 96;
			case DensityName.Xhdpi:
				return 128;
			case DensityName.Xxhdpi:
				return 192;
			default:
				return 192;

			}
		}
	}

	public enum DensityName {
		Xxxhdpi,
		Xxhdpi,
		Xhdpi,
		Hdpi,
		Mdpi,
		Ldpi
	}
}

