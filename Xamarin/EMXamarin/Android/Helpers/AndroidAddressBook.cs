using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Android.Content;
using Android.Database;
using Android.Provider;
using em;
using EMXamarin;

namespace Emdroid {
	class AndroidAddressBook : IAddressBook {

		private static void RequestInformationAsync (Action<bool> completionHandler) {
			bool access = true; // hardcoding access to true
			if (access)
				completionHandler(true); // FIXME should ask users if it's okay
			else {
				Debug.WriteLine ("Permission denied by user or manifest");
				completionHandler(false);
				return;
			}
		}

		public void ListOfContacts (Action<bool, List<AddressBookContact>> completion) {
			EMTask.DispatchBackground (() => {
				RequestInformationAsync ((bool accessGranted) => {
					lock (this) {
						if (!accessGranted) {
							completion (false, null);
						} else {
							List<AddressBookContact> listOfContacts = GetContacts ();
							completion (true, listOfContacts);
						}
					}
				});
			});
		}

		static List<AddressBookContact> GetContacts () {
			List<AddressBookContact> allContacts = new List<AddressBookContact> ();
			int totalContacts = 0;
			var swd = new Stopwatch();
			swd.Start();

			ContentResolver cr = EMApplication.GetMainContext ().ContentResolver;

			string[] contactProjection = {
				ContactsContract.Contacts.InterfaceConsts.Id,
				ContactsContract.Contacts.InterfaceConsts.PhotoThumbnailUri
			};

			ICursor contactCursor = cr.Query (uri: ContactsContract.Contacts.ContentUri, 
				projection: contactProjection,
				selection: null,
				selectionArgs: null,
				sortOrder: ContactsContract.Contacts.InterfaceConsts.DisplayName);


			// Reminder: Cursors start at -1 position, so MoveToNext will get us the 0th position.
			if (contactCursor.Count > 0) {
				int columnIndexContactId = contactCursor.GetColumnIndex (ContactsContract.Contacts.InterfaceConsts.Id);
				int columnIndexPhotoThumbnailUri = contactCursor.GetColumnIndex (ContactsContract.Contacts.InterfaceConsts.PhotoThumbnailUri);

				while (contactCursor.MoveToNext ()) {
					totalContacts++;
					AddressBookContact contact = new AddressBookContact ();

					// id of contact
					string id = contactCursor.GetString (columnIndexContactId);
					contact.clientID = id;

					// thumbnail
					string uri = contactCursor.GetString (columnIndexPhotoThumbnailUri);
					if (uri != null) {
						contact.SetThumbnailUri ("addressbook://" + id + "/thumbnail?cr=" + Uri.EscapeUriString (uri));
					}

					// data related to contact
					string[] dataProjection = {
						ContactsContract.Data.InterfaceConsts.Mimetype,
						ContactsContract.CommonDataKinds.Phone.InterfaceConsts.DisplayName,
						ContactsContract.CommonDataKinds.StructuredName.GivenName,
						ContactsContract.CommonDataKinds.StructuredName.FamilyName,
						ContactsContract.CommonDataKinds.Email.Address,
						ContactsContract.CommonDataKinds.Phone.Number,
						ContactsContract.CommonDataKinds.Email.InterfaceConsts.Type,
						ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type,
					};

					string selection = string.Format ("{0}=?", ContactsContract.Data.InterfaceConsts.ContactId);
					string[] selectionArgs = new string[] { id };

					ICursor dataCursor = cr.Query (uri: ContactsContract.Data.ContentUri,
						                    projection: dataProjection,
						                    selection: selection,
						                    selectionArgs: selectionArgs,
						                    sortOrder: null);

					if (dataCursor.Count > 0) {
						// These are costly calls, get the indices only once before doing the loop.
						int columnIndexDisplayName = dataCursor.GetColumnIndex (ContactsContract.CommonDataKinds.Phone.InterfaceConsts.DisplayName);
						int columnIndexGivenName = dataCursor.GetColumnIndex (ContactsContract.CommonDataKinds.StructuredName.GivenName);
						int columnIndexFamilyName = dataCursor.GetColumnIndex (ContactsContract.CommonDataKinds.StructuredName.FamilyName);
						int columnIndexPhoneValue = dataCursor.GetColumnIndex (ContactsContract.CommonDataKinds.Phone.Number);
						int columnIndexPhoneType = dataCursor.GetColumnIndex (ContactsContract.CommonDataKinds.Phone.InterfaceConsts.Type);
						int columnIndexEmailValue = dataCursor.GetColumnIndex (ContactsContract.CommonDataKinds.Email.Address);
						int columnIndexEmailType = dataCursor.GetColumnIndex (ContactsContract.CommonDataKinds.Email.InterfaceConsts.Type);

						while (dataCursor.MoveToNext ()) {
							string mimeType = dataCursor.GetString (dataCursor.GetColumnIndex (ContactsContract.Data.InterfaceConsts.Mimetype));

							if (mimeType.Equals (ContactsContract.CommonDataKinds.StructuredName.ContentItemType)) {
								contact.displayName = dataCursor.GetString (columnIndexDisplayName);
								contact.firstName = dataCursor.GetString (columnIndexGivenName);
								contact.lastName = dataCursor.GetString (columnIndexFamilyName);
							}

							if (mimeType.Equals (ContactsContract.CommonDataKinds.Phone.ContentItemType)) {
								string phoneValue = dataCursor.GetString (columnIndexPhoneValue);

								PhoneDataKind type = (PhoneDataKind)dataCursor.GetInt (columnIndexPhoneType);
								string label = type.ToString ();
								string localizedLabel = ContactsContract.CommonDataKinds.Phone.GetTypeLabel (EMApplication.GetMainContext ().Resources, type, label);

								ContactInfo phoneInfo = new ContactInfo ();
								phoneInfo.localizedLabel = localizedLabel ?? string.Empty;
								phoneInfo.value = phoneValue;
								phoneInfo.contactInfoType = ContactInfoType.phone;
								contact.contactInfo.Add (phoneInfo);
							}

							if (mimeType.Equals (ContactsContract.CommonDataKinds.Email.ContentItemType)) {
								string emailValue = dataCursor.GetString (columnIndexEmailValue);

								EmailDataKind type = (EmailDataKind)dataCursor.GetInt (columnIndexEmailType);
								string label = type.ToString ();
								string localizedLabel = ContactsContract.CommonDataKinds.Email.GetTypeLabel (EMApplication.GetMainContext ().Resources, type, label);

								ContactInfo emailInfo = new ContactInfo ();
								emailInfo.localizedLabel = localizedLabel ?? string.Empty;
								emailInfo.contactInfoType = ContactInfoType.email;
								emailInfo.value = emailValue;
								contact.contactInfo.Add (emailInfo);
							}
						}

						// Only add the contact to the list if it contained data.
						allContacts.Add (contact);
					}

					dataCursor.Close ();

				}
			}

			contactCursor.Close ();

			swd.Stop();
			Debug.WriteLine(string.Format("Total time taken to process {0} contacts: {1} ms : contact_count: {2}", totalContacts, swd.ElapsedMilliseconds, allContacts.Count));

			return allContacts;
		}

		public void CopyThumbnailFromAddressBook (Uri thumbnailUri, string path) {
			// TODO we have append the content:// url as a parameter to our
			// addressbook:// url.  Ideally we could look up via the query system
			// and regrab the content url for cases where it may change.
			
			if (File.Exists (path))
				return;

			string query = thumbnailUri.Query;
			string[] split = query.Split ('=');

			string cr = split [1];
			if (cr.StartsWith ("content:")) {
				Android.Net.Uri uri = Android.Net.Uri.Parse(cr);
				using (Stream inStream = EMApplication.GetInstance ().ContentResolver.OpenInputStream (uri)) {
					EMApplication.GetInstance ().appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (path, inStream, -1, null);
				}
			}
		}
	}
}