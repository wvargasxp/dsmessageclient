using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AddressBook;
using Foundation;
using UIKit;
using em;
using EMXamarin;

namespace iOS {
	public class IOSAddressBook : IAddressBook {

		ABAddressBook book;

		void RequestInformation (Action<bool, NSError> completionHandler) {
			if (book == null) {
				NSError error;
				Debug.WriteLine ("IOSAddressBook.RequestInformation: Instantiating Addressbook.");
				book = ABAddressBook.Create (out error);
				if (error != null) {
					completionHandler (false, error);
					return;
				}
			}

			var authStatus = ABAddressBook.GetAuthorizationStatus();
			if (authStatus != ABAuthorizationStatus.Authorized)
				book.RequestAccess (completionHandler); 
			else
				completionHandler(true, null);
		}

		public void ListOfContacts (Action<bool, List<AddressBookContact>> completion) {
			var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
			var preferenceExists = Preference.DoesPreferenceExist (appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS);
			var alertPreferenceExists = Preference.DoesPreferenceExist (appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS_HIDE_ALERT);

			if (!alertPreferenceExists)
				Preference.UpdatePreference<bool> (appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS_HIDE_ALERT, false);

			if(!preferenceExists) {
				Preference.UpdatePreference<bool>(appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS, true);
				AccessAddressBook (completion);
			} else {
				var access = Preference.GetPreference<bool>(appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS);
				var dontShowAlert = Preference.GetPreference<bool> (appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS_HIDE_ALERT);

				if (dontShowAlert) {
					completion (false, null);
					return;
				}

				var authStatus = ABAddressBook.GetAuthorizationStatus();
				if(!access && authStatus == ABAuthorizationStatus.Authorized) {
					Preference.UpdatePreference<bool>(appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS, true);
					AccessAddressBook (completion);
				} else if(!access) {
					var analytics = new AnalyticsHelper();

					var title = "APP_TITLE".t ();
					var message = "ACCESS_ADDRESS_BOOK_MESSAGE".t ();
					var action = "CONTINUE_BUTTON".t ();
					var dontShowAgain = "NO_THANKS".t (); //TODO: change to proper translation once it's ready

					var alert = new UIAlertView (title, message, null, "CANCEL_BUTTON".t (), new [] { action, dontShowAgain });
					alert.Show ();
					alert.Clicked += (sender2, buttonArgs) => { 
						switch (buttonArgs.ButtonIndex) {
						case 1:
							analytics.SendEvent(AnalyticsConstants.CATEGORY_UI_ACTION, AnalyticsConstants.ACTION_BUTTON_PRESS, AnalyticsConstants.ADDRESS_BOOK_ALLOW, 0);
							Preference.UpdatePreference<bool>(appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS, true);
							AccessAddressBook (completion);
							break;
						case 2:
							analytics.SendEvent(AnalyticsConstants.CATEGORY_UI_ACTION, AnalyticsConstants.ACTION_BUTTON_PRESS, AnalyticsConstants.ADDRESS_BOOK_DONT_SHOW, 0);
							Preference.UpdatePreference<bool>(appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS, false);
							Preference.UpdatePreference<bool>(appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS_HIDE_ALERT, true);
							completion (false, null);
							break;
						default:
							analytics.SendEvent(AnalyticsConstants.CATEGORY_UI_ACTION, AnalyticsConstants.ACTION_BUTTON_PRESS, AnalyticsConstants.ADDRESS_BOOK_CANCEL, 0);
							Preference.UpdatePreference<bool>(appDelegate.applicationModel, Preference.ADDRESS_BOOK_ACCESS, false);
							completion (false, null);
							break;
						}
					};
				} else {
					AccessAddressBook (completion);
				}
			}
		}

		public void AccessAddressBook(Action<bool, List<AddressBookContact>> completion) {
			RequestInformation((bool accessGranted, NSError error) => EMTask.DispatchBackground (() => {
				if (!accessGranted) {
					completion (false, null);
					if (error != null) {
						Debug.WriteLine ("Error accessing address book: " + error);
					}
				} else {
					var sw = new Stopwatch ();
					sw.Start ();
					int totalContacts = 0;
					int expectedContacts = 0;
					var listOfContacts = new List<AddressBookContact> ();
					IList<int> linkedIdsToIgnore = new List<int> ();
					try {
						ABAddressBook addressBook = ABAddressBook.Create (out error);
						if (error != null) {
							completion (false, null);
							Debug.WriteLine ("Error accessing address book: " + error);
						}
						ABPerson[] contacts = addressBook.GetPeople ();
						expectedContacts = contacts.Length;
						foreach (ABPerson person in contacts) {
							if (linkedIdsToIgnore.Contains (person.Id))
								continue;
							totalContacts++;
							AddressBookContact newContact = AddressBookContactFromPerson (person, linkedIdsToIgnore);
							listOfContacts.Add (newContact);
						}
					} catch (Exception e) {
						Debug.WriteLine (string.Format ("Address Book Exception: {0}\n{1}", e.Message, e.StackTrace));
					}
					sw.Stop ();
					Debug.WriteLine (string.Format ("Total time taken to process {0} contacts: {1} ms. Total contacts expected: {2}", totalContacts, sw.ElapsedMilliseconds, expectedContacts));
					completion (true, listOfContacts);
				}
			}));
		}

