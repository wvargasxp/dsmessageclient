using System.Collections.Generic;
using Android.Content;
using Android.Database;
using Android.Provider;

namespace Emdroid {
	public static class AccountUtils {

		/**
	     * Interface for interacting with the result of {@link AccountUtils#getUserProfile}.
	     */
		public class UserProfile {

			/**
		     * Adds an email address to the list of possible email addresses for the user
		     * @param email the possible email address
		     */
			public void AddPossibleEmail(string email) {
				AddPossibleEmail(email, false);
			}

			/**
	         * Adds an email address to the list of possible email addresses for the user. Retains information about whether this
	         * email address is the primary email address of the user.
	         * @param email the possible email address
	         * @param is_primary whether the email address is the primary email address
	         */
			public void AddPossibleEmail(string email, bool isPrimary) {
				if (email == null) return;
				if (isPrimary) {
					_primary_email = email;
					_possible_emails.Add(email);
				} else
					_possible_emails.Add(email);
			}

			/**
	         * Adds a name to the list of possible names for the user.
	         * @param name the possible name
	         */
			public void AddPossibleName(string name) {
				if (name != null) _possible_names.Add(name);
			}

			/**
	         * Adds a phone number to the list of possible phone numbers for the user.
	         * @param phone_number the possible phone number
	         */
			public void AddPossiblePhoneNumber(string phoneNumber) {
				if (phoneNumber != null) _possible_phone_numbers.Add(phoneNumber);
			}

			/**
	         * Adds a phone number to the list of possible phone numbers for the user.  Retains information about whether this
	         * phone number is the primary phone number of the user.
	         * @param phone_number the possible phone number
	         * @param is_primary whether the phone number is teh primary phone number
	         */
			public void AddPossiblePhoneNumber(string phoneNumber, bool isPrimary) {
				if (phoneNumber == null) return;
				if (isPrimary) {
					_primary_phone_number = phoneNumber;
					_possible_phone_numbers.Add(phoneNumber);
				} else
					_possible_phone_numbers.Add(phoneNumber);
			}

			/**
	         * Sets the possible photo for the user.
	         * @param photo the possible photo
	         */
			public void AddPossiblePhoto(Android.Net.Uri photo) {
				if (photo != null) _possible_photo = photo;
			}

			/**
	         * Retrieves the list of possible email addresses.
	         * @return the list of possible email addresses
	         */
			public IList<string> PossibleEmails() {
				return _possible_emails;
			}

			/**
	         * Retrieves the list of possible names.
	         * @return the list of possible names
	         */
			public IList<string> PossibleNames() {
				return _possible_names;
			}

			/**
	         * Retrieves the list of possible phone numbers
	         * @return the list of possible phone numbers
	         */
			public IList<string> PossiblePhoneNumbers() {
				return _possible_phone_numbers;
			}

			/**
	         * Retrieves the possible photo.
	         * @return the possible photo
	         */
			public Android.Net.Uri PossiblePhoto() {
				return _possible_photo;
			}

			/**
	         * Retrieves the primary email address.
	         * @return the primary email address
	         */
			public string PrimaryEmail() {
				return _primary_email;
			}

			/**
	         * Retrieves the primary phone number
	         * @return the primary phone number
	         */
			public string PrimaryPhoneNumber() {
				return _primary_phone_number;
			}

			/** The primary email address */
			static string _primary_email;
			/** The primary name */
			//static string _primary_name;
			/** The primary phone number */
			static string _primary_phone_number;
			/** A list of possible email addresses for the user */
			static IList<string> _possible_emails = new List<string> ();
			/** A list of possible names for the user */
			static IList<string> _possible_names = new List<string> ();
			/** A list of possible phone numbers for the user */
			static IList<string> _possible_phone_numbers = new List<string> ();
			/** A possible photo for the user */
			static Android.Net.Uri _possible_photo;
		}

		/**
	     * Retrieves the user profile information.
	     * @param context the context from which to retrieve the user profile
	     * @return the user profile
	     */
		public static AccountUtils.UserProfile GetUserProfile(Context context) {
			return getUserProfileOnIcsDevice (context);
		}

