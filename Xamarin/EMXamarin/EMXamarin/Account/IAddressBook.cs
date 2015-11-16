using System;
using System.Collections.Generic;

namespace em {
	public interface IAddressBook {
		void ListOfContacts (Action<bool, List<AddressBookContact>> callback);
		void CopyThumbnailFromAddressBook (Uri thumbnailUri, string path);
	}
}