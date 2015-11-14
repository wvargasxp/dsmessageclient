using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using em;
using EMXamarin;
using System.Threading;

using Android.Graphics.Drawables;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;

namespace Emdroid {

	public class InboxFragment : Fragment {

		InboxChatEntryAdapter listAdapter;
		ChatList chatList;

		private HiddenReference<CommonInbox> _shared;
		private CommonInbox commonInbox {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<CommonInbox> (value); }
		}

		RecyclerView listView;

		private ProgressBar TitleCircleProgress { get; set; }
		private TextView TitleTextView { get; set; }

		#region UI
		Button leftBarButton;
		Button newConversationButton;
		Button NewConversationButton {
			get { return newConversationButton; }
			set { newConversationButton = value; }
		}

		View footer;
		View InboxFooter {
			get { return footer; }
			set { footer = value; }
		}

		public TextView BurgerUnreadIcon { get; set; }

		#endregion

		public static InboxFragment NewInstance () {
			return new InboxFragment ();
		}

		public int ThumbnailSizePixels { get; set; }

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			DisplayMetrics displayMetrics = Resources.DisplayMetrics;
			ThumbnailSizePixels = (int)(Android_Constants.ROUNDED_THUMBNAIL_SIZE / displayMetrics.Density);

			commonInbox = new CommonInbox (this);
		}

		WeakDelegateProxy DidFinishBulkUpdateFromGCM;

		public override void OnResume () {
			base.OnResume ();
			(Activity as MainActivity).EnableSideMenu ();

			if (listAdapter != null)
				listAdapter.NotifyDataSetChanged ();
			
			chatList.DidBecomeVisible ();

			UpdateTitleProgress ();
			if (IsArgumentsFromShareIntent) {
				// Allow time for inbox to show before kicking over to new chat
				EMTask.DispatchBackground (() => {
					Thread.Sleep (300);
					EMTask.DispatchMain (() => {
						this.NewConversationButton.PerformClick ();
					});
				});
			}

			EMApplication.Instance.appModel.notificationList.ObtainUnreadCountAsync ((int count) => {
				this.commonInbox.UpdateBurgerUnreadCount (count);
			});
		}

		public override void OnPause () {
			base.OnPause ();
			(Activity as MainActivity).DisableSideMenu ();
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate (Resource.Layout.inbox, container, false);
			this.NewConversationButton = v.FindViewById<Button> (Resource.Id.NewConvoButton);
			this.InboxFooter = v.FindViewById<View> (Resource.Id.InboxFooter);
			this.TitleCircleProgress = v.FindViewById<ProgressBar> (Resource.Id.TitleCircleProgress);
			this.TitleTextView = v.FindViewById<TextView> (Resource.Id.starterLabel);
			this.BurgerUnreadIcon = v.FindViewById<TextView> (Resource.Id.burgerUnreadIcon);
			ThemeController (v);
			return v;
		}

		public void ThemeController () {
			ThemeController (this.View);
		}

		public void ThemeController (View v) {
			// v should be the InboxFragment's View.
			// It can be null when the InboxFragment is not on screen.
			if (this.IsAdded && v != null) {
				BackgroundColor colorTheme = EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme;
				var states = new StateListDrawable ();
				colorTheme.GetInboxNewMessageResource ((string notPressed, string pressed) => {
					if (states != null && this.NewConversationButton != null) {
						states.AddState (new int[] { Android.Resource.Attribute.StateFocused }, Drawable.CreateFromPath (pressed));
						states.AddState (new int[] { Android.Resource.Attribute.StatePressed }, Drawable.CreateFromPath (pressed));
						states.AddState (new int[] { }, Drawable.CreateFromPath (notPressed));
						BitmapSetter.SetBackground (this.NewConversationButton, states);
					}
				});
				colorTheme.GetBackgroundResource ((string file) => {
					if (v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile (v, this.Resources, file);
					}
				});
				colorTheme.GetInboxFooterResource ((string file, byte[] chunk) => {
					if (this.InboxFooter != null) {
						BitmapSetter.SetBackgroundFromNinePatch (this.InboxFooter, this.Resources, file, chunk);
					}
				});
			}
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState); 
			chatList = EMApplication.GetInstance ().appModel.chatList;

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			View.FindViewById<TextView> (Resource.Id.starterLabel).Typeface = FontHelper.DefaultFont;

			listAdapter = new InboxChatEntryAdapter (this);
			listAdapter.ItemClick += DidTapItem;
			listAdapter.LongItemClick += DidLongTapItem;
			listView = View.FindViewById<RecyclerView> (Resource.Id.InboxList); // get reference to the ListView in the layout

			LinearLayoutManager layoutMgr = new LinearLayoutManager (this.Activity);
			listView.SetLayoutManager (layoutMgr);
			listView.AddItemDecoration (new SimpleDividerItemDecoration (this.Activity, useFullWidthLine: true, shouldDrawBottomLine: false));

			EmItemTouchHelper g = new EmItemTouchHelper (listAdapter, 0, ItemTouchHelper.Left | ItemTouchHelper.Right);
			ItemTouchHelper hel = new ItemTouchHelper (g);
			hel.AttachToRecyclerView (listView);
			listView.SetAdapter (listAdapter);

			leftBarButton = View.FindViewById<Button> (Resource.Id.LeftBarButton);
			leftBarButton.Click += (sender, e) => (Activity as MainActivity).OpenDrawer ();
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			this.NewConversationButton.Click += (object sender, EventArgs e) => {
				chatList.underConstruction = ChatEntry.NewUnderConstructionChatEntry (EMApplication.GetInstance ().appModel, DateTime.Now.ToEMStandardTime (EMApplication.GetInstance ().appModel));
				var fragment = new ChatFragment ();
				var args = new Bundle ();
				args.PutInt ("Position", -1);
				if (this.Arguments != null) {
					string mediaURIString = (string)(this.Arguments.Get (ShareIntentActivity.MEDIA_INTENT_KEY));
					string text = (string)(this.Arguments.Get (ShareIntentActivity.TEXT_INTENT_KEY));
					if (mediaURIString != null) {
						args.PutString (ShareIntentActivity.MEDIA_INTENT_KEY, mediaURIString);
						this.Arguments.Remove (ShareIntentActivity.MEDIA_INTENT_KEY);
					} else if (text != null) {
						args.PutString (ShareIntentActivity.TEXT_INTENT_KEY, text);
						this.Arguments.Remove (ShareIntentActivity.TEXT_INTENT_KEY);
					}
				}
				fragment.Arguments = args;
				Activity.FragmentManager.BeginTransaction ()
					.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
					.Replace (Resource.Id.content_frame, fragment)
					.AddToBackStack (null)
					.Commit ();
			};

			AnalyticsHelper.SendView ("Inbox View");
		}

		private bool IsArgumentsFromShareIntent {
			get {
				if (this.Arguments == null) {
					return false;
				}
				return (this.Arguments.Get (ShareIntentActivity.MEDIA_INTENT_KEY) != null || this.Arguments.Get (ShareIntentActivity.TEXT_INTENT_KEY) != null);
			}
		}

		private bool IsArgumentsFromNotification {
			get {
				if (this.Arguments == null) {
					return false;
				}
				return (this.Arguments.Get (GcmService.NOTIFICATION_GUID_INTENT_KEY) != null);
			}
		}

		public override void OnDestroy () {
			if (commonInbox != null) {
				commonInbox.Dispose ();
				commonInbox = null;
			}

			base.OnDestroy ();
		}

		public void HandleNotificationBanner (ChatEntry entry) {
			this.commonInbox.ShowNotificationBanner (entry);
		}

		public void GoToChatEntry (ChatEntry entry) {
			this.commonInbox.GoToChatEntry (entry);
		}

		public void UpdateTitleProgress () {
			// Can be called before fragment is attached to activity, so don't do anything if we're not yet added.
			if (!this.IsAdded) {
				return;
			}

			TextView titleTextView = this.TitleTextView;
			ProgressBar titleCircleProgress = this.TitleCircleProgress;
			if (titleTextView == null || titleCircleProgress == null) {
				return;
			}

			CommonInbox sharedBox = this.commonInbox;
			if (sharedBox != null && sharedBox.ShowingProgressIndicator) {
				titleTextView.Visibility = ViewStates.Gone;
				titleCircleProgress.Visibility = ViewStates.Visible;
			} else {
				titleTextView.Visibility = ViewStates.Visible;
				titleCircleProgress.Visibility = ViewStates.Gone;
			}
		}

		protected void DidTapItem (object sender, int e) {
			var fragment = new ChatFragment ();
			var args = new Bundle ();
			int position = e;
			ChatEntry ce = commonInbox.viewModel [position];
			position = commonInbox.chatList.entries.IndexOf (ce);
			if (position != -1) {

				args.PutInt ("Position", position);
				fragment.Arguments = args;
				Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, fragment, "chatEntry" + ce.chatEntryID)
				.AddToBackStack ("chatEntry" + ce.chatEntryID)
				.Commit ();
			}
		}

		protected void DidLongTapItem (object sender, int e) {

			RecyclerView listView = this.listView;
			LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();
			InboxChatEntryAdapter adapter = this.listAdapter;

			int firstVisiblePosition = layoutMgr.FindFirstVisibleItemPosition ();
			int uiPosition = adapter.ModelPositionToUIPosition (e);
			int rowPosition = uiPosition - firstVisiblePosition;
			View view = listView.GetChildAt (rowPosition);

			var popupMenu = new Android.Widget.PopupMenu (this.Activity, view);

			ChatEntry chatEntry = commonInbox.viewModel [e];
			if (chatEntry.IsAdHocGroupWeCanLeave ())
				popupMenu.Inflate (Resource.Menu.popup_inbox_long_press_adhoc_group);
			else
				popupMenu.Inflate (Resource.Menu.popup_inbox_long_press);

			popupMenu.MenuItemClick += (s1, arg1) => {
				IMenuItem item = arg1.Item;
				switch (item.ItemId) {
				case Resource.Id.DeleteEntry:
					chatList.RemoveChatEntryAtAsync (e, true);
					break;

				case Resource.Id.LeaveAdhocGroup:
					var builder = new AlertDialog.Builder (Activity);
					builder.SetTitle ("LEAVE_CONVERSATION".t ());
					builder.SetMessage ("LEAVE_CONVERSATION_EXPAINATION".t ());
					builder.SetPositiveButton ("LEAVE".t (), (sender2, dialogClickEventArgs) => chatEntry.LeaveConversationAsync ());
					builder.SetNegativeButton ("CANCEL_BUTTON".t (), (sender2, dialogClickEventArgs) => {
					});
					builder.Create ();
					builder.Show ();
					break;

				default:
					break;
				}

			};

			// Android 4 now has the DismissEvent
			popupMenu.DismissEvent += (s2, arg2) => {
				System.Diagnostics.Debug.WriteLine ("menu dismissed");
			};

			popupMenu.Show ();
		}

		class InboxChatEntryAdapter : EmUndoRecyclerAdapter {
			enum InboxItemType {
				Regular = 0,
				UndoState = 1
			}

			public InboxFragment inboxFragment { get; set; }

			public InboxChatEntryAdapter (InboxFragment fragment) : base () {
				this.inboxFragment = fragment;
			}

			protected override void DeleteTapped (int row) {
				this.RowsWithUndoState.Remove (row);
				ChatList chatList = inboxFragment.chatList;
				chatList.RemoveChatEntryAtAsync (row, true);
			}

			#region implemented abstract members of Adapter

			public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position) {
				InboxItemType viewType = (InboxItemType)this.GetItemViewType (position);
				switch (viewType) {
				default:
				case InboxItemType.Regular:
					{
						ViewHolder currVH = holder as ViewHolder;

						this.RowHeight = currVH.ItemView.LayoutParameters.Height;

						ChatEntry chatEntry = inboxFragment.chatList == null ? null : inboxFragment.commonInbox.viewModel [position];
						currVH.SetChatEntry (chatEntry);

						currVH.SetEven (position % 2 == 0);

						if (chatEntry != null) {
							if (chatEntry.IsAdHocGroupChat ()) {
								currVH.HideThumbnailProgressIndicator (); // since this is an adhoc group, we shouldn't show the progress indicator
							} else {
								currVH.PossibleShowProgressIndicator (chatEntry.FirstContactCounterParty);
							}
						}

						break;
					}
				case InboxItemType.UndoState:
					{
						UndoViewHolder oHolder = holder as UndoViewHolder;
						oHolder.ItemView.LayoutParameters.Height = this.RowHeight;
						break;
					}
				}
			}

			public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType) {
				InboxItemType type = (InboxItemType)viewType;
				switch (type) {
				default:
				case InboxItemType.Regular:
					{
						ViewHolder holder = ViewHolder.NewInstance (parent, OnClick, OnLongClick, inboxFragment);
						return holder;
					}
				case InboxItemType.UndoState:
					{
						UndoViewHolder holder = UndoViewHolder.NewInstance (parent, DeleteTapped, UndoTapped);
						return holder;
					}
				}
			}

			public override int ItemCount {
				get {
					return inboxFragment.chatList == null ? 0 : inboxFragment.commonInbox.viewModel.Count;
				}
			}

			public override int GetItemViewType (int position) {
				if (this.RowsWithUndoState.Contains (position)) {
					return (int)InboxItemType.UndoState;
				} else {
					return (int)InboxItemType.Regular;
				}
			}

			#endregion
		}


		class ViewHolder: RecyclerView.ViewHolder {
			InboxFragment inboxFragment;

			public TextView ChatItemPreview { get; set; }

			public TextView ChatItemPreviewDate { get; set; }

			public TextView ChatItemFrom { get; set; }

			public ImageView UnreadImageView { get; set; }

			public ImageView ThumbnailView { get; set; }

			public ImageView PhotoFrame { get; set; }

			public ImageView AliasIcon { get; set; }

			public View ColorTrimView { get; set; }

			public ProgressBar ProgressBar { get; set; }

			public ImageView AkaMaskIcon { get; set; }

			private bool isNotificationBanner;
			public bool IsNotificationBanner {
				get { return isNotificationBanner; }
				set {
					isNotificationBanner = value;
					if (value) {
						this.UnreadImageView.Alpha = 0;
						this.ChatItemPreviewDate.Alpha = 0;
					} else {
						this.UnreadImageView.Alpha = 1;
						this.ChatItemPreviewDate.Alpha = 1;
					}
				}
			}

			public static ViewHolder NewInstance (ViewGroup parent, Action<int> itemClick, Action<int> longClick, InboxFragment fragment) {
				View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.inbox_entry, parent, false);
				ViewHolder holder = new ViewHolder (view, itemClick, longClick, fragment);
				return holder;
			}

			public ViewHolder (View view, Action<int> itemClick, Action<int> longClick, InboxFragment fragment) : base (view) {
				inboxFragment = fragment;

				this.ChatItemPreview = view.FindViewById<TextView> (Resource.Id.chatItemPreview);
				this.ChatItemPreviewDate = view.FindViewById<TextView> (Resource.Id.previewDate);
				this.ChatItemFrom = view.FindViewById<TextView> (Resource.Id.chatItemFrom);
				this.UnreadImageView = view.FindViewById<ImageView> (Resource.Id.unreadImageView);
				this.ThumbnailView = view.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				this.PhotoFrame = view.FindViewById<ImageView> (Resource.Id.photoFrame);
				this.AliasIcon = view.FindViewById<ImageView> (Resource.Id.aliasIcon);
				this.AliasIcon.Visibility = ViewStates.Invisible;
				this.ColorTrimView = view.FindViewById<View> (Resource.Id.trimView);
				this.ProgressBar = view.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
				this.AkaMaskIcon = view.FindViewById<ImageView> (Resource.Id.akaMaskIcon);

				this.ChatItemPreview.Typeface = FontHelper.DefaultFont;
				this.ChatItemPreviewDate.Typeface = FontHelper.DefaultFont;
				this.ChatItemFrom.Typeface = FontHelper.DefaultBoldFont;

				view.Click += (object sender, EventArgs e) => {
					itemClick (base.AdapterPosition);
				};

				view.LongClick += (object sender, View.LongClickEventArgs e) => {
					longClick (base.AdapterPosition);
				};
			}

			public void PossibleShowProgressIndicator (CounterParty c) {
				if (BitmapSetter.ShouldShowProgressIndicator (c)) {
					ShowThumbnailProgressIndicator ();
				} else {
					HideThumbnailProgressIndicator ();
				}
			}

			public void ShowThumbnailProgressIndicator () {
				this.ProgressBar.Visibility = ViewStates.Visible;
				this.PhotoFrame.Visibility = ViewStates.Invisible;
				this.ThumbnailView.Visibility = ViewStates.Invisible;
			}

			public void HideThumbnailProgressIndicator () {
				this.ProgressBar.Visibility = ViewStates.Gone;
				this.PhotoFrame.Visibility = ViewStates.Visible;
				this.ThumbnailView.Visibility = ViewStates.Visible;
			}

			public void SetChatEntry (ChatEntry chatEntry) {
				SetChatEntryPreviewUpdate (chatEntry, false);
				SetChatEntryNameUpdate (chatEntry, false);
				SetChatEntryColorThemeUpdate (chatEntry, false);
				SetChatEntryThumbnailImage (chatEntry, false);
				SetChatEntryAliasIcon (chatEntry);
			}

			public void SetChatEntryAliasIcon (ChatEntry chatEntry) {
				var account = EMApplication.GetInstance ().appModel.account.accountInfo;
				if (chatEntry == null) {
					this.AkaMaskIcon.Visibility = ViewStates.Gone;
					this.AliasIcon.Visibility = ViewStates.Invisible;
					return;
				} else if (chatEntry.fromAlias == null || account.AliasFromServerID (chatEntry.fromAlias) == null) {
					this.AliasIcon.Visibility = ViewStates.Invisible;
				} else {
					var alias = account.AliasFromServerID (chatEntry.fromAlias);
					AliasIcon.Visibility = ViewStates.Visible;
					BitmapSetter.SetImage (alias.iconMedia, inboxFragment.Resources, AliasIcon, Resource.Drawable.Icon, 100);
				}

				if (chatEntry.HasAKAContact) {
					SetChatEntryAKAMask (chatEntry);
					this.AkaMaskIcon.Visibility = ViewStates.Visible;
				} else {
					this.AkaMaskIcon.Visibility = ViewStates.Gone;
				}
			}

			public void SetChatEntryPreviewUpdate (ChatEntry chatEntry, bool animated) {
				if (animated) {
					if (chatEntry == null) {
						ChatItemPreview.Text = "";
						ChatItemPreviewDate.Text = "";
						UnreadImageView.Alpha = 0;
					} else {
						if (!ChatItemPreview.Text.Equals (chatEntry.preview)) {
							ChatItemPreview.Animate ()
									   .SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
									   .Alpha (0)
									   .WithEndAction (new Java.Lang.Runnable (() => {
								ChatItemPreview.Text = chatEntry.preview;
								ChatItemPreview.Animate ()
														   .SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
											   			   .Alpha (1);
							}));
						}

						string formattedDate = chatEntry.FormattedPreviewDate;
						if (!ChatItemPreviewDate.Text.Equals (formattedDate)) {
							ChatItemPreviewDate.Animate ()
							.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
							.Alpha (0)
							.WithEndAction (new Java.Lang.Runnable (() => {
								ChatItemPreviewDate.Text = formattedDate;
								ChatItemPreviewDate.Animate ()
												   .SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
												   .Alpha (1);
							}));
						}

						bool hasReadShowing = UnreadImageView.Alpha == 1;
						if (hasReadShowing != chatEntry.hasUnread)
							UnreadImageView.Animate ()
									   .SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
									   .Alpha (chatEntry.hasUnread ? 1 : 0);
					}
				} else {
					ChatItemPreview.Text = chatEntry == null ? "" : chatEntry.preview;
					ChatItemPreviewDate.Text = chatEntry == null ? "" : chatEntry.FormattedPreviewDate;
					UnreadImageView.Alpha = (chatEntry != null && chatEntry.hasUnread) ? 1 : 0;
				}
			}

			public void SetChatEntryNameUpdate (ChatEntry chatEntry, bool animated) {
				if (animated) {
					if (chatEntry == null)
						ChatItemFrom.Text = "";
					else {
						if (!ChatItemFrom.Text.Equals (chatEntry.ContactsLabel)) {
							ChatItemFrom.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (0)
								.WithEndAction (new Java.Lang.Runnable (() => {
								ChatItemFrom.Text = chatEntry.ContactsLabel;
								ChatItemFrom.Animate ()
										.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
										.Alpha (1);
							}));
						}
					}
				} else
					ChatItemFrom.Text = chatEntry == null ? "" : chatEntry.ContactsLabel;
			}

			public void SetChatEntryThumbnailImage (ChatEntry chatEntry, bool animated) {
				BackgroundColor colorTheme = chatEntry == null ? BackgroundColor.Default : chatEntry.IncomingColorTheme;
				if (animated) {
					PhotoFrame.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0);
					ThumbnailView.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0)
						.WithEndAction (new Java.Lang.Runnable (() => {
						if (chatEntry != null && chatEntry.contacts.Count > 0) {
							if (chatEntry.IsAdHocGroupChat ()) {
								BitmapRequest request = BitmapRequest.From (this, null, this.ThumbnailView, Resource.Drawable.EMUserImage, inboxFragment.ThumbnailSizePixels, inboxFragment.Resources);
								BitmapSetter.SetThumbnailImage (request);
								HideThumbnailProgressIndicator (); // since this is an adhoc group, we shouldn't show the progress indicator
							} else {
								BitmapRequest request = BitmapRequest.From (this, chatEntry.FirstContactCounterParty, this.ThumbnailView, Resource.Drawable.userDude, inboxFragment.ThumbnailSizePixels, inboxFragment.Resources);
								BitmapSetter.SetThumbnailImage (request);
								PossibleShowProgressIndicator (chatEntry.FirstContactCounterParty);
							}

							PhotoFrame.Animate ()
									.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
									.Alpha (1);
							ThumbnailView.Animate ()
									.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
									.Alpha (1);
						}

					}));
				} else {
					colorTheme.GetPhotoFrameLeftResource ((string file) => {
						if (PhotoFrame != null && inboxFragment != null) {
							BitmapSetter.SetBackgroundFromFile (PhotoFrame, inboxFragment.Resources, file);
						}
						if (chatEntry != null && chatEntry.contacts.Count > 0) {
							if (chatEntry.IsAdHocGroupChat ()) {
								BitmapRequest request = BitmapRequest.From (this, null, this.ThumbnailView, Resource.Drawable.EMUserImage, inboxFragment.ThumbnailSizePixels, inboxFragment.Resources);
								BitmapSetter.SetThumbnailImage (request);
								HideThumbnailProgressIndicator (); // since this is an adhoc group, we shouldn't show the progress indicator
							} else {
								BitmapRequest request = BitmapRequest.From (this, chatEntry.FirstContactCounterParty, this.ThumbnailView, Resource.Drawable.userDude, inboxFragment.ThumbnailSizePixels, inboxFragment.Resources);
								BitmapSetter.SetThumbnailImage (request);
								PossibleShowProgressIndicator (chatEntry.FirstContactCounterParty);
							}
						}
					});
				}
			}

			public void SetChatEntryColorThemeUpdate (ChatEntry chatEntry, bool animated) {
				BackgroundColor colorTheme = chatEntry == null ? BackgroundColor.Default : chatEntry.IncomingColorTheme;
				if (animated) {
					PhotoFrame.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0);
					ChatItemFrom.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0);
					ColorTrimView.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0)
						.WithEndAction (new Java.Lang.Runnable (() => {
						colorTheme.GetPhotoFrameLeftResource ((string filepath) => {
							if (PhotoFrame != null && inboxFragment != null) {
								PhotoFrame.SetBackgroundDrawable (Drawable.CreateFromPath (filepath));
							}
						});
						ChatItemFrom.SetTextColor (colorTheme.GetColor ());
						ColorTrimView.SetBackgroundColor (colorTheme.GetColor ());

						PhotoFrame.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (1);
						ChatItemFrom.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (1);
						ColorTrimView.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (1);
					}));
				} else {
					colorTheme.GetPhotoFrameLeftResource ((string file) => {
						if (PhotoFrame != null && inboxFragment != null) {
							BitmapSetter.SetBackgroundFromFile (PhotoFrame, inboxFragment.Resources, file);
						}
					});
					ChatItemFrom.SetTextColor (colorTheme.GetColor ());
					ColorTrimView.SetBackgroundColor (colorTheme.GetColor ());
				}
			}

			public void SetChatEntryAKAMask (ChatEntry chatEntry) {
				if (chatEntry.HasAKAContact) {
					chatEntry.IncomingColorTheme.GetAkaMaskResource ((string filepath) => {
						if (this.AkaMaskIcon != null && inboxFragment != null) {
							BitmapSetter.SetImageFromFile (this.AkaMaskIcon, inboxFragment.Resources, filepath);
						}	
					});
				}
			}

			public void SetEven (bool isEven) {
				BasicRowColorSetter.SetEven (isEven, this.ItemView);
			}
		}

		class CommonInbox : AbstractInBoxController {
			private WeakReference _r = null;

			private InboxFragment Self {
				get { return this._r != null ? this._r.Target as InboxFragment : null; }
				set { this._r = new WeakReference (value); }
			}

			public CommonInbox (InboxFragment fragment) : base (EMApplication.GetInstance ().appModel.chatList) {
				this.Self = fragment;

				ActiveBanners = new LinkedList<View> ();
				TimerStrongRefs = new Dictionary<int, System.Threading.Timer> ();
			}

			public override void GoToChatEntry (ChatEntry chatEntry) {
				InboxFragment self = this.Self;
				if (chatEntry != null && self != null && self.commonInbox != null) {
					
					int position = self.commonInbox.chatList.entries.IndexOf (chatEntry);
					if (position != -1) {
						ChatFragment target = (ChatFragment)self.Activity.FragmentManager.FindFragmentByTag ("chatEntry" + chatEntry.chatEntryID);
						if (target != null) {
							int backstackCount = self.Activity.FragmentManager.BackStackEntryCount;
							for (int i = 0; i < backstackCount; i++) {
								FragmentManager.IBackStackEntry entry = self.Activity.FragmentManager.GetBackStackEntryAt (i);
								System.Diagnostics.Debug.WriteLine ("Name: " + entry.Name + " ; ID: " + entry.Id);
							}
							self.Activity.FragmentManager.PopBackStack ("chatEntry" + chatEntry.chatEntryID, PopBackStackFlags.None);
						} else {
							ChatFragment fragment = new ChatFragment ();
							Bundle args = new Bundle ();
							args.PutInt ("Position", position);
							fragment.Arguments = args;
							self.Activity.FragmentManager.BeginTransaction ()
								.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
								.Replace (Resource.Id.content_frame, fragment, "chatEntry" + chatEntry.chatEntryID)
								.AddToBackStack ("chatEntry" + chatEntry.chatEntryID)
								.Commit ();
						}
					}
				}
			}

			public override void HandleUpdatesToChatList (IList<ModelStructureChange<ChatEntry>> repositionChatItems, IList<ModelAttributeChange<ChatEntry,object>> previewUpdates, bool animated, Action callback) {
				InboxFragment self = this.Self;
				if (GCCheck.ViewGone (self)) {
					callback ();
					return;
				}

				RecyclerView listView = self.listView;
				InboxChatEntryAdapter adapter = (InboxChatEntryAdapter)listView.GetAdapter ();
				LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();
				if (!animated || !listView.IsShown) {
					adapter.NotifyDataSetChanged ();
					callback ();
				} else {
					if (repositionChatItems == null || repositionChatItems.Count == 0) {
						HandleAttributeUpdates (previewUpdates, callback);
					} else {
						foreach (ModelStructureChange<ChatEntry> ins in repositionChatItems) {
							if (ins.Change == ModelStructureChange.added) {
								adapter.NotifyItemInserted (viewModel.IndexOf (ins.ModelObject));
								layoutMgr.ScrollToPosition (0);
							} else if (ins.Change == ModelStructureChange.deleted) {
								adapter.NotifyItemRemoved (ins.Index);
							} else {
								adapter.NotifyItemMoved (ins.Index, 0);
								layoutMgr.ScrollToPosition (0);
							}
						}

						StartBackgroundCorrectionAnimations ();

						// This here is adding a listener to the listView animation. We use this to correct the colors after the rows have been moved/updated/added/etc.
						// We dispatch here so that the listView gets to start its animations.
						// If we don't dispatch, this returns immediately.
						// https://developer.android.com/reference/android/support/v7/widget/RecyclerView.ItemAnimator.html#isRunning%28android.support.v7.widget.RecyclerView.ItemAnimator.ItemAnimatorFinishedListener%29
						WeakReference listViewRef = new WeakReference (listView);
						WeakReference thisRef = new WeakReference (this);
						listView.PostDelayed (() => {
							RecyclerView listView2 = listViewRef.Target as RecyclerView;
							if (!GCCheck.Gone (listView2)) {
								callback ();
								return;
							}

							listView2.GetItemAnimator ().InvokeIsRunning (RecyclerAnimationListener.FromCallback (() => {
								CommonInbox this2 = thisRef.Target as CommonInbox;
								if (this2 == null) {
									callback ();
								} else {
									this2.HandleAttributeUpdates (previewUpdates, callback);
								}
							}));
						}, Android_Constants.DELAY_BEFORE_CHECKING_LISTVIEW_ANIMATION_RUNNING);
					}
				}
			}

			protected void HandleAttributeUpdates (IList<ModelAttributeChange<ChatEntry,object>> previewUpdates, Action callback) {
				InboxFragment self = this.Self;
				if (GCCheck.ViewGone (self)) {
					callback ();
					return;
				}

				RecyclerView listView = self.listView;
				LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();

				if (previewUpdates != null && previewUpdates.Count > 0) {
					foreach (ModelAttributeChange<ChatEntry,object> changeInstruction in previewUpdates) {

						ChatEntry chatEntry = changeInstruction.ModelObject;
						int position = viewModel.IndexOf (chatEntry);

						int firstVisiblePosition = layoutMgr.FindFirstVisibleItemPosition ();
						int lastVisiblePosition = layoutMgr.FindLastVisibleItemPosition ();

						if (position >= firstVisiblePosition && position <= lastVisiblePosition) {
							ViewHolder viewHolder = (ViewHolder)listView.FindViewHolderForAdapterPosition (position);

							if (changeInstruction.AttributeName.Equals (CHATENTRY_THUMBNAIL) && !chatEntry.IsAdHocGroupChat ()) {
								viewHolder.SetChatEntryThumbnailImage (chatEntry, true && !chatEntry.IsAdHocGroupChat ());
							}

							if (changeInstruction.AttributeName.Equals (CHATENTRY_NAME)) {
								viewHolder.SetChatEntryNameUpdate (chatEntry, true);
							}

							if (changeInstruction.AttributeName.Equals (CHATENTRY_PREVIEW)) {
								viewHolder.SetChatEntryPreviewUpdate (chatEntry, true);
							}

							if (changeInstruction.AttributeName.Equals (CHATENTRY_COLOR_THEME)) {
								viewHolder.SetChatEntryAKAMask (chatEntry);
								viewHolder.SetChatEntryColorThemeUpdate (chatEntry, true);
							}
						}
					}
				}

				callback ();
			}

			// After moving rows around the odd/even backgrounds can be mismatched
			// we run down through the visible rows and correct the backgrounds
			protected void StartBackgroundCorrectionAnimations () {
				InboxFragment self = this.Self;
				if (GCCheck.ViewGone (self))
					return;
				LinearLayoutManager layoutMgr = self.listView.GetLayoutManager () as LinearLayoutManager;
				int firstPos = layoutMgr.FindFirstVisibleItemPosition ();
				int lastPos = layoutMgr.FindLastVisibleItemPosition ();
				ContinueBackgroundCorrectionAnimations (firstPos, lastPos);
			}

			protected void ContinueBackgroundCorrectionAnimations (int currentPos, int lastPos) {
				InboxFragment self = this.Self;
				if (GCCheck.ViewGone (self))
					return;

				ViewHolder holder = (ViewHolder)self.listView.FindViewHolderForAdapterPosition (currentPos);
				if (holder != null) {
					holder.SetEven (currentPos % 2 == 0);
				}

				if (++currentPos <= lastPos) {
					new Timer ((object o) => {
						EMTask.PerformOnMain (() => { 
							ContinueBackgroundCorrectionAnimations (currentPos, lastPos);
						});
					}, null, Constants.ODD_EVEN_BACKGROUND_COLOR_CORRECTION_PAUSE, Timeout.Infinite);
				}
			}

			public override void DidChangeColorTheme () {
				InboxFragment self = this.Self;
				if (GCCheck.ViewGone (self))
					return;
				self.ThemeController ();
				self.listAdapter.NotifyDataSetChanged ();
			}

			public override void UpdateTitleProgressIndicatorVisibility () {
				EMTask.DispatchMain (() => {
					InboxFragment self = this.Self;
					if (GCCheck.ViewGone (self))
						return;
					self.UpdateTitleProgress ();
				});
			}

			public override void UpdateBurgerUnreadCount (int unreadCount) {
				EMTask.DispatchMain (() => {
					InboxFragment self = this.Self;
					if (GCCheck.ViewGone (self))
						return;

					if (unreadCount == 0) {
						self.BurgerUnreadIcon.Visibility = ViewStates.Gone;
					} else {
						self.BurgerUnreadIcon.Visibility = ViewStates.Visible;
					}

					self.BurgerUnreadIcon.Text = unreadCount.ToString ();
				});
			}

			private LinkedList<View> ActiveBanners { get; set; }
			private Dictionary<int, System.Threading.Timer> TimerStrongRefs { get; set; }
			private static Random rgen;
			public static Random RGen { 
				get {
					if (rgen == null) {
						rgen = new Random ();
					}
					return rgen;
				}
			}
			private static string RANDOM_INT_KEY = "RandomTimerString";
			private static string BANNER_VIEW_KEY = "BannerView";

			public override void ShowNotificationBanner (ChatEntry entry) {
				InboxFragment self = this.Self;
				if (self == null || self.IsVisible)
					return;
				
				Fragment chatFragment = self.Activity.FragmentManager.FindFragmentByTag ("chatEntry" + entry.chatEntryID);
				if (chatFragment == null || !chatFragment.IsVisible) {
					EMTask.DispatchMain (() => {
						if (this.ActiveBanners.Count > 20) {
							View bootedView = this.ActiveBanners.First.Value;
							this.ActiveBanners.RemoveFirst ();
							((ViewGroup)bootedView.Parent).RemoveView (bootedView);
						}

						ViewHolder holder = ViewHolder.NewInstance ((ViewGroup)self.Activity.FindViewById (Resource.Id.content_frame), (int i) => {
						}, (int i) => {
						}, self);
						holder.SetChatEntry (entry);
						holder.IsNotificationBanner = true;
						View bannerView = holder.ItemView;

						FrameLayout.LayoutParams layoutParams = (FrameLayout.LayoutParams)bannerView.LayoutParameters;
						layoutParams.Gravity = GravityFlags.Top | GravityFlags.Left;
						bannerView.OffsetLeftAndRight (0);
						bannerView.OffsetTopAndBottom (0);
						bannerView.LayoutParameters = layoutParams;
						bannerView.Click += (object sender, EventArgs e) => {
							while (this.ActiveBanners.Count > 0) {
								View activeView = this.ActiveBanners.First.Value;
								if (this.ActiveBanners.Count == 1) {
									Animation fadeOutAnimation = AnimationUtils.LoadAnimation (self.Activity, Resource.Animation.FadeOut);
									((ViewGroup)activeView).StartAnimation (fadeOutAnimation);
								}
								this.ActiveBanners.RemoveFirst ();
								((ViewGroup)activeView.Parent).RemoveView (activeView);
							}
							self.GoToChatEntry (entry);	
						};
						int randomInt = RGen.Next ();
						while (TimerStrongRefs.ContainsKey (randomInt)) {
							randomInt = RGen.Next ();
						}
						Dictionary<string, object> state = new Dictionary<string, object> ();
						state.Add (BANNER_VIEW_KEY, bannerView);
						state.Add (RANDOM_INT_KEY, randomInt);
						System.Threading.Timer hideBannerTimer = new System.Threading.Timer (new TimerCallback (HideBanner), state, 2500, Timeout.Infinite);
						TimerStrongRefs.Add (randomInt, hideBannerTimer);
						this.ActiveBanners.AddLast (bannerView);
						Animation animation = AnimationUtils.LoadAnimation (self.Activity, Resource.Animation.FadeIn);
						self.Activity.AddContentView (bannerView, bannerView.LayoutParameters);
						((ViewGroup)bannerView).StartAnimation (animation);
					});
				}
			}

			private void HideBanner (object state) {
				InboxFragment self = this.Self;
				if (self == null || self.IsVisible)
					return;
				Dictionary<string, object> stateDict = (Dictionary<string, object>)state;
				View bannerView = (View)stateDict [BANNER_VIEW_KEY];
				int timerKey = (int)stateDict [RANDOM_INT_KEY];
				EMTask.ExecuteNowIfMainOrDispatchMain (() => {
					if (this.ActiveBanners.Contains (bannerView)) {
						this.ActiveBanners.Remove (bannerView);
						if (this.ActiveBanners.Count == 0) {
							Animation animation = AnimationUtils.LoadAnimation (self.Activity, Resource.Animation.FadeOut);
							((ViewGroup)bannerView).StartAnimation (animation);
						}
						((ViewGroup)bannerView.Parent).RemoveView (bannerView);
						TimerStrongRefs.Remove (timerKey);
					}
				});
			}
		}
	}
}