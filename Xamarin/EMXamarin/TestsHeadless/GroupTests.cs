using NUnit.Framework;
using System;
using System.Collections.Generic;
using EMXamarin;
using em;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Diagnostics;

namespace TestsHeadless
{
	[TestFixture ()]
	public class GroupTests : EMClientTests
	{

		private TestUser init(int userIndex) 
		{
			TestUser user = TestUserDB.GetUserAtIndex (userIndex);
			if (user.testManager.RegisterSync () && user.testManager.LoginSync ("11111") && 
				user.testManager.RegisterContactsFromAddressBookSync () == ContactsUpdatedStatus.RegisteredNewContacts)
				return user;
			return null;
		}
	
		private GroupUpdateOutbound newGroupUpdate(TestUser user)
		{
			var groupSave = new GroupUpdateOutbound ();
			groupSave.name = "Randomly Generate";
			groupSave.serverID = null;
			groupSave.members = new List<GroupMember> ();
			for (int i = 1; i <= user.listOfContacts.Count; i++) {
				var groupMember = new GroupMember ();
				Contact c = Contact.FindContactByAddressBookIDAndDescription (user.appModel, i.ToString (), ""); // empty string could break this test
				if (c == null)
					return null;
				groupMember.serverID = c.serverID;
				groupSave.members.Add (groupMember);
			}
			var json = new JObject ();
			json ["color"] = "gray";
			groupSave.attributes = json;
			return groupSave;
		}

		private GroupUpdateOutbound groupUpdateExistingRemoveMember (TestUser user, GroupInput groupInput, string serverIdToRemove) {
			var groupSave = new GroupUpdateOutbound ();
			groupSave.name = groupInput.displayName;
			groupSave.serverID = groupInput.serverID;
			groupSave.attributes = groupInput.attributes;

			groupSave.members = new List<GroupMember> ();
			foreach (ContactInput contactInput in groupInput.contacts) {
				if (!contactInput.serverID.Equals (serverIdToRemove)) {
					GroupMember member = new GroupMember ();
					member.serverID = contactInput.serverID;
					groupSave.members.Add (member);
				}
			}

			return groupSave;
		}

		[Test ()]
		public void CreateGroup ()
		{
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			TestUser user = init (4);
			int groupCount = 0;
			int updatedGroupCount = 0;
			if (user != null) {
				user.testManager.FindAllGroupsSync ((IList<Group> groupsList) => {
					groupCount = groupsList.Count;
				});
				var groupSave = newGroupUpdate (user);
				if (groupSave == null)
					Assert.Fail ("Failed to find a contact by address book id ");
				user.testManager.SaveGroupSync (groupSave, (group) => {
				});

				user.testManager.FindAllGroupsSync ((IList<Group> groupsList) => {
					updatedGroupCount = groupsList.Count;
				});
				Assert.AreEqual (groupCount + 1, updatedGroupCount);
			} else
				Assert.Fail ("Failed to successfully register, log in or load contacts for users");
		}

		[Test ()]
		public void EditGroup ()
		{
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			TestUser user = init (4);
			if (user != null) {
				var groupSave = newGroupUpdate (user);
				if (groupSave == null)
					Assert.Fail ("Failed to find a contact by address book id ");

				bool saveTimeout = true;
				user.testManager.SaveGroupSync (groupSave, (group) => {
					saveTimeout = false;
				});
				if (saveTimeout)
					Assert.Fail ("Attempt to save group timed out.");

				string groupId = null;
				user.testManager.FindAllGroupsSync ((IList<Group> groupsList) => {
					if (groupsList.Count > 0)
						groupId = groupsList [groupsList.Count - 1].serverID;
				});
				if (groupId == null)
					Assert.Fail ("Failed to save group");

				String serverIDtoRemove = null;
				Contact lastContactInAddressBook = Contact.FindContactByAddressBookIDAndDescription (user.appModel, user.listOfContacts.Count.ToString (), ""); // passing in empty string likely breaks this test
				if (lastContactInAddressBook != null)
					serverIDtoRemove = lastContactInAddressBook.serverID;
				else
					Assert.Fail ("Last contact in address book could not be found; contacts not properly registered.");

				EventWaitHandle waitHandle = new AutoResetEvent (false);
				bool wasRemoved = false;

				user.testManager.LookupGroupSync (groupId, (GroupInput grp) => {
					if (grp != null) {
						GroupUpdateOutbound groupUpdate = groupUpdateExistingRemoveMember(user, grp, serverIDtoRemove);

						user.appModel.account.UpdateGroup(groupUpdate, null, (group) => {
							user.testManager.LookupGroupSync(groupId, (GroupInput updated) => {
								if (updated != null) {
									wasRemoved = true;
									foreach (GroupContactInput contact in updated.contacts) {
										if (contact.serverID.Equals(serverIDtoRemove)) {
											wasRemoved = false;
											break;
										}
									}
								}
								waitHandle.Set();
							});
						});
					} else 
						Assert.Fail ("Error: Group found via FindAllGroups call, but could not be found by LookupGroup call");
				});
				waitHandle.WaitOne ();
				Assert.IsTrue (wasRemoved);
			} else
				Assert.Fail ("Failed to successfully register, log in or load contacts for users");
		}

