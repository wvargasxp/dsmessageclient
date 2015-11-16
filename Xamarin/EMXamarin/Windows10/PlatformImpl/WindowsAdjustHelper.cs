using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;

namespace Windows10.PlatformImpl
{
    class WindowsAdjustHelper : IAdjustHelper
    {
        private static WindowsAdjustHelper _shared = null;
        public static WindowsAdjustHelper Shared
        {
            get
            {
                if (_shared == null)
                {
                    _shared = new WindowsAdjustHelper();
                }

                return _shared;
            }
        }

        void IAdjustHelper.Init()
        {
            return;
        }

        void IAdjustHelper.SendEvent(EmAdjustEvent adjustEvent)
        {
            return;
        }

        void IAdjustHelper.SendEvent(EmAdjustEvent adjustEvent, Dictionary<string, string> parameters)
        {
            return;
        }
    }
}
