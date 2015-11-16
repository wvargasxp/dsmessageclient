using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;

namespace Windows10.PlatformImpl
{
    class WindowsAnalyticsHelper : IAnalyticsHelper
    {
        public void SendEvent(string category, string action, string label, int value)
        {
            return;
            throw new NotImplementedException();
        }
    }
}
