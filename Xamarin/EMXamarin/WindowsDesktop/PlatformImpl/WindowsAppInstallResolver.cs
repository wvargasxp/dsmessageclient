using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;

namespace WindowsDesktop.PlatformImpl {
	class WindowsAppInstallResolver : IInstalledAppResolver {
		private static WindowsAppInstallResolver _shared;

		public static WindowsAppInstallResolver Shared {
			get {
				if (_shared == null) {
					_shared = new WindowsAppInstallResolver ();
				}

				return _shared;
			}
		}

		bool IInstalledAppResolver.AppInstalled (OtherApp app) {
			return false;
		}
	}
}
