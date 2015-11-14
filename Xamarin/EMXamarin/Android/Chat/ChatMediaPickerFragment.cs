using System;
using System.Collections.Generic;
using Android.App;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

using em;
using EMXamarin;

namespace Emdroid {	

	public class ChatMediaEntry {
		private bool selected;
		public bool Selected {
			get { return selected; }
			set { selected = value; }
		}

		public string filepath { get; set; }

		public ChatMediaEntry (string file, bool isSelected) {
			this.filepath = file;
			this.Selected = isSelected;
		}
	}

	public class ChatMediaPickerFragment : Fragment {
		// Instance Variables
		public static string MEDIA_LIBRARY_STRING = "MEDIA_LIBRARY".t ();
		public const int MEDIA_LIBRARY_POS = 0;
		public static string TAKE_PHOTO_STRING = "TAKE_PHOTO".t ();
		public const int TAKE_PHOTO_POS = 1;
		public static string TAKE_VIDEO_STRING = "TAKE_VIDEO".t ();
		public const int TAKE_VIDEO_POS = 2;
		public static string WEB_SEARCH_STRING = "WEB_SEARCH".t ();
		public const int WEB_SEARCH_POS = 3;
		public static string CANCEL_STRING = "CANCEL_BUTTON".t ();
		public const int CANCEL_POS = 4;

		private List<ChatMediaEntry> images;
		public List<ChatMediaEntry> Images {
			get { return images; }
			set { images = value; }
		}

		private List<ChatMediaEntry> selectedImages;
		public List<ChatMediaEntry> SelectedImages {
			get { return selectedImages; }
			set { selectedImages = value; }
		}

		private AbstractAcquiresImagesFragment abstractAcquiresImagesFragment;
		public AbstractAcquiresImagesFragment AbstractAcquiresImagesFragment {
			get { return abstractAcquiresImagesFragment; }
			set { abstractAcquiresImagesFragment = value; }
		}

		private RecyclerView scrollView;
		public RecyclerView ScrollView { 
			get { return scrollView; } 
			set{ scrollView = value; }
		}
		public ChatMediaEntryAdapter ScrollViewAdapter { get; set; }

		private ChatMediaButtonListAdapter MenuButtonsAdapter { get; set; }
		private ListView menuButtons;
		public ListView MenuButtons {
			get { return menuButtons; }
			set { menuButtons = value; }
		}

		private Button dismissButton;
		public Button DismissButton {
			get { return dismissButton; }
			set { dismissButton = value; }
		}

		private bool showScroll;
		public bool ShowScroll {
			get { return showScroll; }
			set { showScroll = value; }
		}

		//Constructor
		public ChatMediaPickerFragment (AbstractAcquiresImagesFragment aaic, bool shouldShowScroll) {
			this.Images = getImages ();
			this.SelectedImages = new List<ChatMediaEntry> ();
			this.AbstractAcquiresImagesFragment = aaic;
			this.ShowScroll = shouldShowScroll;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			KeyboardUtil.HideKeyboard (this.Activity);

			Android.Content.Res.Orientation orientation = this.Resources.Configuration.Orientation;
			if (orientation.Equals (Android.Content.Res.Orientation.Landscape)) {
				Activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape;
				
			} else if (orientation.Equals (Android.Content.Res.Orientation.Portrait)) {
				Activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait;
			}
			View v = inflater.Inflate (Resource.Layout.chat_media_picker, container, false);
			this.ScrollView = v.FindViewById<RecyclerView> (Resource.Id.chatMediaScrollView);
			if (this.Images.Count == 0 || !this.ShowScroll) {
				this.ScrollView.Visibility = ViewStates.Gone;
			}
			this.DismissButton = v.FindViewById<Button> (Resource.Id.dismissButton);
			this.MenuButtons = v.FindViewById<ListView> (Resource.Id.chatMediaButtons);
			return v;
		}
			

		public void HandleSelectedUpdate () {
			int numberSelected = this.SelectedImages.Count;
			string updateString = string.Empty;
			if (numberSelected == 0) {
				updateString = "MEDIA_LIBRARY".t ();
			} else if (numberSelected == 1) {
				updateString = string.Format ("SEND_MEDIA_SINGULAR".t (), numberSelected);
			}
			else {
				updateString = string.Format ("SEND_MEDIA_MULTIPLE".t (), numberSelected);
			}
			this.MenuButtonsAdapter.UpdateFirstButton (updateString);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState); 

