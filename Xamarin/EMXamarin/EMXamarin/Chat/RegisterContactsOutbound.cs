using System;
using System.Collections.Generic;

namespace em
{
	public class RegisterContactsOutbound
	{
		public int contactsVersion { get; set; }
		public IList<AddressBookContact> contacts { get; set; }
	}
}

