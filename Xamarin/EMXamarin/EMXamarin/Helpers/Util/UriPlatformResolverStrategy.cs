using System;

namespace em {
	public interface UriPlatformResolverStrategy {

		string VirtualPathToPlatformPath (string virtualParentPath);
	}
}

