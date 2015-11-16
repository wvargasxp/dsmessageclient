using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;
using Windows.Storage;
using System.IO;

namespace Windows10.PlatformImpl
{
    class WindowsDesktopUriPlatformResolverStrategy : UriPlatformResolverStrategy
    {
        public string VirtualPathToPlatformPath(string virtualParentPath)
        {
            // Todo
            return Directory.GetCurrentDirectory();
        }
    }
}
