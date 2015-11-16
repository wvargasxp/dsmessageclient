using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.PlatformImpl {
	class WindowsAnalyticsHelper : IAnalyticsHelper {
		public void SendEvent (string category, string action, string label, int value) {
			return;
			throw new NotImplementedException ();
		}
	}
}
