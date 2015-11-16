using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;
using EMXamarin;

namespace Windows10.PlatformImpl
{
    class WindowsDesktopDeviceInfo : IDeviceInfo
    {
        public Action<string> PushTokenDidUpdate
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string DefaultName()
        {
            throw new NotImplementedException();
        }

        public string DeviceBase64String()
        {
            throw new NotImplementedException();
        }

        public string DeviceJSONString()
        {
            throw new NotImplementedException();
        }

        public void SetPushToken(string token)
        {
            throw new NotImplementedException();
        }
    }
}
