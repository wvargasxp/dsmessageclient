using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;

namespace em {
	public class ContactsManager {
		IAddressBook addressBook;
		ApplicationModel appModel;

		private string Md5Hash { get; set; }
		private bool ModifiedAddressBook { get; set; }
		private RegisterContactsOutbound Outbound { get; set; }

		private bool IsInTheMiddleOfProcessingContacts { 
			get {
				switch (this.ContactProcessingState) {
				case ContactProcessingState.Acquiring_Access:
				case ContactProcessingState.Accessing:
				case ContactProcessingState.Processing:
				case ContactProcessingState.Registering:
					return true;
				default:
					return false;
				}
			}
		}

		private ContactProcessingState contactProcessingState;
		public ContactProcessingState ContactProcessingState {
			get {
				return contactProcessingState;
			}
		}

		private object contactProcessingStateLock = new object ();

		private bool UpdateContactProcessingState (ContactProcessingState newState) {
			bool updated;

			lock (contactProcessingStateLock) {
				ContactProcessingState oldState = this.contactProcessingState;

				if (newState == ContactProcessingState.Inactive) {
					this.contactProcessingState = newState;			// reset
				} else if (this.contactProcessingState < newState) {
					this.contactProcessingState = newState;
				}

				updated = this.contactProcessingState != oldState;
			}

			return updated;
		}

		public ContactsManager (ApplicationModel _appModel, IAddressBook ab) {
			addressBook = ab;
			appModel = _appModel;

			this.contactProcessingState = ContactProcessingState.Inactive;
		}

		private void ClearContactRegistrationInformation () {
			this.Md5Hash = string.Empty;
			this.ModifiedAddressBook = false;
			this.Outbound = null;
		}

		public void AccessContactsWithPermission (Action<ContactsUpdatedStatus> contactsUpdatedCallback, bool sendToServer = true) {
			if (this.IsInTheMiddleOfProcessingContacts) {
				// Don't kick off multiple contact processing tasks while another one is in progress.
				Debug.WriteLine ("ContactsManager: Already in the middle of processing contacts. Returning.");
				return;
			}

			if (this.ContactProcessingState == ContactProcessingState.Awaiting_Registration) {
				if (sendToServer) {
					RegisterContacts (this.Outbound, this.ModifiedAddressBook, this.Md5Hash, contactsUpdatedCallback);
					ClearContactRegistrationInformation ();
				}

				return;
			}

			UpdateContactProcessingState (ContactProcessingState.Acquiring_Access);

			addressBook.ListOfContacts ((accessGranted, contacts) => {
				Debug.Assert (this.ContactProcessingState == ContactProcessingState.Acquiring_Access);
				NotificationCenter.DefaultCenter.PostNotification (null, Constants.ApplicationModel_DidRegisterContactsNotification);

				if (!accessGranted) {
					UpdateContactProcessingState (ContactProcessingState.Inactive);
					contactsUpdatedCallback (ContactsUpdatedStatus.FailedToProccess);
				} else {
					UpdateContactProcessingState (ContactProcessingState.Accessing);
					NotificationCenter.DefaultCenter.PostNotification (null, Constants.ContactsManager_StartAccessedDifferentContacts);
					EMTask.DispatchBackground (() => {
						BackgroundHandleContactsAccessGranted (contacts, contactsUpdatedCallback, sendToServer);
					});
				}
			});
		}

		private void BackgroundHandleContactsAccessGranted (List<AddressBookContact> contacts, Action<ContactsUpdatedStatus> contactsUpdatedCallback, bool sendToServer) {
			Debug.Assert (this.ContactProcessingState == ContactProcessingState.Accessing);
			try {
				UpdateContactProcessingState (ContactProcessingState.Processing);

				string contactsJson = JsonConvert.SerializeObject(contacts);
				ISecurityManager securityManager = ApplicationModel.SharedPlatform.GetSecurityManager ();
				this.Md5Hash = securityManager.CalculateMD5Hash (contactsJson);
				var previousHash = Preference.GetPreference<string>(appModel, Preference.ADDRESS_BOOK_CHECKSUM);

				this.ModifiedAddressBook = !this.Md5Hash.Equals (previousHash);

				foreach (AddressBookContact contact in contacts) {
					foreach (ContactInfo info in contact.contactInfo ) {
						Contact existing = Contact.FindContactByAddressBookIDAndDescription (appModel, contact.clientID, info.value);
						if (existing == null) {
							Contact newContact = Contact.FromAddressBookContact (appModel, contact, info);
							newContact.Save ();
						} else if(Contact.UpdateFromAddressBookContact(existing, contact, info))
							existing.Save();
					}
				}

				this.Outbound = new RegisterContactsOutbound();
				this.Outbound.contacts = contacts;
				this.Outbound.contactsVersion = Preference.GetPreference<int>(appModel, Preference.CONTACTS_VERSION);

				if (sendToServer) {
					RegisterContacts (this.Outbound, this.ModifiedAddressBook, this.Md5Hash, contactsUpdatedCallback);
					ClearContactRegistrationInformation ();
				} else {
					UpdateContactProcessingState (ContactProcessingState.Awaiting_Registration);
				}
			} catch (Exception e) {
				Debug.WriteLine (string.Format ("Failed to read contacts from address book and register them: {0}\n{1}", e.Message, e.StackTrace));
				UpdateContactProcessingState (ContactProcessingState.Inactive);
			} finally {
				NotificationCenter.DefaultCenter.PostNotification (null, Constants.ContactsManager_AccessedDifferentContacts);
			}
		}

