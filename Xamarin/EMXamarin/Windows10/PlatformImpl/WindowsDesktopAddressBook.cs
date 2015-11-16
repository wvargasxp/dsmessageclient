using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;

namespace Windows10.PlatformImpl
{
    class WindowsDesktopAddressBook : IAddressBook
    {
        public void ListOfContacts(Action<bool, List<AddressBookContact>> callback)
        {
            callback(true, new List<AddressBookContact>());
        }

        public void CopyThumbnailFromAddressBook(Uri thumbnailUri, string path)
        {
            throw new NotImplementedException();
        }
    }
}