		[Test ()]
		public void DeleteGroup ()
		{
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			TestUser user = init (4);
			if (user != null) {
				var groupSave = newGroupUpdate (user);
				if (groupSave == null)
					Assert.Fail ("Failed to find a contact by address book id ");

				bool saveTimeout = true;
				user.testManager.SaveGroupSync (groupSave, (group) => {
					saveTimeout = false;
				});
				if (saveTimeout)
					Assert.Fail ("Attempt to save group timed out.");

				string groupId = null;
				user.testManager.FindAllGroupsSync ((IList<Group> groupsList) => {
					if (groupsList.Count > 0)
						groupId = groupsList [groupsList.Count - 1].serverID;
				});
				if (groupId == null)
					Assert.Fail ("Failed to save group");

				EventWaitHandle waitHandle = new AutoResetEvent (false);
				user.appModel.account.DeleteGroup (groupId);
				waitHandle.WaitOne (10000);

				GroupLifecycle deletedGroupLifecycle = GroupLifecycle.Active;
				user.testManager.LookupGroupSync (groupId, (GroupInput grp) => {
					if (grp != null)
						deletedGroupLifecycle = grp.lifecycle;
					else 
						Assert.Fail ("Error: Group found via FindAllGroups call, but could not be found by LookupGroup call");
				});

				Assert.AreEqual (GroupLifecycle.Deleted, deletedGroupLifecycle);
			} else
				Assert.Fail ("Failed to successfully register, log in or load contacts for users");
		}

		[Test ()]
		public void LeaveGroup ()
		{
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			TestUser groupOwner = init (4);
			TestUser groupMember = init (5);

			if (groupOwner != null && groupMember != null) {

				GroupMemberStatus groupMemberStatus = GroupMemberStatus.Joined;

				//creates the groupInfo 
				var groupSave = newGroupUpdate (groupOwner);
				if (groupSave == null)
					Assert.Fail ("Failed to find a contact by address book id ");

				//saves the group
				bool saveTimeout = true;
				groupOwner.testManager.SaveGroupSync (groupSave, (group) => {
					saveTimeout = false;
				});
				if (saveTimeout)
					Assert.Fail ("Attempt to save group timed out.");

				Contact memberContact = Contact.FindContactByAddressBookIDAndDescription (groupOwner.appModel, "2", ""); // Passing in empty string likely breaks this test
				if (memberContact == null)
					Assert.Fail ("Failed to find contact by address book ID of known registered user");

				String ownerToMemberID = memberContact.serverID;
				string[] contactIDs = ownerToMemberID.Split(new char[]{':'});
				if (contactIDs.Length != 2)
					Assert.Fail ("Error: Server ID for contact in different format than expected");
				string ownerId = contactIDs [0];
				string memberId = contactIDs [1];

				// get the groupIDs
				string groupId = null;
				groupOwner.testManager.FindAllGroupsSync ((IList<Group> groupsList) => {
					groupId = groupsList.Count.ToString();
				});
				if (groupId.Equals("0"))
					Assert.Fail ("Error: no groups found via FindAllGroupsSync call even though groups have been created");
				string memberToGroupID = "G:" + memberId + ":" + groupId;
				string ownerToGroupID = "G:" + ownerId + ":" + groupId;

				//make the group member leave the group
				EventWaitHandle waitHandle = new AutoResetEvent (false);
				groupMember.appModel.account.LeaveGroup (memberToGroupID);
				waitHandle.WaitOne (10000);
					
				//check to see that the member has left
				groupOwner.testManager.LookupGroupSync (ownerToGroupID, (GroupInput grp) => {
					if (grp != null) {
						foreach (GroupContactInput member in grp.contacts)
							if (member.serverID.Equals(ownerToMemberID)) {
								groupMemberStatus = member.memberStatus;
								break;
							}
					} else 
						Assert.Fail ("Error: Group found via FindAllGroups call, but could not be found by LookupGroup call");
				});

				Assert.AreEqual (GroupMemberStatus.Abandoned, groupMemberStatus);
			} else
				Assert.Fail ("Failed to successfully register, log in or load contacts for users");
		}

