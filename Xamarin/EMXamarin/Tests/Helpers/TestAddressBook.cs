using System;
using System.IO;
using em;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MonoTouch;
using MonoTouch.AddressBook;
using MonoTouch.Foundation;
using System.Diagnostics;
using MonoTouch.UIKit;

namespace Tests
{
	public class TestAddressBook : IAddressBook
	{
		TestUser user;

		public TestAddressBook (int userIndex)
		{
			user = TestUserDB.GetUserAtIndex (userIndex);
		}

		ABAddressBook book = null;

		public void ListOfContacts (Action<bool, List<AddressBookContact>> completion) {
			completion(true, user.listOfContacts);
		}

		public void CopyThumbnailFromAddressBook (Uri thumbnailUri, string path) {
			if (System.IO.File.Exists (path))
				return;

			string uriString = thumbnailUri.AbsoluteUri;
			string[] split = uriString.Split (new char[] { '/' });
			ABPerson person = book.GetPerson (Convert.ToInt32(split[ split.Length-2]));

			using (NSData imageData = person.GetImage (ABPersonImageFormat.Thumbnail)) {
				using (UIImage image = UIImage.LoadFromData (imageData)) {
					using (NSData jpegData = image.AsJPEG (0.75f)) {
						Debug.WriteLine ("Addressbook image size " + jpegData.Length);

						Directory.CreateDirectory( Path.GetDirectoryName(path));

						NSError err = null;
						jpegData.Save (path, false, out err);
					}
				}
			}
		}
	}
}

