using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace em {
	public class Group : Contact {

		public bool isUserGroupOwner { get; set; }
		public bool canUserRejoinGroup { get; set; }

		public IList<Contact> members { get; set; }
		public IList<string> abandonedContacts { get; set; }

		public static Group CreateNew(ApplicationModel appModel) {
			var retVal = new Group ();
			retVal.appModel = appModel;
			retVal.isPersisted = false;
			retVal.isUserGroupOwner = true;
			retVal.canUserRejoinGroup = false;
			retVal.members = new List<Contact>();
			retVal.abandonedContacts = new List<string>();
			retVal.colorTheme = appModel.account.accountInfo.colorTheme;
			retVal.displayName = string.Empty;
			retVal.isGroup = "Y";
			retVal.media = null;
			retVal.thumbnailURL = null;

			return retVal;
		}

		public static void LoadGroupDetails(ApplicationModel appModel, Group g, Action<Group> completionHandler) {
			EMTask.Dispatch (() => {
				if(g != null) {
					appModel.account.LookupGroup (g.serverID, response => {
						if (response != null) {
							GroupInput grp = response;

							g.appModel = appModel;
							g.isUserGroupOwner = grp.requesterIsOwner;
							g.canUserRejoinGroup = !grp.requesterJoined;

							g.displayName = grp.displayName;
							g.isGroup = grp.group ? "Y" : "N";
							g.me = grp.me;
							g.fromAlias = grp.toAlias;

							g.thumbnailURL = grp.thumbnailURL;
							if(grp.thumbnailURL != null) {
								g.media = Media.FindOrCreateMedia (new Uri (grp.thumbnailURL));
								/*
								EMTask.DispatchMain (() => {
									retVal.media.DelegateDidCompleteDownload += DelegateDidDownloadThumbnail (this);
								});
								*/
							}

							var attrs = JToken.Parse (grp.attributes.ToString ());
							g.colorTheme = BackgroundColor.FromHexString ((string)attrs ["color"]);

							IList<string> ac = new List<string> (); 
							foreach (ContactInput member in grp.contacts) {
								GroupMemberStatus memberStatus = GroupMemberStatusHelper.FromString(member.memberStatus);
								if (memberStatus == GroupMemberStatus.Abandoned)
									ac.Add (member.serverID);
							}
							g.abandonedContacts = ac;

							IList<Contact> contactsInGroup = new List<Contact> ();
							foreach (ContactInput contactInput in grp.contacts) {
								Contact existing = Contact.FindContactByServerID (appModel, contactInput.serverID);

								//if existing contact is null, it is safe to assume this is a temporary contact.
								//Example: Nick creates a group with James & Nick's Mom. James has Nick in his address book, but not Nick's mom.
								//Therefore, Nick's Mom is a temporary contact in James' address book.
								if (existing == null) {
									if (ContactLifecycleHelper.EMCanSendTo(contactInput.lifecycle)) {
										Contact fromServer = Contact.FromContactInput (appModel, contactInput);
										fromServer.tempContact.Value = true;
										fromServer.Save ();
										existing = fromServer;
									}
								} else if (Contact.UpdateFromContactInput (existing, contactInput)) {
									existing.Save();
								}

								if(existing != null)
									contactsInGroup.Add (existing);
								else
									Debug.WriteLine("NULL contact, not adding member to Group! Contact Input: " + contactInput);
							}
							g.members = contactsInGroup;

							EMTask.DispatchMain (() => completionHandler (g));
						} else {
							Debug.WriteLine("NULL response when looking up group: " + g);
							EMTask.DispatchMain (() => completionHandler (null));
						}
					});
				}
			});
		}
	}
}