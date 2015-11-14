using System;
using System.IO;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidHUD;
using em;
using System.Collections.Generic;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Support.V4.View;
using System.Threading;

namespace Emdroid {

	public class EditGroupFragment : BasicAccountFragment {

		private HiddenReference<ApplicationModel> _appModel;
		private ApplicationModel appModel {
			get { return this._appModel != null ? this._appModel.Value : null; }
			set { this._appModel = new HiddenReference<ApplicationModel> (value); }
		}

		private HiddenReference<SharedEditGroupController> _shared;
		private SharedEditGroupController sharedEditGroupController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedEditGroupController> (value); }
		}

		public AbstractEditGroupController SharedController { get { return sharedEditGroupController; } }

		EditGroupListAdapter editGroupListAdapter;

		public bool EditMode, ViewLoaded, ShouldNotReloadUI;
		byte[] updatedThumbnail;
		BackgroundColor colorTheme;

		public Group Group { get { return sharedEditGroupController.Group; } set { sharedEditGroupController.Group = value; } }

		#region UI
		View v;
		Button leftBarDoneButton, sendMessageButton, deleteGroupButton;
		ImageView addMembersButton;
		protected EditText GroupNameEditText { get; set; }
		TextView headingTitle, addMembersLabel, groupNameLabel, members;
		RecyclerView listView;
		#endregion

		#region picking from alias
		Spinner spinner;
		bool spinnerShouldActivate; // This is needed because android's spinner calls its ItemSelected delegate when it's being layed out...
		protected Spinner FromAliasSpinner {
			get { return spinner; }
			set { spinner = value; }
		}

		AliasPickerAdapter aliasPickerAdapter;

		EditText fromAliasTextField;
		protected EditText FromAliasTextField { 
			get { return fromAliasTextField; }
			set { fromAliasTextField = value; }
		}
		#endregion

		public static EditGroupFragment NewInstance (bool edit, Group g) {
			var f = new EditGroupFragment ();
			f.EditMode = edit;
			f.colorTheme = g != null ? g.colorTheme : f.appModel.account.accountInfo.colorTheme;
			f.ShouldNotReloadUI |= g != null;
			f.sharedEditGroupController = new SharedEditGroupController (f, f.appModel, g);
			return f;
		}

		public EditGroupFragment() {
			appModel = EMApplication.GetInstance ().appModel;
			editGroupListAdapter = new EditGroupListAdapter(this);
			editGroupListAdapter.ItemClick += DidTapMember;
			PendingState = true;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			spinnerShouldActivate = false;
			v = inflater.Inflate(Resource.Layout.group_edit, container, false);
			colorTheme.GetBackgroundResource ((string file) => {
				if (v != null && this.Resources != null) {
					BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
				}
			});
			FromAliasTextField = v.FindViewById <EditText> (Resource.Id.fromAliasTextField);
			FromAliasTextField.Visibility = ViewStates.Gone;
			FromAliasSpinner = v.FindViewById <Spinner> (Resource.Id.FromAliasSpinner);
			FromAliasSpinner.Visibility = ViewStates.Gone;

			ProgressBar = v.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
			LeftBarButton = v.FindViewById<Button> (Resource.Id.LeftBarButton);
			LeftBarButton.Typeface = FontHelper.DefaultFont;

			RightBarButton = v.FindViewById<Button> (Resource.Id.RightBarButton);
			RightBarButton.Typeface = FontHelper.DefaultFont;

			ThumbnailBackgroundView = v.FindViewById<ImageView> (Resource.Id.GroupThumbnailBackgroundView);
			ThumbnailButton = v.FindViewById<ImageView> (Resource.Id.GroupThumbnailButton);

			#region color spinner
			ColorThemeSpinner = v.FindViewById<Spinner> (Resource.Id.ColorThemeSpinner);
			ColorThemeButton = v.FindViewById<ImageView> (Resource.Id.ColorThemeButton);
			ColorThemeText = v.FindViewById<TextView> (Resource.Id.ColorThemeLabel);
			#endregion

			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			EMTask.DispatchMain (() => AndHUD.Shared.Show (Activity, "LOADING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null));

			#region from alias
			FromAliasTextField.Click += (sender, e) => FromAliasSpinner.PerformClick ();

			aliasPickerAdapter = new AliasPickerAdapter (Activity, Android.Resource.Layout.SimpleListItem1, EMApplication.Instance.appModel.account.accountInfo.aliases);
			FromAliasSpinner.Adapter = aliasPickerAdapter;
			FromAliasSpinner.ItemSelected += (sender, e) => {
				if (!spinnerShouldActivate) {
					spinnerShouldActivate = true;
					return;
				}

				AliasInfo aliasInfo = aliasPickerAdapter.ResultFromPosition (e.Position);
				sharedEditGroupController.UpdateFromAlias (aliasInfo);
			};
			FromAliasSpinner.SetSelection (sharedEditGroupController.CurrentRowForFromAliasPicker ());
			#endregion

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			headingTitle = View.FindViewById<TextView> (Resource.Id.starterLabel);
			headingTitle.Typeface = FontHelper.DefaultFont;

			leftBarDoneButton = View.FindViewById<Button> (Resource.Id.LeftBarDoneButton);
			leftBarDoneButton.Click += DidTapSaveButton;
			leftBarDoneButton.Typeface = FontHelper.DefaultFont;

			#region dynamic text or label
			GroupNameEditText = View.FindViewById<EditText> (Resource.Id.GroupNameText);
			GroupNameEditText.Typeface = FontHelper.DefaultFont;
			GroupNameEditText.EditorAction += HandleDoneAction;
			GroupNameEditText.FocusChange += (sender, e) => {
				if (!e.HasFocus) {
					KeyboardUtil.HideKeyboard (this.View); //dismiss keyboard when clicking outside
				}
			};

			groupNameLabel = View.FindViewById<TextView>(Resource.Id.GroupNameLabel);
			groupNameLabel.Typeface = FontHelper.DefaultFont;
			#endregion

			#region add members
			addMembersButton = View.FindViewById<ImageView> (Resource.Id.AddContactButton);
			addMembersButton.Click += (sender, e) => {
				ShouldNotReloadUI = false;
				AddressBookArgs args = AddressBookArgs.From (true, false, false, this.sharedEditGroupController.ManageableListOfContacts, null);
				AddressBookFragment fragment = AddressBookFragment.NewInstance (args);

				fragment.CompletionCallback += result => {
					this.sharedEditGroupController.ManageContactsAfterAddressBookResult (result);
				};

				Activity.FragmentManager.BeginTransaction ()
					.SetTransition (FragmentTransit.FragmentOpen)
					.Replace (Resource.Id.content_frame, fragment)
					.AddToBackStack (null)
					.Commit ();
			};

			addMembersLabel = View.FindViewById<TextView> (Resource.Id.AddContactLabel);
			#endregion

			sendMessageButton = View.FindViewById<Button> (Resource.Id.SendMessageButton);
			colorTheme.GetButtonResource ((Drawable drawable) => {
				sendMessageButton.SetBackgroundDrawable (drawable);
			});
			sendMessageButton.Typeface = FontHelper.DefaultFont;
			sendMessageButton.Click += DidTapSendMessageButton;

			#region delete, leave or rejoin
			deleteGroupButton = View.FindViewById<Button> (Resource.Id.DeleteButton);
			colorTheme.GetButtonResource ((Drawable drawable) => {
				deleteGroupButton.SetBackgroundDrawable (drawable);
			});
			deleteGroupButton.Typeface = FontHelper.DefaultFont;
			#endregion

			#region members
			members = View.FindViewById<TextView> (Resource.Id.MemberLabel);
			members.Typeface = FontHelper.DefaultFont;
			#endregion

			listView = View.FindViewById<RecyclerView>(Resource.Id.GroupMembersListView);

			LinearLayoutManager mLayoutManager = new LinearLayoutManager (this.Activity);
			listView.SetLayoutManager (mLayoutManager);
			listView.AddItemDecoration (new SimpleDividerItemDecoration (this.Activity, useFullWidthLine: true, shouldDrawBottomLine: false));

			EmItemTouchHelper g = new EmItemTouchHelper (this.editGroupListAdapter, 0, ItemTouchHelper.Left | ItemTouchHelper.Right);
			ItemTouchHelper hel = new ItemTouchHelper (g);

			hel.AttachToRecyclerView (listView);

			//hide all elements here and they will be shown (if necessary) in FinalizeUI
			LeftBarButton.Visibility = ViewStates.Gone;
			leftBarDoneButton.Visibility = ViewStates.Gone;
			RightBarButton.Visibility = ViewStates.Gone;
			GroupNameEditText.Visibility = ViewStates.Gone;
			groupNameLabel.Visibility = ViewStates.Gone;
			ColorThemeButton.Visibility = ViewStates.Gone;
			ColorThemeText.Visibility = ViewStates.Gone;
			addMembersButton.Visibility = ViewStates.Gone;
			addMembersLabel.Visibility = ViewStates.Gone;
			deleteGroupButton.Visibility = ViewStates.Gone;

			ViewLoaded = true;

			if (Group.serverID == null || !ShouldNotReloadUI)
				FinalizeUI ();
		}

		public void SwitchFromSaveToEdit() {
			EditMode = true;
			sharedEditGroupController.Changed = false;

			updatedThumbnail = null;

			editGroupListAdapter.NotifyDataSetChanged ();

			headingTitle.Text = "EDIT_GROUP_TITLE".t ();

			sendMessageButton.Enabled = true;
			sendMessageButton.Alpha = 1;

			deleteGroupButton.Enabled = true;
			deleteGroupButton.Alpha = 1;
			deleteGroupButton.Visibility = ViewStates.Visible;
			deleteGroupButton.Text = "DELETE_GROUP_BUTTON".t ();
			deleteGroupButton.Click += DidTapDeleteButton;

			//dynamically position members bar below delete group button
			var memberLayoutParams = (RelativeLayout.LayoutParams)members.LayoutParameters;
			memberLayoutParams.AddRule (LayoutRules.Below, Resource.Id.DeleteButton);
			members.LayoutParameters = memberLayoutParams;

			LeftBarButton.Visibility = ViewStates.Gone;
			RightBarButton.Visibility = ViewStates.Gone;

			leftBarDoneButton.Visibility = ViewStates.Visible;
			leftBarDoneButton.Enabled = true;
		}

		public void FinalizeUI() {
			PendingState = false;
			if (ViewLoaded) {
				#region visibility
				if (!EditMode || (EditMode && !Group.isUserGroupOwner)) {
					LeftBarButton.Visibility = ViewStates.Visible;
				}
				if (EditMode && Group.isUserGroupOwner) {
					leftBarDoneButton.Visibility = ViewStates.Visible;
				}
				if (!EditMode) {
					RightBarButton.Visibility = ViewStates.Visible;
				}

				GroupNameEditText.Visibility = Group.isUserGroupOwner ? ViewStates.Visible : ViewStates.Gone;
				groupNameLabel.Visibility = Group.isUserGroupOwner ? ViewStates.Gone : ViewStates.Visible;
				ColorThemeButton.Visibility = Group.isUserGroupOwner ? ViewStates.Visible : ViewStates.Gone;
				ColorThemeText.Visibility = Group.isUserGroupOwner ? ViewStates.Visible : ViewStates.Gone;
				addMembersButton.Visibility = Group.isUserGroupOwner ? ViewStates.Visible : ViewStates.Gone;
				addMembersLabel.Visibility = Group.isUserGroupOwner ? ViewStates.Visible : ViewStates.Gone;
				sendMessageButton.Enabled = Group.serverID != null;
				deleteGroupButton.Visibility = !EditMode ? ViewStates.Gone : ViewStates.Visible;
				#endregion

				#region text
				var rigtBtnTitle = "SAVE_BUTTON".t ();
				if (EditMode && Group.isUserGroupOwner)
					rigtBtnTitle = "UPDATE_BUTTON".t ();
				RightBarButton.Text = rigtBtnTitle;

				if(Group.serverID == null) {
					sendMessageButton.Alpha = 0.6f;
				}

				var btnTitle = "LEAVE_GROUP_BUTTON".t ();
				if (Group.canUserRejoinGroup) {
					btnTitle = "REJOIN_GROUP_BUTTON".t ();
					deleteGroupButton.Click += DidTapRejoinButton;
				} else if (EditMode && Group.isUserGroupOwner) {
					btnTitle = "DELETE_GROUP_BUTTON".t ();
					deleteGroupButton.Click += DidTapDeleteButton;
				} else if (EditMode) {
					deleteGroupButton.Click += DidTapLeaveButton;
				}
				deleteGroupButton.Text = btnTitle;

				if (Group.isUserGroupOwner && !EditMode) 
					headingTitle.Text = "CREATE_GROUP_TITLE".t ();
				else
					headingTitle.Text = Group.isUserGroupOwner ? "EDIT_GROUP_TITLE".t () : "GROUP_DETAILS_TITLE".t ();
				var ViewName = "Create Group View";
				if (EditMode && Group.isUserGroupOwner)
					ViewName = "Edit Group View";
				else if (EditMode && !Group.isUserGroupOwner)
					ViewName = "Group Details View";

				AnalyticsHelper.SendView (ViewName);
				#endregion

				colorTheme = Group.colorTheme;
				this.sharedEditGroupController.ColorTheme = colorTheme;
				this.sharedEditGroupController.OriginalColorTheme = colorTheme;

				GroupNameEditText.Text = Group.displayName;
				groupNameLabel.Text = Group.displayName;

				UpdateThumbnailPicture ();

				if (this.Group.isUserGroupOwner) {
					this.ThumbnailButton.Click += DidTapThumbnail;
				}

				listView.SetAdapter (editGroupListAdapter);

				if (Group.isUserGroupOwner && !EditMode) {
					GroupNameEditText.RequestFocus ();
					KeyboardUtil.ShowKeyboard (this.GroupNameEditText);
				}

				if (Group.isUserGroupOwner) {
					//dynamically position send message button below textbox, not label
					var sendMessageLayoutParams = (RelativeLayout.LayoutParams)sendMessageButton.LayoutParameters;
					sendMessageLayoutParams.AddRule (LayoutRules.Below, Resource.Id.GroupNameText);
					sendMessageButton.LayoutParameters = sendMessageLayoutParams;
				}

				if (!EditMode) {
					//dynamically position members bar below send message button
					var memberLayoutParams = (RelativeLayout.LayoutParams)members.LayoutParameters;
					memberLayoutParams.AddRule (LayoutRules.Below, Resource.Id.SendMessageButton);
					members.LayoutParameters = memberLayoutParams;
				}

				editGroupListAdapter.NotifyDataSetChanged ();

				ShouldNotReloadUI = true;
				ThemeController ();

				EMTask.DispatchMain (() => AndHUD.Shared.Dismiss (Activity));
			}
		}

		public override void OnResume () {
			base.OnResume ();

			if (editGroupListAdapter != null)
				editGroupListAdapter.NotifyDataSetChanged ();
		}

		public override void OnDestroy() {
			sharedEditGroupController.Dispose ();
			base.OnDestroy ();
		}

		void HandleDoneAction(object sender, TextView.EditorActionEventArgs e) {
			e.Handled = false; 
			if (e.ActionId == ImeAction.Done) {
				KeyboardUtil.HideKeyboard (this.View);
				e.Handled = true;   
			}
		}

		protected void DidTapSaveButton(object sender, EventArgs eventArgs) {
			// perform validation
			if(string.IsNullOrEmpty(GroupNameEditText.Text)) {
				UserPrompter.PromptUser ("APP_TITLE".t (), "GROUP_NAME_REQUIRED".t (), this.Activity);
				return;
			}

			if (!this.sharedEditGroupController.HasSuitableNumberOfMembers) {
				UserPrompter.PromptUser ("APP_TITLE".t (), "GROUP_MEMBERS_REQUIRED".t (), this.Activity);
				return;
			}

			leftBarDoneButton.Enabled = false;
			this.RightBarButton.Enabled = false;

			sharedEditGroupController.SaveOrUpdateAsync (GroupNameEditText.Text, colorTheme, updatedThumbnail, !EditMode);
		}

		protected void DidTapMember (object sender, int e) {
			Contact contact = sharedEditGroupController.Group.members [e];
			if(!contact.me) {
				ProfileFragment fragment = ProfileFragment.NewInstance (contact);

				ShouldNotReloadUI = false;

				Activity.FragmentManager.BeginTransaction ()
					.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
					.Replace (Resource.Id.content_frame, fragment)
					.AddToBackStack (null)
					.Commit ();
			}
		}

		protected void DidTapSendMessageButton(object sender, EventArgs eventArgs) {
			ShouldNotReloadUI = false;
			sharedEditGroupController.SendMessageToGroup ();
		}

		void Rejoin() {
			sharedEditGroupController.RejoinAsync (Group.serverID);
			FragmentManager.PopBackStack ();
		}

		void Leave() {
			sharedEditGroupController.LeaveAsync (Group.serverID);
			FragmentManager.PopBackStack ();
		}

		void Delete() {
			sharedEditGroupController.DeleteAsync (Group.serverID);
			FragmentManager.PopBackStack ();
		}

		public void Exit() {
			if(AndHUD.Shared.CurrentDialog != null)
				AndHUD.Shared.Dismiss (Activity);
			this.sharedEditGroupController.UserChoseToLeaveUponBeingAsked = true;
			FragmentManager.PopBackStack ();
		}

		public void EnableActionButton() {
			EMTask.DispatchMain (delegate {
				leftBarDoneButton.Enabled = true;
				RightBarButton.Enabled = true;
			});
		}

		protected void DidTapLeaveButton(object sender, EventArgs eventArgs) {
			var title = "ALERT_ARE_YOU_SURE".t ();
			var message = "LEAVE_GROUP_EXPLAINATION".t ();
			var action = "LEAVE_GROUP_BUTTON".t ();
			UserPrompter.PromptUserWithAction (title, message, action, Leave, this.Activity);
		}

		protected void DidTapDeleteButton(object sender, EventArgs eventArgs) {
			var title = "ALERT_ARE_YOU_SURE".t ();
			var message = "DELETE_GROUP_EXPLAINATION".t ();
			var action = "DELETE_GROUP_BUTTON".t ();
			UserPrompter.PromptUserWithAction (title, message, action, Delete, this.Activity);
		}

		protected void DidTapRejoinButton(object sender, EventArgs eventArgs) {
			var title = "ALERT_ARE_YOU_SURE".t ();
			var message = "REJOIN_GROUP_EXPLAINATION".t ();
			var action = "REJOIN_GROUP_BUTTON".t ();

			UserPrompter.PromptUserWithAction (title, message, action, Rejoin, this.Activity);
		}
			
		void DidTapThumbnail(object sender, EventArgs e) {
			ShouldNotReloadUI = false;
			StartAcquiringImage ();
		}

		protected override int PopupMenuInflateResource () {
			return Resource.Menu.account_thumbnail_options;
		}

		protected override View PopupMenuAnchorView () {
			return this.ThumbnailButton;
		}

		protected override void DidAcquireMedia (string mediaType, string path) {
			if (path != null) {
				byte[] fileAtPath = File.ReadAllBytes (path);
				if (fileAtPath != null) {
					string thumbnailPath = sharedEditGroupController.GetStagingFilePathForGroupThumbnail ();
					appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (thumbnailPath);
					appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (thumbnailPath, fileAtPath, null);
					sharedEditGroupController.Group.UpdateThumbnailUrlAfterMovingFromCache (thumbnailPath);
					updatedThumbnail = fileAtPath;
					UpdateThumbnailPicture ();
				}
			}
		}

		protected override bool AllowsImageCropping () {
			return true;
		}

		public override BackgroundColor ColorTheme { 
			get { return colorTheme; }
		}

		public override CounterParty CounterParty { 
			get { return Group; }
		}

		public override string TextInDisplayField {
			get { return GroupNameEditText.Text; }
			set { GroupNameEditText.Text = value; }
		}
			
		public override void LeftBarButtonClicked (object sender, EventArgs e) {
			if (this.sharedEditGroupController.ShouldStopUserFromExiting) {
				string title = "ALERT_ARE_YOU_SURE".t ();
				string message = "UNSAVED_CHANGES".t ();
				string action = "EXIT".t ();
				UserPrompter.PromptUserWithAction (title, message, action, Exit, this.Activity);
			} else {
				FragmentManager.PopBackStackImmediate ();
			}
		}

		public override void RightBarButtonClicked (object sender, EventArgs e) {
			DidTapSaveButton (sender, e);
		}

		public override void ColorThemeSpinnerItemClicked (object sender, AdapterView.ItemSelectedEventArgs e) {
			DidSelectColorTheme (sender, e);
		}

		public override void AdditionalUIChangesOnResume () {}

		public override void AdditionalThemeController () {
			if(ColorThemeSpinner != null)
				ColorThemeSpinner.SetSelection( Array.IndexOf (BackgroundColor.AllColors, colorTheme));
			colorTheme.GetColorThemeSelectionImageResource ((string file) => {
				if (ColorThemeButton != null && this != null) {
					BitmapSetter.SetBackgroundFromFile (ColorThemeButton, Resources, file);
				}
			});
			colorTheme.GetAddImageResource ((string file) => {
				if (addMembersButton != null && this != null) {
					BitmapSetter.SetBackgroundFromFile (addMembersButton, Resources, file);
				}
			});
			members.SetBackgroundColor (colorTheme.GetColor());
			colorTheme.GetButtonResource ((Drawable drawable) => {
				sendMessageButton.SetBackgroundDrawable (drawable);
				deleteGroupButton.SetBackgroundDrawable (drawable);
			});
		}

		protected void DidSelectColorTheme (object sender, AdapterView.ItemSelectedEventArgs e) {
			colorTheme = BackgroundColor.AllColors [e.Position];
			this.sharedEditGroupController.ColorTheme = colorTheme;
			ThemeController ();
		}

		public override string ImageSearchSeedString { 
			get {
				string seedString = GroupNameEditText != null ? GroupNameEditText.Text : string.Empty;
				if (seedString.Equals (string.Empty)) {
					seedString = groupNameLabel != null ? groupNameLabel.Text : string.Empty;
				}
				return seedString;
			}
		}

		public void TransitionToChatController (ChatEntry chatEntry) {
			var chatFragment = ChatFragment.NewInstance (chatEntry);
			var args = new Bundle ();
			var index = EMApplication.Instance.appModel.chatList.entries.IndexOf (chatEntry);
			args.PutInt ("Position", index >= 0 ? index : ChatFragment.NEW_MESSAGE_INITIATED_FROM_NOTIFICATION_POSITION);
			chatFragment.Arguments = args;
			Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, chatFragment, "chatEntry" + chatEntry.chatEntryID)
				.AddToBackStack ("chatEntry" + chatEntry.chatEntryID)
				.Commit ();
		}
			
		class EditGroupListAdapter : EmUndoRecyclerAdapter {
			enum EditGroupItemType {
				Regular = 0,
				UndoState = 1
			}

			readonly EditGroupFragment editGroupFragment;

			public EditGroupListAdapter(EditGroupFragment fragment) {
				editGroupFragment = fragment;
			}

			protected override void DeleteTapped (int row) {
				Contact contact = this.editGroupFragment.sharedEditGroupController.Group.members [row];
				this.editGroupFragment.sharedEditGroupController.RemoveContact (contact);
				base.DeleteTapped (row);

				StartBackgroundCorrectionAnimations ();
			}

			// After moving rows around the odd/even backgrounds can be mismatched
			// we run down through the visible rows and correct the backgrounds
			protected void StartBackgroundCorrectionAnimations () {
				EditGroupFragment self = this.editGroupFragment;
				if (GCCheck.ViewGone (self)) return;
				LinearLayoutManager layoutMgr = self.listView.GetLayoutManager () as LinearLayoutManager;
				int firstPos = layoutMgr.FindFirstVisibleItemPosition ();
				int lastPos = layoutMgr.FindLastVisibleItemPosition ();
				ContinueBackgroundCorrectionAnimations (firstPos, lastPos);
			}

			protected void ContinueBackgroundCorrectionAnimations (int currentPos, int lastPos) {
				EditGroupFragment self = this.editGroupFragment;
				if (GCCheck.ViewGone (self)) return;

				ViewHolder holder = (ViewHolder)self.listView.FindViewHolderForAdapterPosition (currentPos);
				if (holder != null) {
					holder.SetEven (currentPos % 2 == 0);
				}

				if (++currentPos <= lastPos) {
					new Timer ((object o) => {
						EMTask.PerformOnMain( () => { 
							ContinueBackgroundCorrectionAnimations(currentPos, lastPos);
						} );
					}, null, Constants.ODD_EVEN_BACKGROUND_COLOR_CORRECTION_PAUSE, Timeout.Infinite);
				}
			}

			public override bool CanSwipeAtRow (int row) {
				SharedEditGroupController shared = this.editGroupFragment.sharedEditGroupController;
				Group group = shared.Group;
				// Don't allow swipes if it's the group owner row (group owner can't be deleted).
				// If group is null for some reason..
				// If the user isn't the owner of the group, they can't modify its contents.
				if (!base.CanSwipeAtRow (row) || row == AbstractEditGroupController.GROUP_OWNER_INDEX || group == null || !group.isUserGroupOwner) {
					return false;
				} else {
					return true;
				}
			}
				
			public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType) {
				EditGroupItemType type = (EditGroupItemType)viewType;
				switch (type) {
				default:
				case EditGroupItemType.Regular:
					{
						View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.group_entry, parent, false);
						ViewHolder holder = new ViewHolder (this.editGroupFragment, view, OnClick);

						return holder;
					}
				case EditGroupItemType.UndoState:
					{
						UndoViewHolder holder = UndoViewHolder.NewInstance (parent, DeleteTapped, UndoTapped);
						return holder;
					}
				}
			}

			public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position) {
				EditGroupItemType viewType = (EditGroupItemType)this.GetItemViewType (position);
				switch (viewType) {
				default:
				case EditGroupItemType.UndoState:
					{
						UndoViewHolder oHolder = holder as UndoViewHolder;
						oHolder.ItemView.LayoutParameters.Height = this.RowHeight;
						break;
					}
				case EditGroupItemType.Regular:
					{
						Contact contact = editGroupFragment.sharedEditGroupController.Group.members [position];

						bool abandoned = editGroupFragment.sharedEditGroupController.Group.abandonedContacts.Contains (contact.serverID);

						ViewHolder currVH = (ViewHolder)holder;

						this.RowHeight = currVH.ItemView.LayoutParameters.Height;

						currVH.SetContact (contact, abandoned);
						currVH.SetEven (position % 2 == 0);
						currVH.SetAbandoned (abandoned);
						currVH.PossibleShowProgressIndicator (contact);

						currVH.SendButtonClicked = () => {
							if(!contact.me) {
								currVH.SendButtonClicked = null; // prevent double taps
								editGroupFragment.ShouldNotReloadUI = false;
								this.editGroupFragment.sharedEditGroupController.GoToNewOrExistingChatEntry (contact);
							}
						};
						break;
					}
				}
			}

			public override int GetItemViewType (int position) {
				if (this.RowsWithUndoState.Contains (position)) {
					return (int)EditGroupItemType.UndoState;
				} else {
					return (int)EditGroupItemType.Regular;
				}
			}

			public override int ItemCount {
				get {
					return editGroupFragment.sharedEditGroupController.Group.members == null ? 0 : editGroupFragment.sharedEditGroupController.Group.members.Count; 
				}
			}
		}

		class ViewHolder: RecyclerView.ViewHolder {
			EditGroupFragment editGroupFragment;

			public ImageView ThumbnailImageview { get; set; }
			public ImageView PhotoFrame { get; set; }
			public ImageView AliasIcon { get; set; }
			public TextView MemberName { get; set; }
			public View ColorTrimView { get; set; }
			public ProgressBar ProgressBar { get; set; }

			ImageView sendButton;
			public ImageView SendButton {
				get { return sendButton; }
				set { sendButton = value; }
			}

			Action sendButtonClicked;
			public Action SendButtonClicked {
				get { return sendButtonClicked; }
				set { sendButtonClicked = value; }
			}

			public ViewHolder(EditGroupFragment fragment, View convertView, Action<int> onItemClick) : base (convertView) {
				editGroupFragment = fragment;
				ThumbnailImageview = convertView.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				PhotoFrame = convertView.FindViewById<ImageView> (Resource.Id.photoFrame);
				AliasIcon = convertView.FindViewById<ImageView> (Resource.Id.aliasIcon);
				MemberName = convertView.FindViewById<TextView> (Resource.Id.GroupTextView);
				ColorTrimView = convertView.FindViewById<View> (Resource.Id.trimView);
				ProgressBar = convertView.FindViewById<ProgressBar> (Resource.Id.ProgressBar);

				MemberName.Typeface = FontHelper.DefaultFont;

				SendButton = convertView.FindViewById<ImageView> (Resource.Id.sendButton);
				SendButton.Click += DidPressSendButton;
				convertView.Click += (object sender, EventArgs e) => { 
					onItemClick (base.AdapterPosition); 
				};
			}

			public void PossibleShowProgressIndicator (CounterParty c) {
				if (BitmapSetter.ShouldShowProgressIndicator (c)) {
					ProgressBar.Visibility = ViewStates.Visible;
					PhotoFrame.Visibility = ViewStates.Invisible;
					ThumbnailImageview.Visibility = ViewStates.Invisible;
				} else {
					ProgressBar.Visibility = ViewStates.Gone;
					PhotoFrame.Visibility = ViewStates.Visible;
					ThumbnailImageview.Visibility = ViewStates.Visible;
				}
			}

			public void SetContact(Contact contact, bool abandoned) {
				MemberName.Text = contact == null ? "" : contact.displayName;
				MemberName.SetTextColor (Android_Constants.BLACK_COLOR);
				if (abandoned)
					MemberName.Text += " " + "LEFT_GROUP_EXTENSION".t ();

				BackgroundColor ct = contact == null ? BackgroundColor.Default : contact.colorTheme;
				Color trimAndTextColor = ct.GetColor ();
				ColorTrimView.SetBackgroundColor (trimAndTextColor);

				if (contact.me)
					SendButton.Visibility = ViewStates.Gone;
				else {
					ct.GetChatSendButtonResource ((string filepath) => {
						if (SendButton != null && editGroupFragment != null) {
							BitmapSetter.SetBackgroundFromFile (SendButton, editGroupFragment.Resources, filepath);
						}
					});
				}
				ct.GetPhotoFrameLeftResource ((string filepath) => {
					if (PhotoFrame != null && editGroupFragment != null) {
						BitmapSetter.SetBackgroundFromFile (PhotoFrame, editGroupFragment.Resources, filepath);
					}
				});
				if (contact != null) {
					BitmapRequest request = BitmapRequest.From (this, contact, ThumbnailImageview, Resource.Drawable.userDude, editGroupFragment.ThumbnailSizePixels, editGroupFragment.Resources);
					BitmapSetter.SetThumbnailImage (request);
				}

				if(contact.fromAlias != null) {
					var alias = EMApplication.GetInstance().appModel.account.accountInfo.AliasFromServerID (contact.fromAlias);
					if(alias != null) {
						AliasIcon.Visibility = ViewStates.Visible;

						BitmapRequest request = BitmapRequest.FromMedia (
							holder: this, 
							media: alias.iconMedia, 
							view: AliasIcon, 
							defaultResource: Resource.Drawable.Icon, 
							maxHeightInPixels: 100, 
							resources: editGroupFragment.Resources);
						
						BitmapSetter.SetImage (request);
					} else {
						AliasIcon.Visibility = ViewStates.Invisible;
					}
				} else {
					AliasIcon.Visibility = ViewStates.Invisible;
				}
			}

			public void SetEven(bool isEven) {
				BasicRowColorSetter.SetEven (isEven, this.ItemView);
			}

			public void SetAbandoned(bool abandoned) {
				this.ItemView.Alpha = abandoned ? 0.5f : 1.0f;
			}

			public void DidPressSendButton (object sender, EventArgs e) {
				if (SendButtonClicked != null)
					SendButtonClicked ();
			}
		}

		class SharedEditGroupController : AbstractEditGroupController {
			private WeakReference _r = null;
			private EditGroupFragment Self {
				get { return this._r != null ? this._r.Target as EditGroupFragment : null; }
				set { this._r = new WeakReference (value); }
			}

			readonly ApplicationModel _appModel;

			public SharedEditGroupController(EditGroupFragment fragment, ApplicationModel applicationModel, Group g) : base(applicationModel, g) {
				this.Self = fragment;
				_appModel = applicationModel;
			}

			public override void ListOfMembersUpdated () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.editGroupListAdapter.NotifyDataSetChanged ();
				});
			}

			protected override void DidLoadGroup () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.FinalizeUI ();
				});
			}

			protected override void DidSaveGroup () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.SwitchFromSaveToEdit ();
				});
			}

			protected override void DidUpdateGroup () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.Exit ();
				});
			}

			public override void DidChangeColorTheme() {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.ThemeController ();
				});
			}

			public override void UpdateAliasText (string text) {

			}

			protected override void DidLoadGroupFailed () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					Activity acc = self.Activity;
					if (GCCheck.Gone (acc)) return;
					UserPrompter.PromptUserWithActionNoNegative ("APP_TITLE".t (), "GROUP_LOAD_FAILED".t (), self.Exit, acc);
				});
			}

			protected override void DidSaveGroupFailed () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					Activity acc = self.Activity;
					if (GCCheck.Gone (acc)) return;
					UserPrompter.PromptUserWithActionNoNegative ("APP_TITLE".t (), "GROUP_SAVE_FAILED".t (), self.EnableActionButton, acc);
				});
			}

			protected override void DidSaveOrUpdateGroupFailed () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					Activity acc = self.Activity;
					if (GCCheck.Gone (acc)) return;
					UserPrompter.PromptUserWithActionNoNegative ("APP_TITLE".t (), "GROUP_SAVE_OR_UPDATE_FAILED".t (), self.EnableActionButton, acc);
				});
			}

			protected override void DidUpdateGroupFailed () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					Activity acc = self.Activity;
					if (GCCheck.Gone (acc)) return;
					UserPrompter.PromptUserWithActionNoNegative ("APP_TITLE".t (), "GROUP_UPDATE_FAILED".t (), self.EnableActionButton, acc);
				});
			}

			protected override void DidLeaveOrRejoinGroup () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.editGroupListAdapter.NotifyDataSetChanged ();
				});
			}

			protected override void DidLeaveGroupFailed () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					Activity acc = self.Activity;
					if (GCCheck.Gone (acc)) return;
					UserPrompter.PromptUser ("APP_TITLE".t (), "GROUP_LEAVE_FAILED".t (), acc);
				});
			}

			protected override void DidRejoinGroupFailed () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					Activity acc = self.Activity;
					if (GCCheck.Gone (acc)) return;
					UserPrompter.PromptUser ("APP_TITLE".t (), "GROUP_REJOIN_FAILED".t (), acc);
				});
			}

			protected override void ContactDidChangeThumbnail () {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.editGroupListAdapter.NotifyDataSetChanged ();
				});
			}

			public override void TransitionToChatController (ChatEntry chatEntry) {
				EMTask.DispatchMain (() => {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.TransitionToChatController (chatEntry);
				});
			}

			public override string TextInDisplayField {
				get {
					EditGroupFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return string.Empty;
					return self.GroupNameEditText.Text;
				}
			}
		}
	}
}