		void RegisterContacts (RegisterContactsOutbound registerContacts, bool modified, string hash, Action<ContactsUpdatedStatus> contactsUpdatedCallback) {
			UpdateContactProcessingState (ContactProcessingState.Registering);
			EMHttpClient httpClient = appModel.account.httpClient;
			if (modified) {
				httpClient.SendApiRequestAsync ("registerContacts", registerContacts, HttpMethod.Post, "application/json", obj => {
					if (obj.IsSuccess) {
						string responseString = obj.ResponseAsString;
						ProcessContactsResponse (true, responseString, contactsUpdatedCallback, true, hash);
					} else {
						ProcessContactsResponse(false, "NULL response from server", contactsUpdatedCallback, false, null);
					}
				}, Constants.TIMEOUT_REGISTER_CONTACTS);
			} else {
				httpClient.SendApiRequestAsync ("registerContactsNoChanges", registerContacts, HttpMethod.Post, "application/json", obj => {
					if (obj.IsSuccess) {
						string responseString = obj.ResponseAsString;
						ProcessContactsResponse (true, responseString, contactsUpdatedCallback, false, null);
					} else {
						ProcessContactsResponse(false, "NULL response from server", contactsUpdatedCallback, false, null);
					}
				}, Constants.TIMEOUT_REGISTER_CONTACTS);
			}
		}

		void ProcessContactsResponse (bool success, string responsestr, Action<ContactsUpdatedStatus> contactsUpdatedCallback, bool hasNewContacts, string hash) {
			Debug.Assert (this.ContactProcessingState == ContactProcessingState.Registering);
			if (!success) {
				//TODO: if failed, retry?
				Debug.WriteLine ("Failed to process contacts response\n{0}", responsestr);
				UpdateContactProcessingState (ContactProcessingState.Inactive);
				NotificationCenter.DefaultCenter.PostNotification (null, Constants.ContactsManager_FailedProcessedDifferentContacts);
				contactsUpdatedCallback (ContactsUpdatedStatus.FailedToProccess);
			}
			else {
				Contact.FindAllNonTemporaryContacts (appModel, 
					onCompletion: ((IList<Contact> nonTempContacts) => {
						BackgroundHandleProcessContactsSetTemps (nonTempContacts, responsestr, contactsUpdatedCallback, hasNewContacts, hash);
					})
					, onFailure: () => {
						UpdateContactProcessingState (ContactProcessingState.Inactive);
					});
			}
		}

