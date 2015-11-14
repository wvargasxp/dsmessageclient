using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Text;
using Android.Text.Method;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidHUD;
using Com.EM.Android;
using Android.Webkit;
using em;
using System.IO;
using System.Timers;
using Android.Support.V7.Widget;

namespace Emdroid {

	enum MessageListItemType {
		INCOMING_MESSAGE = 0,
		INCOMING_MEDIA_MESSAGE = 1,
		OUTGOING_MESSAGE = 2,
		OUTGOING_MEDIA_MESSAGE = 3,
		HEADER = 4,
		INCOMING_REMOTE_ACTION_BUTTON = 5
	}
			
	public class ChatFragment : AbstractAcquiresImagesFragment, Animation.IAnimationListener, IContactSource, IContactSearchController {

		public static int NEW_MESSAGE_INITIATED_FROM_NOTIFICATION_POSITION { get { return -999; } }

		const float OPAQUE = 1;
		const float INVISIBLE = 0;
		const float SEMI_OPAQUE = 0.5f;
		
		View v;

		ChatEntry chatEntry;
		public ChatEntry ChatEntry {
			get {
				return chatEntry;
			}
		}

		ChatMessageAdapter listAdapter;
		public RecyclerView MainListView { get; set; }

		public TextView titleTextView;
		TextView messageTypingView;
		protected TextView MessageTypingView {
			get { return messageTypingView; }
			set { messageTypingView = value; }
		}

		RelativeLayout messageTypingViewWrapper;
		protected RelativeLayout MessageTypingViewWrapper {
			get { return messageTypingViewWrapper; }
			set { messageTypingViewWrapper = value; }
		}
			
