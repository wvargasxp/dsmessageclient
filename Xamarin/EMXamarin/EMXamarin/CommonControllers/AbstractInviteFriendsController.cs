using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace em {
	public class AbstractInviteFriendsController {

		#region string
		// Title of alert when device is incapable of sending SMS.
		public string CantSendSMSTitle {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_DEVICE_FAILED_SMS_TITLE"); }
		}

		// Title of alert when device is incapable of sending Email.
		public string CantSendEmailTitle {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_DEVICE_FAILED_EMAIL_TITLE"); }
		}

		// Message body of alert when device is incapable of sending SMS.
		public string CantSendSMSBody {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_DEVICE_FAILED_SMS_MESSAGE"); }
		}

		// Message body of alert when device is incapable of sending Email.
		public string CantSendEmailBody {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_DEVICE_FAILED_EMAIL_MESSAGE"); }
		}

		// Body of SMS invite.
		public string MessageBodySMS {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_SMS_MESSAGE"); }
		}

		// Body of HTML Email invite.
		public string MessageBodyEmail {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_EMAIL_MESSAGE"); }
		}

		// Title of alert when there was an error in sending SMS or Email.
		public string ErrorTitle {
			get { return appModel.platformFactory.GetTranslation ("SEND_MESSAGE_FAILED_TITLE"); }
		}

		// Message body of alert when there was an error in sending SMS or Email invite.
		public string ErrorBody {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_ERROR_MESSAGE"); }
		}

		// Title of alert when user cancelled invite.
		public string CancelTitle {
			get { return appModel.platformFactory.GetTranslation ("CANCELLED"); }
		}

		// Message body of alert when user cancelled the alert.
		public string CancelBody {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_CANCELLED_MESSAGE"); }
		}

		// Title of alert for when user saved the email as a draft instead of sending it.
		public string SavedTitle {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_EMAIL_DRAFT_SAVED_TITLE"); }
		}

		// Message body of alert for when user saved the email as a draft instead of sending it.
		public string SavedBody {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_EMAIL_DRAFT_SAVED_MESSAGE"); }
		}

		// Title of alert when user has successfully sent an invite (SMS/Email).
		public string SentTitle {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_INVITATION_SENT_TITLE"); }
		}

		// Message body of when the user has sent the invite successfully.
		public string SentBody {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_INVITATION_SENT_MESSAGE"); }
		}

		// Subject of email
		public string EmailSubject {
			get { return appModel.platformFactory.GetTranslation ("INVITE_FRIENDS_EMAIL_SUBJECT"); }
		}

		#endregion

		ApplicationModel appModel;

		JToken _properties;
		public JToken Properties {
			set {
				_properties = value;
				/*
				JObject asObject = value as JObject;
				if ( asObject != null ) {
					JToken tok;
					tok = asObject ["groupName"];
					if (tok != null)
						Group.displayName = tok.Value<string>();
					tok = asObject ["groupPhotoURL"];
					if (tok != null)
						Group.thumbnailURL = tok.Value<string>();
					tok = asObject ["groupAttributes"];
					if (tok != null)
						Group.attributes = (JObject) tok;
				}*/
			}
		}

		// TODO on close, if this is set we should post back to the server
		public string ResponseDestination { get; set; }

		private AddressBookSelectionResult _result = null;
		public AddressBookSelectionResult Result { 
			get { return this._result; }
			set { 
				this._result = value; 
				GenerateRecipients ();
			}
		}

		public IList<Contact> PhoneContacts { get; set; }
		public IList<Contact> EmailContacts { get; set; }

		private void GenerateRecipients () {
			this.PhoneContacts = new List<Contact> ();
			this.EmailContacts = new List<Contact> ();

			IList<Contact> contacts = this.Result.Contacts;
			int count = contacts.Count;
			for (int i = 0; i < count; i++) {
				Contact contact = contacts [i];
				ContactIdentifierType identifierType = contact.identifierType;
				if (identifierType == ContactIdentifierType.Phone) {
					this.PhoneContacts.Add (contact);
				} else if (identifierType == ContactIdentifierType.Email) {
					this.EmailContacts.Add (contact);
				} else {
					// do nothing
				}
			}
		}

		public AbstractInviteFriendsController (ApplicationModel applicationModel) {
			appModel = applicationModel;
		}

		public bool HasSmsContacts {
			get {
				return this.PhoneContacts.Count > 0;
			}
		}

		public bool HasEmailContact {
			get {
				return this.EmailContacts.Count > 0;
			}
		}

		public string[] SMSRecipients {
			get {
				int count = this.PhoneContacts.Count;
				string[] arr = new string[count];
				for (int i = 0; i < count; i ++) {
					Contact contact = this.PhoneContacts [i];
					string description = contact.description;
					arr [i] = description;
				}

				return arr;
			}
		}

		public string[] EmailRecipients {
			get {
				int count = this.EmailContacts.Count;
				string[] arr = new string[count];
				for (int i = 0; i< count; i++) {
					Contact contact = this.EmailContacts [i];
					string description = contact.description;
					arr [i] = description;
				}

				return arr;
			}
		}
	}
}