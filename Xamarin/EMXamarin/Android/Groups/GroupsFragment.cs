using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Nhaarman.Listviewanimations.Appearance.Simple;
using Com.Nhaarman.Listviewanimations.Itemmanipulation;
using em;

namespace Emdroid {

	public class GroupsFragment : Fragment {

		private HiddenReference<SharedGroupsController> _shared;
		private SharedGroupsController sharedGroupController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedGroupsController> (value); }
		}

		GroupListAdapter groupListAdapter;

		Button leftBarButton, rightBarButton;

		public static GroupsFragment NewInstance () {
			return new GroupsFragment ();
		}

		public int ThumbnailSizePixels { get; set; }

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			sharedGroupController = new SharedGroupsController (this);

			DisplayMetrics displayMetrics = Resources.DisplayMetrics;
			ThumbnailSizePixels = (int) (Android_Constants.ROUNDED_THUMBNAIL_SIZE / displayMetrics.Density);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			var v = inflater.Inflate(Resource.Layout.groups, container, false);
			ThemeController (v);
			return v;
		}

		public void ThemeController () {
			ThemeController (View);
		}

		public void ThemeController (View v) {
			if (IsAdded && v != null) {
				EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme.GetBackgroundResource ((string file) => {
					if (v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
					}
				});
			}
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			groupListAdapter = new GroupListAdapter(this);

			#region animation
			var listView = View.FindViewById<DynamicListView>(Resource.Id.GroupsListView);
			var animationAdapter = new AlphaInAnimationAdapter(groupListAdapter);
			animationAdapter.SetAbsListView(listView);
			listView.Adapter = animationAdapter;
			listView.ItemClick += DidTapGroup;
			#endregion

			leftBarButton = View.FindViewById<Button> (Resource.Id.LeftBarButton);
			leftBarButton.Click += (sender, e) => FragmentManager.PopBackStack ();
			leftBarButton.Typeface = FontHelper.DefaultFont;
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			rightBarButton = View.FindViewById<Button> (Resource.Id.RightBarButton);
			rightBarButton.Click += DidTapAddGroup;
			rightBarButton.Typeface = FontHelper.DefaultFont;

			View.FindViewById<TextView> (Resource.Id.starterLabel).Typeface = FontHelper.DefaultFont;

			AnalyticsHelper.SendView ("Groups View");
		}

		public override void OnResume () {
			base.OnResume ();

			if (groupListAdapter != null)
				groupListAdapter.NotifyDataSetChanged ();
		}

		public override void OnPause() {
			base.OnPause ();
		}

		public override void OnDestroy() {
			base.OnDestroy ();
		}

		protected void DidTapAddGroup(object sender, EventArgs eventArgs) {
			EditGroupFragment fragment = EditGroupFragment.NewInstance (false, null);
			Activity.FragmentManager.BeginTransaction ()
				.SetTransition (FragmentTransit.FragmentOpen)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit();
		}

		protected void DidTapGroup(object sender, AdapterView.ItemClickEventArgs eventArgs) {
			Group group = sharedGroupController.Groups[eventArgs.Position];

			EditGroupFragment fragment = EditGroupFragment.NewInstance (true, group);

			Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit();
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

		class GroupListAdapter : BaseAdapter<Group> {

			readonly GroupsFragment groupsFragment;

			public GroupListAdapter(GroupsFragment fragment) {
				groupsFragment = fragment;
			}

			public override long GetItemId(int position) {
				return position;
			}

			public override Group this[int index] {  
				get { return groupsFragment.sharedGroupController.Groups == null ? null : groupsFragment.sharedGroupController.Groups[index]; }
			}

			public override int Count {
				get { return groupsFragment.sharedGroupController.Groups == null ? 0 : groupsFragment.sharedGroupController.Groups.Count; }
			}

			public override View GetView(int position, View convertView, ViewGroup parent) {
				ViewHolder currVH = convertView != null ? (ViewHolder)convertView.Tag : new ViewHolder (groupsFragment);
				currVH.Position = position;
				Group group = this [position];
				currVH.SetGroup (group);
				currVH.SetEven (position % 2 == 0);
				currVH.PossibleShowProgressIndicator (group);

				currVH.SendButtonClicked = () => {
					currVH.SendButtonClicked = null; // prevent double taps
					this.groupsFragment.sharedGroupController.GoToNewOrExistingChatEntry (group);
				};

				return currVH.view;
			}
		}

		class ViewHolder: EMBitmapViewHolder {
			GroupsFragment groupsFragment;
			public bool InflationNeeded { get; set; }
			public View view { get; set; }
			public bool IsInsertion { get; set; }
			public ImageView ThumbnailImageview { get; set; }
			public ImageView PhotoFrame { get; set; }
			public ImageView AliasIcon { get; set; }
			public TextView GroupName { get; set; }
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

			public ViewHolder(GroupsFragment fragment) {
				groupsFragment = fragment;
				view = null;
				this.IsInsertion = false;
				this.InflationNeeded = true;
			}

			void Inflate() {
				view = View.Inflate (groupsFragment.Activity, Resource.Layout.group_entry, null);
				view.Tag = this;
				this.ThumbnailImageview = view.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				this.PhotoFrame = view.FindViewById<ImageView> (Resource.Id.photoFrame);
				this.AliasIcon = view.FindViewById<ImageView> (Resource.Id.aliasIcon);
				this.GroupName = view.FindViewById<TextView> (Resource.Id.GroupTextView);
				this.ColorTrimView = view.FindViewById<View> (Resource.Id.trimView);
				this.ProgressBar = view.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
				this.SendButton = view.FindViewById<ImageView> (Resource.Id.sendButton);
				this.SendButton.Click += DidPressSendButton;

				this.GroupName.Typeface = FontHelper.DefaultFont;

				this.InflationNeeded = false;
			}

			public void PossibleShowProgressIndicator (CounterParty c) {
				if (BitmapSetter.ShouldShowProgressIndicator (c)) {
					this.ProgressBar.Visibility = ViewStates.Visible;
					this.PhotoFrame.Visibility = ViewStates.Invisible;
					this.ThumbnailImageview.Visibility = ViewStates.Invisible;
				} else {
					this.ProgressBar.Visibility = ViewStates.Gone;
					this.PhotoFrame.Visibility = ViewStates.Visible;
					this.ThumbnailImageview.Visibility = ViewStates.Visible;
				}
			}

			void DidPressSendButton (object sender, EventArgs e) {
				if (SendButtonClicked != null)
					SendButtonClicked ();
			}

			public void SetGroup(Group group) {
				if (InflationNeeded)
					Inflate ();

				GroupName.Text = group == null ? "" : group.displayName;

				BackgroundColor colorTheme = group == null ? BackgroundColor.Default : group.colorTheme;
				Color trimAndTextColor = colorTheme.GetColor ();
				ColorTrimView.SetBackgroundColor (trimAndTextColor);

				if (group.me)
					SendButton.Visibility = ViewStates.Gone;
				else {
					colorTheme.GetChatSendButtonResource ((string filepath) => {
						if (SendButton != null && groupsFragment != null) {
							BitmapSetter.SetBackgroundFromFile (SendButton, groupsFragment.Resources, filepath);
						}
					});
				}
				colorTheme.GetPhotoFrameLeftResource ((string filepath) => {
					if (PhotoFrame != null && groupsFragment != null) {
						BitmapSetter.SetBackgroundFromFile (PhotoFrame, groupsFragment.Resources, filepath);
					}
				});
				if (group != null) {
					BitmapSetter.SetThumbnailImage (this, group, groupsFragment.Resources, ThumbnailImageview, Resource.Drawable.userDude, groupsFragment.ThumbnailSizePixels);
				}

				if(group.fromAlias != null) {
					var alias = EMApplication.GetInstance().appModel.account.accountInfo.AliasFromServerID (group.fromAlias);
					if(alias != null) {
						AliasIcon.Visibility = ViewStates.Visible;
						BitmapSetter.SetImage (this, alias.iconMedia, groupsFragment.Resources, AliasIcon, Resource.Drawable.Icon, 100);
					} else {
						AliasIcon.Visibility = ViewStates.Invisible;
					}
				} else {
					AliasIcon.Visibility = ViewStates.Invisible;
				}
			}

			public void SetEven(bool isEven) {
				BasicRowColorSetter.SetEven (isEven, view);
			}
		}

		class SharedGroupsController : AbstractGroupsController {
			private WeakReference _r = null;
			private GroupsFragment Self {
				get { return this._r != null ? this._r.Target as GroupsFragment : null; }
				set { this._r = new WeakReference (value); }
			}

			public SharedGroupsController(GroupsFragment fragment) : base(EMApplication.GetInstance().appModel) {
				this.Self = fragment;
			}

			public override void GroupsValuesDidChange() {
				GroupsFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.groupListAdapter.NotifyDataSetChanged ();
			}

			public override void ReloadGroup(Contact group) {
				GroupsFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.groupListAdapter.NotifyDataSetChanged ();
			}

			public override void DidChangeColorTheme () {
				GroupsFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.ThemeController ();
			}

			public override void TransitionToChatController (ChatEntry chatEntry) {
				EMTask.DispatchMain (() => {
					GroupsFragment self = this.Self;
					if (GCCheck.ViewGone (self)) return;
					self.TransitionToChatController (chatEntry);
				});
			}
		}
	}
}