		private HiddenReference<SharedChatController> _shared;
		private SharedChatController sharedChatController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedChatController> (value); }
		}

		PopupWindowController PopupController { get; set; }

		Button leftBarButton;
		public Button rightBarButton;

		TextView leftBarButtonText;
		TextView LeftBarButtonText {
			get { return leftBarButtonText; }
			set { leftBarButtonText = value; }
		}

		ImageButton attachMediaButton;
		ImageButton AttachMediaButton {
			get { return attachMediaButton; }
			set { attachMediaButton = value; }
		}

		EditText textEntryArea;
		public EditText TextEntryArea {
			get { return textEntryArea; }
			set { textEntryArea = value; }
		}

		bool editTextEntryAreaFlag; // a flag to keep track of when EditText is changed manually (by the programmer), used by the listener to avoid triggering unintended callback code

		ImageButton addContactButton;
		public ImageButton AddContactButton {
			get { return addContactButton; }
			set { addContactButton = value; }
		}

		SendImageButton sendButton;
		SendImageButton SendButton {
			get { return sendButton; }
			set { sendButton = value; }
		}

		ContactSearchEditText contactSearchEntryField;
		public ContactSearchEditText ContactSearchEntryField {
			get {
				//Debug.WriteLine ("contactSearchEntryField.Text: " + contactSearchEntryField.Text);
				return contactSearchEntryField;
			}

			set {
				contactSearchEntryField = value;
			}
		}
			
		RelativeLayout textEntryWrapper;
		// staged media
		RelativeLayout mediaEntryWrapper;
		View mediaView;
		private ImageButton RemoveStagedMediaButton { get; set; }
		public SurfaceView AudioSurface { get; set; }

		public RelativeLayout ChatTopBar { get; set; }
		public View TopBarSeparator { get; set; }

		#region group name bar
		TextView groupNameTextView;
		LinearLayout groupNameBarLayout;

		protected TextView GroupNameTextView {
			get { return groupNameTextView; }
			set { groupNameTextView = value; }
		}

		protected LinearLayout GroupNameBarLayout {
			get { return groupNameBarLayout; }
			set { groupNameBarLayout = value; }
		}
		#endregion

		#region title bar
		RelativeLayout titlebarLayout;
		#endregion

		#region contact search related
		public ListView ContactSearchListView { get; set; }
		public ContactSearchListAdapter ContactListAdapter { get; set; }

		#region from alias bar
		private Spinner spinner = null;
		bool spinnerShouldActivate = false; // This is needed because android's spinner calls its ItemSelected delegate when it's being laid out...
		protected Spinner FromAliasSpinner {
			get { return spinner; }
			set { spinner = value; }
		}

		private AliasPickerAdapter aliasPickerAdapter;

		private TextView fromAliasTextField = null;
		protected TextView FromAliasTextField { 
			get { return fromAliasTextField; }
			set { fromAliasTextField = value;  }
		}
			
		protected RelativeLayout FromTopBar { get; set; }
		#endregion
		#endregion

		#region IContactSource for searching contacts
		public IList<Contact> ContactList { 
			get { return sharedChatController.SearchContacts; }
			set { sharedChatController.SearchContacts = value; } 
		}

		public Context Context {
			get { return this.Activity; }
		}
		#endregion

		public static ChatFragment NewInstance (ChatEntry ce) {
			var fragment = new ChatFragment ();
			fragment.chatEntry = ce;
			return fragment;
		}

		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			editTextEntryAreaFlag = false;

			ChatList chatList = EMApplication.GetInstance ().appModel.chatList;

			int position = -1;
			if (Arguments != null) {
				position = Arguments.GetInt ("Position");
			}

			bool isConstructingNewConvo = position == -1 || position == NEW_MESSAGE_INITIATED_FROM_NOTIFICATION_POSITION;

			chatEntry = isConstructingNewConvo ? chatList.underConstruction : chatList.entries [position];
			sharedChatController = new SharedChatController (EMApplication.Instance.appModel, chatEntry, isConstructingNewConvo ? null : chatEntry, this);

			sharedChatController.SoundRecordingRecorderController.OnFinishRecordingSuccess = this.OnFinishRecordingSuccess;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			spinnerShouldActivate = false;
			v = inflater.Inflate (Resource.Layout.chat, container, false);

			#region setting the title bar 
			titlebarLayout = v.FindViewById<RelativeLayout> (Resource.Id.titlebarlayout);

			leftBarButton = titlebarLayout.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += (object sender, EventArgs e) =>  {
				this.FragmentManager.PopBackStack ();
			};

			//Details button
			rightBarButton = titlebarLayout.FindViewById<Button> (Resource.Id.rightBarButton);
			rightBarButton.Typeface = FontHelper.DefaultFont;
			rightBarButton.Click += DidTapDetailsButton;
			rightBarButton.Visibility = ViewStates.Gone;
			rightBarButton.Text = "DETAILS".t ();

			titleTextView = titlebarLayout.FindViewById<TextView> (Resource.Id.titleTextView);
			titleTextView.Typeface = FontHelper.DefaultFont;
			titleTextView.Text = "NEW_MESSAGE_TITLE".t ();

			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			this.LeftBarButtonText = titlebarLayout.FindViewById<TextView> (Resource.Id.leftBarButtonText);
			#endregion
			#region contact search related
			this.FromAliasTextField = v.FindViewById<TextView> (Resource.Id.fromAliasTextField);
			this.FromTopBar = v.FindViewById<RelativeLayout> (Resource.Id.fromTopBar);
			this.FromAliasSpinner = v.FindViewById<Spinner> (Resource.Id.FromAliasSpinner);
			#endregion

			this.TopBarSeparator = v.FindViewById<View> (Resource.Id.topDividerLine);
			this.AddContactButton = v.FindViewById<ImageButton>(Resource.Id.AddContactButton);
			this.TextEntryArea = v.FindViewById<EditText>(Resource.Id.ChatTextEntryField);
			this.ContactSearchEntryField = v.FindViewById<ContactSearchEditText> (Resource.Id.ContactSearchEntryField);
			this.ContactSearchListView = v.FindViewById<ListView> (Resource.Id.ContactSearchList);

			this.SendButton = v.FindViewById<SendImageButton>(Resource.Id.ChatSendButton);
			this.AttachMediaButton = v.FindViewById<ImageButton> (Resource.Id.ChatAttachMediaButton);
			this.MessageTypingView = v.FindViewById<TextView> (Resource.Id.typingTextView);
			this.MessageTypingViewWrapper = v.FindViewById<RelativeLayout> (Resource.Id.MessageTypingViewWrapper);

			this.GroupNameTextView = v.FindViewById<TextView> (Resource.Id.GroupNameTextView);
			this.GroupNameBarLayout = v.FindViewById<LinearLayout> (Resource.Id.GroupNameBar);
			this.AudioSurface = v.FindViewById<SurfaceView> (Resource.Id.audioSurface);

			TouchDelegateComposite.ExpandClickArea (this.SendButton, v, 30, setEnabled: false);
			TouchDelegateComposite.ExpandClickArea (this.AttachMediaButton, v, 30);

			setColorTheme ();
			return v;
		}

		void setColorTheme() {
			if (this.IsAdded && v != null) {
				BackgroundColor mainColor = sharedChatController.backgroundColor;
				mainColor.GetBackgroundResource ( (string file) => {
					if (v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
					}
				});
				// set buttons
				if (this.RecordingHeld) {
					this.attachMediaButton.SetImageResource (mainColor.GetChatRecordingIndicatorResource ());
				} else {
					mainColor.GetChatAttachmentsResource ((string filepath) => {
						if (this.AttachMediaButton != null) {
							BitmapSetter.SetImageFromFile (this.AttachMediaButton, this.Resources, filepath);

						}
					});
				}

				var states = new StateListDrawable ();
				switch (this.SendButton.Mode) {
				case SendImageButton.SendImageButtonMode.Record:
					mainColor.GetChatVoiceRecordingButtonResource ((string filepath) => {
						states.AddState (new int[] {Android.Resource.Attribute.StateEnabled}, Drawable.CreateFromPath (filepath));
						states.AddState (new int[] {}, Resources.GetDrawable (Resource.Drawable.iconSendDisabled));
						this.SendButton.SetImageDrawable (states);
					});
					break;
				case SendImageButton.SendImageButtonMode.Send:
					mainColor.GetChatSendButtonResource ((string filepath) => {
						states.AddState (new int[] {Android.Resource.Attribute.StateEnabled}, Drawable.CreateFromPath (filepath));
						states.AddState (new int[] {}, Resources.GetDrawable (Resource.Drawable.iconSendDisabled));
						this.SendButton.SetImageDrawable (states);
					});
					break;
				case SendImageButton.SendImageButtonMode.Disabled:
					mainColor.GetChatSendButtonResource ((string filepath) => {
						states.AddState (new int[] {Android.Resource.Attribute.StateEnabled}, Drawable.CreateFromPath (filepath));
						states.AddState (new int[] {}, Resources.GetDrawable (Resource.Drawable.iconSendDisabled));
						this.SendButton.SetImageDrawable (states);
					});
					break;
				}
				mainColor.GetChatAddContactButtonResource ((string filepath) => {
					if (this.AddContactButton != null) {
						BitmapSetter.SetImageFromFile(this.AddContactButton, Resources, filepath);
					}
				});
			}
		}
			
		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			#region from alias spinner selector
			this.FromAliasTextField.Click += (object sender, EventArgs e) => {
				if (sharedChatController.AllowsFromAliasChange) {
					this.FromAliasSpinner.PerformClick ();
				}
			};

			aliasPickerAdapter = new AliasPickerAdapter (this.Activity, Android.Resource.Layout.SimpleListItem1, EMApplication.Instance.appModel.account.accountInfo.ActiveAliases);
			this.FromAliasSpinner.Adapter = aliasPickerAdapter;
			this.FromAliasSpinner.ItemSelected += (object sender, AdapterView.ItemSelectedEventArgs e) => {

				if (!spinnerShouldActivate) {
					spinnerShouldActivate = true;
					return;
				}

				if (sharedChatController.HasNotDisposed() && sharedChatController.IsNewMessage) {
					AliasInfo aliasInfo = aliasPickerAdapter.ResultFromPosition (e.Position);
					sharedChatController.UpdateFromAlias (aliasInfo);
				}
			};

			this.FromAliasSpinner.SetSelection (sharedChatController.CurrentRowForFromAliasPicker ());
			#endregion

			listAdapter = new ChatMessageAdapter (this);
			listAdapter.ItemClick += DidTapItem;
			listAdapter.LongItemClick += DidLongTapItem;
			listAdapter.RemoteActionClick += DidTapRemoteActionButton;

			RecyclerScrollListener scrollListener = new RecyclerScrollListener ();
			scrollListener.OnScrolledEvent += WeakDelegateProxy.CreateProxy<object, OnScrollEventArgs> (ListViewDidScroll).HandleEvent<object, OnScrollEventArgs>;
			scrollListener.OnScrollStateChangedEvent += WeakDelegateProxy.CreateProxy<object, OnScrollStateChangedArgs> (ListViewScrollStateChanged).HandleEvent<object, OnScrollStateChangedArgs>;

			MainListView = View.FindViewById<RecyclerView>(Resource.Id.ChatList); // get reference to the ListView in the layout

			LinearLayoutManager mLayoutManager = new LinearLayoutManager (this.Activity);
			MainListView.SetLayoutManager (mLayoutManager);
			MainListView.SetAdapter (listAdapter);
			MainListView.AddOnScrollListener (scrollListener);

			sharedChatController.ShowDetailsOption (true, false);

			ChatTopBar = this.View.FindViewById<RelativeLayout>(Resource.Id.ChatTopBar);

			TextView toLabelText = v.FindViewById<TextView> (Resource.Id.toLabelText);
			toLabelText.Typeface = FontHelper.DefaultFont;

			#region contact search
			this.ContactSearchEntryField.Enabled = true;
			this.ContactSearchEntryField.SetParent (this);

			this.ContactSearchListView.ItemClick += (object sender, AdapterView.ItemClickEventArgs e) => {
				this.ContactSearchEntryField.PossibleReplaceContact ();
				HandleContactSelectionResult (this.ContactListAdapter.ResultFromPosition (e.Position));
			};
			#endregion

			textEntryWrapper = View.FindViewById<RelativeLayout> (Resource.Id.TextEntryWrapper);
			mediaEntryWrapper = View.FindViewById<RelativeLayout> (Resource.Id.MediaEntryWrapper);
			mediaView = View.FindViewById<View> (Resource.Id.MediaView);
			RemoveStagedMediaButton = View.FindViewById<ImageButton> (Resource.Id.RemoveStagedMediaButton);
			RemoveStagedMediaButton.Click += (object sender, EventArgs e) => {
				if ( sharedChatController.HasNotDisposed() )
					sharedChatController.RemoveStagedItem();
			};
				
			this.TextEntryArea.Text = chatEntry.underConstruction;
			if (this.TextEntryArea.Text.Length != 0)
				sharedChatController.IsStagingText = true;
			if (chatEntry.underConstructionMediaPath != null)
				sharedChatController.AddStagedItem (chatEntry.underConstructionMediaPath);

			if (sharedChatController.IsStagingMediaAndText)
				MoveTextEntryBelowMedia ();

			this.AttachMediaButton.Click += (sender, e) => StartAcquiringImageForChat ();

			ChatFragment f = this;

			PopupController = new PopupWindowController (this, this.sharedChatController.backgroundColor.GetColor ());
			LayoutInflater inflater = (LayoutInflater)f.Context.GetSystemService (Context.LayoutInflaterService);
			View popupView = inflater.Inflate (Resource.Layout.chat_sound_recording_popup, null);

			//DUC1
			PressAndHoldGestureListener soundRecordingPopupListener = new PressAndHoldGestureListener ();
			soundRecordingPopupListener.OnKeyDown = () => {
				DidHoldSendButton ();
			};

			soundRecordingPopupListener.OnKeyUp = () => {
				DidTapSendButton ();
			};

			soundRecordingPopupListener.OnCancel = () => {
				DidCancelTapSendButton ();
			};

			this.SendButton.SetOnTouchListener (soundRecordingPopupListener);

			sharedChatController.UpdateSendingMode ();

			if (!sharedChatController.IsNewMessage) {
				sharedChatController.HideAddContactsOption (false);
			} else {
				this.ContactListAdapter = new ContactSearchListAdapter (this, Android.Resource.Layout.SimpleListItem1, this.sharedChatController.sendingChatEntry, false, true);
				this.ContactSearchListView.Adapter = this.ContactListAdapter;
				this.ContactSearchEntryField.SetResultCallback ();
				this.AddContactButton.Click += (sender, e) => {
					AddressBookArgs args = AddressBookArgs.From (false, true, false, this.sharedChatController.sendingChatEntry.contacts, this.sharedChatController.sendingChatEntry);
					AddressBookFragment fragment = AddressBookFragment.NewInstance (args);

					fragment.CompletionCallback += (AddressBookSelectionResult result) => {
						this.sharedChatController.ManageContactsAfterAddressBookResult (result);
						setColorTheme ();
					};

					Activity.FragmentManager.BeginTransaction ()
						.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
						.Replace (Resource.Id.content_frame, fragment)
						.AddToBackStack (null)
						.Commit ();
				};
			}

			UpdateGroupNamebarVisibility ();


			AnalyticsHelper.SendView ("Chat View");

			//if new message, give To: textbox focus and bring up the keyboard
			if (sharedChatController.IsNewMessage) {
				KeyboardUtil.ShowKeyboard (this.ContactSearchEntryField);
			}

			CheckSharingIntents ();
			UpdateTitle ();
				
			NotificationCenter.DefaultCenter.AddWeakObserver (null, SoftKeyboardListener.HIDE, HandleHideKeyboardNotification);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, SoftKeyboardListener.SHOW, HandleShowKeyboardNotification);
		}

		private void HandleHideKeyboardNotification (em.Notification obj) {
			EMTask.DispatchMain (() => {
				if (GCCheck.ViewGone (this)) return;
				UpdateRecyclerViewStackFromState ();
			});
		}

		private void HandleShowKeyboardNotification (em.Notification obj) {
			EMTask.DispatchMain (() => {
				if (GCCheck.ViewGone (this)) return;
				UpdateRecyclerViewStackFromState ();
			});
		}

		private void CheckSharingIntents () {
			if (this.Arguments != null) {
				string uriString = (string)(this.Arguments.Get (ShareIntentActivity.MEDIA_INTENT_KEY));
				if (uriString != null) {
					Android.Net.Uri uri = Android.Net.Uri.Parse (uriString);
					string mimeType = "";
					GetMimeTypeFromURI (uri, ref mimeType);
					if (mimeType.StartsWith ("audio")) {
						BeginStagingAudioFromURI (uri, mimeType);
					} else {
						BeginStagingImageOrVideoFromURI (uri);
					}
					this.Arguments.Remove (ShareIntentActivity.MEDIA_INTENT_KEY);
				}
				string textToBeSent = (string)(this.Arguments.Get (ShareIntentActivity.TEXT_INTENT_KEY));
				if (textToBeSent != null) {
					this.sharedChatController.StageTextEntryFromString (textToBeSent);
					this.Arguments.Remove (ShareIntentActivity.TEXT_INTENT_KEY);
				}
			}
		}

		private void BeginStagingAudioFromURI (Android.Net.Uri uri, string mimeType) {
			SharedChatController scc = this.sharedChatController;
			if (scc != null) {
				string path = ApplicationModel.SharedPlatform.GetUriGenerator ().GetNewMediaFileNameForStagingContents ();
				path = path + "." + ContentTypeHelper.MimeToExtension (mimeType);
				mediaFile = new Java.IO.File (path);
				ContentResolver resolver = Application.Context.ContentResolver;
				Stream s = null;
				if (IsDownloadsDocument(uri)) {
					string id = Android.Provider.DocumentsContract.GetDocumentId(uri);
					Android.Net.Uri contentUri = ContentUris.WithAppendedId(
						Android.Net.Uri.Parse("content://downloads/public_downloads"), long.Parse(id));
					s = new FileStream(GetDataColumn(EMApplication.Context, contentUri, null, null), FileMode.Open);
				} else {
					s = uri.Scheme.StartsWith ("content") ? resolver.OpenInputStream (uri) : new FileStream (uri.Path, FileMode.Open);
				}
				try {
					scc.StageAudioFromStream (s, mediaFile.AbsolutePath);
				} finally {
					s.Close ();
				}
			}
		}

		private string GetDataColumn(Context context, Android.Net.Uri uri, String selection,
			string[] selectionArgs) {
			Android.Database.ICursor cursor = null;
			string column = "_data";
			string[] projection = {column};
			try {
				cursor = context.ContentResolver.Query(uri, projection, selection, selectionArgs,
					null);
				if (cursor != null && cursor.MoveToFirst()) {
					int index = cursor.GetColumnIndexOrThrow(column);
					return cursor.GetString(index);
				}
			} finally {
				if (cursor != null)
					cursor.Close();
			}
			return null;
		}

		private bool IsDownloadsDocument(Android.Net.Uri uri) {
			return "com.android.providers.downloads.documents".Equals(uri.Authority);
		}

		public void ReloadSearchContacts () {
			if (this.ContactListAdapter != null) {
				this.ContactListAdapter.FindAllContacts ();
			}
		}

		public void UpdateGroupNamebarVisibility () {
			//if group chat, change title
			TextView groupnNameTextView = this.GroupNameTextView;
			LinearLayout groupnNameBarLayout = this.GroupNameBarLayout;
			if (sharedChatController.IsGroupConversation) {
				if (groupNameTextView != null) {
					groupNameTextView.Text = this.chatEntry.contacts [0].displayName;
				}

				if (groupnNameBarLayout != null) {
					groupnNameBarLayout.Visibility = ViewStates.Visible;
				}
			} else {
				if (groupnNameBarLayout != null) {
					groupnNameBarLayout.Visibility = ViewStates.Gone;
				}
			}
		}

		private void UpdateTitle () {
			titleTextView.Text = this.sharedChatController.Title;
		}

		public void HandleContactSelectionResult (Contact result) {
			sharedChatController.AddContactToReplyTo (result);

			setColorTheme();
		}

		public override void OnResume () {
			base.OnResume ();

			if (listAdapter != null)
				listAdapter.NotifyDataSetChanged ();

			this.TextEntryArea.AfterTextChanged += AfterTextChanged;
			this.TextEntryArea.TextChanged += TextChanged;
			this.TextEntryArea.BeforeTextChanged += BeforeTextChanged;
			this.TextEntryArea.FocusChange += TextEntryFocusChanged;

			if (this.ContactSearchEntryField != null && this.ChatTopBar.Visibility == ViewStates.Visible)
				this.ContactSearchEntryField.UpdateContactSearchTextView ();

			sharedChatController.PossibleFromBarVisibilityChange ();

			if (this.SavedRowPosition != SaveScrollNoOp) {
				// We have a saved scroll position, restore it.
				RestoreScrollPosition ();
			} else {
				// We don't have a saved scroll position, so just scroll the chat to the bottom.
				ScrollChatToBottom ();
			}
		}

		public override void OnPause () {

			SaveScrollPosition ();
			base.OnPause ();

			KeyboardUtil.HideKeyboard (this.TextEntryArea);

			this.TextEntryArea.AfterTextChanged -= AfterTextChanged;
			this.TextEntryArea.TextChanged -= TextChanged;
			this.TextEntryArea.BeforeTextChanged -= BeforeTextChanged;
			this.TextEntryArea.FocusChange -= TextEntryFocusChanged;

			ClearInProgressRemoteActionMessages ();
		}

		public override void OnStart () {
			base.OnStart ();
			sharedChatController.chatList.ObtainUnreadCountAsync (sharedChatController.DidChangeTotalUnread);
			sharedChatController.ViewBecameVisible ();
		}

		public override void OnStop () {
			base.OnStop ();

			sharedChatController.ViewBecameHidden ();

			sharedChatController.SoundRecordingInlineController.Stop ();

			if (chatEntry.isPersisted) {
				chatEntry.underConstruction = this.TextEntryArea.Text;
				chatEntry.SaveAsync ();
			}
		}

		public override void OnDestroyView () {
			base.OnDestroyView ();
		}

		public override void OnDestroy () {
			if ( GroupNameBarLayout != null )
				GroupNameBarLayout.Visibility = ViewStates.Gone;
			if (MainListView != null)
				MainListView.SetAdapter (null);

			if (sharedChatController != null)
				sharedChatController.Dispose ();

			base.OnDestroy ();
		}

		public override void OnDetach () {
			base.OnDetach ();

//			View v = this.View; // View would be null at this point.
//			bool a = this.IsAdded; // Detached from activity.
//			Activity x = this.Activity; // Can still be non null.
		}
			
		protected override void Dispose (bool disposing)  {
			base.Dispose (disposing);
		}

		public override void HandleBulkImages (List<ChatMediaEntry> mediaPaths) {
			EMTask.DispatchBackground (() => {
				NotificationCenter.DefaultCenter.PostNotification (Constants.STAGE_MEDIA_BEGIN);
				IList<em.Message> messages = new List<em.Message> ();
				try {
					foreach (ChatMediaEntry entry in mediaPaths) {
						string p = entry.filepath;
						EmThreadDelay.WaitMilli (350);
						string path = EMApplication.GetInstance ().appModel.uriGenerator.GetNewMediaFileNameForStagingContents ();
						string mimeType = ContentTypeHelper.GetContentTypeFromPath (p);
						if (mimeType != null) {
							int indexOf = mimeType.LastIndexOf ("/");
							if (indexOf != -1)
								path = path + "." + mimeType.Substring (indexOf + 1);
						}
						AndroidFileSystemManager fsm = (AndroidFileSystemManager)ApplicationModel.SharedPlatform.GetFileSystemManager ();

						fsm.CopyFileAtPath (p, path);
						Uri uri = new Uri("file://" + Uri.EscapeUriString (path));
						Media media = Media.FindOrCreateMedia (uri);
						EMNativeBitmapWrapper bitmapWrapper = media.GetNativeThumbnail<EMNativeBitmapWrapper> ();
						if (bitmapWrapper == null) {
							bitmapWrapper = new EMNativeBitmapWrapper (this.Resources, new EMMediaDescription (media), DrawableResources.Default);
							media.SetNativeThumbnail<EMNativeBitmapWrapper> (bitmapWrapper);
						}
						bitmapWrapper.GetHeightToWidth ();
						float heightToWidth = bitmapWrapper.StagedMediaHeightToWidth;
						em.Message message = this.sharedChatController.CreateMessageFromStagedMedia (path, heightToWidth);
						messages .Add (message);
					}
				} finally {
					NotificationCenter.DefaultCenter.PostNotification (Constants.STAGE_MEDIA_DONE);
				}

				this.sharedChatController.SendMessagesFromStagedMedia (messages);
			});
		}

		public void UpdateUnreadCount (int unreadCount) {
			string text = "";
			if (unreadCount > 0) {
				text = string.Format ("({0})", unreadCount);
				this.LeftBarButtonText.Visibility = ViewStates.Visible;
			} else {
				this.LeftBarButtonText.Visibility = ViewStates.Gone;
			}
			this.LeftBarButtonText.Text = text;
		}

		#region scroll positioning
		private const int SaveScrollNoOp = -1;

        private int _savedRowPosition = SaveScrollNoOp;
		private int SavedRowPosition { get { return this._savedRowPosition; } set { this._savedRowPosition = value; } }

        private int _savedPaddingForSavedRowPosition = SaveScrollNoOp;
        private int SavedPaddingForSavedRowPosition { get { return this._savedPaddingForSavedRowPosition; } set { this._savedPaddingForSavedRowPosition = value; } }

		private bool StackFromEnd { get; set; }

		private void SaveScrollPosition () {
			RecyclerView listView = this.MainListView;
			LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();

			this.StackFromEnd = layoutMgr.StackFromEnd;
			this.SavedRowPosition = layoutMgr.FindFirstVisibleItemPosition ();
			View view = listView.GetChildAt (0);
			this.SavedPaddingForSavedRowPosition = (view == null) ? 0 : (view.Top - layoutMgr.PaddingTop);
		}

		private void RestoreScrollPosition () {
			RecyclerView listView = this.MainListView;
			LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();
			layoutMgr.StackFromEnd = this.StackFromEnd;

			SharedChatController shared = this.sharedChatController;

			// If we have a tapped media message (that means we entered the media gallery, lets scroll to the last media message we looked at instead of our saved scroll position.
			if (shared.TappedMediaMessage == null) {
				layoutMgr.ScrollToPositionWithOffset (this.SavedRowPosition, this.SavedPaddingForSavedRowPosition);
			} else {
				int index = shared.viewModel.IndexOf (shared.TappedMediaMessage);
				layoutMgr.ScrollToPositionWithOffset (index, 0);
				shared.TappedMediaMessage = null;
			}

			// Resetting our saved scroll state.
			this.SavedRowPosition = SaveScrollNoOp;
			this.SavedPaddingForSavedRowPosition = SaveScrollNoOp;
		}

		public void ScrollChatToBottom () {
			int itemCount = this.listAdapter.ItemCount;
			if (itemCount > 0) {
				this.MainListView.ScrollToPosition (itemCount - 1);
			}

			UpdateRecyclerViewStackFromState ();
		}
		#endregion

		public void ChangeFromBarSelectionEnabled (bool shouldEnableAliasSelection) {
			this.FromAliasTextField.Enabled = shouldEnableAliasSelection;
		}

		protected void ChangeAliasBarVisibility (bool showFromBar) {
			if (showFromBar && this.FromTopBar.Visibility == ViewStates.Gone) {
				this.FromTopBar.Visibility = ViewStates.Visible;
			} else if (!showFromBar && this.FromTopBar.Visibility == ViewStates.Visible) {
				this.FromTopBar.Visibility = ViewStates.Gone;
				this.TopBarSeparator.Visibility = ViewStates.Gone;
			}
		}

		#region text change callbacks

		public void AfterTextChanged (object sender, AfterTextChangedEventArgs args) {

			// https://stackoverflow.com/questions/13721063/aftertextchanged-callback-being-called-without-the-text-being-actually-changed

			if (editTextEntryAreaFlag)
				return;

			sharedChatController.UpdateUnderConstructionText(this.TextEntryArea.Text);

			if (sharedChatController.IsStagingMedia && !sharedChatController.IsStagingText) {
				MoveTextEntryBelowMedia ();
			}

			if (this.TextEntryArea.Text.Length != 0)
				sharedChatController.IsStagingText = true;
			else {
				sharedChatController.IsStagingText = false;
				if (sharedChatController.IsStagingMedia) {
					MoveTextEntryNextToMedia ();
				}
			}

		}

		public void TextChanged (object sender, TextChangedEventArgs e) {

		}

		public void BeforeTextChanged (object sender, TextChangedEventArgs e) {

		}

		public void TextEntryFocusChanged (object sender, View.FocusChangeEventArgs e) {
		}
		#endregion

		#region laying out the text entry area around media
		public void MoveTextEntryNextToMedia () {
			RelativeLayout.LayoutParams textEntryAreaLayout = (RelativeLayout.LayoutParams)this.TextEntryArea.LayoutParameters;
			if (AndroidDeviceInfo.IsRightLeftLanguage ())
				textEntryAreaLayout.AddRule (LayoutRules.LeftOf, mediaEntryWrapper.Id);
			else
				textEntryAreaLayout.AddRule (LayoutRules.RightOf, mediaEntryWrapper.Id);
			textEntryAreaLayout.AddRule (LayoutRules.Below, 0); // 0 removes the rule
			this.TextEntryArea.LayoutParameters = textEntryAreaLayout;
		}

		public void MoveTextEntryBelowMedia () {
			RelativeLayout.LayoutParams textEntryAreaLayout = (RelativeLayout.LayoutParams)this.TextEntryArea.LayoutParameters;
			textEntryAreaLayout.AddRule (LayoutRules.RightOf, 0); // 0 removes the rule
			if (AndroidDeviceInfo.IsRightLeftLanguage ())
				textEntryAreaLayout.AddRule (LayoutRules.LeftOf, 0); // 0 removes the rule
			else 
				textEntryAreaLayout.AddRule (LayoutRules.RightOf, 0); // 0 removes the rule
			textEntryAreaLayout.AddRule (LayoutRules.Below, mediaEntryWrapper.Id);
			this.TextEntryArea.LayoutParameters = textEntryAreaLayout;
		}
		#endregion
			
		protected void MoveMediaFileToStagingDirectory(string mimeType) {
			string path = EMApplication.GetInstance ().appModel.uriGenerator.GetNewMediaFileNameForStagingContents ();
			if (mimeType != null) {
				int indexOf = mimeType.LastIndexOf ("/");
				if (indexOf != -1)
					path = path + "." + mimeType.Substring (indexOf + 1);
			}
				
			MoveToPathAsync (path, () => {
				sharedChatController.AddStagedItem (path);
			});
		}

		protected override int PopupMenuInflateResource () {
			return Resource.Menu.popup_chat_attachments;
		}

		protected override View PopupMenuAnchorView () {
			return this.AttachMediaButton;
		}

		protected override string ImageIntentMediaType () {
			return "video/*, image/*";
		}

		protected override void DidAcquireMedia (string mediaType, string path) {
			MoveMediaFileToStagingDirectory (mediaType);
		}

		protected void AddStagedItem (string path) {
			sharedChatController.AddStagedItem (path);
		}

		protected override bool AllowsImageCropping () {
			return false;
		}

		public override string ImageSearchSeedString { 
			get {
				return sharedChatController != null ? sharedChatController.ImageSearchSeedString : string.Empty;
			}
		}

		public void ShowAudioInStagingArea () {
			this.mediaView.Visibility = ViewStates.Gone;
			this.mediaEntryWrapper.Visibility = ViewStates.Visible;
			this.TextEntryArea.Visibility = ViewStates.Visible;
			this.RemoveStagedMediaButton.Visibility = ViewStates.Gone;
			this.AudioSurface.Visibility = ViewStates.Visible;
			MoveTextEntryNextToMedia ();
		}

		public void HideAudioInStagingArea () {
			this.mediaView.Visibility = ViewStates.Gone;
			this.mediaEntryWrapper.Visibility = ViewStates.Gone;
			this.TextEntryArea.Visibility = ViewStates.Visible;
			this.RemoveStagedMediaButton.Visibility = ViewStates.Gone;
			this.AudioSurface.Visibility = ViewStates.Gone;
		}

		protected void BeginRecording () {
			ShowAudioInStagingArea ();
			sharedChatController.SoundRecordingRecorderController.Record ();
		}

		protected void FinishRecording () {
			HideAudioInStagingArea ();
			sharedChatController.SoundRecordingRecorderController.Finish ();
		}

		protected void CancelRecording () {
			HideAudioInStagingArea ();
			sharedChatController.SoundRecordingRecorderController.Cancel ();
		}

		protected void OnFinishRecordingSuccess (string recordingPath) {
			this.AddStagedItem (recordingPath);
		}

		private bool recordingHeld = false;
		private bool RecordingHeld {
			get {
				return this.recordingHeld;
			}
			set {
				bool oldValue = this.recordingHeld;
				this.recordingHeld = value;

				if (this.recordingHeld != oldValue) {
					this.setColorTheme ();
				}
			}
		}

		protected void DidTapSendButton() {
			if (recordingHeld) {
				this.RecordingHeld = false;
				this.PopupController.Dismiss ();
				this.FinishRecording ();
			} else {
				var msg = this.TextEntryArea.Text;
				this.chatEntry.underConstruction = msg;
				this.sharedChatController.SendMessage ();
			}
		}

		//DUC2
		protected void DidHoldSendButton () {
			if (this.SendButton.Mode == SendImageButton.SendImageButtonMode.Record) {
				this.RecordingHeld = true;
				this.PopupController.Show ();
				BeginRecording ();
			}
		}

		protected void DidCancelTapSendButton () {
			if (recordingHeld) {
				this.RecordingHeld = false;
				this.PopupController.Dismiss ();
				this.CancelRecording ();
			}
		}

		protected void DidTapMediaSound (em.Message message, int index) {
			sharedChatController.SoundRecordingInlineController.DidTapMediaButton (message.media);
		}

		protected void DidTapMediaImage(em.Message message, int index) {
			SharedChatController shared = this.sharedChatController;
			shared.TappedMediaMessage = message;
			MediaGalleryFragment fragment = MediaGalleryFragment.NewInstance (shared);

			FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit ();
		}

		public void DidTapDetailsButton (object sender, EventArgs e) {
			Fragment fragment = null;
			if (sharedChatController.IsGroupConversation) {
				//go to group details page
				Group g = Contact.FindGroupByServerID(EMApplication.Instance.appModel, chatEntry.contacts [0].serverID);
				fragment = EditGroupFragment.NewInstance (true, g);
			} else if (chatEntry.contacts != null && chatEntry.contacts.Count == 1) {
				//go to profile page
				fragment = ProfileFragment.NewInstance (chatEntry.contacts [0]);
			} else if (chatEntry.contacts != null && chatEntry.contacts.Count > 1) {
				//go to profile list page
				fragment = ProfileListFragment.NewInstance (chatEntry);
			}

			if (fragment != null) {
				this.Activity.FragmentManager.BeginTransaction ()
					.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
					.Replace (Resource.Id.content_frame, fragment)
					.AddToBackStack (null)
					.Commit ();
			}
		}

		#region managing refresh and pull of new messages
		private Timer TimerBeforePullingOldMessages { get; set; }
		private bool ShouldCaptureScrollRow { get; set; }
		private int VisibleItemIndex { get; set; }
		private bool InitializedRefreshingTableViewIndicator { get; set; }
		private const int VisibleIndexToPullMessages = 0;

		/* 
		 * What we're doing here is keeping track of the scroll offset and using a timer.
		 * If the timer elapses, we check the scroll offset. If it's still near the top, we continue with pulling old messages.
		 * The reason we do this is to eliminate an instantaneous refresh that causes a jerky animation. (Mimic iMessage's behaviour)
		 */ 
		public void ListViewDidScroll (object sender, OnScrollEventArgs args) {
			// Don't bother worrying about offsets if this is a new chat. We aren't going to be loading previous messages.
			SharedChatController shared = this.sharedChatController;
			if (shared.IsNewMessage || !shared.CanLoadMorePreviousMessages) return;
			if (!this.ShouldCaptureScrollRow) return;

			LinearLayoutManager layoutMgr = (LinearLayoutManager)this.MainListView.GetLayoutManager ();

			this.VisibleItemIndex = layoutMgr.FindFirstVisibleItemPosition ();

			if (this.TimerBeforePullingOldMessages == null && this.VisibleItemIndex == VisibleIndexToPullMessages) {
				this.TimerBeforePullingOldMessages = new Timer (Constants.TIMER_INTERVAL_BEFORE_RETRIEVING_OLD_MESSAGES);
				this.TimerBeforePullingOldMessages.AutoReset = false;
				this.TimerBeforePullingOldMessages.Elapsed += WeakDelegateProxy.CreateProxy<object, ElapsedEventArgs> (HandleTimerElapsed).HandleEvent<object, ElapsedEventArgs>;
				this.TimerBeforePullingOldMessages.Start ();
			}

			if (!this.InitializedRefreshingTableViewIndicator) {
				this.listAdapter.UpdateHeaderVisibility (showHeader: true);
				this.InitializedRefreshingTableViewIndicator = true;
			}

			if (this.VisibleItemIndex != VisibleIndexToPullMessages) {
				DisposeOfTimer ();
			}
		}

		public void ListViewScrollStateChanged (object sender, OnScrollStateChangedArgs args) {
			// Don't bother worrying about offsets if this is a new chat. We aren't going to be loading previous messages.
			SharedChatController shared = this.sharedChatController;
			if (shared.IsNewMessage) return;

			if (args.State == RecyclerScrollState.Dragging) {
				// If we can scroll, stack from bottom.
				UpdateRecyclerViewStackFromState ();

				if (shared.CanLoadMorePreviousMessages) {
					this.ShouldCaptureScrollRow = true;
				}
			}
		}

		private void HandleTimerElapsed (object sender, ElapsedEventArgs e) {
			SharedChatController shared = this.sharedChatController;
			if (this.VisibleItemIndex == VisibleIndexToPullMessages && shared.CanLoadMorePreviousMessages) {
				shared.LoadMorePreviousMessages ();
			}

			// Everytime the timer elapses, reset it back to normal until the next time the user reaches the top of the tableview.
			DisposeOfTimer ();
		}

		private void DisposeOfTimer () {
			if (this.TimerBeforePullingOldMessages != null) {
				this.TimerBeforePullingOldMessages.Stop ();
				this.TimerBeforePullingOldMessages = null;
			}
		}
		#endregion

		/*
		 * This function updates the list view's StackFromEnd state.
		 * This state changes how rows are laid out. If StackFromEnd is true, the list's contents is inserted at the bottom of the view.
		 * If StackFromEnd is false, it's inserted at the top.
		 * We want StackFromEnd to be false if there aren't many rows so that the rows can be aligned towards the top of the view.
		 * In cases where we have enough rows to fill the entire view, we want StackFromEnd to be true to get it benefits.
		 * One important benefit would be auto scrolling its contents when a keyboard goes up.
		 */
		private void UpdateRecyclerViewStackFromState () {
			// TODO Do we need to account for the header count here?
			SharedChatController shared = this.sharedChatController;
			RecyclerView listView = this.MainListView;
			ChatMessageAdapter adapter = this.listAdapter;
			System.Diagnostics.Debug.Assert (shared != null, "Shared chat controller is null when it should have already been initialized.");
			LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();
			int numberOfVisibleRows = listView.ChildCount;

			int firstVisibleRow = layoutMgr.FindFirstVisibleItemPosition ();

			IList<em.Message> viewModel = shared.viewModel;
			// These are the cases where we'd want listview rows to be aligned to the top of the view.
			// 1. If view model is null, we have not yet loaded messages or we don't have any.
			// 2. If first visible row is less than 0, our first row has not been shown on screen yet (even if our viewModel count is at 1).
			// 3. If the first visible row is currently on screen and the number of visible rows on screen is equal to how many is in our view model. 
			// Use -1 on the viewmodel when doing the compare with numberOfVisibleRows because it can be before our layoutmanager displays the view.
			if (viewModel == null || firstVisibleRow < 0 || firstVisibleRow == 0 && numberOfVisibleRows >= this.sharedChatController.viewModel.Count-1) {
				layoutMgr.StackFromEnd = false;
			} else {
				layoutMgr.StackFromEnd = true;
			}
		}

		public void DidTapRemoteActionButton (object sender, int itemClickPos) {
			RecyclerView listView = (RecyclerView)this.MainListView;
			int position = itemClickPos;
			em.Message msg = sharedChatController.viewModel[position];
			System.Diagnostics.Debug.Assert (msg.HasRemoteAction, "Tapped remote action button but message does not have remote action.");
			this.sharedChatController.ResponseToRemoteActionMessage (msg);
		}

		public void DidTapItem (object sender, int itemClickPos) {
			RecyclerView listView = (RecyclerView)this.MainListView;
			int position = itemClickPos;
			em.Message msg = sharedChatController.viewModel[position];
			if (msg.HasMedia ()) {
				ContentType type = ContentTypeHelper.FromMessage (msg);

				if(ContentTypeHelper.IsAudio(type)) {
					DidTapMediaSound(msg, position);
				} else {
					DidTapMediaImage(msg, position);
				}
			}
		}

		public void DidLongTapItem (object sender, int position) {
			#region hack alert - begin hack - Blocking TextView from triggering an 'autolink'.
			RecyclerView listView = this.MainListView;
			LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();
			ChatMessageAdapter adapter = this.listAdapter;

			int firstVisiblePosition = layoutMgr.FindFirstVisibleItemPosition ();
			int uiPosition = adapter.ModelPositionToUIPosition (position);
			int rowPosition = uiPosition - firstVisiblePosition;
			View view = listView.GetChildAt (rowPosition);

			MessageViewHolder holder = (MessageViewHolder)listView.GetChildViewHolder (view);

			// https://stackoverflow.com/questions/27392650/android-long-pressing-to-open-the-context-menu-on-a-textview-also-triggers-an
			// ListView long press + TextView autolink conflicts with each other.
			// When we handle a long press, set the autolink to something else so that it doesn't trigger.
			TextView messageTextView = holder.MessageTextView;
			messageTextView.MovementMethod = ArrowKeyMovementMethod.Instance;
			#endregion

			int messageIndex = position;

			bool remoteDeleteOkay = sharedChatController.IsRemoteDeleteOkay(messageIndex);
			bool copyTextOkay = sharedChatController.IsCopyTextOkay (messageIndex);
			bool hasContextMenuItemToDisplay = remoteDeleteOkay | copyTextOkay;

			if (hasContextMenuItemToDisplay) {
				Android.Widget.PopupMenu popupMenu = new Android.Widget.PopupMenu (this.Activity, view);
				popupMenu.Inflate (Resource.Menu.popup_chat_context_menu);

				if (!remoteDeleteOkay) {
					IMenuItem remoteMenuItem = popupMenu.Menu.FindItem (Resource.Id.RemoteTakeBack);
					remoteMenuItem.SetVisible (false);
				}

				if (!copyTextOkay) {
					IMenuItem copyMenuItem = popupMenu.Menu.FindItem (Resource.Id.CopyText);
					copyMenuItem.SetVisible (false);
				}

				popupMenu.MenuItemClick += ((object popup, Android.Widget.PopupMenu.MenuItemClickEventArgs args) => {
					IMenuItem item = args.Item;
					switch ( item.ItemId ) {
					case Resource.Id.RemoteTakeBack:
						{
							sharedChatController.InitiateRemoteTakeBack (messageIndex);
							break;
						}
					case Resource.Id.CopyText:
						{
							sharedChatController.CopyTextToClipboard (messageIndex);
							break;
						}
					default:
						break;
					}

					ResetAnySelectedRowsToNotSelected ();
				});

				// Android 4 now has the DismissEvent
				popupMenu.DismissEvent += (s2, arg2) => {
					System.Diagnostics.Debug.WriteLine ("menu dismissed");
					#region hack alert - end hack - Blocking TextView from triggering an 'autolink'.
					messageTextView.MovementMethod = LinkMovementMethod.Instance;
					#endregion
					ResetAnySelectedRowsToNotSelected ();
				};

				popupMenu.Show ();
			}
		}

		private void ResetAnySelectedRowsToNotSelected () {
			ChatMessageAdapter adapter = this.listAdapter;
			if (adapter == null) return;
			adapter.ResetSelectedItems ();
		}

		public void DidRemoveContact (int index) {
			sharedChatController.RemoveContactToReplyToAt (index);
		}

		#region IContactSearchConteroller
		public void UpdateContactsAfterSearch (IList<Contact> listOfContacts, string currentSearchFilter) {
			this.ContactListAdapter.UpdateSearchContacts (listOfContacts, currentSearchFilter);
		}

		public void ShowList (bool shouldShowMainList) {
			if (shouldShowMainList) {
				this.ContactSearchListView.Visibility = ViewStates.Gone;
				this.MainListView.Visibility = ViewStates.Visible;
			} else {
				this.ContactSearchListView.Visibility = ViewStates.Visible;
				this.MainListView.Visibility = ViewStates.Gone;
			}
		}

		public void RemoveContactAtIndex (int index) {
			DidRemoveContact (index);
		}

		public void InvokeFilter (string currentSearchFilter) {
			ContactSearchListAdapter adapter = this.ContactListAdapter;
			if (adapter != null) {
				adapter.Filter.InvokeFilter (currentSearchFilter);
			}
		}

		public string GetDisplayLabelString () {
			return this.sharedChatController.ToFieldStringLabel;
		}

		public void SetQueryResultCallback (Action callback) {
			this.ContactListAdapter.QueryResultsFinished = callback;
		}

		public bool HasResults () {
			return this.ContactListAdapter.HasResults;
		}

		#endregion

		#region sizing media
		public void FixAspectRatio (View viewToAdjust, float heightToWidth, Media media, em.Message msg) {
			if (media == null) {
				return;
			}

			ViewGroup.LayoutParams parms = viewToAdjust.LayoutParameters;

			// Use these values to obtain the size the layout needs to be.
			int heightInDp = 0;
			int widthInDp = 0;

			//msg can be null, so need to check both media & msg objects
			ContentType type = ContentTypeHelper.FromMessage (msg);
			bool isAudio = ContentTypeHelper.IsAudio(type) || ContentTypeHelper.IsAudio (media.contentType) || ContentTypeHelper.IsAudio (media.uri.AbsolutePath);
			if (isAudio) {
				// Waveform is wider, no need to check height to width.
				heightInDp = (int)Android_Constants.AUDIO_WAVEFORM_THUMBNAIL_HEIGHT;
				widthInDp = (int)(heightInDp / heightToWidth);
			} else {
				// Sizing based off aspect ratio.
				if (heightToWidth > 1.0) {
					// taller than wide
					heightInDp = (int)Android_Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT;
					widthInDp = (int)(heightInDp / heightToWidth);
				} else {
					// wider than tall
					widthInDp = (int)Android_Constants.LANDSCAPE_CHAT_THUMBNAIL_WIDTH;
					heightInDp = (int)(widthInDp * heightToWidth);
				}
			}

			// Get the size in pixels as that's what the parameters are expecting.
			int finalizedHeightInPixels = heightInDp.DpToPixelUnit ();
			int finalizedWidthInPixels = widthInDp.DpToPixelUnit ();

			bool changed = false;

			if (parms.Height != finalizedHeightInPixels) {
				parms.Height = finalizedHeightInPixels;
				changed = true;
			}

			if (parms.Width != finalizedWidthInPixels) {
				parms.Width = finalizedWidthInPixels;
				changed = true;
			}

			if (changed) {	
				viewToAdjust.LayoutParameters = parms;
			}
		}
		#endregion

		#region chat animation
		public void OnAnimationEnd (Android.Views.Animations.Animation animation) {}

		public void OnAnimationRepeat (Android.Views.Animations.Animation animation) {}

		public void OnAnimationStart (Android.Views.Animations.Animation animation) {}
		#endregion
		public void ClearInProgressRemoteActionMessages () {
			this.listAdapter.ClearRemotelyClickedMessagesData ();
		}

		class ChatMessageAdapter : EmRecyclerViewAdapter {

			public bool ShowingHeader { get; private set; }
			public IList<em.Message> RemotelyClickedMessages { get; set; }

			readonly ChatFragment chatFragment;

			public ChatMessageAdapter(ChatFragment fragment) {
				chatFragment = fragment;
				this.ShowingHeader = false;
				this.RemotelyClickedMessages = new List<em.Message> ();
			}

			public override int ModelPositionToUIPosition (int modelPosition) {
				if (this.ShowingHeader) {
					modelPosition++;
				}

				return modelPosition;
			}

			public override int UIPositionToModelPosition (int uiPosition) {
				if (this.ShowingHeader) {
					uiPosition--;
				}

				return uiPosition;
			}

			/*
			 * @param modelPosition - A position that would be used in conjunction with our backing model, unrelated to UI.
			 * This function wraps the NotifyItemInserted call and converts the modelPosition to a uiPosition which can then be used for NotifyItemInserted.
			 */ 
			public void NotifyInsert (int modelPosition) {
				int position = ModelPositionToUIPosition (modelPosition);
				this.NotifyItemInserted (position);
			}

			/*
		 	 * The int that this returns is the position of the element in the model.
			 * Not the position of the UI element. If a header is showing, the UI position would be incremented by one.
		 	 */ 
			public event EventHandler<int> RemoteActionClick;

			/*
		 	 * @param uiPosition - A position that would be used in conjunction with our view, including a header if present.
		 	 * Calls the ItemClick event and returns back a model position.
		 	 */ 
			protected void OnRemoteActionClick (int uiPosition) {
				ShowTapFeedback (uiPosition);
				if (RemoteActionClick != null) {
					int position = UIPositionToModelPosition (uiPosition);

					// Keeping track of remotely clicked messages.
					// We do this so we can display the UI properly on scrolls (ViewHolder pattern) since this data is not in the model.
					// And also we can can clear it at a later point. (OnPause)
					em.Message message = chatFragment.sharedChatController.viewModel [position];
					if (!this.RemotelyClickedMessages.Contains (message)) {
						this.RemotelyClickedMessages.Add (message);
						RemoteActionClick (this, position);
					}
				}
			}

			/*
		 	 * Function to clear the list containing all Message objects that have had their remote button clicked.
		 	 * This allows the UI to refresh and clear any loading indicators.
		 	 */ 
			public void ClearRemotelyClickedMessagesData () {
				if (this.RemotelyClickedMessages.Count > 0) {
					this.RemotelyClickedMessages.Clear ();
					this.NotifyDataSetChanged ();
				}
			}
				
			/*
			 * @param modelPosition - A position that would be used in conjunction with our backing model, unrelated to UI.
			 * This function wraps the NotifyItemRemoved call and converts the modelPosition to a uiPosition which can then be used for NotifyItemRemoved.
			 */ 
			public void NotifyRemove (int modelPosition) {
				int position = ModelPositionToUIPosition (modelPosition);
				this.NotifyItemRemoved (position);
			}

			public void UpdateHeaderVisibility (bool showHeader) {
				this.ShowingHeader = showHeader;

				// TODO Maybe we don't have to reload the entire view.
				NotifyDataSetChanged (); 
//				NotifyItemInserted (0);
			}

			public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType) {
				switch ((MessageListItemType)viewType) {
				default:
				case MessageListItemType.HEADER: 
					{
						View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.chat_header, parent, false);
						HeaderViewHolder holder = new HeaderViewHolder (view);
						return holder;
					}
				case MessageListItemType.INCOMING_REMOTE_ACTION_BUTTON: 
					{
						View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.chat_incoming_msg, parent, false);
						IncomingRemoteActionViewHolder holder = new IncomingRemoteActionViewHolder (chatFragment, view, OnClick, OnLongClick, OnRemoteActionClick);
						return holder;
					}
				case MessageListItemType.INCOMING_MEDIA_MESSAGE:
				case MessageListItemType.INCOMING_MESSAGE:
					{
						View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.chat_incoming_msg, parent, false);
						IncomingViewHolder holder = new IncomingViewHolder (chatFragment, view, OnClick, OnLongClick);
						return holder;
					}

				case MessageListItemType.OUTGOING_MEDIA_MESSAGE:
				case MessageListItemType.OUTGOING_MESSAGE:
					{
						View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.chat_outgoing_msg, parent, false);
						OutgoingViewHolder holder = new OutgoingViewHolder (chatFragment, view, OnClick, OnLongClick);
						return holder;
					}
				}
			}

			public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position) {
				int viewType = GetItemViewType (position);

				// Do the convert after getting the view type as the view type expect a UI position.
				position = UIPositionToModelPosition (position);

				switch ((MessageListItemType)viewType) {
				default:
				case MessageListItemType.HEADER:
					{
						break;
					}
				case MessageListItemType.INCOMING_REMOTE_ACTION_BUTTON: 
					{
						em.Message message = chatFragment.sharedChatController.viewModel [position];
						IncomingRemoteActionViewHolder incomingRemoteHolder = holder as IncomingRemoteActionViewHolder;

						incomingRemoteHolder.Selected = position == this.SelectedPositionInModel;

						Contact fromContact = message.fromContact;
						BackgroundColor colorTheme = fromContact.colorTheme;
						incomingRemoteHolder.SilentColorTheme = colorTheme; // workaround, we need the color theme available by the time we call set message
						incomingRemoteHolder.SetMessage (message);
						incomingRemoteHolder.ColorTheme = colorTheme;

						WeakReference thisRef = new WeakReference (this);
						incomingRemoteHolder.thumbnailClickCallback = () => {
							if(!fromContact.me) {
								ProfileFragment fragment = ProfileFragment.NewInstance (fromContact);
								ChatMessageAdapter self = thisRef.Target as ChatMessageAdapter;
								if (self != null) {
									self.chatFragment.Activity.FragmentManager.BeginTransaction ()
										.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
										.Replace (Resource.Id.content_frame, fragment)
										.AddToBackStack (null)
										.Commit ();
								}
							}
						};

						BitmapRequest request = BitmapRequest.From (incomingRemoteHolder, fromContact, incomingRemoteHolder.FromImageView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE, chatFragment.Resources);
						BitmapSetter.SetThumbnailImage (request);

						incomingRemoteHolder.ShowProgressIndicator (fromContact);
						incomingRemoteHolder.ColorTheme.GetPhotoFrameLeftResource ((string file) => {
							if (incomingRemoteHolder != null && incomingRemoteHolder.PhotoFrameImageView != null && chatFragment != null) {
								BitmapSetter.SetBackgroundFromFile (incomingRemoteHolder.PhotoFrameImageView, chatFragment.Resources, file);
								incomingRemoteHolder.SentFromTextView.Text = fromContact.displayName;
								fromContact.colorTheme.GetButtonResource ( (Drawable drawable) => {
									incomingRemoteHolder.RemoteActionButton.SetBackgroundDrawable (drawable);
								});

								if (this.RemotelyClickedMessages.Contains (message)) {
									incomingRemoteHolder.ShowInProgress ();
								} else {
									incomingRemoteHolder.HideInProgress ();
								}
							}
						});
						break;
					}
				case MessageListItemType.INCOMING_MEDIA_MESSAGE:
				case MessageListItemType.INCOMING_MESSAGE:
					{

						em.Message message = chatFragment.sharedChatController.viewModel [position];
						IncomingViewHolder incomingHolder = holder as IncomingViewHolder;
						incomingHolder.Selected = position == this.SelectedPositionInModel;

						Contact fromContact = message.fromContact;
						BackgroundColor colorTheme = fromContact.colorTheme;
						incomingHolder.SilentColorTheme = colorTheme; // workaround, we need the color theme available by the time we call set message
						incomingHolder.SetMessage (message);
						incomingHolder.ColorTheme = colorTheme;


						WeakReference thisRef = new WeakReference (this);
						incomingHolder.thumbnailClickCallback = () => {
							if(!fromContact.me) {
								ProfileFragment fragment = ProfileFragment.NewInstance (fromContact);
								ChatMessageAdapter self = thisRef.Target as ChatMessageAdapter;
								if (self != null) {
									self.chatFragment.Activity.FragmentManager.BeginTransaction ()
										.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
										.Replace (Resource.Id.content_frame, fragment)
										.AddToBackStack (null)
										.Commit ();
								}
							}
						};
							
						BitmapRequest request = BitmapRequest.From (incomingHolder, fromContact, incomingHolder.FromImageView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE, chatFragment.Resources);
						BitmapSetter.SetThumbnailImage (request);

						incomingHolder.ShowProgressIndicator (fromContact);

						incomingHolder.ColorTheme.GetPhotoFrameLeftResource ((string filepath) => {
							if (incomingHolder != null && incomingHolder.PhotoFrameImageView != null) {
								BitmapSetter.SetBackgroundFromFile (incomingHolder.PhotoFrameImageView, chatFragment.Resources, filepath);
								incomingHolder.SentFromTextView.Text = fromContact.displayName;
							}
						});
						break;
					}

				case MessageListItemType.OUTGOING_MEDIA_MESSAGE:
				case MessageListItemType.OUTGOING_MESSAGE:
					{
						em.Message message = chatFragment.sharedChatController.viewModel [position];
						OutgoingViewHolder outgoingHolder = holder as OutgoingViewHolder;

						BackgroundColor colorTheme = chatFragment.chatEntry.SenderColorTheme;
						outgoingHolder.Selected = position == this.SelectedPositionInModel;
						outgoingHolder.SilentColorTheme = colorTheme; // setting color theme silently so that audio can be drawn with the right color ; workaround
						outgoingHolder.SetMessage (message);
						outgoingHolder.ColorTheme = colorTheme;
						outgoingHolder.SentFromTextView.Text = chatFragment.chatEntry.SenderName;

						BitmapRequest request = BitmapRequest.From (outgoingHolder, chatFragment.chatEntry.SenderCounterParty, outgoingHolder.FromImageView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE, chatFragment.Resources);
						BitmapSetter.SetThumbnailImage (request);

						outgoingHolder.ShowProgressIndicator (chatFragment.chatEntry.SenderCounterParty);
						outgoingHolder.ColorTheme.GetPhotoFrameRightResource ((string filepath) => {
							if (outgoingHolder != null && outgoingHolder.PhotoFrameImageView != null) {
								BitmapSetter.SetBackgroundFromFile (outgoingHolder.PhotoFrameImageView, chatFragment.Resources, filepath);
							}
						});
						break;
					}
				}
			}

			public override int GetItemViewType (int position) {
				if (this.ShowingHeader) {
					if (position == 0) {
						return (int)MessageListItemType.HEADER;
					}
				}

				position = UIPositionToModelPosition (position);
					
				em.Message message = chatFragment.sharedChatController.viewModel [position];

				if (message.HasRemoteAction) {
					return (int)MessageListItemType.INCOMING_REMOTE_ACTION_BUTTON;
				}

				if (message.IsInbound()) {
					if (message.HasMedia ())
						return (int)MessageListItemType.INCOMING_MEDIA_MESSAGE;

					return (int)MessageListItemType.INCOMING_MESSAGE;
				}

				if (message.HasMedia ())
					return (int)MessageListItemType.OUTGOING_MEDIA_MESSAGE;

				return (int) MessageListItemType.OUTGOING_MESSAGE;
			}
				
			public override int ItemCount {
				get {
					int messageCount = chatFragment.sharedChatController.viewModel == null ? 0 : chatFragment.sharedChatController.viewModel.Count;

					if (this.ShowingHeader) {
						messageCount++;
					}

					return messageCount;
				}
			}

			/*
			 * @param uiPosition - This position would be the position where our row can be selected or unselected. Can also be Unselected value.
		 	 * Updates the old selected row and the new selected row to show selection feedback.
		 	 */ 
			protected override void ManageSelectionState (int uiPosition) {
				int oldPosition = this.SelectedPosition;
				this.SelectedPosition = uiPosition;

				if (uiPosition != UnSelected) {
					this.NotifyItemChanged (uiPosition);
				}

				if (oldPosition != UnSelected) {
					this.NotifyItemChanged (oldPosition);
				}
			}

			protected override void ShowTapFeedback (int uiPosition) {
				// We use the fact that a NotifyItemChanged event will trigger a flash of the row to indicate that its been pressed.
				// Safe to subclass this method and block this call or change its behavior.
				this.NotifyItemChanged (uiPosition);
			}
		}

		public class MessageViewHolder : RecyclerView.ViewHolder {

			public Action thumbnailClickCallback;

			public em.Message message { get; set; }
			public TextView TimestampTextView { get; set; }
			public TextView MessageTextView { get; set; }
			public TextView SentFromTextView { get; set; }
			public ImageView FromImageView { get; set; }
			public ImageView PhotoFrameImageView { get; set; }
			public ImageView MediaButtonOverlay { get; set; }
			public ImageButton MediaButton { get; set; }
			public ProgressBar MediaProgressBar { get; set; }
			public ProgressBar EncodingProgressBar { get; set; }
			public View BubbleView { get; set; }
			public ProgressBar ProgressBar { get; set; }

			BackgroundColor colorTheme = BackgroundColor.Default;

			public BackgroundColor SilentColorTheme {
				set {
					colorTheme = value;
				}
			}

			public BackgroundColor ColorTheme {
				get { return colorTheme; }
				set {
					colorTheme = value;
					SetColorTheme ();
				}
			}

			protected ChatFragment chatFragment;

			public MessageViewHolder (ChatFragment fragment, View convertView, Action<int> listener, Action<int> longPressListener) : base (convertView) {
				this.chatFragment = fragment;
				this.BubbleView = convertView.FindViewById<View> (Resource.Id.BubbleView);
				this.TimestampTextView = convertView.FindViewById<TextView> (Resource.Id.messageTimestamp);
				this.MessageTextView = convertView.FindViewById<TextView> (Resource.Id.MessageTextView);
				this.SentFromTextView = convertView.FindViewById<TextView> (Resource.Id.SentFromTextView);
				this.FromImageView = convertView.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				this.PhotoFrameImageView = convertView.FindViewById<ImageView> (Resource.Id.photoFrame);
				this.MediaButtonOverlay = convertView.FindViewById<ImageView> (Resource.Id.MediaButtonOverlay);
				this.MediaButton = convertView.FindViewById<ImageButton> (Resource.Id.MediaImageButton);
				this.MediaProgressBar = convertView.FindViewById<ProgressBar> (Resource.Id.MediaProgressBar);
				this.ProgressBar = convertView.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
				this.EncodingProgressBar = convertView.FindViewById<ProgressBar> (Resource.Id.EncodingProgressBar);

				this.PhotoFrameImageView.Click += DidTapThumbnailImage;

				this.TimestampTextView.Typeface = FontHelper.DefaultFont;
				this.MessageTextView.Typeface = FontHelper.DefaultFont;
				this.SentFromTextView.Typeface = FontHelper.DefaultBoldFont;

				if (AndroidDeviceInfo.IsRightLeftLanguage ()) {
					this.SentFromTextView.Gravity = GravityFlags.Right;
					this.MessageTextView.Gravity = GravityFlags.Right;

					ViewGroup.LayoutParams baseLayoutParms = this.MediaButton.LayoutParameters;
					if (baseLayoutParms.GetType () == typeof(LinearLayout.LayoutParams)) {
						LinearLayout.LayoutParams layoutParams = (LinearLayout.LayoutParams)baseLayoutParms;
						layoutParams.Gravity = GravityFlags.Right;
						this.MediaButton.LayoutParameters = layoutParams;
					} else {
						FrameLayout.LayoutParams layoutParams = (FrameLayout.LayoutParams)baseLayoutParms;
						layoutParams.Gravity = GravityFlags.Right;
						this.MediaButton.LayoutParameters = layoutParams;
					}
				}

				// Detect user clicks on the item view and report which item
				// was clicked (by position) to the listener:
				convertView.Click += (sender, e) => listener (base.Position);
				convertView.LongClick += (object sender, View.LongClickEventArgs e) => longPressListener (base.Position);
			}

			public virtual void SetColorTheme () {
				this.SentFromTextView.SetTextColor (this.ColorTheme.GetColor());
				this.ColorTheme.GetRoundedRectangleResource ((string filepath, byte[] chunk) => {
					if (this.BubbleView != null && chatFragment != null) {
						BitmapSetter.SetBackgroundFromNinePatch (this.BubbleView, chatFragment.Resources, filepath, chunk);
					}
				});
				em.Message curMessage = this.message;
				if (curMessage != null) {
					UpdateCellFromMediaState (curMessage);
				}
			}

			private void SetSoundRecordingControlsIfEligible (em.Message m) {
				ContentType type = ContentTypeHelper.FromMessage (m);

				if (ContentTypeHelper.IsAudio (type)) {
					Media media = m.media;
					this.MediaButtonOverlay.Visibility = ViewStates.Visible;
					Action<string> handler = ((string filepath) => {
						if (this.MediaButtonOverlay != null) {
							BitmapSetter.SetBackgroundFromFile (this.MediaButtonOverlay, chatFragment.Resources, filepath);
						}
					});
					switch (media.SoundState) {
					default:
					case MediaSoundState.Stopped:
						this.ColorTheme.GetSoundRecordingControlPlayLineResource (handler);
						break;

					case MediaSoundState.Playing:
						this.ColorTheme.GetSoundRecordingControlStopLineResource (handler);
						break;
					}
				}
			}

			public virtual void SetMessage (em.Message m) {
				message = m;

				SetTimestamp ();
				SetMessageContents ();
			}

			protected void SetTimestamp() {
				if (!message.showSentDate) {
					TimestampTextView.Visibility = ViewStates.Gone;
				} else {
					TimestampTextView.Visibility = ViewStates.Visible;
					TimestampTextView.Text = message.FormattedSentDate;
				}
			}

			private bool _selected = false;
			public bool Selected {
				get { return this._selected; }
				set { this._selected = value; }
			}

			public void UpdateBubbleViewAlpha () {
				if (this._selected) {
					this.BubbleView.Alpha = .7f;
				} else {
					if (message.messageLifecycle == MessageLifecycle.deleted) {
						this.BubbleView.Alpha = 0.5f;
					} else {
						this.BubbleView.Alpha = 1f;
					}
				}
			}

			private static int LARGE_FONT = 84;
			private static int SMALL_FONT = 14;

			protected void SetMessageContents() {
				int index = chatFragment.sharedChatController.viewModel.IndexOf (message);
				UpdateBubbleViewAlpha ();
				bool shouldCenterThumbnailFrame = false;
				bool largeFont = true;
				if (!message.HasMedia ()) {
					MediaButton.Visibility = ViewStates.Gone;
					MediaProgressBar.Visibility = ViewStates.Gone;
					MediaButtonOverlay.Visibility = ViewStates.Gone;

					MessageTextView.Visibility = ViewStates.Visible;
					message.StripEmojiSkinModifier ();
					if (message.ShouldEnlargeEmoji ()) {
						MessageTextView.SetTextSize (Android.Util.ComplexUnitType.Sp, LARGE_FONT);
					} else {
						largeFont = false;
						MessageTextView.SetTextSize (Android.Util.ComplexUnitType.Sp, SMALL_FONT);
					}
					MessageTextView.Text = message.message;
				}
				else {
					MessageTextView.Visibility = ViewStates.Gone;
					Media media = message.media;
					int maxHeightInPixels = BitmapSetter.MaxHeightForMediaInPixels (media);

					BitmapRequest request = BitmapRequest.From (this, media, this.MediaButton, -1, maxHeightInPixels, this.ColorTheme.GetColor (), chatFragment.Resources);
					BitmapSetter.SetMediaImage (request);

					if (media == null) {
						MediaButton.Visibility = ViewStates.Gone;
						MediaProgressBar.Visibility = ViewStates.Visible;
						BitmapSetter.SetBackground (MediaButton, null);
					} else {
						// Disable it so that it doesn't intercept the listview's touch event.
						MediaButton.Focusable = false;
						MediaButton.Clickable = false;
					}

					float heightToWidth = message.heightToWidth;
					chatFragment.FixAspectRatio (MediaButton, heightToWidth, media, message);

					// Make the progress bar width the same size as media button.
					MediaProgressBar.LayoutParameters.Width = MediaButton.LayoutParameters.Width;

					MediaButton.Tag = new Java.Lang.Integer (index);
					UpdateCellFromMediaState (message);
				}
				Action redrawCounterpartyImage = () => {
					shouldCenterThumbnailFrame = !largeFont && this.MessageTextView.LineCount <= 1;
					int dpToPixels = 4.DpToPixelUnit (); // adjustment between photo frame and thumbnail view
					FrameLayout.LayoutParams photoFrame = (FrameLayout.LayoutParams)this.PhotoFrameImageView.LayoutParameters;
					FrameLayout.LayoutParams thumbnailFrame = (FrameLayout.LayoutParams)this.FromImageView.LayoutParameters;
					photoFrame.TopMargin = shouldCenterThumbnailFrame ? -12 : 20;
					thumbnailFrame.TopMargin = photoFrame.TopMargin + dpToPixels;
					this.PhotoFrameImageView.LayoutParameters = photoFrame;
					this.FromImageView.LayoutParameters = thumbnailFrame;
				};

				if (this.MessageTextView.LineCount == 0) {
					this.MessageTextView.Post (redrawCounterpartyImage); // LineCount == 0 if MessageTextView not rendered yet.
				} else {
					redrawCounterpartyImage ();
				}
			}

			public virtual void UpdateMessageContents () {
				SetMessageContents ();
			}

			public void ShowProgressIndicator (CounterParty c) {
				if (BitmapSetter.ShouldShowProgressIndicator (c)) {
					this.ProgressBar.Visibility = ViewStates.Visible;
					this.FromImageView.Visibility = ViewStates.Invisible;
					this.PhotoFrameImageView.Visibility = ViewStates.Invisible;;
				} else {
					this.ProgressBar.Visibility = ViewStates.Invisible;
					this.FromImageView.Visibility = ViewStates.Visible;
					this.PhotoFrameImageView.Visibility = ViewStates.Visible;
				}
			}

			public void DidTapThumbnailImage (object sender, EventArgs e) {
				if (thumbnailClickCallback != null) {
					thumbnailClickCallback ();
				}
			}

			private void ShowHideEncodingProgressBar (bool show) {
				ProgressBar encodingProgressBar = this.EncodingProgressBar;
				if (encodingProgressBar != null) {
					if (show) {
						encodingProgressBar.Visibility = ViewStates.Visible;
					} else {
						encodingProgressBar.Visibility = ViewStates.Gone;
					}
				}
			}

			private void UpdateCellFromMediaState (em.Message m) {
				Media media = m.media;
				if (media == null) {
					return;
				}

				this.MediaButtonOverlay.Visibility = ViewStates.Gone;

				ProgressBar encodingProgressBar = this.EncodingProgressBar;

				EMApplication.Instance.appModel.mediaManager.ResolveState (media);
				switch (media.MediaState) {
				case MediaState.Absent:
					{
						this.MediaButton.Visibility = ViewStates.Visible;
						this.MediaButton.Alpha = SEMI_OPAQUE;
						this.MediaProgressBar.Visibility = ViewStates.Visible;
						this.MediaProgressBar.Progress = (int)(em.Constants.BASE_PROGRESS_ON_PROGRESS_VIEW * 10 * 100);
						this.MediaProgressBar.SetBackgroundColor (Color.Transparent);
						ShowHideEncodingProgressBar (show: false);
						break;
					}
				case MediaState.Encoding:
					{
						this.MediaButton.Visibility = ViewStates.Visible;
						this.MediaButton.Alpha = SEMI_OPAQUE;
						this.MediaProgressBar.Visibility = ViewStates.Invisible;
						ShowHideEncodingProgressBar (show: true);
						break;
					}
				case MediaState.Uploading: 
					{
						this.MediaButton.Visibility = ViewStates.Visible;
						this.MediaButton.Alpha = SEMI_OPAQUE;
						this.MediaProgressBar.Visibility = ViewStates.Visible;
						ShowHideEncodingProgressBar (show: false);

						float percentage = (float)media.Percentage;
						if (percentage < em.Constants.BASE_PROGRESS_ON_PROGRESS_VIEW * 10)
							percentage = em.Constants.BASE_PROGRESS_ON_PROGRESS_VIEW * 10;
						this.MediaProgressBar.Progress = (int)(percentage * 100);
						this.MediaProgressBar.SetBackgroundColor (Color.Transparent);
						SetSoundRecordingControlsIfEligible (m);
						break;
					}
				case MediaState.Present:
					{
						ShowHideEncodingProgressBar (show: false);

						this.MediaButton.Visibility = ViewStates.Visible;
						this.MediaButton.Alpha = OPAQUE;
						this.MediaProgressBar.Visibility = ViewStates.Gone;
						this.MediaProgressBar.SetBackgroundColor (Color.Transparent);
						SetSoundRecordingControlsIfEligible (m);
						break;
					}
				case MediaState.Downloading:
					{
						ShowHideEncodingProgressBar (show: false);
						if (ContentTypeHelper.IsVideo (media.contentType)) {
							this.MediaButton.Visibility = ViewStates.Visible;
						} else {
							this.MediaButton.Visibility = ViewStates.Invisible;
						}
						this.MediaProgressBar.Visibility = ViewStates.Visible;
						float percentage = (float)media.Percentage;
						if (percentage < em.Constants.BASE_PROGRESS_ON_PROGRESS_VIEW * 10)
							percentage = em.Constants.BASE_PROGRESS_ON_PROGRESS_VIEW * 10;
						this.MediaProgressBar.Progress = (int)(percentage * 100);
						this.MediaProgressBar.SetBackgroundColor (Color.Transparent);
						break;
					}
				case MediaState.FailedDownload:
					{
						ShowHideEncodingProgressBar (show: false);

						this.MediaButton.Visibility = ViewStates.Visible;
						if (AppEnv.DEBUG_MODE_ENABLED) {
							this.MediaButton.SetBackgroundColor (Color.Red);
						}
						break;
					}
				case MediaState.FailedUpload:
					{
						ShowHideEncodingProgressBar (show: false);

						this.MediaButton.Visibility = ViewStates.Visible;
						this.MediaButton.Alpha = SEMI_OPAQUE;
						this.MediaProgressBar.Visibility = ViewStates.Visible;
						this.MediaProgressBar.SetBackgroundColor (Android.Graphics.Color.Red);
						SetSoundRecordingControlsIfEligible (m);
						break;
					}
				}
			}
		}

		public class HeaderViewHolder : RecyclerView.ViewHolder {
			public ProgressBar HeaderSpinner { get; set; }
			public HeaderViewHolder (View convertView) : base (convertView) {
				this.HeaderSpinner = convertView.FindViewById <ProgressBar> (Resource.Id.progressBarHeader);
			}
		}

		public class IncomingViewHolder : MessageViewHolder {

			public ImageView AkaIconMaskImageView { get; set; }

			public IncomingViewHolder (ChatFragment chatFragment, View convertView, Action<int> listener, Action<int> longPressListener) : base (chatFragment, convertView, listener, longPressListener) {
				this.AkaIconMaskImageView = convertView.FindViewById<ImageView> (Resource.Id.akaMaskIcon);
			}

			public override void SetColorTheme () {
				base.SetColorTheme ();
			}

			public override void SetMessage (em.Message m) {
				base.SetMessage (m);
				ShowHideAkaMaskIcon ();
			}

			private void ShowHideAkaMaskIcon () {
				Contact contact = this.message.fromContact;
				if (contact.IsAKA) {
					contact.colorTheme.GetAkaMaskResource ((string filepath) => {
						if (this.AkaIconMaskImageView != null && chatFragment != null) {
							BitmapSetter.SetImageFromFile (this.AkaIconMaskImageView, chatFragment.Resources, filepath);
						}	
					});
					this.AkaIconMaskImageView.Visibility = ViewStates.Visible;
				} else {
					this.AkaIconMaskImageView.Visibility = ViewStates.Gone;
				}
			}
		}

		public class OutgoingViewHolder : MessageViewHolder {
			public ImageView MessageStatusImageView { get; set; }

			public OutgoingViewHolder(ChatFragment chatFragment, View convertView, Action<int> listener, Action<int> longPressListener) : base(chatFragment, convertView, listener, longPressListener) {
				MessageStatusImageView = convertView.FindViewById<ImageView> (Resource.Id.MessageStatusImageView);
			}

			public override void SetColorTheme () {
				base.SetColorTheme ();
			}

			public override void SetMessage (em.Message message) {
				base.SetMessage (message);

				SetOutgoingMessage (message);
			}

			bool showDebugMessageStatusUpdates = false;
			public void SetOutgoingMessage(em.Message message) {
				if (showDebugMessageStatusUpdates || EMApplication.Instance.appModel.ShowVerboseMessageStatusUpdates) {
					switch (message.messageStatus) {
					default:
					case MessageStatus.pending:
						MessageStatusImageView.Visibility = ViewStates.Visible;
						BitmapSetter.SetBackground (MessageStatusImageView, chatFragment.Resources, Resource.Drawable.pending);
						break;
					case MessageStatus.sent:
						MessageStatusImageView.Visibility = ViewStates.Visible;
						BitmapSetter.SetBackground (MessageStatusImageView, chatFragment.Resources, Resource.Drawable.sent);
						break;
					case MessageStatus.delivered:
						MessageStatusImageView.Visibility = ViewStates.Visible;
						BitmapSetter.SetBackground (MessageStatusImageView, chatFragment.Resources, Resource.Drawable.envelope);
						break;
					
					case MessageStatus.ignored:
					case MessageStatus.read:
						MessageStatusImageView.Visibility = ViewStates.Invisible;
						BitmapSetter.SetBackground (MessageStatusImageView, null);
						break;

					case MessageStatus.failed:
						MessageStatusImageView.Visibility = ViewStates.Visible;
						BitmapSetter.SetBackground (MessageStatusImageView, chatFragment.Resources.GetDrawable (Resource.Drawable.failed));
						break;
					}
				} else {
					switch (message.messageStatus) {
					default:
					case MessageStatus.pending:
						MessageStatusImageView.Visibility = ViewStates.Visible;
						BitmapSetter.SetBackground (MessageStatusImageView, chatFragment.Resources, Resource.Drawable.sent);
						break;

					case MessageStatus.sent:
					case MessageStatus.delivered:
						MessageStatusImageView.Visibility = ViewStates.Visible;
						BitmapSetter.SetBackground (MessageStatusImageView, chatFragment.Resources, Resource.Drawable.envelope);
						break;

					case MessageStatus.ignored:
					case MessageStatus.read:
						MessageStatusImageView.Visibility = ViewStates.Invisible;
						BitmapSetter.SetBackground (MessageStatusImageView, null);
						break;

					case MessageStatus.failed:
						MessageStatusImageView.Visibility = ViewStates.Visible;
						BitmapSetter.SetBackground (MessageStatusImageView, chatFragment.Resources.GetDrawable (Resource.Drawable.failed));
						break;
					}
				}
			}
		}

		public class IncomingRemoteActionViewHolder : MessageViewHolder {
			public Button RemoteActionButton { get; set; }
			public FrameLayout RemoteActionButtonWrapper { get; set; }
			public ProgressBar RemoteActionProgressBar { get; set; }

			public IncomingRemoteActionViewHolder (ChatFragment chatFragment, View convertView, Action<int> listener, Action<int> longPressListener, Action<int> remoteActionClickListener) : base (chatFragment, convertView, listener, longPressListener) {

				this.RemoteActionButtonWrapper = convertView.FindViewById<FrameLayout> (Resource.Id.remoteActionButtonWrapper);
				this.RemoteActionButtonWrapper.Visibility = ViewStates.Visible;

				this.RemoteActionButton = this.RemoteActionButtonWrapper.FindViewById<Button> (Resource.Id.remoteActionButton);
				this.RemoteActionProgressBar = this.RemoteActionButtonWrapper.FindViewById<ProgressBar> (Resource.Id.remoteActionProgress);

				this.RemoteActionButton.Click += (object sender, EventArgs e) => {
					remoteActionClickListener (base.Position);
					ShowInProgress ();
				};

				this.RemoteActionButton.Typeface = FontHelper.DefaultFont;
			}

			public override void SetColorTheme () {
				base.SetColorTheme ();
			}

			public override void SetMessage (em.Message m) {
				base.SetMessage (m);
				UpdateRemoteButtonVisibilityAndContents ();
			}

			public override void UpdateMessageContents () {
				base.UpdateMessageContents ();
				UpdateRemoteButtonVisibilityAndContents ();
			}

			private void UpdateRemoteButtonVisibilityAndContents () {
				em.Message m = this.message;
				this.RemoteActionButton.Visibility = ViewStates.Visible;
				this.RemoteActionButton.Text = m.RemoteAction.label;
			}

			public void ShowInProgress () {
				this.RemoteActionProgressBar.Visibility = ViewStates.Visible;
			}

			public void HideInProgress () {
				this.RemoteActionProgressBar.Visibility = ViewStates.Gone;
			}
		}

		class SharedChatController : AbstractChatController {
			private WeakReference fragmentRef;
			private ChatFragment ChatFragment {
				get { return fragmentRef != null ? fragmentRef.Target as ChatFragment : null; }
				set { fragmentRef = new WeakReference (value); }
			}

			public SharedChatController (ApplicationModel appModel, ChatEntry sendingChatEntry, ChatEntry displayedChatEntry, ChatFragment fragment) : base(appModel, sendingChatEntry, displayedChatEntry) {
				this.ChatFragment = fragment;
			}

			public override void ClearInProgressRemoteActionMessages () {
				EMTask.DispatchMain (() => {
					ChatFragment self = this.ChatFragment;
					if (GCCheck.ViewGone (self)) return;
					self.ClearInProgressRemoteActionMessages ();
				});
			}

			public override void ReloadContactSearchContacts () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.ReloadSearchContacts ();
				}
			}

			public override void PreloadImages (IList<em.Message> messages) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					BitmapSetter.PreloadMediaListAsync (chatFragment.Resources, messages);
				}
			}

			public override void ShowDetailsOption (bool animated, bool forceShow) {
				//show only for already initiated chats (not new message or deleted group)
				ChatFragment chatFragment = this.ChatFragment;

				if (!GCCheck.ViewGone (chatFragment)) {
					if ((!IsNewMessage || forceShow) && !IsDeletedGroupConversation && chatFragment.rightBarButton.Visibility == ViewStates.Gone) {
						chatFragment.rightBarButton.Visibility = ViewStates.Visible;
					}
				}
			}

			public override void UpdateFromAliasPickerInteraction (bool shouldAllowInteraction) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.ChangeFromBarSelectionEnabled (shouldAllowInteraction);
				}
			}

			public override void UpdateFromBarVisibility (bool showFromBar) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.ChangeAliasBarVisibility (showFromBar);
				}
			}

			public override void UpdateAliasText (string text) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.FromAliasTextField.Text = text;
				}
			}

			public override void UpdateTextEntryArea (string text) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.TextEntryArea.Text = text;
				}
			}

			public override void DidFinishLoadingMessages () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.UpdateGroupNamebarVisibility (); // though this might not be the best place to put this, this is the delegate call that is triggered when an existing chat entry exists
					chatFragment.listAdapter.NotifyDataSetChanged ();
					if (HasNotDisposed () && viewModel != null && ChatFragment.View != null) {
						chatFragment.ScrollChatToBottom ();
					}
				}
			}

			public override void DidFinishLoadingPreviousMessages (int count) {
				ChatFragment chatFragment = this.ChatFragment;
				if (GCCheck.ViewGone (chatFragment)) return;

				ChatMessageAdapter listAdapter = chatFragment.listAdapter;
				if (!chatFragment.sharedChatController.CanLoadMorePreviousMessages) {
					// No more previous messages, hide spinner.
					listAdapter.UpdateHeaderVisibility (showHeader: false);
				} else {
					RecyclerView listView = chatFragment.MainListView;
					LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();

					// Grab the first visible position so we can return to this exact position later.
					int firstVisPos = layoutMgr.FindFirstVisibleItemPosition ();

					// Notify the adapter we have items that need to be inserted.
					// Since these are previous messages, it's safe to insert it at the start (0th index).
					// The range would then be the count of how many previous messages are loaded.
					listAdapter.NotifyItemRangeInserted (0, count);

					// After the notify, scroll back to the original position.
					// It should be the first visible position from earlier + the offset which is the count of previous messages.
					layoutMgr.ScrollToPosition (firstVisPos + count);
				}
			}

			public override void HideAddContactsOption (bool animated) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.AddContactButton.Visibility = ViewStates.Gone;
					chatFragment.ChatTopBar.Visibility = ViewStates.Gone;
					chatFragment.TopBarSeparator.Visibility = ViewStates.Gone;
					chatFragment.FromAliasTextField.Visibility = ViewStates.Gone;
					chatFragment.FromTopBar.Visibility = ViewStates.Gone;
				}
			}

			public override void ClearTextEntryArea () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.editTextEntryAreaFlag = true;
					chatFragment.TextEntryArea.Text = string.Empty;
					this.IsStagingText = false;
					chatFragment.editTextEntryAreaFlag = false;
				}
			}

			#region showing progress indicator 
			public void PauseUI () {
				EMTask.DispatchMain (() => {
					ChatFragment chatFragment = this.ChatFragment;
					if (!GCCheck.ViewGone (chatFragment)) {
						AndHUD.Shared.Show (chatFragment.Activity, null, -1, MaskType.Clear, default(TimeSpan?), null, true, null);
					}
				});
			}

			public void ResumeUI () {
				EMTask.DispatchMain (() => {
					ChatFragment chatFragment = this.ChatFragment;
					if (!GCCheck.ViewGone (chatFragment)) {
						AndHUD.Shared.Dismiss (chatFragment.Activity);
					}
				});
			}
			#endregion

			#region STAGING MEDIA
			public override void StagedMediaBegin () {
				ChatFragment chatFragment = this.ChatFragment;
				if (chatFragment != null) {
					InProgressHelper.Show (chatFragment);
				}
			}

			public override void StagedMediaAddedToStagingAndPreload() {

				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					int maxHeightInPixels = BitmapSetter.MaxHeightForMediaInPixels (this.StagedMedia);
					BitmapSetter.SetStagedMedia (StagedMedia, chatFragment.Resources, chatFragment.mediaView, maxHeightInPixels, chatFragment.sharedChatController.backgroundColor.GetColor ());
				} else {
					this.StagingHelper.EndStagingItemProcedure ();
				}
			}

			public override void StagedMediaEnd () {
				ChatFragment chatFragment = this.ChatFragment;
				if (chatFragment != null) {
					InProgressHelper.Hide (chatFragment);
				}
			}

			public override float StagedMediaGetAspectRatio() {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					// Look in AbstractAcquiresImagesFragment OnActivityResult for the previous Show ().
					InProgressHelper.Hide (chatFragment);


					chatFragment.mediaEntryWrapper.Visibility = ViewStates.Visible;
					chatFragment.TextEntryArea.Visibility = ViewStates.Visible;
					chatFragment.mediaView.Visibility = ViewStates.Visible;
					chatFragment.RemoveStagedMediaButton.Visibility = ViewStates.Visible;
					chatFragment.AudioSurface.Visibility = ViewStates.Gone;

					EMNativeBitmapWrapper bitmapWrapper = this.StagedMedia.GetNativeThumbnail<EMNativeBitmapWrapper> ();
					float heightToWidth = 0;
					if (bitmapWrapper != null) {
						heightToWidth = bitmapWrapper.StagedMediaHeightToWidth;
					} else {
						BitmapDrawable img = chatFragment.mediaView.Background as BitmapDrawable; // TODO: check if this works under jelly bean version
						this.StagedMediaHeight = img.Bitmap.Height;
						this.StagedMediaWidth = img.Bitmap.Width;
						this.IsStagingMedia = true;
						heightToWidth = (float)img.Bitmap.Height / (float)img.Bitmap.Width;
					}

					chatFragment.mediaView.Invalidate ();

					chatFragment.FixAspectRatio (chatFragment.mediaView, heightToWidth, this.StagedMedia, null);

					// The reason we don't account for the else |if we're staging text| is because by default,
					// the text entry area is separate from the staged media view.
					if (!this.IsStagingText) {
						chatFragment.MoveTextEntryNextToMedia ();
					}

					return heightToWidth;
				}

				return 1f;
			}

			public override float StagedMediaGetSoundRecordingDurationSeconds () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					if (this.StagedMedia == null) {
						return 0;
					}

					string soundPath = this.StagedMedia.GetPathForUri (ApplicationModel.SharedPlatform);

					if (!ContentTypeHelper.IsAudio (soundPath)) {
						return 0;
					}

					float durationSeconds = (float) AndroidSoundRecordingRecorder.GetAudioDuration (soundPath);
					return durationSeconds;
				}

				return 0;
			}

			public override void StagedMediaRemovedFromStaging () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.mediaEntryWrapper.Visibility = ViewStates.Gone;
					BitmapSetter.SetBackground (chatFragment.mediaView, null);
					this.IsStagingMedia = false;
				}
			}
			#endregion

			public override void UpdateToContactsView () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.ContactSearchEntryField.UpdateContactSearchTextView ();
				}
			}

			public override void GoToBottom () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.ScrollChatToBottom ();
				}
			}

			public override void ConversationContainsActive (bool active, InactiveConversationReason reason) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					View view = chatFragment.View;
					if (!GCCheck.Gone (view)) {
						if (!active) {

							AlertDialog.Builder builder = new AlertDialog.Builder (chatFragment.Activity);

							switch (reason) {
							case InactiveConversationReason.FromAliasInActive:
								{
									builder.SetTitle ("ALIAS_DELETED_TITLE".t ());
									builder.SetMessage ("ALIAS_DELETED_CHAT_HISTORY_MESSAGE".t ());
									break;
								}
							case InactiveConversationReason.Other:
								{
									builder.SetTitle ("SEND_MESSAGE_FAILED_TITLE".t ());
									builder.SetMessage ("SEND_MESSAGE_FAILED_REASON".t ());
									break;
								}
							}

							builder.SetPositiveButton ("OK_BUTTON".t (), (sender, dialogClickEventArgs) => {});
							builder.Create ();
							builder.Show ();
						}

						UpdateSendingMode ();
					}
				}
			}

			public override void WarnLeftAdhoc() {
				ChatFragment chatFragment = this.ChatFragment;
				if (GCCheck.ViewGone (chatFragment)) return;
				var builder = new AlertDialog.Builder (chatFragment.Activity);
				builder.SetTitle ("LEFT_CONVERSATION".t ());
				builder.SetMessage ("LEFT_CONVERSATION_EXPLAINATION".t ());
				builder.SetPositiveButton ("OK_BUTTON".t (), (sender, dialogClickEventArgs) => {});
				builder.Create ();
				builder.Show ();
			}

			public override void PrepopulateToWithAKA (string toAka) {
				ChatFragment chatFragment = this.ChatFragment;
				if (GCCheck.ViewGone (chatFragment)) return;

				ContactSearchListAdapter adapter = chatFragment.ContactListAdapter;
				if (adapter == null) return;

				adapter.InitialQueryResultsFinished = () => {
					chatFragment.ContactSearchEntryField.Text = toAka;
				};
			}

			public override bool CanScrollToBottom {
				get {
					ChatFragment chatFragment = this.ChatFragment;
					if (GCCheck.ViewGone (chatFragment)) {
						return false;
					} else {
						RecyclerView listView = chatFragment.MainListView;
						LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();

						int firstVisiblePosition = layoutMgr.FindFirstVisibleItemPosition ();
						int lastVisiblePosition = layoutMgr.FindLastVisibleItemPosition ();

						int visibleRows = listView.ChildCount;

						int messageToBeAdded = this.viewModel.Count - 1;

						int numberOfRowsAwayFromBottom = messageToBeAdded - lastVisiblePosition;
					
						if ((numberOfRowsAwayFromBottom) > visibleRows) {
							return false;
						} else {
							return true;
						}
					}
				}
			}

			public override void HandleMessageUpdates (IList<ModelStructureChange<em.Message>> structureChanges, IList<ModelAttributeChange<em.Message,object>> attributeChanges, bool animated, Action doneAnimatingCallback) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.Gone (chatFragment)) {
					if (chatFragment.View == null || !animated || !chatFragment.MainListView.IsShown) {
						chatFragment.listAdapter.NotifyDataSetChanged ();
						doneAnimatingCallback ();
					}
					else {
						List<int> rowsToAdd = new List<int>();
						List<int> rowsToDelete = new List<int>();
						if (structureChanges != null) {
							chatFragment.UpdateTitle ();
							foreach (ModelStructureChange<em.Message> change in structureChanges) {
								int position = viewModel.IndexOf (change.ModelObject);
								if (change.Change == ModelStructureChange.added)
									rowsToAdd.Add (position);
								else if (change.Change == ModelStructureChange.deleted)
									rowsToDelete.Add (position);
							}
						}

						if (rowsToAdd.Count == 0 && rowsToDelete.Count == 0) {
							HandleAttributeChanges (attributeChanges);
							doneAnimatingCallback ();
						}
						else {
							if (rowsToAdd.Count > 0) {
								foreach (int position in rowsToAdd) {
									chatFragment.listAdapter.NotifyInsert (position);
								}
							}

							if (rowsToDelete.Count > 0) {
								foreach (int position in rowsToAdd) {
									chatFragment.listAdapter.NotifyRemove (position);
								}
									
							}

							HandleAttributeChanges (attributeChanges);
							doneAnimatingCallback ();

							if (!EMApplication.Instance.appModel.IsHandlingMissedMessages && this.CanScrollToBottom) {
								chatFragment.ScrollChatToBottom ();
							}
						}
					}
				}
			}

			protected void HandleAttributeChanges(IList<ModelAttributeChange<em.Message,object>> attributeChanges) {
				if (attributeChanges == null)
					return;
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {

					RecyclerView listView = chatFragment.MainListView;
					LinearLayoutManager layoutMgr = (LinearLayoutManager)listView.GetLayoutManager ();
					ChatMessageAdapter adapter = chatFragment.listAdapter;

					foreach (ModelAttributeChange<em.Message,object> attrChange in attributeChanges) {
						int modelPosition = viewModel.IndexOf (attrChange.ModelObject);

						int uiPosition = adapter.ModelPositionToUIPosition (modelPosition); // convert, GetChildAt expects a UI position.

						int firstVisiblePosition = layoutMgr.FindFirstVisibleItemPosition ();
						int lastVisiblePosition = layoutMgr.FindLastVisibleItemPosition ();

						if (uiPosition >= firstVisiblePosition && uiPosition <= lastVisiblePosition) {
							if (modelPosition < viewModel.Count) {
								em.Message message = attrChange.ModelObject;
								if (attrChange.AttributeName.Equals (MESSAGE_ATTRIBUTE_MESSAGE_STATUS)) {
									if (!message.IsInbound ()) {
										View v = layoutMgr.GetChildAt (uiPosition - firstVisiblePosition);
										if (v != null) {
											RecyclerView.ViewHolder oHolder = listView.GetChildViewHolder (v);
											if (oHolder is MessageViewHolder) {
												MessageViewHolder holder = (MessageViewHolder)oHolder;
												holder.SetMessage (message);
											}
										}
									}
								}
								else if (attrChange.AttributeName.Equals (MESSAGE_ATTRIBUTE_TAKEN_BACK)) {
									adapter.NotifyItemChanged (uiPosition);
								}
							}
						}
					}
				}
			}

			public void UpdateSendingMode () {
				ChatFragment chatFragment = this.ChatFragment;

				SetSendButtonMode (sendingChatEntry.MessageSendingAllowed, sendingChatEntry.SoundRecordingAllowed);
				chatFragment.attachMediaButton.Enabled = this.Editable;
				chatFragment.textEntryArea.Enabled = this.Editable;
			}

			public override void SetSendButtonMode (bool messageSendingAllowed, bool soundRecordingAllowed) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					bool enableButton = false;
					SendImageButton.SendImageButtonMode oldState;
					SendImageButton.SendImageButtonMode newState;
					if (this.Editable) {
						if (messageSendingAllowed) {
							enableButton = true;
							newState = SendImageButton.SendImageButtonMode.Send;
							chatFragment.AllowMediaPickerFragment = true;
						} else if (soundRecordingAllowed) {
							enableButton = true;
							newState = SendImageButton.SendImageButtonMode.Record;
							chatFragment.AllowMediaPickerFragment = true;
						} else {
							enableButton = false;
							newState = SendImageButton.SendImageButtonMode.Disabled;
							chatFragment.AllowMediaPickerFragment = false;
						}
					} else {
						enableButton = false;
						newState = SendImageButton.SendImageButtonMode.Disabled;
						chatFragment.AllowMediaPickerFragment = false;
					}
					oldState = chatFragment.SendButton.Mode;
					chatFragment.SendButton.Mode = newState;

					if (oldState != newState) {
						chatFragment.SendButton.Enabled = enableButton;
						chatFragment.setColorTheme ();
					}
				}
			}

			public override void ShowContactIsTyping (String typingMessage) {
				// don't do anything if we are already showing this exact message
				// this cuts down on flicker/animation affects
				if ((new EqualsBuilder<string> (typingMessage, displayedMessage)).Equals ()) {
					return;
				}

				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.MessageTypingViewWrapper.Visibility = ViewStates.Visible;
					Color color = backgroundColor.GetColor ();
					chatFragment.MessageTypingView.Text = typingMessage;
					chatFragment.sharedChatController.displayedMessage = typingMessage;
					chatFragment.MessageTypingView.SetTextColor (Android_Constants.WHITE_COLOR);
					chatFragment.MessageTypingViewWrapper.SetBackgroundColor (color);

					Animation animation = AnimationUtils.LoadAnimation (chatFragment.Activity, Resource.Animation.FadeIn);
					animation.SetAnimationListener (chatFragment);
					chatFragment.MessageTypingViewWrapper.StartAnimation (animation);
				}
			}

			public override void HideContactIsTyping () {
				this.typingMessage = null; // HACK alert, manually changing this flag to help with our animations
				this.displayedMessage = null;

				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.MessageTypingView.Text = this.displayedMessage;
					chatFragment.MessageTypingViewWrapper.Visibility = ViewStates.Gone;
					Animation animation = AnimationUtils.LoadAnimation (chatFragment.Activity, Resource.Animation.FadeOut);
					animation.SetAnimationListener (chatFragment);
					chatFragment.MessageTypingViewWrapper.StartAnimation (animation);
				}
			}

			public override void DidChangeColorTheme () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.setColorTheme ();
					chatFragment.listAdapter.NotifyDataSetChanged ();
				}
			}

			public override void DidChangeDisplayName () {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.setColorTheme ();
					chatFragment.listAdapter.NotifyDataSetChanged ();
				}
			}

			public override void UpdateChatRows (em.Message message) {
				if (viewModel == null) {
					return;
				}

				EMTask.DispatchMain (() => {
					ChatFragment chatFragment = this.ChatFragment;
					if (!GCCheck.ViewGone (chatFragment) && viewModel != null ) {
						Media media = message.media;
						if (media != null) {
							int position = viewModel.IndexOf (message);
							View cfv = chatFragment.View;
							if ( cfv != null ) {
								RecyclerView listView = chatFragment.MainListView;
								ChatMessageAdapter adapter = chatFragment.listAdapter;

								int uiPosition = adapter.ModelPositionToUIPosition (position);

								LinearLayoutManager layoutManager = (LinearLayoutManager)listView.GetLayoutManager ();
								int firstVisibleItem = layoutManager.FindFirstVisibleItemPosition ();
								View v = layoutManager.GetChildAt (uiPosition - firstVisibleItem);
								if (v != null) {
									RecyclerView.ViewHolder oholder = listView.GetChildViewHolder (v);
									if (oholder is MessageViewHolder) {
										MessageViewHolder holder = (MessageViewHolder)oholder;
										holder.UpdateMessageContents ();
									}
								}
							}
						}
					}
				});
			}

			public override void CounterpartyPhotoDownloaded (CounterParty counterparty) {
				// reload if the contact photos have changed
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.listAdapter.NotifyDataSetChanged ();
				}
			}

			public override void DidChangeTotalUnread (int unreadCount) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					chatFragment.UpdateUnreadCount (unreadCount);
				}
			}

			public override void ConfirmRemoteTakeBack (int index) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					AlertDialog.Builder builder = new AlertDialog.Builder (chatFragment.Activity);
					builder.SetTitle ("REMOTE_DELETE_BUTTON".t ());
					builder.SetMessage ("REMOTE_DELETE_EXPLAINATION".t ());
					//builder.SetView(connection_string_view);
					builder.SetPositiveButton ("REMOTE_DELETE_BUTTON".t (), (sender, dialogClickEventArgs) => ContinueRemoteTakeBack (index));
					builder.SetNegativeButton ("CANCEL_BUTTON".t (), (sender, dialogClickEventArgs) => {});
					builder.Create ();
					builder.Show ();
				}
			}

			public override void ConfirmMarkHistorical (int index) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					AlertDialog.Builder builder = new AlertDialog.Builder (chatFragment.Activity);
					builder.SetTitle ("DELETE_BUTTON".t ());
					builder.SetMessage ("DELETE_EXPLAINATION".t ());
					//builder.SetView(connection_string_view);
					builder.SetPositiveButton ("DELETE_BUTTON".t (), (sender, dialogClickEventArgs) => displayedChatEntry.MarkHistoricalAsync (viewModel [index]));
					builder.SetNegativeButton ("CANCEL_BUTTON".t (), (sender, dialogClickEventArgs) => {});
					builder.Create ();
					builder.Show ();
				}
			}

			public override void ContactSearchPhotoUpdated (Contact c) {
				ChatFragment chatFragment = this.ChatFragment;
				if (!GCCheck.ViewGone (chatFragment)) {
					IList<Contact> contactList = chatFragment.ContactList;
					if (contactList == null) {
						return;
					}
					ListView listView = chatFragment.ContactSearchListView;
					int firstVisiblePosition = listView.FirstVisiblePosition;
					int lastVisiblePosition = listView.LastVisiblePosition;
					int row = 0;
					bool isVisible = false;
					AggregateContact aggContact = null;
					for (; row + firstVisiblePosition <= lastVisiblePosition; row++) {
						if (row + firstVisiblePosition >= contactList.Count) {
							return;
						}
						if (contactList[row + firstVisiblePosition].serverContactID == c.serverContactID) {
							aggContact = new AggregateContact(contactList [row + firstVisiblePosition]);
							isVisible = true;
							break;
						}
					}
					if (!isVisible || row >= listView.Count) {
						return;
					}

					View rowView = listView.GetChildAt (row);
					rowView = chatFragment.ContactListAdapter.GetView (row + firstVisiblePosition, rowView, listView);
					ContactListViewHolder viewHolder = rowView.Tag as ContactListViewHolder;
					BitmapSetter.SetThumbnailImage (viewHolder, aggContact.ContactForDisplay, chatFragment.Context.Resources, viewHolder.ThumbnailView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
				}
			}
		}
	}
}