		public AddressBookContact AddressBookContactFromPerson (ABPerson person, IList<int> linkedIdsToIgnore) {
			// Something to look at more carefully for dates.
			// http://stackoverflow.com/questions/2148906/create-nsdate-monotouch
			//NSDate birth = person.Birthday;
			var newContact = new AddressBookContact ();
			newContact.clientID = Convert.ToString (person.Id);
			newContact.firstName = person.FirstName;
			newContact.lastName = person.LastName;
			if (person.Nickname != null)
				newContact.displayName = person.Nickname;
			else {
				if ((newContact.firstName == null || newContact.firstName.Trim ().Equals ("")) && (newContact.lastName == null || newContact.lastName.Trim ().Equals (""))) {
					string org = person.Organization;
					newContact.displayName = org;
					newContact.firstName = org;
				} else
					newContact.displayName = string.Format ("{0} {1}", person.FirstName, person.LastName);
			}

			/*
							if (birth != null) {
								DateTime birthdate = DateTime.SpecifyKind (birth.NSDateToDateTime (), DateTimeKind.Unspecified);
								// Might not be accurate in its conversion. NSDate -> DateTime
								var offset = new DateTimeOffset (birthdate, TimeSpan.Zero);
								newContact.birthDate = offset.ToString ("yyyy-MM-dd'T'HH:mm:ss'Z'");
							}
			*/


			ABPerson[] linkedPeople = person.GetLinkedPeople ();

			foreach (ABPerson linkedPerson in linkedPeople) {
				AddPhoneInfoFromPersonToContact (linkedPerson, newContact);
				AddEmailInfoFromPersonToContact (linkedPerson, newContact);

				if (linkedPerson.Id != person.Id)
					linkedIdsToIgnore.Add (linkedPerson.Id);
			}

			if (person.HasImage)
				newContact.SetThumbnailUri ("addressbook://" + newContact.clientID + "/thumbnail");

			return newContact;
		}

		public void AddPhoneInfoFromPersonToContact (ABPerson person, AddressBookContact contact) {
			ABMultiValue<string> phone = person.GetPhones ();
			foreach (ABMultiValueEntry<string> phoneEntry in phone) {
				var phoneInfo = new ContactInfo ();
				phoneInfo.localizedLabel = phoneEntry.Label != null ? ABAddressBook.LocalizedLabel (phoneEntry.Label) : "";
				phoneInfo.value = phoneEntry.Value;
				phoneInfo.contactInfoType = ContactInfoType.phone;
				contact.contactInfo.Add (phoneInfo);
			}
		}

		public void AddEmailInfoFromPersonToContact (ABPerson person, AddressBookContact contact) {
			ABMultiValue<string> emails = person.GetEmails ();
			foreach (ABMultiValueEntry<string> emailEntry in emails) {
				var emailInfo = new ContactInfo ();
				emailInfo.localizedLabel = emailEntry.Label != null ? ABAddressBook.LocalizedLabel (emailEntry.Label) : "";
				emailInfo.value = emailEntry.Value;
				emailInfo.contactInfoType = ContactInfoType.email;
				contact.contactInfo.Add (emailInfo);
			}
		}

		public void CopyThumbnailFromAddressBook (Uri thumbnailUri, string path) {
			if (File.Exists (path))
				return;

			string uriString = thumbnailUri.AbsoluteUri;
			string[] split = uriString.Split ('/');
			ABPerson person = book.GetPerson (Convert.ToInt32(split[ split.Length-2]));

			using (NSData imageData = person.GetImage (ABPersonImageFormat.Thumbnail)) {
				using (UIImage image = UIImage.LoadFromData (imageData)) {
					using (NSData jpegData = image.AsJPEG (0.75f)) {
						//Debug.WriteLine ("Addressbook image size " + jpegData.Length);

						ApplicationModel appModel = (UIApplication.SharedApplication.Delegate as AppDelegate).applicationModel;
						appModel.platformFactory.GetFileSystemManager ().CreateParentDirectories (path);

						NSError err;
						jpegData.Save (path, false, out err);

						if (err != null)
							Debug.WriteLine ("Error saving address book thumbnail. Msg: " + err.LocalizedDescription);
					}
				}
			}
		}
	}
}