			this.ScrollViewAdapter = new ChatMediaEntryAdapter(this);
			LinearLayoutManager layoutMgr = new LinearLayoutManager (this.Activity);
			layoutMgr.Orientation = LinearLayoutManager.Horizontal;
			layoutMgr.StackFromEnd = false;
			this.ScrollView.SetLayoutManager (layoutMgr);
			this.ScrollView.AddItemDecoration (new SimpleDividerItemDecoration (this.Activity, useFullWidthLine: true, shouldDrawBottomLine: false));
			ScrollView.SetAdapter (ScrollViewAdapter);

			List<string> listOfButtons = new List<string> {
				MEDIA_LIBRARY_STRING,
				TAKE_PHOTO_STRING,
				TAKE_VIDEO_STRING,
				WEB_SEARCH_STRING,
				CANCEL_STRING
			};

			this.MenuButtons.ItemClick += ((object sender, AdapterView.ItemClickEventArgs e) => {
				e.View.SetBackgroundColor (Android.Graphics.Color.Aqua);
				EMTask.DispatchMain ( () => {
					e.View.SetBackgroundColor (Android.Graphics.Color.Transparent);
					HandleButtonClick (e.Position);
				});
			});

			this.MenuButtonsAdapter = new ChatMediaButtonListAdapter (EMApplication.GetMainContext (), Android.Resource.Layout.SimpleListItem1, listOfButtons);
			this.MenuButtons.Adapter = this.MenuButtonsAdapter;

			this.DismissButton.Click += ((object sender, EventArgs e) => {
				Activity.FragmentManager.PopBackStackImmediate ();
			});
		}

		public override void OnDestroy () {
			base.OnDestroy ();
			Activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Unspecified;
		}

		private void HandleButtonClick (int pos) {
			switch (pos) {
			case MEDIA_LIBRARY_POS:
				{
					int numSelected = this.SelectedImages.Count;
					Activity.FragmentManager.PopBackStackImmediate ();
					if (numSelected == 0) {
						MainActivity ma = Activity as MainActivity;
						this.AbstractAcquiresImagesFragment.LaunchMediaLibrary ();
						if (ma != null) {
							ma.LaunchingExternalActivity ();
						}
					} else {
						this.AbstractAcquiresImagesFragment.HandleBulkImages (this.SelectedImages);
					}
					break;
				}
			case TAKE_PHOTO_POS:
				{
					MainActivity ma = Activity as MainActivity;
					this.AbstractAcquiresImagesFragment.LaunchCamera ();
					if (ma != null) {
						ma.LaunchingExternalActivity ();
					}
					Activity.FragmentManager.PopBackStackImmediate ();
					break;
				}
			case TAKE_VIDEO_POS:
				{
					MainActivity ma = Activity as MainActivity;
					this.AbstractAcquiresImagesFragment.LaunchVideoCamera ();
					if (ma != null) {
						ma.LaunchingExternalActivity ();
					}
					Activity.FragmentManager.PopBackStackImmediate ();
					break;
				} 
			case WEB_SEARCH_POS:
				{
					Activity.FragmentManager.PopBackStackImmediate ();
					this.AbstractAcquiresImagesFragment.LaunchImageSearch ();
					break;
				}
			case CANCEL_POS:
				{
					Activity.FragmentManager.PopBackStackImmediate ();
					break;
				}
			default:
				break;
			}
		}

		public static ChatMediaPickerFragment NewInstance (AbstractAcquiresImagesFragment aaic, bool showScroll) {
			return new ChatMediaPickerFragment (aaic, showScroll);
		}