		/**
	     * Retrieves the user profile information in a manner supported by Ice Cream Sandwich devices.
	     * @param context the context from which to retrieve the user's email address and name
	     * @return  a list of the possible user's email address and name
	     */
		static AccountUtils.UserProfile getUserProfileOnIcsDevice(Context context) {
			ContentResolver content = context.ContentResolver;
			ICursor cursor = content.Query(
				// Retrieves data rows for the device user's 'profile' contact
				Android.Net.Uri.WithAppendedPath(
					ContactsContract.Profile.ContentUri,
					ContactsContract.Contacts.Data.ContentDirectory),
				ProfileQuery.PROJECTION,

				// Selects only email addresses or names
				ContactsContract.Contacts.Data.InterfaceConsts.Mimetype + "=? OR "
				+ ContactsContract.Contacts.Data.InterfaceConsts.Mimetype + "=? OR "
				+ ContactsContract.Contacts.Data.InterfaceConsts.Mimetype + "=? OR "
				+ ContactsContract.Contacts.Data.InterfaceConsts.Mimetype + "=?",
				new [] {
					ContactsContract.CommonDataKinds.Email.ContentItemType,
					ContactsContract.CommonDataKinds.StructuredName.ContentItemType,
					ContactsContract.CommonDataKinds.Phone.ContentItemType,
					ContactsContract.CommonDataKinds.Photo.ContentItemType
				},

				// Show primary rows first. Note that there won't be a primary email address if the
				// user hasn't specified one.
				ContactsContract.Contacts.Data.InterfaceConsts.IsPrimary + " DESC"
			);

			var user_profile = new UserProfile();
			string mime_type;
			while (cursor.MoveToNext()) {
				
				mime_type = cursor.GetString (ProfileQuery.MIME_TYPE);

				if (mime_type.Equals (ContactsContract.CommonDataKinds.Email.ContentItemType)) {
					user_profile.AddPossibleEmail (cursor.GetString (ProfileQuery.EMAIL), cursor.GetInt (ProfileQuery.IS_PRIMARY_EMAIL) > 0);
				} else if (mime_type.Equals (ContactsContract.CommonDataKinds.StructuredName.ContentItemType)) {
					user_profile.AddPossibleName (cursor.GetString (ProfileQuery.GIVEN_NAME) + " " + cursor.GetString (ProfileQuery.FAMILY_NAME));
				} else if (mime_type.Equals (ContactsContract.CommonDataKinds.Phone.ContentItemType)) {
					user_profile.AddPossiblePhoneNumber (cursor.GetString (ProfileQuery.PHONE_NUMBER), cursor.GetInt (ProfileQuery.IS_PRIMARY_PHONE_NUMBER) > 0);
				} else if (mime_type.Equals (ContactsContract.CommonDataKinds.Photo.ContentItemType)) {
					string uri = cursor.GetString (ProfileQuery.PHOTO);
					if (uri != null) {
						user_profile.AddPossiblePhoto (Android.Net.Uri.Parse (uri));
					}
				}
			}

			cursor.Close ();

			return user_profile;
		}

		/**
	     * Contacts user profile query interface.
	     */
		static class ProfileQuery {
			/** The set of columns to extract from the profile query results */
			public static string[] PROJECTION = {
				ContactsContract.CommonDataKinds.Email.Address,
				ContactsContract.CommonDataKinds.Email.InterfaceConsts.IsPrimary,
				ContactsContract.CommonDataKinds.StructuredName.FamilyName,
				ContactsContract.CommonDataKinds.StructuredName.GivenName,
				ContactsContract.CommonDataKinds.Phone.Number,
				ContactsContract.CommonDataKinds.Phone.InterfaceConsts.IsPrimary,
				ContactsContract.CommonDataKinds.Photo.InterfaceConsts.PhotoUri,
				ContactsContract.Contacts.Data.InterfaceConsts.Mimetype
			};

			/** Column index for the email address in the profile query results */
			public const int EMAIL = 0;

			/** Column index for the primary email address indicator in the profile query results */
			public const int IS_PRIMARY_EMAIL = 1;

			/** Column index for the family name in the profile query results */
			public const int FAMILY_NAME = 2;

			/** Column index for the given name in the profile query results */
			public const int GIVEN_NAME = 3;

			/** Column index for the phone number in the profile query results */
			public const int PHONE_NUMBER = 4;

			/** Column index for the primary phone number in the profile query results */
			public const int IS_PRIMARY_PHONE_NUMBER = 5;

			/** Column index for the photo in the profile query results */
			public const int PHOTO = 6;

			/** Column index for the MIME type in the profile query results */
			public const int MIME_TYPE = 7;
		}

	}
}