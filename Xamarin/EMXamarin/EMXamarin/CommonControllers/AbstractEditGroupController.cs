using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace em {
	public abstract class AbstractEditGroupController {

		public static int GROUP_OWNER_INDEX = 0;

		readonly ApplicationModel appModel;

		string fA; // fromAlias
		public string FromAlias { 
			get  { return fA; } 
			set { fA = value; }
		}

		public Group Group { get; set; }
		public bool IsNewGroup { get; private set; }

		// If the user made any edits on a new alias, we'd ask them if they wanted to confirm the exit.
		// This is a flag tracking their choice.
		public bool UserChoseToLeaveUponBeingAsked { get; set; }
		public BackgroundColor OriginalColorTheme { get; set; }
		public BackgroundColor ColorTheme { get; set; }

		public bool Changed { get; set; }

		public abstract string TextInDisplayField { get; }

		public bool HasSuitableNumberOfMembers {
			get {
				return this.MemberCount >= 2;
			}
		}

		public int MemberCount {
			get {
				int memberCount = 0;
				if (this.Group != null) {
					memberCount = this.Group.members.Count;
				}

				if (this.IsNewGroup) {
					memberCount++;
				}

				return memberCount;
			}
		}

		public JToken Properties {
			set {
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
					this.ColorTheme = this.OriginalColorTheme = BackgroundColor.FromHexString ((string)(Group.attributes ["color"]));
				}
			}
		}

		public string ResponseDestination { set; get; }

		protected AbstractEditGroupController (ApplicationModel applicationModel, Group g) {
			appModel = applicationModel;

			Changed = false | g == null;

			if (g == null) {
				this.Group = Group.CreateNew (applicationModel);
				this.IsNewGroup = true;
			} else {
				this.IsNewGroup = false;
				this.Group = g;
			}

			// Note: The null check here is for g not Group.
			if(g != null) {
				// Existing group
				Group.LoadGroupDetails (applicationModel, g, response => {
					if (response != null) {
						Group = response;
						AddDownloadCallbacks ();

						if ( response.fromAlias != null )
							FromAlias = response.fromAlias;

						DidLoadGroup();
					} else {
						Debug.WriteLine("Failed to load group: " + g);
						DidLoadGroupFailed();
					}
				});
			} else {
				// New group
				AddDownloadCallbacks ();
			}
		}

		public void Dispose() {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
		}

		private bool EditsWereMade {
			get {
				if (this.Group.media != null) {
					return true;
				}

				if (this.ColorTheme != this.OriginalColorTheme) {
					return true;
				}

				if (!string.IsNullOrWhiteSpace (this.TextInDisplayField)) {
					return true;
				}

				if (this.Group.members.Count > 0) {
					return true;
				}

				return false;
			}
		}

		public bool ShouldStopUserFromExiting {
			get {
				if (this.UserChoseToLeaveUponBeingAsked) {
					return false;
				}

				if (this.IsNewGroup && this.EditsWereMade) {
					return true;
				}

				return false;
			}
		}

		#region notifications
		private void AddDownloadCallbacks () {
			// Download callbacks for the group.
			NotificationCenter.DefaultCenter.AddWeakObserver (this.Group, Constants.Counterparty_DownloadFailed, BackgroundCounterpartyDownloadFailed);
			NotificationCenter.DefaultCenter.AddWeakObserver (this.Group, Constants.Counterparty_DownloadCompleted, BackgroundCounterpartyDownloadCompleted);
			NotificationCenter.DefaultCenter.AddWeakObserver (this.Group, Constants.Counterparty_ThumbnailChanged, BackgroundCounterpartyThumbnailChanged);

			// Download callbacks for members of the group.
			IList<Contact> listOfMembers = this.Group.members; // Can be zero count.
			NotificationCenter.DefaultCenter.AddWeakObservers<Contact> (listOfMembers, Constants.Counterparty_DownloadFailed, BackgroundCounterpartyDownloadFailed);
			NotificationCenter.DefaultCenter.AddWeakObservers<Contact> (listOfMembers, Constants.Counterparty_DownloadCompleted, BackgroundCounterpartyDownloadCompleted);
			NotificationCenter.DefaultCenter.AddWeakObservers<Contact> (listOfMembers, Constants.Counterparty_ThumbnailChanged, BackgroundCounterpartyThumbnailChanged);
		}

		// TODO: This was a quick way to get the event to propgate to the UI. We could use more granularity here.
		protected void BackgroundCounterpartyDownloadCompleted (Notification n) {
			ContactDidChangeThumbnail ();
		}

		protected void BackgroundCounterpartyThumbnailChanged (Notification n) {
			ContactDidChangeThumbnail ();
		}

		protected void BackgroundCounterpartyDownloadFailed (Notification n) {
			ContactDidChangeThumbnail ();
		}
		#endregion

		#region picking from alias
		public abstract void UpdateAliasText (string text);
		public int CurrentRowForFromAliasPicker () {
			IList<AliasInfo> aliases = this.appModel.account.accountInfo.aliases;
			string currentFromAlias = this.FromAlias;
			int currentRow = 0;
			if (currentFromAlias == null)
				currentRow = aliases.Count;
			else {
				foreach (AliasInfo aI in aliases) {
					if (aI.Equals (currentFromAlias))
						currentRow = aliases.IndexOf (aI);
				}
			}
			return currentRow;
		}

		public void UpdateFromAlias (AliasInfo aliasInfo) {
			FromAlias = aliasInfo == null ? null : aliasInfo.serverID;
			EMTask.DispatchMain (() => {
				if (FromAlias == null) {
					UpdateAliasText (appModel.account.accountInfo.defaultName);
				} else {
					if (aliasInfo != null)
						UpdateAliasText (aliasInfo.displayName);
				}
			});
		}
		#endregion

		protected abstract void ContactDidChangeThumbnail ();
		protected abstract void DidLoadGroup ();
		protected abstract void DidLoadGroupFailed ();

		protected abstract void DidSaveGroup ();
		protected abstract void DidSaveGroupFailed ();
		protected abstract void DidSaveOrUpdateGroupFailed ();
		protected abstract void DidUpdateGroup ();
		protected abstract void DidUpdateGroupFailed ();

		protected abstract void DidLeaveOrRejoinGroup ();
		protected abstract void DidLeaveGroupFailed ();
		protected abstract void DidRejoinGroupFailed ();
		public abstract void ListOfMembersUpdated ();

		public abstract void DidChangeColorTheme ();
		public abstract void TransitionToChatController (ChatEntry chatEntry);

		protected void DidChangeColorTheme(CounterParty accountInfo) {
			DidChangeColorTheme ();
		}

		public IList<Contact> ManageableListOfContacts {
			get {
				Group group = this.Group;
				if (group == null) {
					return new List<Contact> ();
				}

				IList<Contact> members = new List<Contact> (group.members); // do a copy here so we can modify the new list

				if (!this.IsNewGroup) {
					members.RemoveAt (GROUP_OWNER_INDEX);
				}

				return members;
			}
		}

		/**
		 * Helper routine indicating that the user
		 * has made changes to the group's member list.
		 * Handles group owner management along with the rest of the members.
		 */
		public void ManageContactsAfterAddressBookResult (AddressBookSelectionResult result) {
			IList<Contact> newContacts = result.Contacts;

			IList<Contact> oldContactList = this.Group.members;

			Contact ownerContact = null;

			// If not a new group, make sure we remove the owner contact out since we didn't include him in the list of contacts we passed to the Address book controllers.
			if (!this.IsNewGroup) {
				ownerContact = oldContactList [GROUP_OWNER_INDEX];
				oldContactList.Remove (ownerContact);
			}

			// Check if our contacts have changed after returning from address book. 
			this.Changed = ! (new HashSet<Contact> (oldContactList).SetEquals (newContacts));

			// If contacts have changed, update.
			if (Changed) {
				this.Group.members = newContacts;
			}

			// Readd the owner contact back in.
			if (ownerContact != null) {
				this.Group.members.Insert (GROUP_OWNER_INDEX, ownerContact);
			}

			ListOfMembersUpdated ();
		}

		public void AddContact(Contact contact) {
			if (Group.members == null)
				Group.members = new List<Contact> ();

			if (!Group.members.Contains (contact)) {
				Group.members.Add (contact);
				Changed = true;
			}
		}

		public void RemoveContact(Contact contact) {
			Group.members.Remove (contact);
			Changed = true;
		}
			
		/*
		 * Pass in save param as true to save, false to update
		 */
		public void SaveOrUpdateAsync(string groupName, BackgroundColor colorTheme, byte[] thumbnail, bool save) {
			if (ShouldSaveOrUpdateGroup (groupName, colorTheme, thumbnail)) {
				EMTask.DispatchBackground (() => {
					try {
						var groupSave = new GroupUpdateOutbound ();
						groupSave.name = groupName;
						//groupSave.fromAlias = FromAlias;
						groupSave.serverID = Group.serverID;
						IList<GroupMember> updatedMembers = new List<GroupMember> ();
						foreach (Contact member in Group.members) {
							var groupMember = new GroupMember ();
							groupMember.serverID = member.serverID;
							updatedMembers.Add (groupMember);
						}
						groupSave.members = updatedMembers;

						var json = new JObject ();
						json ["color"] = colorTheme.ToHexString ();
						groupSave.attributes = json;

						if (save) {
							appModel.account.SaveGroup (groupSave, thumbnail, group => {
								if(group != null) {
									//set server id so we can lookup group from the server
									Group.serverID = group.serverID;

									//if a thumbnail exists, move thumbnail from staging path for unregistered groups, to staging path for registered groups (with server id)
									if(thumbnail != null && thumbnail.Length > 0)
										MoveThumbnailFromLocalGroupStagingToServerGroupStaging();

									EMTask.DispatchMain (() => Contact.DelegateDidAddGroup (group));

									//load group from server to get updated member list
									Group.LoadGroupDetails (appModel, Group, response => {
										if (response != null) {
											this.IsNewGroup = false;
											Group = response;
											if (response.fromAlias != null)
												FromAlias = response.fromAlias;

											//if group media exists, move to cached path
											MoveThumbnailFromGroupStagingPathToMediaPath();

											//call abstract method to signal UI to update
											DidSaveGroup ();
										} else {
											Debug.WriteLine ("Failed to load group after save!");
											DidSaveGroupFailed ();
										}
									});

									appModel.RecordGAGoal(Preference.GA_CREATED_GROUP, AnalyticsConstants.CATEGORY_GA_GOAL, 
										AnalyticsConstants.ACTION_CREATE_GROUP, AnalyticsConstants.CREATED_GROUP, AnalyticsConstants.VALUE_CREATE_GROUP);
								} else {
									Debug.WriteLine("Error saving group! NULL response!");
									DidSaveGroupFailed ();
								}
							});
						} else {
							appModel.account.UpdateGroup (groupSave, thumbnail, group => {
								if (group != null) {
									Group.serverID = group.serverID;

									//if group media exists, move to cached path
									MoveThumbnailFromGroupStagingPathToMediaPath();

									EMTask.DispatchMain (() => Contact.DelegateDidUpdateGroup (group));

									DidUpdateGroup ();
								} else {
									Debug.WriteLine ("Failed to update group!");
									DidUpdateGroupFailed ();
								}
							});
						}
					} catch (Exception e) {
						Debug.WriteLine (string.Format ("Failed to Save or Update Group: {0}\n{1}", e.Message, e.StackTrace));
						DidSaveOrUpdateGroupFailed ();
					}
				});
			} else
				DidUpdateGroup (); //nothing to update, call abstract method to handle UI
		}

		public void DeleteAsync(string serverID) {
			EMTask.DispatchBackground (() => {
				try {
					appModel.account.DeleteGroup (serverID, success => {
						if(success) {
							Contact group = Contact.FindContactByServerID(appModel, serverID);
							group.tempContact.Value = true;
							group.Save();

							EMTask.DispatchMain(() => Contact.DelegateDidDeleteGroup (group));
						} else {
							Debug.WriteLine ("Failed to Delete Group");
							DidSaveOrUpdateGroupFailed ();
						}
					});
				} catch (Exception e) {
					Debug.WriteLine(string.Format("Failed to Delete Group: {0}\n{1}", e.Message, e.StackTrace));
					DidSaveOrUpdateGroupFailed ();
				}
			});
		}

		public void LeaveAsync(string groupID) {
			EMTask.DispatchBackground (() => {
				try {
					appModel.account.LeaveGroup (groupID, success => {
						if(success)
							DidLeaveOrRejoinGroup();
						else {
							Debug.WriteLine ("Failed to Leave Group");
							DidLeaveGroupFailed ();
						}
					});
				} catch (Exception e) {
					Debug.WriteLine(string.Format("Failed to Leave Group: {0}\n{1}", e.Message, e.StackTrace));
					DidLeaveGroupFailed();
				}
			});
		}

		public void RejoinAsync(string groupID) {
			EMTask.DispatchBackground (() => {
				try {
					appModel.account.RejoinGroup (groupID, success => {
						if(success)
							DidLeaveOrRejoinGroup();
						else {
							Debug.WriteLine ("Failed to Rejoin Group");
							DidRejoinGroupFailed ();
						}
					});
				} catch (Exception e) {
					Debug.WriteLine(string.Format("Failed to Rejoin Group: {0}\n{1}", e.Message, e.StackTrace));
					DidRejoinGroupFailed();
				}
			});
		}

		bool ShouldSaveOrUpdateGroup(string groupName, BackgroundColor colorTheme, byte[] thumbnail) {
			if (Changed)
				return true;

			bool same = true;

			if (Group.displayName != groupName)
				same = false;

			if (Group.colorTheme != colorTheme)
				same = false;

			if (thumbnail != null && thumbnail.Length > 0)
				same = false;

			return !same;
		}

		private bool UseStagingPath (Group group) {
			if (Group != null && Group.serverID != null) {
				return false;
			}

			return true;
		}

		public string GetStagingFilePathForGroupThumbnail () {
			if (UseStagingPath (Group)) {
				return appModel.uriGenerator.GetStagingPathForGroupThumbnailLocal ();
			}

			return appModel.uriGenerator.GetStagingPathForGroupThumbnailServer (Group);
		}

		public void MoveThumbnailFromLocalGroupStagingToServerGroupStaging () {
			string oldThumbnailPath = appModel.uriGenerator.GetStagingPathForGroupThumbnailLocal ();
			string newThumbnailPath = appModel.uriGenerator.GetStagingPathForGroupThumbnailServer (Group);

			appModel.platformFactory.GetFileSystemManager ().MoveFileAtPath (oldThumbnailPath, newThumbnailPath);
			appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (oldThumbnailPath);
			Group.UpdateThumbnailUrlAfterMovingFromCache (newThumbnailPath);
		}

		public void MoveThumbnailFromGroupStagingPathToMediaPath () {
			//if group media exists, move to cached path
			if(Group.media != null) {
				string thumbnailKnownPath = appModel.uriGenerator.GetStagingPathForGroupThumbnailServer (Group);
				if(appModel.platformFactory.GetFileSystemManager ().FileExistsAtPath(thumbnailKnownPath)) {
					string thumbnailCachedFilePath = appModel.uriGenerator.GetCachedFilePathForUri (Group.media.uri);

					if (appModel.platformFactory.GetFileSystemManager ().FileExistsAtPath (thumbnailCachedFilePath))
						appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (thumbnailCachedFilePath);

					appModel.platformFactory.GetFileSystemManager ().MoveFileAtPath (thumbnailKnownPath, thumbnailCachedFilePath);
					Group.UpdateThumbnailUrlAfterMovingFromCache (thumbnailCachedFilePath);
				}
			}
		}

		public void SendMessageToGroup () {
			EMTask.DispatchBackground (() => {
				Group group = appModel.contactDao.FindGroupByServerID (this.Group.serverID); // querying for group object backed by dao
				ChatEntry ce = appModel.chatList.FindChatEntryByReplyToServerIDs (new List<string> () { group.serverID }, group.fromAlias);

				if (ce == null) {
					ce = ChatEntry.NewUnderConstructionChatEntry (appModel, DateTime.Now.ToEMStandardTime(appModel));
					appModel.chatList.underConstruction = ce;
					ce.contacts = new List<Contact> () { group };
				}

				TransitionToChatController (ce);

			});
		}

		private string FindFromAliasFromGroup () {
			string fromAlias = null;
			if (this.Group != null) {
				Group group = appModel.contactDao.FindGroupByServerID (this.Group.serverID); // querying for group object backed by dao
				if (group != null) {
					fromAlias = group.fromAlias;
				}
			}

			return fromAlias;
		}

		public void GoToNewOrExistingChatEntry (Contact contact) {
			// Get the group so we can pull the fromAlias out.
			string fromAlias = FindFromAliasFromGroup ();

			ChatList chatList = this.appModel.chatList;
			ChatEntry ce = chatList.FindChatEntryByReplyToServerIDs (new List<string> { contact.serverID }, fromAlias);
			if ( ce == null ) {
				ce = ChatEntry.NewUnderConstructionChatEntry (this.appModel, DateTime.Now.ToEMStandardTime (this.appModel));
				chatList.underConstruction = ce;
				ce.contacts = new List<Contact> { contact };
			}

			TransitionToChatController (ce);
		}
	}
}