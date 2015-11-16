using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;
using EMXamarin;

namespace WindowsDesktop.PlatformImpl {
	class WindowsDesktopUriPlatformResolverStrategy : UriPlatformResolverStrategy {
		public string VirtualPathToPlatformPath (string virtualParentPath) {
			// Todo
			return Environment.CurrentDirectory;
		}
	}
}
