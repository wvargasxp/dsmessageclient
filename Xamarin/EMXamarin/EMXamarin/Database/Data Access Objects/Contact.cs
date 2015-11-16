using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace em {
	public class Contact : CounterParty {
		static readonly List<Dictionary<string,Contact>> cacheList = new List<Dictionary<string,Contact>>();

		public int contactID { get; set; }

		public string addressBookID { get; set; }
		public string serverID { get; set; }
		public string label { get; set; }
		public string description { get; set; }
		public bool preferred { get; set; }
		public string preferredString {
			set { preferred = value != null && value.Equals ("Y"); }
			get { return preferred ? "Y" : "N"; }
		}
		public string serverContactID { get; set; }
		public string addressBookFirstName { get; set; }
		public string addressBookLastName { get; set; }
		public string fromAlias { get; set; }
		public string isGroup { get; set; }
		public bool IsAGroup {
			get { return isGroup != null && isGroup.Equals ("Y") ? true : false; }
		}

		public string GroupStatusString {
			get { return GroupMemberStatusHelper.ToDatabase (GroupStatus); }
			set { GroupStatus = GroupMemberStatusHelper.FromDatabase (value); }
		}
		GroupMemberStatus gms;
		public GroupMemberStatus GroupStatus { 
			get { return gms; } 
			set {
				GroupMemberStatus old = gms;
				gms = value;
				if (gms != old)
					DelegateDidChangeGroupMemberStatus (this);
			}
		}

		public bool IsBlocked { get { return BlockStatus != BlockStatus.HasBlockedUs && BlockStatus != BlockStatus.NotBlocked; } }
		public string BlockStatusString {
			get { return BlockStatusHelper.ToDatabase (bs); }
			set { bs = BlockStatusHelper.FromDatabase (value); }
		}
		BlockStatus bs;
		public BlockStatus BlockStatus { 
			get { return bs; }
			set {
				BlockStatus old = bs;
				bs = value;
				if (bs != old)
					DelegateDidChangeBlockStatus (this);
			}
		}

		public string GroupMemberLifeCycleString {
			get { return GroupMemberLifecycleHelper.ToDatabase (this.GroupMemberLifeCycle); }
			set { this.GroupMemberLifeCycle = GroupMemberLifecycleHelper.FromDatabase (value); }
		}
		GroupMemberLifecycle gml;
		public GroupMemberLifecycle GroupMemberLifeCycle { 
			get { return gml; } 
			set { 
				GroupMemberLifecycle old = gml;
				gml = value;
				if (gml != old)
					DelegateDidChangeGroupMemberLifecycle (this);
			} 
		}

		public string AddressBookLifeCycleString {
			get { return AddressBookContactLifeCycleHelper.ToDatabase (this.AddressBookLifeCycle); }
			set { this.AddressBookLifeCycle = AddressBookContactLifeCycleHelper.FromDatabase (value); }
		}
		AddressBookContactLifeCycle abcl;
		public AddressBookContactLifeCycle AddressBookLifeCycle { 
			get { return abcl; } 
			set {
				AddressBookContactLifeCycle old = abcl;
				abcl = value;
				if (abcl != old)
					DelegatDidChangeAddressBookContactLifecycle (this);
			} 
		}

		// Contact: Bob
		// LastUsedIdentifierToSendFrom: Me (Could be an Alias)
		// So this property is the identifier we used to last message Bob (this Contact)
		public string LastUsedIdentifierToSendFrom { get; set; }

		bool m;
		public bool me { get { return m; } set { m = value; } }
		public string meString { get { return m ? "Y" : "N"; } set { m = value.Equals ("Y"); } }
		Property<bool> t;
		// A temp contact is one that we know about due to incoming messages but one that's not
		// part of our address book.
		public Property<bool> tempContact { get { return t; } set { t = value; }}
		public string tempContactString { get { return t.Value ? "Y" : "N"; } set { t.Value = value.Equals("Y"); }}

		string it = "U";
		public string identifierTypeString { get { return it; } set { it = value; } }
		public ContactIdentifierType identifierType {
			get { return ContactIdentifierTypeHelper.FromDatabase (identifierTypeString); }
			set { ; } // DON'T CALL THIS, Set the string directly
		}

		string pnt = "A";
		public string phoneNumberTypeString { get { return pnt; } set { pnt = value; } }
		public PhoneNumberType phoneNumberType {
			get { return PhoneNumberTypeHelper.FromDatabase (phoneNumberTypeString); }
			set { ; } // DON'T CALL THIS, Set the string directly
		}


		public delegate void DidAddGroup(Contact group);
		public static DidAddGroup DelegateDidAddGroup = delegate(Contact group) {
		};

		public delegate void DidUpdateGroup(Contact group);
		public static DidUpdateGroup DelegateDidUpdateGroup = delegate(Contact group) {
		};

		public delegate void DidDeleteGroup(Contact group);
		public static DidDeleteGroup DelegateDidDeleteGroup = delegate(Contact group) {
		};

		public delegate void DidChangeBlockStatus(Contact c);
		public DidChangeBlockStatus DelegateDidChangeBlockStatus = delegate (Contact cw) {
		};

		public delegate void DidChangeGroupMemberStatus(Contact c);
		public DidChangeGroupMemberStatus DelegateDidChangeGroupMemberStatus = delegate (Contact gm) {
		};

		public delegate void DidChangeGroupMemberLifecycle(Contact c);
		public DidChangeGroupMemberLifecycle DelegateDidChangeGroupMemberLifecycle = delegate (Contact ml) {
		};

		public delegate void DidChangeAddressBookContactLifecycle(Contact c);
		public DidChangeAddressBookContactLifecycle DelegatDidChangeAddressBookContactLifecycle = delegate (Contact abl) {
		};

		public Contact () {
			tempContact = new Property<bool> ();
		}

		public override int GetHashCode() {
			return contactID;
		}

		public override bool Equals(object o) {
			var other = o as Contact;
			if (other == null)
				return false;
				
			return other.contactID == contactID;
		}

		public bool MatchesByServerId (Contact o) {
			if (o == null) return false;
			
			string serverId = this.serverID;
			string otherServerId = o.serverID;

			// If either contact's don't have server ids, lets just return false. 
			// We're only interested in pairing contacts that have server ids.
			if (string.IsNullOrWhiteSpace (serverID) || string.IsNullOrWhiteSpace (otherServerId)) {
				return false;
			}

			bool serverIdMatches = serverID.Equals (otherServerId);
			return serverIdMatches;
		}

		public string GetSortableString() {
			string s = displayName.Trim ();
			int indexOf = s.LastIndexOf (" ");
			if (indexOf == -1)
				return s;

			string last = s.Substring (indexOf+1);
			string first = s.Substring (0, indexOf);

			return string.Format ("{0} {1}", last, first);
		}

		public void SaveAsync () {
			EMTask.DispatchBackground (Save);
		}

		public void Save () {
			Contact contact = this;
			if (serverID != null) {
				if (cacheList [appModel.cacheIndex].ContainsKey (serverID)) {
					Contact fromCache = cacheList [appModel.cacheIndex] [serverID];

					// This is to handle the case where missed messages arrive first and a contact is created with only its serverId.
					// We update the contact in the cache (and the corresponding contact row in the database) and delete the extraneous contact.
					if (fromCache.addressBookID == null && fromCache.addressBookID != contact.addressBookID) {
						fromCache.addressBookID = this.addressBookID;
						fromCache.serverID = this.serverID;
						fromCache.serverContactID = this.serverContactID;
						fromCache.displayName = this.displayName;
						fromCache.addressBookFirstName = this.addressBookFirstName;
						fromCache.addressBookLastName = this.addressBookLastName;
						fromCache.fromAlias = this.fromAlias;
						fromCache.isGroup = this.isGroup;
						fromCache.meString = this.meString;
						fromCache.tempContactString = this.tempContactString;
						fromCache.label = this.label;
						fromCache.description = this.description;
						fromCache.thumbnailURL = this.thumbnailURL;
						fromCache.preferred = this.preferred;
						fromCache.attributesString = this.attributesString;
						fromCache.lifecycleString = this.lifecycleString;
						fromCache.identifierTypeString = this.identifierTypeString;
						fromCache.phoneNumberTypeString = this.phoneNumberTypeString;
						fromCache.AddressBookLifeCycleString = this.AddressBookLifeCycleString;
						fromCache.GroupMemberLifeCycleString = this.GroupMemberLifeCycleString;
						fromCache.GroupStatusString = this.GroupStatusString;
						fromCache.LastUsedIdentifierToSendFrom = this.LastUsedIdentifierToSendFrom;
						fromCache.BlockStatus = this.BlockStatus;
						appModel.contactDao.DeleteContactWithContactId (contact);
						contact = fromCache;
					}

				} else {
					cacheList[appModel.cacheIndex].Add (serverID, contact);
				}
			} 

			if (contact.isPersisted)
				appModel.contactDao.UpdateContact (contact);
			else
				appModel.contactDao.InsertContact (contact);


			// get thumbnail if needed.
			if (media != null && !media.IsLocal())
				CopyPhotoFromAddressBook ();
		}

		public void CopyPhotoFromAddressBook() {
			if (addressBookID != null && thumbnailURL != null && thumbnailURL.StartsWith("addressbook")) {
				string localPath = media.GetPathForUri (appModel.platformFactory);
				appModel.platformFactory.getAddressBook ().CopyThumbnailFromAddressBook (media.uri, localPath);
			}
		}

		public static void AddCacheAtIndex(int index) {
			cacheList.Insert(index, new Dictionary<string, Contact> ());
		}

		public static Contact NewContact(ApplicationModel _appModel) {
			var retVal = new Contact ();
			retVal.appModel = _appModel;
			return retVal;
		}

		public static Contact FromAddressBookContact(ApplicationModel _appModel, AddressBookContact abContact, ContactInfo info) {
			Contact retVal = Contact.NewContact (_appModel );
			retVal.addressBookID = abContact.clientID;
			retVal.addressBookFirstName = abContact.firstName;
			retVal.addressBookLastName = abContact.lastName;
			retVal.displayName = abContact.displayName;
			retVal.isGroup = "N";
			retVal.tempContact.Value = false;
			retVal.isPersisted = false;
			retVal.thumbnailURL = abContact.GetThumbnailUri ();
			retVal.label = info.localizedLabel;
			retVal.description = info.value;
			retVal.preferred = false;

			return retVal;
		}

		public static bool UpdateFromAddressBookContact(Contact contact, AddressBookContact abContact, ContactInfo info) {
			bool changed = false;

			if (!(new EqualsBuilder<string> (contact.addressBookFirstName, abContact.firstName).Equals ())) {
				contact.addressBookFirstName = abContact.firstName;
				changed = true;
			}

			if (!(new EqualsBuilder<string> (contact.addressBookLastName, abContact.lastName).Equals ())) {
				contact.addressBookLastName = abContact.lastName;
				changed = true;
			}

			if (!(new EqualsBuilder<string> (contact.displayName, abContact.displayName).Equals ())) {
				contact.displayName = abContact.displayName;
				changed = true;
			}

			if (!(new EqualsBuilder<string> (contact.label, info.localizedLabel).Equals ())) {
				contact.label = info.localizedLabel;
				changed = true;
			}

			return changed;
		}

		public static Contact FromContactInput (ApplicationModel _appModel, ContactInput contactInput) {
			return FromContactInput (_appModel, contactInput, false);
		}

		public static Contact FromContactInput (ApplicationModel _appModel, ContactInput contactInput, bool isRegisteredContactInput) {
			Contact newContact = Contact.NewContact (_appModel );
			UpdateFromContactInput (newContact, contactInput, isRegisteredContactInput);
			newContact.isPersisted = false;
			return newContact;
		}

		public static bool UpdateFromContactInput(Contact contact, ContactInput contactInput) {
			return innerUpdateFromContactInput (contact, contactInput, false, false);
		}

		public static bool UpdateFromContactInput(Contact contact, ContactInput contactInput, bool isRegisteredContactInput) {
			return innerUpdateFromContactInput (contact, contactInput, isRegisteredContactInput, false);
		}

		public static bool UpdateFromContactInput(Contact contact, ContactInput contactInput, bool isRegisteredContactInput, bool isGroupContactInput) {
			return innerUpdateFromContactInput (contact, contactInput, isRegisteredContactInput, isGroupContactInput);
		}

		static bool innerUpdateFromContactInput(Contact contact, ContactInput contactInput, bool isRegisteredContactInput, bool isGroupContactInput) {
			bool changed = false;
			if (!(new EqualsBuilder<string> (contact.serverID, contactInput.serverID).Equals ())) {
				contact.serverID = contactInput.serverID;
				changed = true;
			}

			// checking for !contactInput.attributes.Any () because attributes is usually non-null but doesn't have any contents
			if (!(new EqualsBuilder<string> (contact.attributesString, contactInput.attributes == null || !contactInput.attributes.Any () ? null : contactInput.attributes.ToString()).Equals ())) {
				contact.attributes = contactInput.attributes as JObject;
				changed = true;
			}

			if ( contact.addressBookID == null && !(new EqualsBuilder<string> (contact.displayName, contactInput.displayName).Equals ())) {
				contact.displayName = contactInput.displayName;
				changed = true;
			}

			if (!(new EqualsBuilder<string> (contact.isGroup, contactInput.group ? "Y" : "N").Equals ())) {
				contact.isGroup = contactInput.group ? "Y" : "N";
				changed = true;
			}

			if (!(new EqualsBuilder<bool> (contact.me, contactInput.me).Equals ())) {
				contact.me = contactInput.me;
				changed = true;
			}
				
			if (!(new EqualsBuilder<string> (contact.thumbnailURL, contactInput.thumbnailURL).Equals ())) {
				// we don't clear the thumbnail to null
				if (contactInput.thumbnailURL != null) {
					contact.thumbnailURL = contactInput.thumbnailURL;
					changed = true;
				}
			}

			if (!(new EqualsBuilder<string> (contact.fromAlias, contactInput.toAlias).Equals ())) {
				contact.fromAlias = contactInput.toAlias;
				changed = true;
			}

			if (isRegisteredContactInput) {

				// We need to check the AddressBookContactLifeCycle too.
				// If they're deleted, we don't want to change the contact back to non-temp.
				if (CanSetAsPermanentContact (contact)) {
					contact.tempContact.Value = false;
					changed = true;
				}

				if (!(new EqualsBuilder<string> (contact.label, contactInput.label).Equals ())) {
					contact.label = contactInput.label;
					changed = true;
				}

				if (!(new EqualsBuilder<string> (contact.description, contactInput.description).Equals ())) {
					contact.description = contactInput.description;
					changed = true;
				}

				if ( contact.preferred != contactInput.preferredContact ) {
					contact.preferred = contactInput.preferredContact;
					changed = true;
				}

				if (!(new EqualsBuilder<string> (contact.serverContactID, contactInput.contactID).Equals ())) {
					contact.serverContactID = contactInput.contactID;
					changed = true;
				}

				if (!(new EqualsBuilder<string> (contact.lifecycleString, contactInput.lifecycle).Equals ())) {
					contact.lifecycleString = contactInput.lifecycle;
					if (contact.lifecycle == ContactLifecycle.Deleted) {
						contact.tempContact.Value = true;
					}

					changed = true;
				}

				if (!(new EqualsBuilder<string> (contact.identifierTypeString, contactInput.identifierType).Equals ())) {
					contact.identifierTypeString = contactInput.identifierType;
					changed = true;
				}

				if (contactInput.phoneNumberType != null && !(new EqualsBuilder<string> (contact.phoneNumberTypeString, contactInput.phoneNumberType).Equals ())) {
					contact.phoneNumberTypeString = contactInput.phoneNumberType;
					changed = true;
				}

				if ( contact.IsAGroup ) {
					changed = UpdateGroupFromContactInput (contact, contactInput, changed);
				}

				if (contactInput.addressBookLifeCycle != null && !(new EqualsBuilder<string> (contact.AddressBookLifeCycleString, contactInput.addressBookLifeCycle).Equals ())) {
					AddressBookContactLifeCycle lifecycle = AddressBookContactLifeCycleHelper.FromDatabase (contactInput.addressBookLifeCycle);
					contact.AddressBookLifeCycle = lifecycle;
					if (contact.AddressBookLifeCycle == AddressBookContactLifeCycle.Deleted) {
						// Lets set the contact to be temp if the addressbook lifecycle deems it deleted.
						if (!contact.tempContact.Value) {
							contact.tempContact.Value = true;
						}
					}

					changed = true;
				}

				BlockStatus blockStatus = BlockStatusHelper.FromString (contactInput.blockStatus);
				if (blockStatus != contact.BlockStatus) {
					contact.BlockStatus = blockStatus;
					changed = true;
				}
			} else if(isGroupContactInput) {
				// We need to check the AddressBookContactLifeCycle too.
				// If they're deleted, we don't want to change the contact back to non-temp.
				if (CanSetAsPermanentContact (contact)) {
					contact.tempContact.Value = false;
					changed = true;
				}

				if (!(new EqualsBuilder<string> (contact.lifecycleString, contactInput.lifecycle).Equals ())) {
					contact.lifecycleString = contactInput.lifecycle;
					if (contact.lifecycle == ContactLifecycle.Deleted) {
						contact.tempContact.Value = true;
					}

					changed = true;
				}

				if ( contact.IsAGroup ) {
					changed = UpdateGroupFromContactInput (contact, contactInput, changed);
				}
			}

			return changed;
		}

		static bool UpdateGroupFromContactInput(Contact contact, ContactInput contactInput, bool changed) {
			GroupMemberStatus groupStatus = GroupMemberStatusHelper.FromString (contactInput.memberStatus);
			if (groupStatus != contact.GroupStatus) {
				contact.GroupStatus = groupStatus;
				changed = true;
			}

			string groupMemberLifecycleString = contactInput.groupMemberLifecycle;
			if (!string.IsNullOrEmpty(groupMemberLifecycleString)) {
				if (!(new EqualsBuilder<string> (contact.GroupMemberLifeCycleString, groupMemberLifecycleString).Equals ())) {
					GroupMemberLifecycle lifecycle = GroupMemberLifecycleHelper.FromDatabase (groupMemberLifecycleString);
					contact.GroupMemberLifeCycle = lifecycle;
					// Set this particular group to be temp if the member (yourself) was removed.
					if (contact.GroupMemberLifeCycle == GroupMemberLifecycle.Removed)
						contact.tempContact.Value = true;

					changed = true;
				}
			}

			string groupLifecycleString = contactInput.lifecycle;
			if(!string.IsNullOrEmpty(groupLifecycleString)) {
				ContactLifecycle lifecycle = ContactLifecycleHelper.FromDatabase (groupLifecycleString);
				if(lifecycle == ContactLifecycle.Deleted) {
					contact.tempContact.Value = true;
					EMTask.DispatchMain (() => Contact.DelegateDidDeleteGroup (contact));

					changed = true;
				}
			}

			return changed;
		}

		public static bool CanSetAsPermanentContact (Contact contact) {
			bool tempValue = contact.tempContact.Value;
			bool addressBookLifeCycleOk = contact.AddressBookLifeCycle != AddressBookContactLifeCycle.Deleted;
			bool groupMemberLifeCycleOk = contact.GroupMemberLifeCycle != GroupMemberLifecycle.Removed;
			bool lifecycleOk = contact.lifecycle != ContactLifecycle.Deleted;
			return (tempValue && addressBookLifeCycleOk && groupMemberLifeCycleOk && lifecycleOk);
		}

		public static Contact FindContactByAddressBookIDAndDescription(ApplicationModel _appModel, string addressBookID, string description) {
			lock (_appModel.daoConnection) {
				// check cache though cache is indexed by serverID
				// so this isn't our best way to look these things up
				foreach (Contact existing in cacheList[_appModel.cacheIndex].Values) {
					if (existing.addressBookID != null && existing.addressBookID.Equals (addressBookID) && existing.description != null && existing.description.Equals(description))
						return existing;
				}

				Contact cached =  _appModel.contactDao.ContactWithAddressBookID (addressBookID, description);
				if (cached != null) {
					cached.appModel = _appModel;
					if (cached.serverID != null && !cacheList [_appModel.cacheIndex].ContainsKey (cached.serverID))
						cacheList [_appModel.cacheIndex].Add (cached.serverID, cached);
				}
				return cached;
			}
		}

		public static Contact FindContactByServerID(ApplicationModel _appModel, string serverID) {
			if (serverID != null) {
				lock (_appModel.daoConnection) {
					Contact cached;
					if (!cacheList [_appModel.cacheIndex].TryGetValue (serverID, out cached)) {
						cached = _appModel.contactDao.ContactWithServerID (serverID);
						if (cached != null) {
							cached.appModel = _appModel;
							if (cached.serverID != null && !cacheList [_appModel.cacheIndex].ContainsKey (cached.serverID))
								cacheList [_appModel.cacheIndex].Add (cached.serverID, cached);
						}
					}

					return cached;
				}
			}

			Debug.WriteLine ("NULL serverID when finding contact!");

			return null;
		}

		public static Contact FindContactByContactID(ApplicationModel _appModel, int contactID) {
			// We're checking if contactID is 0 here to avoid extraneous DAO calls.
			// Contact will always be null when contactID is 0.
			if (contactID == 0)
				return null;

			lock (_appModel.daoConnection) {
				foreach (KeyValuePair<string, Contact> entry in cacheList[_appModel.cacheIndex]) {
					if (entry.Value.contactID == contactID)
						return entry.Value;
				}

				Contact cached = _appModel.contactDao.ContactWithContactID (contactID);
				if (cached != null) {
					cached.appModel = _appModel;
					if (cached.serverID != null && !cacheList [_appModel.cacheIndex].ContainsKey (cached.serverID))
						cacheList [_appModel.cacheIndex].Add (cached.serverID, cached);
				}

				return cached;
			}
		}

		public static IList<Contact> FindAllContactsForChatEntry(ApplicationModel _appModel, int chatEntryID) {
			lock (_appModel.daoConnection) {
				IList<Contact> fromDB = _appModel.contactDao.FindContactsWithChatEntryID (chatEntryID);

				var retVal = new List<Contact> ();
				// keep cache in sync
				foreach (Contact dbContact in fromDB) {
					Contact cached;
					if (cacheList [_appModel.cacheIndex].TryGetValue (dbContact.serverID, out cached)) {
						cached.appModel = _appModel;
						retVal.Add (cached);
					} else {
						dbContact.appModel = _appModel;
						cacheList[_appModel.cacheIndex].Add (dbContact.serverID, dbContact);
						retVal.Add (dbContact);
					}
				}

				return retVal;
			}
		}

		public static void FindAllGroupsAsync(ApplicationModel _appModel, Action<IList<Group>> onCompletion) {
			EMTask.Dispatch (() => {
				var retVal = new List<Group> ();
				lock (_appModel.daoConnection) {
					IList<Group> fromDB = _appModel.contactDao.FindAllGroups();
					foreach (Group dbGroup in fromDB) {
						dbGroup.appModel = _appModel;
						retVal.Add (dbGroup);
					}
				}

				EMTask.DispatchMain (() => onCompletion (retVal));
			});
		}

		public static Group FindGroupByServerID(ApplicationModel _appModel, string serverID) {
			lock (_appModel.daoConnection) {
				return _appModel.contactDao.FindGroupByServerID (serverID);
			}
		}

		public static void FindAllContactsWithServerIDsRolledUpAsync (ApplicationModel _appModel, Action<IList<AggregateContact>> onCompletion, AddressBookArgs args) {
			EMTask.Dispatch (() => {
				var retVal = new List<AggregateContact> ();

				var groupedByContactID = new Dictionary<string, AggregateContact> ();
				lock (_appModel.daoConnection) {
					IList<Contact> fromDB = _appModel.contactDao.FindAllContactsWithServerIDs();

					// keep cache in sync
					foreach (Contact dbContact in fromDB) {
						Contact contact = ContactFromCacheUsingDbContact (_appModel, dbContact);
						if (args.ExcludePreferred) {
							if (contact.preferred) {
								continue;
							}
						}

						if ( contact.serverContactID == null ) {
							retVal.Add (new AggregateContact (contact));
						} else {
							Debug.Assert (contact.serverContactID != null, "contact.serverContactID == NULL, logic error in FindAllContactsWithServerIDsRolledUpAsync");
							AggregateContact rolledUpContact = groupedByContactID.ContainsKey (contact.serverContactID) ? groupedByContactID [contact.serverContactID] : null;
							if ( rolledUpContact == null ) {
								rolledUpContact = new AggregateContact (contact);
								groupedByContactID [contact.serverContactID] = rolledUpContact;
							} else {
								if (!rolledUpContact.Contains (contact)) {
									rolledUpContact.AddContact (contact);
								}
							}
						}
					}

					foreach (AggregateContact rolledUpContact in groupedByContactID.Values)
						retVal.Add (rolledUpContact);

					#region removing temp contacts from results
					if (args.ExcludeTemp) {
						var retValExcludingTemporaryContacts = new List<AggregateContact> ();
						foreach (AggregateContact contact in retVal) {
							if (contact.HasTempContact) {
								// handling the case where one identifier is temp but the others are not
								if (!contact.SingleContact) {
									contact.RemoveTempContacts ();

									// handle the case where every contact in the aggregated list is temp and has been removed
									if (contact.HasContacts) {
										retValExcludingTemporaryContacts.Add (contact);
									}
								} 
							} else {
								retValExcludingTemporaryContacts.Add (contact);
							}
						}

						retVal = retValExcludingTemporaryContacts;
					}
					#endregion

					if (args.ExcludeGroups) {
						var retValExcludingGroups = new List<AggregateContact> ();
						foreach (AggregateContact contact in retVal) {
							if (!contact.IsGroupContact)
								retValExcludingGroups.Add (contact);
							retVal = retValExcludingGroups;
						}
					}

					retVal = retVal.OrderBy (c => c.DisplayName, new CompareContactStrings ()).ToList ();
				}

				EMTask.DispatchMain (() => onCompletion (retVal));
			});
		}

		public class CompareContactStrings : IComparer<string> {
			public int Compare (string one, string two)  {
				if (one == null || one.Equals (" ")) {
					return 1;
				} 
				if (two == null || two.Equals (" ")) {
					return -1;
				} 
				int oneLength = one.Length;
				int twoLength = two.Length;
				char[] oneArr = one.ToUpper ().ToCharArray ();
				char[] twoArr = two.ToUpper ().ToCharArray ();
				bool firstIsShorter = false;
				int minLength;
				if (oneLength < twoLength) {
					firstIsShorter = true;
					minLength = oneLength;
				} else {
					minLength = twoLength;
				}
				for (int i = 0; i < minLength; i++) {
					char oneChar = oneArr [i];
					char twoChar = twoArr [i];
					bool isOneLetter = char.IsLetter (oneChar);
					bool isTwoLetter = char.IsLetter (twoChar);
					if (isOneLetter) {
						if (isTwoLetter) {
							int result = oneChar - twoChar;
							if (result == 0) {
								continue;
							} else {
								return result;
							}
						}
						return -1;
					} else if (isTwoLetter) {
						return 1;
					} else {
						int result = oneChar - twoChar;
						if (result == 0) {
							continue;
						} else {
							return result;
						}
					}
				}
				if (firstIsShorter) {
					return -1;
				} else {
					return 1;
				}
			}
		}

		public static Contact ContactFromCacheUsingDbContact (ApplicationModel appModel, Contact dbContact) {

			if (dbContact == null) {
				return null;
			}

			Contact contact = null;
			string key = dbContact.serverID;

			if (key != null) {
				if (cacheList [appModel.cacheIndex].ContainsKey (key)) {
					contact = cacheList [appModel.cacheIndex] [key];
				}

				if (contact != null) {
					if (contact.addressBookID != dbContact.addressBookID) {
						dbContact.appModel = appModel;
						cacheList [appModel.cacheIndex] [key] = dbContact;
						contact = dbContact;
					} else {
						contact.appModel = appModel;
					}
				} else {
					cacheList [appModel.cacheIndex].Add (key, dbContact);
					dbContact.appModel = appModel;
					contact = dbContact;
				}
			} else {
				dbContact.appModel = appModel;
				contact = dbContact;
			}

			return contact;
		}

		public static void FindAllNonTemporaryContacts (ApplicationModel _appModel, Action<IList<Contact>> onCompletion, Action onFailure) {
			EMTask.DispatchBackground (() => {
				lock (_appModel.daoConnection) {
					IList<Contact> retVal = new List<Contact> ();

					try {
						IList<Contact> fromDB = _appModel.contactDao.FindAllPermanentContacts ();

						foreach (Contact frDb in fromDB) {
							// Try to get the contact from cache.
							Contact contact = ContactFromCacheUsingDbContact (_appModel, frDb);
							retVal.Add (contact);
						}
					} catch (Exception e) {
						Debug.WriteLine ("issue with finding all non-temp contacts", e);
						onFailure ();
						return;
					}

					onCompletion (retVal);
				}
			});
		}

		public static void FindAllContactsWithServerIDsAsync(ApplicationModel _appModel, Action<IList<Contact>> onCompletion, bool excludeGroups, bool excludeTemp) {
			EMTask.Dispatch (() => {
				var retVal = new List<Contact> ();
				var groupedByContactID = new Dictionary<string,IList<Contact>>();
				lock (_appModel.daoConnection) {
					IList<Contact> fromDB = _appModel.contactDao.FindAllContactsWithServerIDs();

					// keep cache in sync
					foreach (Contact dbContact in fromDB) {
						Contact contact = ContactFromCacheUsingDbContact (_appModel, dbContact);

						if ( contact.serverContactID == null )
							retVal.Add(contact);
						else {
							IList<Contact> contacts = groupedByContactID.ContainsKey(contact.serverContactID) ? groupedByContactID[contact.serverContactID] : null;
							if ( contacts == null ) {
								contacts = new List<Contact>();
								groupedByContactID[contact.serverContactID] = contacts;
							}

							if (!contacts.Contains (contact)) {
								contacts.Add(contact);
							}
						}
					}

					// if a contact has preferred access, we remove the non preferred contacts.
					foreach ( IList<Contact> contacts in groupedByContactID.Values ) {
						bool hasPreferred = false;
						foreach ( Contact contact in contacts ) {
							if ( contact.preferred ) {
								hasPreferred = true;
								retVal.Add(contact);
							}
						}

						// if there's no preferred they all get added.
						if ( !hasPreferred )
							retVal.AddRange(contacts);
					}

					#region removing temp contacts from results
					if (excludeTemp) {
						var retValExcludingTemporaryContacts = new List<Contact> ();
						foreach (Contact contact in retVal) {
							if (!contact.tempContact.Value)
								retValExcludingTemporaryContacts.Add (contact);
						}

						retVal = retValExcludingTemporaryContacts;
					}

					#endregion

					if (excludeGroups) {
						var retValExcludingGroups = new List<Contact> ();
						foreach (Contact contact in retVal) {
							if (!contact.IsAGroup)
								retValExcludingGroups.Add (contact);
							retVal = retValExcludingGroups;
						}
					}

					retVal = retVal.OrderBy (c => c.displayName).ToList ();
				}

				EMTask.DispatchMain (() => onCompletion (retVal));
			});
		}

		public static void GetHeaderGroupsForContactList(IList<Contact> contactsList, Action<List<String>, List<int[]>> onCompletion) {

			var headerList = new List<String> ();
			var headerBoundaries = new List<int[]> ();
			var rgx = new Regex("[^a-zA-Z]");
			if (contactsList != null && contactsList.Count > 0) {

				int contactsListIndex = 0;
				for (char ch = 'A'; ch <= 'Z'; ch++) {
					String firstLetter = ch + "";
					em.Contact contact = contactsList [contactsListIndex];

					while (contact.displayName == null || contact.displayName != null & contact.displayName.Equals (" ")) {
						// skip those without a display name and skip those whose display names equal " "
						if (++contactsListIndex > contactsList.Count - 1)
							break;
						contact = contactsList [contactsListIndex];
					}

					if (contact.displayName != null && !contact.displayName.Equals (" ")) {
						string displayNameWithOnlyAlphabet = rgx.Replace (contact.displayName, string.Empty);;
						if (displayNameWithOnlyAlphabet.ToUpper().StartsWith (firstLetter)) {
							headerList.Add (firstLetter);
							var pos = new int[2];
							pos [0] = contactsListIndex;
							contactsListIndex++;
							while (contactsListIndex < contactsList.Count) {
								em.Contact nextContact = contactsList [contactsListIndex];
								if (nextContact.displayName != null) {
									displayNameWithOnlyAlphabet = rgx.Replace (nextContact.displayName, string.Empty);
									if (displayNameWithOnlyAlphabet.ToUpper ().StartsWith (firstLetter))
										contactsListIndex++;
									else // when the name doesn't match the letter anymore
										break;
								} else // when the name is null
									break;
							}
							pos [1] = contactsListIndex - 1;
							headerBoundaries.Add (pos);
							if (contactsListIndex == contactsList.Count) {
								break;
							}
						}

					}
						
				}
					
				if (contactsListIndex < contactsList.Count) {
					headerList.Add ("#");
					headerBoundaries.Add (new int[] { contactsListIndex, contactsList.Count - 1 });
				}
			}
				
			onCompletion (headerList, headerBoundaries);
		}

		public static void GetHeaderGroupsForRolledUpContactList (IList<AggregateContact> contactsList, Action<List<String>, List<int[]>> onCompletion) {
			var headerList = new List<String> ();
			var headerBoundaries = new List<int[]> ();
			var rgx = new Regex("[^a-zA-Z]");
			if (contactsList != null && contactsList.Count > 0) {
				int contactsListIndex = 0;
				for (char ch = 'A'; ch <= 'Z'; ch++) {
					String firstLetter = ch + "";
					AggregateContact contact = contactsList [contactsListIndex];

					while (contact.DisplayName == null || (contact.DisplayName != null && contact.DisplayName.Equals (" "))) {
						// skip those without a display name and skip those whose display names equal " "
						if (++contactsListIndex > contactsList.Count - 1)
							break;
						contact = contactsList [contactsListIndex];
					}
					if (contact.DisplayName != null && !contact.DisplayName.Equals (" ")) {
						string displayNameWithOnlyAlphabet = rgx.Replace (contact.DisplayName, string.Empty);
						if (displayNameWithOnlyAlphabet.ToUpper().StartsWith (firstLetter)) {
							headerList.Add (firstLetter);
							var pos = new int[2];
							pos [0] = contactsListIndex;
							contactsListIndex++;
							while (contactsListIndex < contactsList.Count) {
								AggregateContact nextContact = contactsList [contactsListIndex];
								if (nextContact.DisplayName != null) {
									displayNameWithOnlyAlphabet = rgx.Replace (nextContact.DisplayName, string.Empty);
									if (displayNameWithOnlyAlphabet.ToUpper ().StartsWith (firstLetter))
										contactsListIndex++;
									else // when the name doesn't match the letter anymore
										break;
								} else // when the name is null
									break;
							}
							pos [1] = contactsListIndex - 1;
							headerBoundaries.Add (pos);
							if (contactsListIndex == contactsList.Count) {
								break;
							}
						}
					}
				}
				if (contactsListIndex < contactsList.Count) {
					headerList.Add ("#");
					headerBoundaries.Add (new int[] { contactsListIndex, contactsList.Count - 1 });
				}
			}

			onCompletion (headerList, headerBoundaries);
		}

		public static Contact FindOrCreateContactAndUpdatePreferredIdentifierToSendFrom (ApplicationModel _appModel, ContactInput contactInput, string aliasToSendFrom) {
			lock (_appModel.daoConnection) {
				Contact cached = FindContactByServerID (_appModel, contactInput.serverID);
				bool needsSave = false;
				if (cached != null) {
					cached.appModel = _appModel;
					needsSave = Contact.UpdateFromContactInput (cached, contactInput);

					if (cached.LastUsedIdentifierToSendFrom != aliasToSendFrom) {
						needsSave = true;
						cached.LastUsedIdentifierToSendFrom = aliasToSendFrom;
					}

					if (needsSave) {
						cached.Save ();
					}

					return cached;
				} else {
					Contact newContact = Contact.FromContactInput (_appModel, contactInput);
					newContact.tempContact.Value = true;
					newContact.LastUsedIdentifierToSendFrom = aliasToSendFrom;
					newContact.Save ();
					return newContact;
				}
			}
		}

		public static Contact FindOrCreateContact(ApplicationModel _appModel, ContactInput contactInput) {
			lock (_appModel.daoConnection) {
				Contact cached = FindContactByServerID (_appModel, contactInput.serverID);
				if (cached != null) {
					cached.appModel = _appModel;
					if (Contact.UpdateFromContactInput (cached, contactInput))
						cached.Save ();
					return cached;
				} else {
					Contact newContact = Contact.FromContactInput (_appModel, contactInput);
					newContact.tempContact.Value = true;
					newContact.Save ();
					return newContact;
				}
			}
		}

		public static Contact FindOrCreateContactAfterSearch(ApplicationModel _appModel, ContactInput contactInput) {
			// Same as FindOrCreateContact but doesn't update from contact input. 
			// This is to avoid having the contactInput overwrite the cached copy of contact when the contact has its fromAliasServerId set already.
			// TODO: Remove this function and use the original after server sends down the correct contactinput.
			lock (_appModel.daoConnection) {
				Contact cached;
				if (cacheList[_appModel.cacheIndex].TryGetValue (contactInput.serverID, out cached)) {
					cached.appModel = _appModel;
					cached.Save ();
					return cached;
				}

				Contact newContact = Contact.FromContactInput (_appModel, contactInput);
				newContact.tempContact.Value = true;
				newContact.Save ();

				return newContact;
			}
		}

		public static void RemoveUnusedTemporaryContacts(ApplicationModel _appModel) {
			lock (_appModel.daoConnection) {
				IList<Contact> removed = _appModel.contactDao.RemoveUnusedTemporaryContacts ();

				foreach ( Contact c in removed ) {
					if ( c.serverID != null && cacheList[_appModel.cacheIndex].ContainsKey(c.serverID))
						cacheList [_appModel.cacheIndex].Remove (c.serverID);
				}
			}
		}

		public static void ProcessModifiedContactList(ApplicationModel appModel, ContactListModifiedInput contactListModifiedInput) {
			IList<ContactInput> contactInputs = contactListModifiedInput.contacts;

			foreach (ContactInput contactInput in contactInputs) {
				Contact contact = Contact.FindOrCreateContact(appModel, contactInput);
				if ( contactListModifiedInput.type.Equals("add")) {
					if(contact.IsAGroup) {
						if (UpdateFromContactInput (contact, contactInput, false, true))
							contact.Save ();
					} else {
						if ( contact.tempContact.Value ) {
							contact.tempContact.Value = false;
							contact.Save();
						}
					}
				}
				else if ( contactListModifiedInput.type.Equals("remove")) {
					if(contact.IsAGroup) {
						if (UpdateFromContactInput (contact, contactInput, false, true))
							contact.Save ();
					} else {
						if (!contact.tempContact.Value) {
							contact.tempContact.Value = true;
							contact.Save ();
						}
					}
				}
				else if ( contactListModifiedInput.type.Equals("update")) {
					if ( Contact.UpdateFromContactInput(contact, contactInput, true))
						contact.Save();
				}
			}
		}
	}
}