		[Test ()]
		public void RejoinGroup ()
		{
			SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
			TestUser groupOwner = init (4);
			TestUser groupMember = init (5);

			if (groupOwner != null && groupMember != null) {

				GroupMemberStatus memberStatus = GroupMemberStatus.Abandoned;

				//creates the groupInfo 
				var groupSave = newGroupUpdate (groupOwner);
				if (groupSave == null)
					Assert.Fail ("Failed to find a contact by address book id ");

				//saves the group
				bool saveTimeout = true;
				groupOwner.testManager.SaveGroupSync (groupSave, (group) => {
					saveTimeout = false;
				});
				if (saveTimeout)
					Assert.Fail ("Attempt to save group timed out.");

				Contact memberContact = Contact.FindContactByAddressBookIDAndDescription (groupOwner.appModel, "2", ""); // Passing in empty string likely breaks this test
				if (memberContact == null)
					Assert.Fail ("Failed to find contact by address book ID of known registered user");

				String ownerToMemberID = memberContact.serverID;
				string[] contactIDs = ownerToMemberID.Split(new char[]{':'});
				if (contactIDs.Length != 2)
					Assert.Fail ("Error: Server ID for contact in different format than expected");
				string ownerId = contactIDs [0];
				string memberId = contactIDs [1];

				// get the groupIDs
				string groupId = null;
				groupOwner.testManager.FindAllGroupsSync ((IList<Group> groupsList) => {
					groupId = groupsList.Count.ToString();
				});
				if (groupId.Equals("0"))
					Assert.Fail ("Error: no groups found via FindAllGroupsSync call even though groups have been created");
				string memberToGroupID = "G:" + memberId + ":" + groupId;
				string ownerToGroupID = "G:" + ownerId + ":" + groupId;

				//make the group member leave the group
				EventWaitHandle waitHandle = new AutoResetEvent (false);
				groupMember.appModel.account.LeaveGroup (memberToGroupID);
				waitHandle.WaitOne (10000);

				bool memberHasLeftGroup = false;
				groupOwner.testManager.LookupGroupSync (ownerToGroupID, (GroupInput grp) => {
					if (grp != null) {
						foreach (GroupContactInput member in grp.contacts)
							if (member.serverID.Equals(ownerToMemberID)) {
								if (member.memberStatus == GroupMemberStatus.Abandoned)
									memberHasLeftGroup = true;
								break;
							}
					} 
				});
				if (!memberHasLeftGroup)
					Assert.Fail ("Failed to leave group");

				groupMember.appModel.account.RejoinGroup (memberToGroupID);
				waitHandle.WaitOne (10000);

				groupOwner.testManager.LookupGroupSync (ownerToGroupID, (GroupInput grp) => {
					if (grp != null) {
						foreach (GroupContactInput member in grp.contacts)
							if (member.serverID.Equals(ownerToMemberID)) {
								memberStatus = member.memberStatus;
								break;
							}
					} else 
						Assert.Fail ("Error: Group found via FindAllGroups call, but could not be found by LookupGroup call");
				});
	
				Assert.AreEqual (GroupMemberStatus.Joined, memberStatus);
			} else
				Assert.Fail ("Failed to successfully register, log in or load contacts for users");
		}
	}
}