		public static List<ChatMediaEntry> getImages() {
			string[] projection = { MediaStore.MediaColumns.Data };
			string selection = MediaStore.MediaColumns.MimeType + " LIKE 'image%'";
			string order = MediaStore.Images.ImageColumns.DateModified + " DESC";
			ICursor cursor = EMApplication.GetMainContext ().ContentResolver.Query (MediaStore.Images.Media.ExternalContentUri, 
				projection, 
				selection,
				null, 
				order);
			List<ChatMediaEntry> result = new List<ChatMediaEntry>();
			if (cursor.MoveToFirst()) {
				int dataColumn = cursor.GetColumnIndexOrThrow(MediaStore.MediaColumns.Data);
				do {
					string data = cursor.GetString(dataColumn);
					ChatMediaEntry newEntry = new ChatMediaEntry (data, false);
					result.Add(newEntry);
					if (result.Count >= 20) {
						break;
					}
				} while (cursor.MoveToNext());
			}
			cursor.Close();
			return result;
		}
	}

	public class ChatMediaEntryAdapter : EmRecyclerViewAdapter {
		private ChatMediaPickerFragment fragment { get; set; }

		public ChatMediaEntryAdapter (ChatMediaPickerFragment frag) {
			fragment = frag;
			ColumnsWithSelected = new List<int> ();
		}

		private enum ChatMediaItemType {
			Unselected,
			Selected
		}

		private IList<int> ColumnsWithSelected;

		#region implemented abstract members of Adapter
		public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position) {
			ChatMediaEntry currEntry = this.fragment.Images [position];
			ChatMediaEntryViewHolder currVH = holder as ChatMediaEntryViewHolder;
			ChatMediaEntry oldEntry = currVH.Entry;
			if (oldEntry == null || !(currEntry.filepath.Equals (oldEntry.filepath))) {
				currVH.Entry = currEntry;

			} else {
				if (currEntry.Selected != oldEntry.Selected) {
					currVH.UpdateCheckbox (currEntry.Selected);
				}
			}
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType) {
			Action<int> OnClick = (int position) => {
				ChatMediaEntry entry = this.fragment.Images [position];
				if (entry.Selected) {
					entry.Selected = false;
					fragment.SelectedImages.Remove (entry);
					this.ColumnsWithSelected.Remove (position);
				} else {
					entry.Selected = true;
					fragment.SelectedImages.Add (entry);
					this.ColumnsWithSelected.Add (position);
				}
				fragment.HandleSelectedUpdate ();
				this.NotifyItemChanged (position);
			};
			ChatMediaEntryViewHolder vH = ChatMediaEntryViewHolder.NewInstance (parent, OnClick, null, fragment);
			vH.Entry = this.fragment.Images [0];
			return vH;
		}

		public override int ItemCount {
			get {
				if (fragment.Images == null || fragment.Images.Count == 0)
					return 0;

				return fragment.Images == null ? 0 : fragment.Images.Count;
			}
		}

		public override int GetItemViewType (int position) {
			if (this.ColumnsWithSelected.Contains (position)) {
				return (int)ChatMediaItemType.Selected;
			} else {
				return (int)ChatMediaItemType.Unselected;
			}
		}
		#endregion	
	}

	public class ChatMediaEntryViewHolder : RecyclerView.ViewHolder {
		private int cardHeight;
		private int screenWidth;

		ChatMediaPickerFragment fragment { get; set; }
		public CardView PhotoCardView { get; set; }
		public CheckBox PhotoCheckbox { get; set; }
		public ImageView PhotoView { get; set; }
		private ChatMediaEntry entry;
		public ChatMediaEntry Entry {
			get { return entry; }
			set {
				this.entry = value;
				if (PhotoView != null) {
					BitmapSetter.SetImageFromFileWithMaxWidth (this.PhotoView, EMApplication.GetInstance ().Resources, this.entry.filepath, cardHeight, screenWidth);
				}
				this.PhotoCheckbox.Checked = this.entry.Selected;
			}
		}

		public static ChatMediaEntryViewHolder NewInstance (ViewGroup parent, Action<int> itemClick, Action<int> longClick, ChatMediaPickerFragment frag) {
			View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.chat_media_item, parent, false);
			ChatMediaEntryViewHolder holder = new ChatMediaEntryViewHolder (view, itemClick, longClick, frag);
			return holder;
		}

		public ChatMediaEntryViewHolder(View view, Action<int> itemClick, Action<int> longClick, ChatMediaPickerFragment frag) : base (view) {
			this.fragment = frag;
			this.PhotoCardView = view.FindViewById<CardView> (Resource.Id.photoCard);
			this.PhotoView = view.FindViewById<ImageView> (Resource.Id.photoEntry);
			this.PhotoCheckbox = view.FindViewById<CheckBox> (Resource.Id.photoCheckbox);
			this.PhotoCheckbox.Clickable = false;
			Android.Util.DisplayMetrics screenSize = Application.Context.Resources.DisplayMetrics;
			cardHeight = 500;
			screenWidth = (int)(screenSize.WidthPixels);
			view.Click += (object sender, EventArgs e) => {
				itemClick (base.AdapterPosition);
			};

			view.LongClick += (object sender, View.LongClickEventArgs e) => {
				if (longClick != null) {
					longClick (base.AdapterPosition);
				}
			};
		}

		public void UpdateCheckbox (bool selected) {
			this.PhotoCheckbox.Checked = selected;
		}
	}
}