		private void BackgroundHandleProcessContactsSetTemps (IList<Contact> nonTempContacts, string responsestr, Action<ContactsUpdatedStatus> contactsUpdatedCallback, bool hasNewContacts, string hash) {
			Debug.Assert (this.ContactProcessingState == ContactProcessingState.Registering);
			try {
				var setOfNonTempContacts = new HashSet<Contact>(nonTempContacts);

				RegisterContactsResponse response = JsonConvert.DeserializeObject<RegisterContactsResponse>(responsestr);
				int responseContactsVersion = response.contactsVersion;
				int currentContactsVersion = Preference.GetPreference<int>(appModel, Preference.CONTACTS_VERSION);
				if (responseContactsVersion != currentContactsVersion) {
					//Debug.WriteLine ("response string is {0}", responsestr);

					foreach (ContactInput contactInput in response.contacts) {
						if (!string.IsNullOrEmpty (contactInput.clientID)) {
							Contact existing = Contact.FindContactByAddressBookIDAndDescription (appModel, contactInput.clientID, contactInput.description);
							if (existing != null) {
								if (Contact.UpdateFromContactInput (existing, contactInput, true)) {
									existing.Save ();
								}
									
								if (setOfNonTempContacts.Contains (existing)) {
									//Debug.WriteLine ("remove existing from set {0} {1} {2}", existing.displayName, existing.contactID, existing.description);
									setOfNonTempContacts.Remove (existing);
								} else {
//									Debug.WriteLine ("existing contact is not in set {0} {1} {2}", existing.displayName, existing.contactID, existing.description);
								}

							} else {
//								Debug.WriteLine ("contact with clientid was null {0} {1}", contactInput.contactID, contactInput.description);
							}
						} else {
							Contact existing = Contact.FindContactByServerID (appModel, contactInput.serverID);
							if (existing == null) {
								if (ContactLifecycleHelper.EMCanSendTo(contactInput.lifecycle)) {
									if (contactInput.group && !GroupMemberLifecycleHelper.EMCanSendTo (contactInput.groupMemberLifecycle))
										continue;

									Contact fromServer = Contact.FromContactInput (appModel, contactInput, true);
									fromServer.Save ();

									if (fromServer.IsAGroup)
										EMTask.DispatchMain (() => Contact.DelegateDidAddGroup (fromServer)); // Notify via delegates group added
								}
							} else {
								if (Contact.UpdateFromContactInput (existing, contactInput, true)) {
									existing.Save();

									if (existing.IsAGroup) {
										if (ContactLifecycleHelper.EMCanSendTo(contactInput.lifecycle) && GroupMemberLifecycleHelper.EMCanSendTo(contactInput.groupMemberLifecycle))
											EMTask.DispatchMain (() => Contact.DelegateDidUpdateGroup (existing)); // Notify via delegates group updated
										else
											EMTask.DispatchMain (() => Contact.DelegateDidDeleteGroup (existing)); // Notify via delegates group deleted
									}	
								}

								if (setOfNonTempContacts.Contains (existing)) {
//									Debug.WriteLine ("remove existing from set {0} {1} {2}", existing.displayName, existing.contactID, existing.description);
									setOfNonTempContacts.Remove (existing);
								} else {
//									Debug.WriteLine ("existing contact is not in set {0} {1} {2}", existing.displayName, existing.contactID, existing.description);
								}
							}
						}
					}

					// If the set contains any remaining contacts, that means the server didn't return them as part of the registercontacts call.
					// So they should be marked temporary.
					foreach (Contact c in setOfNonTempContacts) {
						//Debug.WriteLine ("contact {0} {1} {2} will  be marked temp", c.displayName, c.contactID, c.description);
						c.tempContact.Value = true;
						c.SaveAsync ();

						if (c.IsAGroup)
							EMTask.DispatchMain (() => Contact.DelegateDidDeleteGroup (c)); // Notify via delegates group deleted
					}

					// Update the contacts version once we've finished processing the contact's response.
					Preference.UpdatePreference<int>(appModel, Preference.CONTACTS_VERSION, responseContactsVersion);

					if (hash != null) {
						Preference.UpdatePreference (appModel, Preference.ADDRESS_BOOK_CHECKSUM, hash);
					}
				}
			}
			catch (Exception e) {
				Debug.WriteLine(string.Format("Unable to Process Contacts Response {0}\n{1}", e.Message, e.StackTrace));
			} finally {
				UpdateContactProcessingState (ContactProcessingState.Inactive);
				NotificationCenter.DefaultCenter.PostNotification (null, Constants.ContactsManager_ProcessedDifferentContacts);
			}

			if (hasNewContacts)
				contactsUpdatedCallback(ContactsUpdatedStatus.RegisteredNewContacts);
			else 
				contactsUpdatedCallback(ContactsUpdatedStatus.RegisteredNoChanges);
		}

		void PopulateContactInfoArray (IList<Dictionary<string, string>> contactInfo, IList<string> contactArray, IList<string> labelArray, string infoType) {
			// Currently unused.
			if (contactArray == null || labelArray == null)
				return;

			for (int i = 0; i < contactArray.Count; i++) {
				string identifier = contactArray [i];

				#region Localization
				string label = labelArray [i]; // TODO: Localization
				/*
				CFStringRef localized = ABAddressBookCopyLocalizedLabel( (__bridge CFStringRef) label);
				NSString* local = (__bridge_transfer NSString*) localized;
				*/
				string local = label;
				#endregion

				var retVal = new Dictionary<string, string> ();
				retVal ["value"] = identifier;
				retVal ["label"] = label;
				retVal ["localizedLabel"] = local; 
				retVal ["contactInfoType"] = infoType;
				contactInfo.Add (retVal);
			}
		}
	}

	public enum ContactsUpdatedStatus{
		RegisteredNewContacts,
		FailedToProccess,
		RegisteredNoChanges
	}
}
