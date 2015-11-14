using System;
using System.Collections.Generic;
using System.Threading;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Util;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class NotificationsFragment : Fragment {

		NotificationEntryAdapter listAdapter;
		NotificationList notificationList;

		private HiddenReference<CommonNotification> _shared;
		private CommonNotification commonNotification {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<CommonNotification> (value); }
		}

		RecyclerView listView;
		const int DELETE_ENTRY = 0;
		const int ADD_ENTRY = 1;
		const int MOVE_ENTRY = 2;

		private HiddenReference<ApplicationModel> _appModel;
		private ApplicationModel appModel {
			get { return this._appModel != null ? this._appModel.Value : null; }
			set { this._appModel = new HiddenReference<ApplicationModel> (value); }
		}

		AccountInfo accountInfo;

		#region UI
		Button leftBarButton;
		LinearLayout buttonTab;
		Button readButton;
		Button unreadButton;
		Button allButton;

		int? selectedTabResourceId = null;
		#endregion

		public static NotificationsFragment NewInstance () {
			return new NotificationsFragment ();
		}

		public int ThumbnailSizePixels { get; set; }

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			DisplayMetrics displayMetrics = Resources.DisplayMetrics;
			ThumbnailSizePixels = (int) (Android_Constants.ROUNDED_THUMBNAIL_SIZE / displayMetrics.Density);

			this.commonNotification = new CommonNotification (this);

			appModel = EMApplication.GetInstance ().appModel;
			accountInfo = appModel.account.accountInfo;
			notificationList = appModel.notificationList;

			if (selectedTabResourceId == null)
				selectedTabResourceId = Resource.Id.AllButton;
		}

		public override void OnResume () {
			base.OnResume ();

			if (listAdapter != null)
				listAdapter.NotifyDataSetChanged ();
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.notifications, container, false);
			ThemeController (v);

			if (notificationList != null) {
				switch (selectedTabResourceId.Value) {
				case Resource.Id.ReadButton:
					notificationList.ShowReadNotifications ();
					break;
				case Resource.Id.UnreadButton:
					notificationList.ShowUnreadNotifications ();
					break;
				default:
				case Resource.Id.AllButton:
					notificationList.ShowAllNotifications ();
					break;
				}

				if (listAdapter != null)
					listAdapter.NotifyDataSetChanged ();
			}

			return v;
		}

		public void ThemeController () {
			ThemeController (this.View);
		}

		public void ThemeController (View v) {
			// v should be the NotificationFragment's View.
			// It can be null when the NotificationFragment is not on screen.
			if (this.IsAdded && v != null) {
				BackgroundColor colorTheme = accountInfo.colorTheme;
				colorTheme.GetBackgroundResource ((string file) => {
					if (v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
					}
				});
				if(buttonTab != null)
					buttonTab.SetBackgroundColor (colorTheme.GetColor ());
			}
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			View.FindViewById<TextView> (Resource.Id.titleTextView).Text = "NOTIFICATIONS_TITLE".t ();
			View.FindViewById<TextView> (Resource.Id.titleTextView).Typeface = FontHelper.DefaultFont;

			leftBarButton = View.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += (sender, e) => FragmentManager.PopBackStack ();
			leftBarButton.Typeface = FontHelper.DefaultFont;
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			buttonTab = View.FindViewById<LinearLayout> (Resource.Id.TabButtonView);
			buttonTab.SetBackgroundColor (accountInfo.colorTheme.GetColor ());

			readButton = View.FindViewById<Button> (Resource.Id.ReadButton);
			readButton.Typeface = FontHelper.DefaultFont;
			readButton.SetBackgroundColor (Color.Transparent);

			readButton.Click += (sender, e) => {
				if(!readButton.Selected) {
					selectedTabResourceId = Resource.Id.ReadButton;

					readButton.Selected = true;
					readButton.Typeface = FontHelper.DefaultBoldFont;

					unreadButton.Selected = false;
					unreadButton.Typeface = FontHelper.DefaultFont;
					allButton.Selected = false;
					allButton.Typeface = FontHelper.DefaultFont;

					notificationList.ShowReadNotifications ();

					listAdapter.NotifyDataSetChanged ();
				}
			};

			unreadButton = View.FindViewById<Button> (Resource.Id.UnreadButton);
			unreadButton.Typeface = FontHelper.DefaultFont;
			unreadButton.SetBackgroundColor (Color.Transparent);
			unreadButton.Click += (sender, e) => {
				if(!unreadButton.Selected) {
					selectedTabResourceId = Resource.Id.UnreadButton;

					unreadButton.Selected = true;
					unreadButton.Typeface = FontHelper.DefaultBoldFont;

					readButton.Selected = false;
					readButton.Typeface = FontHelper.DefaultFont;
					allButton.Selected = false;
					allButton.Typeface = FontHelper.DefaultFont;

					notificationList.ShowUnreadNotifications ();

					listAdapter.NotifyDataSetChanged ();
				}
			};

			allButton = View.FindViewById<Button> (Resource.Id.AllButton);
			allButton.Typeface = FontHelper.DefaultFont;
			allButton.SetBackgroundColor (Color.Transparent);
			allButton.Click += (sender, e) => {
				if(!allButton.Selected) {
					selectedTabResourceId = Resource.Id.AllButton;

					allButton.Selected = true;
					allButton.Typeface = FontHelper.DefaultBoldFont;

					readButton.Selected = false;
					readButton.Typeface = FontHelper.DefaultFont;
					unreadButton.Selected = false;
					unreadButton.Typeface = FontHelper.DefaultFont;

					notificationList.ShowAllNotifications ();

					listAdapter.NotifyDataSetChanged ();
				}
			};

			View.FindViewById<Button> (selectedTabResourceId.Value).Selected = true;
			View.FindViewById<Button> (selectedTabResourceId.Value).Typeface = FontHelper.DefaultBoldFont;

			listView = View.FindViewById<RecyclerView> (Resource.Id.NotificationsList); // get reference to the ListView in the layout

			listAdapter = new NotificationEntryAdapter(this);
			listAdapter.ItemClick += DidTapItem;

			LinearLayoutManager layoutMgr = new LinearLayoutManager (this.Activity);
			layoutMgr.StackFromEnd = false;
			listView.SetLayoutManager (layoutMgr);
			listView.AddItemDecoration (new SimpleDividerItemDecoration (this.Activity, useFullWidthLine: true, shouldDrawBottomLine: false));

			EmItemTouchHelper g = new EmItemTouchHelper (listAdapter, 0, ItemTouchHelper.Left | ItemTouchHelper.Right);
			ItemTouchHelper hel = new ItemTouchHelper (g);

			hel.AttachToRecyclerView (listView);

			listView.SetAdapter (listAdapter);

			AnalyticsHelper.SendView ("Notification View");
		}

		public override void OnDestroy() {
			if (commonNotification != null)
				commonNotification.Dispose ();
			
			base.OnDestroy ();
		}

		protected void DidTapItem(object sender, int e) {
			NotificationEntry notificationEntry = notificationList.Entries [e];
			notificationList.MarkNotificationEntryReadAsync (notificationEntry);

			var fragment = NotificationsWebFragment.NewInstance (notificationEntry);

			Activity.FragmentManager.BeginTransaction ()
				.SetTransition (FragmentTransit.FragmentOpen)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit();

			if (unreadButton.Selected) {
				notificationList.ShowUnreadNotifications ();
				listAdapter.NotifyDataSetChanged ();
			}
		}

		class NotificationEntryAdapter : EmUndoRecyclerAdapter {

			enum NotificationItemType {
				Regular = 0,
				UndoState = 1
			}

			public NotificationsFragment notificationsFragment { get; set; }

			public NotificationEntryAdapter(NotificationsFragment fragment) {
				notificationsFragment = fragment;
			}

			protected override void DeleteTapped (int row) {
				// Since they're an async remove here, we don't call base.DeleteTapped.
				// When the remove finally finishes, it'll properly update through the HandleUpdates call.
				this.RowsWithUndoState.Remove (row);
				NotificationList notificationList = notificationsFragment.notificationList;
				notificationList.RemoveNotificationEntryAtAsync (row);
			}

			#region implemented abstract members of Adapter
			public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position) {

				NotificationItemType viewType = (NotificationItemType)this.GetItemViewType (position);
				switch (viewType) {
				default:
				case NotificationItemType.Regular:
					{
						ViewHolder currVH = holder as ViewHolder;
						NotificationEntry notificationEntry = notificationsFragment.notificationList == null ? null : notificationsFragment.notificationList.UIEntries [position];

						currVH.Notification = notificationEntry;
						currVH.SetEven (position % 2 == 0);

						this.RowHeight = currVH.ItemView.LayoutParameters.Height;

						if (notificationEntry != null) {
							if (!notificationEntry.Title.Equals ("NOTIFICATION_WELCOME_TITLE".t ())) {
								currVH.PossibleShowProgressIndicator (notificationEntry.counterparty);
							} else {
								currVH.ProgressBar.Visibility = ViewStates.Gone;
							}
						}

						break;
					}
				case NotificationItemType.UndoState: 
					{
						UndoViewHolder oHolder = holder as UndoViewHolder;
						oHolder.ItemView.LayoutParameters.Height = this.RowHeight;
						break;
					}
				}
			}

			public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType) {
				NotificationItemType type = (NotificationItemType)viewType;
				switch(type) {
				default:
				case NotificationItemType.Regular:
					{
						return ViewHolder.NewInstance (parent, OnClick, OnLongClick, notificationsFragment);
					}
				case NotificationItemType.UndoState:
					{
						UndoViewHolder holder = UndoViewHolder.NewInstance (parent, DeleteTapped, UndoTapped);
						return holder;
					}
				}
			}

			public override int ItemCount {
				get {
					if (notificationsFragment.notificationList == null || notificationsFragment.notificationList.UIEntries == null || notificationsFragment.notificationList.UIEntries.Count == 0)
						return 0;

					return notificationsFragment.notificationList == null ? 0 : notificationsFragment.notificationList.UIEntries.Count;
				}
			}

			public override int GetItemViewType (int position) {
				if (this.RowsWithUndoState.Contains (position)) {
					return (int)NotificationItemType.UndoState;
				} else {
					return (int)NotificationItemType.Regular;
				}
			}
			#endregion	
		}

		class ViewHolder: RecyclerView.ViewHolder {
			NotificationsFragment notificationFragment;
			public TextView NotificationItemPreview { get; set; }
			public TextView NotificationItemPreviewDate { get; set; }
			public ImageView UnreadNotificationView { get; set; }
			public ImageView ThumbnailView { get; set; }
			public ImageView PhotoFrame { get; set; }
			public View ColorTrimView { get; set; }
			public ProgressBar ProgressBar { get; set; }

			private NotificationEntry _entry = null;
			public NotificationEntry Notification {
				get { return this._entry; }
				set { 
					this._entry = value;
					SetNotificationEntry (this._entry);
				}
			}

			public static ViewHolder NewInstance (ViewGroup parent, Action<int> itemClick, Action<int> longClick, NotificationsFragment fragment) {
				View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.notification_entry, parent, false);
				ViewHolder holder = new ViewHolder (view, itemClick, longClick, fragment);
				return holder;
			}

			public ViewHolder(View view, Action<int> itemClick, Action<int> longClick, NotificationsFragment fragment) : base (view) {
				notificationFragment = fragment;

				this.NotificationItemPreview = view.FindViewById<TextView> (Resource.Id.notificationItemPreview);
				this.NotificationItemPreviewDate = view.FindViewById<TextView> (Resource.Id.previewDate);
				this.UnreadNotificationView = view.FindViewById<ImageView> (Resource.Id.unreadNotificationView);
				this.ThumbnailView = view.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				this.PhotoFrame = view.FindViewById<ImageView> (Resource.Id.photoFrame);
				this.ColorTrimView = view.FindViewById<View> (Resource.Id.trimView);
				this.ProgressBar = view.FindViewById<ProgressBar> (Resource.Id.ProgressBar);

				this.NotificationItemPreview.Typeface = FontHelper.DefaultFont;
				this.NotificationItemPreviewDate.Typeface = FontHelper.DefaultFont;

				view.Click += (object sender, EventArgs e) => {
					itemClick (base.AdapterPosition);
				};

				view.LongClick += (object sender, View.LongClickEventArgs e) => {
					longClick (base.AdapterPosition);
				};
			}

			public void PossibleShowProgressIndicator (CounterParty c) {
				if (BitmapSetter.ShouldShowProgressIndicator (c)) {
					ProgressBar.Visibility = ViewStates.Visible;
					PhotoFrame.Visibility = ViewStates.Invisible;
					ThumbnailView.Visibility = ViewStates.Invisible;
				} else {
					ProgressBar.Visibility = ViewStates.Gone;
					PhotoFrame.Visibility = ViewStates.Visible;
					ThumbnailView.Visibility = ViewStates.Visible;
				}
			}

			private void SetNotificationEntry (NotificationEntry notificationEntry) {
				SetNotificationEntryPreviewUpdate (notificationEntry, false);
				SetNotificationEntryColorThemeUpdate (notificationEntry, false);
				SetNotificationEntryThumbnailImage (notificationEntry, false);
			}

			public void SetNotificationEntryPreviewUpdate(NotificationEntry ne, bool animated) {
				if (animated) {
					if (ne == null) {
						NotificationItemPreview.Text = "";
						NotificationItemPreviewDate.Text = "";
						UnreadNotificationView.Alpha = 0;
					}
					else {
						if (!NotificationItemPreview.Text.Equals (ne.Title)) {
							NotificationItemPreview.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (0)
								.WithEndAction (new Java.Lang.Runnable (() => {
									NotificationItemPreview.Text = ne.Title;
									NotificationItemPreview.Animate ()
										.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
										.Alpha (1);
								}));
						}

						string formattedDate = ne.FormattedNotificationDate;
						if (!NotificationItemPreviewDate.Text.Equals (formattedDate)) {
							NotificationItemPreviewDate.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (0)
								.WithEndAction (new Java.Lang.Runnable (() => {
									NotificationItemPreviewDate.Text = formattedDate;
									NotificationItemPreviewDate.Animate ()
										.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
										.Alpha (1);
								}));
						}

						bool hasUnReadShowing = UnreadNotificationView.Alpha == 1;
						if (hasUnReadShowing != ne.Read)
							UnreadNotificationView.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (ne.Read ? 0 : 1);
					}
				}
				else {
					NotificationItemPreview.Text = ne == null ? "" : ne.Title;
					NotificationItemPreviewDate.Text = ne == null ? "" : ne.FormattedNotificationDate;
					UnreadNotificationView.Alpha = (ne != null && ne.Read) ? 0 : 1;
				}
			}

			public void SetNotificationEntryThumbnailImage(NotificationEntry ne, bool animated) {
				BackgroundColor colorTheme = ne == null || ne.counterparty == null ? BackgroundColor.Default : ne.counterparty.colorTheme;
				if (animated) {
					PhotoFrame.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0);
					ThumbnailView.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0)
						.WithEndAction (new Java.Lang.Runnable (() => {
							if (ne != null) {
								if(ne.counterparty == null && ne.Title.Equals("NOTIFICATION_WELCOME_TITLE".t ())) {
									BitmapRequest request = BitmapRequest.From (this, null, this.ThumbnailView, Resource.Drawable.EMUserImage, notificationFragment.ThumbnailSizePixels, notificationFragment.Resources);
									BitmapSetter.SetThumbnailImage (request);
								} else {
									BitmapRequest request = BitmapRequest.From (this, ne.counterparty, this.ThumbnailView, Resource.Drawable.userDude, notificationFragment.ThumbnailSizePixels, notificationFragment.Resources);
									BitmapSetter.SetThumbnailImage (request);
									PossibleShowProgressIndicator (ne.counterparty);
								}

								PhotoFrame.Animate ()
									.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
									.Alpha (1);
								ThumbnailView.Animate ()
									.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
									.Alpha (1);
							}

						}));
				}
				else {
					colorTheme.GetPhotoFrameLeftResource ( (string file) => {
						if (PhotoFrame != null && notificationFragment != null) {
							BitmapSetter.SetBackgroundFromFile (PhotoFrame, notificationFragment.Resources, file);
						}
					});
					if (ne != null) {
						if(ne.counterparty == null && ne.Title.Equals("NOTIFICATION_WELCOME_TITLE".t ())) {
							BitmapRequest request = BitmapRequest.From (this, null, this.ThumbnailView, Resource.Drawable.EMUserImage, notificationFragment.ThumbnailSizePixels, notificationFragment.Resources);
							BitmapSetter.SetThumbnailImage (request);
						} else {
							BitmapRequest request = BitmapRequest.From (this, ne.counterparty, this.ThumbnailView, Resource.Drawable.userDude, notificationFragment.ThumbnailSizePixels, notificationFragment.Resources);
							BitmapSetter.SetThumbnailImage (request);
							PossibleShowProgressIndicator (ne.counterparty);
						}
					}
				}
			}

			public void SetNotificationEntryColorThemeUpdate(NotificationEntry ne, bool animated) {
				BackgroundColor colorTheme = ne == null || ne.counterparty == null ? BackgroundColor.Default : ne.counterparty.colorTheme;
				if (animated) {
					PhotoFrame.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0);
					ColorTrimView.Animate ()
						.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
						.Alpha (0)
						.WithEndAction (new Java.Lang.Runnable (() => {
							colorTheme.GetPhotoFrameLeftResource ( (string file) => {
								if (notificationFragment != null && PhotoFrame != null) {
									PhotoFrame.SetBackgroundDrawable (Drawable.CreateFromPath (file));
								}
							});
							ColorTrimView.SetBackgroundColor (colorTheme.GetColor());

							PhotoFrame.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (1);
							ColorTrimView.Animate ()
								.SetDuration (Constants.FADE_ANIMATION_DURATION_MILLIS)
								.Alpha (1);
						}));
				} else {
					colorTheme.GetPhotoFrameLeftResource ( (string file) => {
						if (notificationFragment != null && PhotoFrame != null) {
							BitmapSetter.SetBackgroundFromFile (PhotoFrame, notificationFragment.Resources, file);
						}
					});
					ColorTrimView.SetBackgroundColor (colorTheme.GetColor());
				}
			}

			public void SetEven (bool isEven) {
				BasicRowColorSetter.SetEven (isEven, this.ItemView);
			}
		}

		class CommonNotification : AbstractNotificationController {
			private WeakReference _r = null;
			private NotificationsFragment Self {
				get { return this._r != null ? this._r.Target as NotificationsFragment : null; }
				set { this._r = new WeakReference (value); }
			}

			public CommonNotification (NotificationsFragment nf) : base (EMApplication.GetInstance ().appModel) {
				this.Self = nf;
			}

			public override void HandleUpdatesToNotificationList (IList<MoveOrInsertInstruction<NotificationEntry>> repositionItems, IList<ChangeInstruction<NotificationEntry>> previewUpdates, bool animated, Action<bool> callback) {
				NotificationsFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;

				if (!animated) {
					if (self.selectedTabResourceId == null)
						self.selectedTabResourceId = Resource.Id.AllButton;
					
					switch (self.selectedTabResourceId.Value) {
					case Resource.Id.UnreadButton:
						self.notificationList.ShowUnreadNotifications ();
						break;
					case Resource.Id.ReadButton:
						self.notificationList.ShowReadNotifications ();
						break;
					default:
					case Resource.Id.AllButton:
						self.notificationList.ShowAllNotifications ();
						break;
					}

					self.listAdapter.NotifyDataSetChanged ();
					callback (animated);
				} else {
					RecyclerView listView = self.listView;
					EmRecyclerViewAdapter adapter = listView.GetAdapter () as EmRecyclerViewAdapter;

					if (repositionItems.Count > 0) {
						if (self.selectedTabResourceId == null)
							self.selectedTabResourceId = Resource.Id.AllButton;
						
						foreach (MoveOrInsertInstruction<NotificationEntry> instruction in repositionItems) {
							switch (self.selectedTabResourceId.Value) {
							case Resource.Id.UnreadButton:
								if (instruction.Entry.Read)
									continue;
								break;
							case Resource.Id.ReadButton:
								if (!instruction.Entry.Read)
									continue;
								break;
							default:
							case Resource.Id.AllButton:
								
								break;
							}

							if (instruction.ToPosition == -1) {
								adapter.NotifyItemRemoved (instruction.FromPosition);
							} else if (instruction.FromPosition == -1) {
								adapter.NotifyItemInserted (instruction.ToPosition);
							} else {
								adapter.NotifyItemMoved (instruction.FromPosition, instruction.ToPosition);
							}
						}

						StartBackgroundCorrectionAnimations ();
					}

					foreach (ChangeInstruction<NotificationEntry> ins in previewUpdates) {
						NotificationEntry ne = ins.Entry;
						int position = this.notificationList.UIEntries.IndexOf (ne);
						LinearLayoutManager layoutMgr = listView.GetLayoutManager () as LinearLayoutManager;
						int firstVisible = layoutMgr.FindFirstVisibleItemPosition ();
						int lastVisible = layoutMgr.FindLastVisibleItemPosition ();

						if (position >= firstVisible && position <= lastVisible) {
							View v = listView.GetChildAt (position - firstVisible);
							ViewHolder viewHolder = (ViewHolder)listView.GetChildViewHolder (v);

							if (ins.PhotoChanged) {
								viewHolder.SetNotificationEntryThumbnailImage (ne, animated);
							}

							if (ins.ColorThemeChanged) {
								viewHolder.SetNotificationEntryColorThemeUpdate (ne, animated);
							}
						}
					}

					callback (animated);
				}
			}

			// After moving rows around the odd/even backgrounds can be mismatched
			// we run down through the visible rows and correct the backgrounds
			protected void StartBackgroundCorrectionAnimations () {
				NotificationsFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				LinearLayoutManager layoutMgr = self.listView.GetLayoutManager () as LinearLayoutManager;
				int firstPos = layoutMgr.FindFirstVisibleItemPosition ();
				int lastPos = layoutMgr.FindLastVisibleItemPosition ();
				ContinueBackgroundCorrectionAnimations (firstPos, lastPos);
			}

			protected void ContinueBackgroundCorrectionAnimations (int currentPos, int lastPos) {
				NotificationsFragment self = this.Self;
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
				
			public override void DidChangeColorTheme () {
				NotificationsFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.ThemeController ();
				self.listAdapter.NotifyDataSetChanged ();
			}
		}
	}
}