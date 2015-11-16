using System;
using System.IO;
using em;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace TestsHeadless
{
	public class TestAddressBook : IAddressBook
	{
		TestUser user;

		public TestAddressBook (int userIndex)
		{
			user = TestUserDB.GetUserAtIndex (userIndex);
		}


		public void ListOfContacts (Action<bool, List<AddressBookContact>> completion) {
			completion(true, user.listOfContacts);
		}

		public void CopyThumbnailFromAddressBook (Uri thumbnailUri, string path) {

		}
	}
}

