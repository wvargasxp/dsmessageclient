using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using AVFoundation;
using CoreAnimation;
using CoreGraphics;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using MBProgressHUD;
using UIKit;
using System.Timers;

namespace iOS {

	using Media_iOS_Extension;
	using String_UIKit_Extension;
	using UIDevice_Extension;

	public class ChatViewController : AbstractAcquiresImagesController, IContactSource, IContactSearchController {

		public bool NEW_MESSAGE_INITIATED_FROM_NOTIFICATION = false; 

		static readonly int TAG_STAGED_ITEM = 0x10;
		static readonly int TAG_BUTTON_BASE = 0xFA0;

		readonly int GROUP_NAMEBAR_HEIGHT = 20;

		ChatEntry chatEntry;
		public ChatEntry ChatEntry {
			get { return chatEntry; }
			set { chatEntry = value; }
		}

		SharedChatController sharedChatController;

		AppDelegate appDelegate;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		UINavigationItem nI;
		protected UINavigationItem ChatNavigationItem {
			get { 
				if (nI == null) {
					UINavigationController nav = this.NavigationController;
					if (nav != null) {
						int numControllers = nav.ViewControllers.Length;
						if (numControllers >= 2) {
							UIViewController v = nav.ViewControllers [numControllers - 2];
							nI = v.NavigationItem;
						}
					}
				}
			
				return nI;
			}
		}

		#region UI Related vars + constants

		RemoteMessageTypingView typingView;
		protected RemoteMessageTypingView TypingView {
			get { return typingView; }
			set { typingView = value; }
		}

		UIBarButtonItem cancelContactSearchNavigationButton;
		public UIBarButtonItem CancelContactSearchNavigationButton {
			get {
				if (cancelContactSearchNavigationButton == null) {
					cancelContactSearchNavigationButton = new UIBarButtonItem (UIBarButtonSystemItem.Cancel);
					cancelContactSearchNavigationButton.Title = "CANCEL_BUTTON".t ();
					cancelContactSearchNavigationButton.Clicked += WeakDelegateProxy.CreateProxy<object,EventArgs> (DidTapCancelSearchButton).HandleEvent<object,EventArgs>;
				}
				return cancelContactSearchNavigationButton;
			}
		}

		protected void DidTapCancelSearchButton(object sender, EventArgs e) {
			if (sharedChatController != null)
				sharedChatController.ClearContactsToReplyTo ();
			ShowMainTable ();
		}

		public UIBarButtonItem detailsButton;

		#region bottom bar in chat screen
		UIView chattyBottomBar;
		UIButton attachmentsButton;
		SendUIButton sendButton;

		private PopupWindowController PopupController { get; set; }

        private nfloat _keyboardOrigin = 0;
		private nfloat KeyboardOrigin { get { return this._keyboardOrigin; } set { this._keyboardOrigin = value; } }

		readonly int SEND_BUTTON_SIZE = 48;
		readonly int ATTACHMENT_BUTTON_SIZE = 48;

		NSObject enterForeGroundObserver = null;
		NSObject statusBarChangedObserver = null;
		NSObject applicationDidBecomeActiveObserver = null;
		NSObject remoteDeleteSelectedObserver = null;
		NSObject chatCopySelectedObserver = null;
		NSObject chatTextViewTappedObserver = null;
		NSObject menuDidHideObserver = null;

		#region text entry height
		ResizableCaretTextView textEntryTextField;
		public nfloat textFieldHeightDifference = 0;
		readonly int TEXT_ENTRY_Y_ORIGIN = 6;
		nfloat textEntryWidthPortrait;
		nfloat textEntryWidthLandscape;
		public UIImageView textFieldImage;
		bool reachedTopEdge = false;
		readonly int IPHONE_TEXT_ENTRY_HEIGHT = 36;
		bool finishedSending = false;
		UIEdgeInsets originalInsets;
		#endregion

		#endregion

		// main view where all the chats are going to be located
		UITableView tableView;
		public UITableView ChatTableView {
			get { return tableView; }
		}

		#region group name bar
		UIView groupNameBar;
		UILabel groupNameLabel;
		#endregion

		#region top bar in chat screen
		UIView chattyTopBar;
		UIButton addContactsButton;
		bool topBarHidden;
		UIView lineView;
		UILabel toLabel;
		ContactSearchTextView contactSearchTextView;
		public ContactSearchTextView ContactSearchTextView {
			get {
				if (contactSearchTextView == null) {
					contactSearchTextView = new ContactSearchTextView (new CGRect (0, 0, chattyTopBar.Frame.Width, chattyTopBar.Frame.Height)); // chattyTopBar shouldn't be null here.
					contactSearchTextView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
					contactSearchTextView.Delegate = new ContactSearchTextViewDelegate (contactSearchTextView, this);
					contactSearchTextView.Font = FontHelper.DefaultFontForTextFields ();
					contactSearchTextView.ClipsToBounds = true;
					contactSearchTextView.AutocorrectionType = UITextAutocorrectionType.No;
					contactSearchTextView.BackgroundColor = UIColor.Clear;
					//contactSearchTextView.Text = "SEARCH_TITLE".t ();
				}
				return contactSearchTextView;
			}
		}

		int TOP_BAR_HEIGHT_IPHONE = 40;

		FromAliasPickerViewModel pickerModel;
		UIPickerView picker;
		UIToolbar fromAliasPickerToolbar = null;
		protected UIPickerView FromAliasPicker {
			get { return picker; }
			set { picker = value; }
		}

		protected FromAliasPickerViewModel FromAliasPickerViewModel {
			get { return pickerModel; }
			set { pickerModel = value; }
		}

		protected UIToolbar FromAliasPickerToolbar {
			get { 
				if (fromAliasPickerToolbar == null) {
					var toolbar = new UIToolbar ();
					toolbar.BarStyle = UIBarStyle.Default;
					toolbar.Translucent = true;
					toolbar.SizeToFit ();

					var doneButton = new UIBarButtonItem("DONE_BUTTON".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapDoneFromAliasPickerToolbar).HandleEvent<object,EventArgs>);

					doneButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
					toolbar.SetItems (new []{ doneButton }, true); 
					fromAliasPickerToolbar = toolbar;
				}
				return fromAliasPickerToolbar; 
			}
			set { fromAliasPickerToolbar = value; }
		}

		protected void DidTapDoneFromAliasPickerToolbar(object s, EventArgs e) {
			fromAliasTextView.ResignFirstResponder ();
		}

		bool fromAliasBarShowing = false;
		protected bool FromAliasBarShowing {
			get { return fromAliasBarShowing; }
			set { fromAliasBarShowing = value; }
		}

		UIView fromAliasBar;
		protected UIView FromAliasBar {
			get {
				if (fromAliasBar == null) {
					fromAliasBar = new UIView (new CGRect (0, 0, View.Frame.Width, TOP_BAR_HEIGHT_IPHONE));
					fromAliasBar.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
					fromAliasBar.BackgroundColor = UIColor.FromRGBA (Constants.RGB_TOOLBAR_COLOR [0], Constants.RGB_TOOLBAR_COLOR[1], Constants.RGB_TOOLBAR_COLOR[2], 255);
					fromAliasBar.Layer.BorderWidth = 0.5f;
				}
				return fromAliasBar;
			}
		}

		UILabel fromAliasToLabel;
		protected UILabel FromAliasToLabel {
			get {
				if (fromAliasToLabel == null) {
					fromAliasToLabel = new UILabel (new CGRect (0, 0, UI_CONSTANTS.TO_LABEL_SIZE + 10, UI_CONSTANTS.TO_LABEL_SIZE));
					fromAliasToLabel.Text = "FROM".t ();
					fromAliasToLabel.Font = FontHelper.DefaultFontForLabels (13f);
				}
				return fromAliasToLabel;
			}
		}

		BlockContextMenuTextView fromAliasTextView;
		protected BlockContextMenuTextView FromAliasTextView {
			get {
				if (fromAliasTextView == null) {
					fromAliasTextView = new BlockContextMenuTextView (new CGRect (0, 0, this.FromAliasBar.Frame.Width, this.FromAliasBar.Frame.Height));
					fromAliasTextView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
					fromAliasTextView.Font = FontHelper.DefaultFontForTextFields ();
					fromAliasTextView.ClipsToBounds = true;
					fromAliasTextView.AutocorrectionType = UITextAutocorrectionType.No;
					fromAliasTextView.BackgroundColor = UIColor.Clear;

					FromAliasPicker = new UIPickerView ();
					IList<AliasInfo> aliases = AppDelegate.Instance.applicationModel.account.accountInfo.ActiveAliases;
					FromAliasPickerViewModel = new FromAliasPickerViewModel (aliases);
					FromAliasPickerViewModel.PickerChanged += WeakDelegateProxy.CreateProxy<object,AliasInfoPickerChangedEventArgs> (AliasPickerChanged).HandleEvent<object,AliasInfoPickerChangedEventArgs>;
						
					FromAliasPicker.Model = FromAliasPickerViewModel;
					FromAliasPicker.ShowSelectionIndicator = true;
					FromAliasTextView.InputView = FromAliasPicker;
					FromAliasTextView.InputAccessoryView = FromAliasPickerToolbar;

					picker.Select (sharedChatController.CurrentRowForFromAliasPicker (), 0, true);

					#region selection
					UITapGestureRecognizer tap = new UITapGestureRecognizer ((Action) WeakDelegateProxy.CreateProxy(DidTapAliasTextView).HandleEvent);
					fromAliasTextView.AddGestureRecognizer (tap);

					#endregion
				}
				return fromAliasTextView;
			}
		}

		protected void AliasPickerChanged(object sender, AliasInfoPickerChangedEventArgs e) {
			sharedChatController.UpdateFromAlias (e.SelectedValue);
		}

		protected void DidTapAliasTextView() {
			if (sharedChatController.AllowsFromAliasChange) {
				ShowMainTable ();
				fromAliasTextView.BecomeFirstResponder ();
			}
		}

		#endregion
		static readonly int TYPING_VIEW_HEIGHT = 18;

		bool scrollToBottomAfterLayout;

		// constants used for UI
		int CHAT_TEXTFIELD_PADDING = 6; // The text entry field will have a padding of 6 above and below it.
		float PADDING = 10; // Default variable used when adding unspecific padding.

		// For iPhone, a size of 48 perfectly matches the font where the bottom toolbar will not be resized when a user presses a key on the keyboard.
		// For example, if it's set at 45 and the user presses a letter, the bottom toolbar will resize a few pixels to the new height of 48.
		int CHAT_TOOLBAR_HEIGHT_IPHONE = 48; 

		// related to UI updates, rotation, keyboard updates
		#region ui updates that requires tracking or callbacks
		bool isRotating = false; // tracking rotation

		// callback methods that get called when keyboard updates its state
		NSObject keyboardWillShowCallback;
		NSObject keyboardWillHideCallback;

		// callback method for when a textview has become first responder - iOS9 related
		NSObject textViewBecameFirstResponderObserver;

		// keeping track of the bottombar and tableview frame for when laying out subviews
		CGRect expectedBottomBarFrame;
		CGRect expectedTableViewFrame;
		#endregion

		#endregion

		#region updating contact search thumbnails
		public IList<Contact> ContactList { 
			get { return sharedChatController.SearchContacts; }
			set { sharedChatController.SearchContacts = value; } 
		}
		#endregion

		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		ContactSearchController contactSearchController;

		public ContactSearchController ContactSearchController {
			get { return contactSearchController; }
		}

		public ChatViewController (ChatEntry ce) {
			chatEntry = ce;

			appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			sharedChatController = new SharedChatController (appDelegate.applicationModel, chatEntry, chatEntry.entryOrder < 0 ? null : chatEntry,  this);
			scrollToBottomAfterLayout = false;

			sharedChatController.SoundRecordingRecorderController.OnFinishRecordingSuccess = this.OnFinishRecordingSuccess;
		}

		protected override void Dispose(bool disposing) {
			base.Dispose (disposing);
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations () {
			return UIInterfaceOrientationMask.All;
		}

		public override bool ShouldAutorotate () {
			return true;
		}

		public override bool ShouldAutomaticallyForwardRotationMethods {
			get {
				return true;
			}
		}

		protected void DidTapDetailsButton(object sender, EventArgs evetArgs) {
			if (sharedChatController.IsGroupConversation) {
				//go to group details page
				Group g = Contact.FindGroupByServerID(((AppDelegate)UIApplication.SharedApplication.Delegate).applicationModel, chatEntry.contacts[0].serverID);
				NavigationController.PushViewController (new EditGroupViewController (true, g, false), true);
			} else if (chatEntry.contacts != null && chatEntry.contacts.Count == 1) {
				//go to profile page
				NavigationController.PushViewController (new ProfileViewController (chatEntry.contacts [0], false), true);
			} else if (chatEntry.contacts != null && chatEntry.contacts.Count > 1) {
				//go to profile list page
				NavigationController.PushViewController (new ProfileListViewController (chatEntry), true);
			}
		}

		protected void BeginRecording () {
			SoundRecordingRecorderController controller = sharedChatController.SoundRecordingRecorderController;
			controller.Record ();
		}

		protected void FinishRecording () {
			SoundRecordingRecorderController controller = sharedChatController.SoundRecordingRecorderController;
			controller.Finish ();
		}

		protected void CancelRecording () {
			SoundRecordingRecorderController controller = sharedChatController.SoundRecordingRecorderController;
			controller.Cancel ();
		}

		protected void OnFinishRecordingSuccess (string recordingPath) {
			sharedChatController.AddStagedItem (recordingPath);
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
					ThemeController (InterfaceOrientation);
				}
			}
		}

		protected void DidTapSendButton(object sender, EventArgs eventArgs) {
			if (recordingHeld) {
				this.RecordingHeld = false;
				this.PopupController.Dismiss ();
				this.FinishRecording ();
			} else {
				var msg = textEntryTextField.Text;
				chatEntry.underConstruction = msg;
				sharedChatController.SendMessage ();
			}
		}

		protected void DidHoldSendButton (object sender, EventArgs eventArgs) {
			if (sendButton.Mode == SendUIButton.SendUIButtonState.Record) {
				this.RecordingHeld = true;
				this.sharedChatController.SoundRecordingInlineController.Stop ();
				this.BeginRecording ();
				this.PopupController.Show ();
			} else {
			}
		}

		protected void DidCancelTapSendButton (object sender, EventArgs eventArgs) {
			if (recordingHeld) {
				this.RecordingHeld = false;
				this.PopupController.Dismiss ();
				this.CancelRecording ();
			} else {
			}
		}

		protected void DidTapAddContactsButton(object sender, EventArgs eventArgs) {
			AddressBookArgs args = AddressBookArgs.From (false, true, false, this.sharedChatController.sendingChatEntry.contacts, this.sharedChatController.sendingChatEntry);
			AddressBookViewController addressBookViewController = new AddressBookViewController (args);
			addressBookViewController.DelegateContactSelected += WeakDelegateProxy.CreateProxy<AddressBookSelectionResult> (HandleAddressBookSelectionResult).HandleEvent<AddressBookSelectionResult>;

			this.NavigationController.PushViewController (addressBookViewController, true);
			this.ContactSearchTextView.ResignFirstResponder ();
			this.textEntryTextField.ResignFirstResponder ();
			ShowMainTable ();
		}

		public void ShowMediaInStagingArea (UIView view) {
			if (sharedChatController != null) {
				// We are disallowing multiple pictures as of now.
				// If we're currently staging media, just remove the old media and then adding the new one should be seamless.
				if (sharedChatController.IsStagingMedia) {
					UIView stagedMediaView = this.View.ViewWithTag (TAG_STAGED_ITEM);
					if (stagedMediaView != null) {
						stagedMediaView.RemoveFromSuperview ();
					}
				}

				float heightToWidth = (float)view.Frame.Height / (float)view.Frame.Width;
				CGSize stagedMediaSize = MediaSizeHelper.SharedInstance.ThumbnailSizeForHeightToWidth (heightToWidth);
				view.Frame = new CGRect (new CGPoint (sharedChatController.StagedMediaOffset, sharedChatController.StagedMediaOffset), stagedMediaSize);

				// round the corners
				view.Layer.CornerRadius = iOS_Constants.DEFAULT_CORNER_RADIUS;
				view.Layer.MasksToBounds = true;

				view.Tag = TAG_STAGED_ITEM;

				sharedChatController.StagedMediaHeight = (float)view.Frame.Height;
				sharedChatController.StagedMediaWidth = (float)view.Frame.Width;

				textEntryTextField.AddSubview (view);
				View.SetNeedsLayout ();
			}
		}

		#region showing progress indicator
		MTMBProgressHUD progressHud;

		public void PauseUI () {
			EMTask.DispatchMain (() => {
				this.View.EndEditing (true);
				progressHud = new MTMBProgressHUD (View) {
					LabelText = "WAITING".t (),
					LabelFont = FontHelper.DefaultFontForLabels(),
					RemoveFromSuperViewOnHide = true
				};

				this.View.Add (progressHud);
				progressHud.Show (animated: true);
			});
		}

		public void ResumeUI () {
			EMTask.DispatchMain (() => {
				if (progressHud == null) {
					return;
				}

				progressHud.Hide (animated: true, delay: 0);
			});
		}
		#endregion

		#region media selection
		public override string ImageSearchSeedString {
			get {
				return sharedChatController != null ? sharedChatController.ImageSearchSeedString : string.Empty;
			}
		}

		protected void DidTapAttachementsButton(object sender, EventArgs eventArgs) {
			if (this.contactSearchTextView != null) {
				this.contactSearchTextView.ResignFirstResponder ();
			}
			this.textEntryTextField.ResignFirstResponder ();
			this.StartAcquiringImageForChat ();
		}

		protected override void HandleImageSelected (object sender, UIImagePickerMediaPickedEventArgs e, bool isImage) {
			AppDelegate appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			string path = appDelegate.applicationModel.uriGenerator.GetNewMediaFileNameForStagingContents ();

			// get common info (shared between images and video)
			NSUrl referenceURL = e.Info[new NSString("UIImagePickerControllerReferenceUrl")] as NSUrl;
			if (referenceURL != null)
				Debug.WriteLine("Url:"+referenceURL.ToString ());
			NSObject imagePickerString = null;
			e.Info.TryGetValue (ChatMediaPickerController.SendAfterStagingKey, out imagePickerString);
			bool didNotComeFromImagePicker = imagePickerString != null;

			// if it was an image, get the other image info
			if(isImage) {
				path = path + ".jpeg";
				string systemPath = appDelegate.applicationModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (path);
				// get the original image
				UIImage originalImage = e.Info[UIImagePickerController.OriginalImage] as UIImage;
				ScaleImageAndFinish (originalImage, systemPath, path);
			} else { // if it's a video
				path = path + ".mp4";
				string systemPath = appDelegate.applicationModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (path);
				appDelegate.applicationModel.platformFactory.GetFileSystemManager ().CreateParentDirectories (path);

				// get video url
				NSUrl mediaURL = e.Info[UIImagePickerController.MediaURL] as NSUrl;
				if (mediaURL == null) {
					DisplayErrorMessage ();
				} else {
					PauseUI ();
					string mediaUrlPath = mediaURL.Path;
					VideoConverter.Shared.ConvertVideo (new ConvertVideoInstruction (mediaUrlPath, (bool success) => {
						ResumeUI ();
						if (success) {
							EMTask.DispatchMain (() => {
								AppDelegate.Instance.applicationModel.platformFactory.GetFileSystemManager ().MoveFileAtPath (mediaUrlPath, path);
								sharedChatController.AddStagedItem (path);
							});
						} else {
							DisplayErrorMessage ();
						}
					}));
				}
			}
			if (!didNotComeFromImagePicker) {
				this.NavigationController.DismissViewController (true, null);
			} else {
				EMTask.DispatchMain (() => {
					this.sendButton.SendActionForControlEvents (UIControlEvent.TouchUpInside);	
				});
			}
		}

		public override void HandleBulkMedia (List<AssetsLibrary.ALAsset> assets) {
			EMTask.DispatchBackground (() => {
				em.NotificationCenter.DefaultCenter.PostNotification (Constants.STAGE_MEDIA_BEGIN);
				IList<Message> messages = new List<Message> ();
				try {
					iOSFileSystemManager fsm = (iOSFileSystemManager)ApplicationModel.SharedPlatform.GetFileSystemManager ();
					foreach (AssetsLibrary.ALAsset asset in assets) {
						string assetPath = asset.AssetUrl.ToString ();
						string path = appDelegate.applicationModel.uriGenerator.GetNewMediaFileNameForStagingContents ();
						CGImage imageRef = null;
						string mimeType = "";
						if (asset.AssetType.Equals (AssetsLibrary.ALAssetType.Photo)) {
							mimeType = "image/png";
							path += ".png";
							imageRef = asset.DefaultRepresentation.GetFullScreenImage ();
							UIImage image = UIImage.FromImage (imageRef);
							using (Stream stream = image.AsPNG ().AsStream ()) {
								fsm.CopyBytesToPath (path, stream, null);
							}
						}
						float heightToWidth = ((float)imageRef.Height) / imageRef.Width;
						Message message = this.sharedChatController.CreateMessageFromStagedMedia (path, heightToWidth);
						messages .Add (message);
					}
				} finally {
					em.NotificationCenter.DefaultCenter.PostNotification (Constants.STAGE_MEDIA_DONE);
				}

				this.sharedChatController.SendMessagesFromStagedMedia (messages);
			});
		}

		protected override void HandleSearchImageSelected (UIImage originalImage) {
			AppDelegate appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			string path = appDelegate.applicationModel.uriGenerator.GetNewMediaFileNameForStagingContents ();
			path = path + ".jpeg";
			string systemPath = appDelegate.applicationModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (path);
			ScaleImageAndFinish (originalImage, systemPath, path);
		}

		public void ScaleImageAndFinish (UIImage originalImage, string systemPath, string path) {
			if (originalImage != null) {
				UIImage scaledAndRotated = originalImage.ScaleImage (Constants.MAX_DIMENSION_SENT_PHOTO);

				// do something with the image
				Debug.WriteLine ("got the original image");

				using (NSData imageData = scaledAndRotated.AsJPEG(Constants.JPEG_CONVERSION_QUALITY)) {
					byte[] updatedMedia = imageData != null ? imageData.ToByteArray () : null;
					if (updatedMedia != null) {
						ApplicationModel appModel = (UIApplication.SharedApplication.Delegate as AppDelegate).applicationModel;
						appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (systemPath, updatedMedia, null);
					}
				}

				sharedChatController.AddStagedItem (path);
			}
		}

		static void DisplayErrorMessage () {
			EMTask.DispatchMain (() => {
				var alert = new UIAlertView ("APP_TITLE".t (), "VIDEO_CONVERT_ERROR".t (), null, "OK_BUTTON".t (), null);
				alert.Show ();
			});
		}

		#endregion

		protected void DidTapRemoteActionButton (object sender, EventArgs EventArgs) {
			var button = sender as UIButton;
			nint indexOfMessageTapped = button.Tag - TAG_BUTTON_BASE;
			IList<Message> messages = sharedChatController.viewModel;
			Message message = messages [ (int) indexOfMessageTapped];  // TODO NPE check?
			Debug.Assert (message.HasRemoteAction, "DidTapRemoteActionButton called for a message with no remote action.");
			this.sharedChatController.ResponseToRemoteActionMessage (message);
		}

		protected void DidTapOpenMediaButton (object sender, EventArgs eventArgs) {
			var button = sender as UIButton;
			nint indexOfMessageTapped = button.Tag - TAG_BUTTON_BASE;

			IList<Message> messages = sharedChatController.viewModel;
			Message message = messages [ (int) indexOfMessageTapped];  // TODO NPE check?

			SharedChatController shared = this.sharedChatController;

			ContentType type = ContentType.Unknown;
			if (message.contentType != null) {
				type = ContentTypeHelper.FromString (message.contentType);
			}

			if ((type != ContentType.Unknown && ContentTypeHelper.IsAudio (type)) || ContentTypeHelper.IsAudio(message.media.uri.AbsolutePath)) {
				if (!this.recordingHeld) {
					shared.SoundRecordingInlineController.DidTapMediaButton (message.media);
				}
			} else {
				shared.TappedMediaMessage = message;
				this.NavigationController.PushViewController (new PhotoVideoController (shared), true);
			}
		}

		public void HandleAddressBookSelectionResult (AddressBookSelectionResult result) {
			this.sharedChatController.ManageContactsAfterAddressBookResult (result);
			ThemeController (this.InterfaceOrientation);
		}

		public void DidAddNewContact(Contact contact) {
			sharedChatController.AddContactToReplyTo (contact);
		}

		public void DidRemoveLastContact () {
			sharedChatController.RemoveContactToReplyTo ();
		}
			
		#region IContactSearchController
		public void UpdateContactsAfterSearch (IList<Contact> listOfContacts, string currentSearchFilter) {
			this.ContactSearchController.UpdateSearchContacts (listOfContacts, currentSearchFilter);
		}

		public void ShowList (bool shouldShowMainList) {
			if (shouldShowMainList) {
				ShowMainTable ();
			} else {
				ShowContactSearchTable ();
			}
		}
			
		public void RemoveContactAtIndex (int index) {
			sharedChatController.RemoveContactToReplyToAt (index);
			ThemeController (InterfaceOrientation);
		}

		public void InvokeFilter (string currentSearchFilter) {
			this.ContactSearchController.ShouldReloadForSearchString (currentSearchFilter);
		}

		public string GetDisplayLabelString () {
			return this.sharedChatController.ToFieldStringLabel;
		}

		public bool HasResults () {
			return this.ContactSearchController.HasResults;
		}

		#endregion

		#region Rotation
		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
			isRotating = true;
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);

			// Staged item seems to not be updated properly sometimes on rotation.
			// So we queue a redraw ourselves.
			if (sharedChatController.IsStagingMedia)
				sharedChatController.StagedMediaGetAspectRatio ();
		}

		public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation) {
			isRotating = false;
		}
		#endregion

		public BackgroundColor MainColor {
			get {
				BackgroundColor color;
				if (sharedChatController != null) {
					color = sharedChatController.backgroundColor;
				} else {
					color = BackgroundColor.Default;
				}

				return color;
			}
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			BackgroundColor mainColor = sharedChatController.backgroundColor;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && lineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					lineView.BackgroundColor = mainColor.GetColor ();
				}
			});


			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);

			if (addContactsButton != null) {
				mainColor.GetChatAddContactButtonResource ((UIImage image) => {
					if (addContactsButton != null) {
						addContactsButton.SetBackgroundImage (image, UIControlState.Normal);
					}
				});
			}

			if (attachmentsButton != null) {
				if (this.recordingHeld) {
					attachmentsButton.SetImage (ImageSetter.GetResourceImage (mainColor.GetChatRecordingIndicatorResource ()), UIControlState.Normal);
				} else {
					mainColor.GetChatAttachmentsResource ((UIImage image) => {
						if (attachmentsButton != null) {
							attachmentsButton.SetImage (image, UIControlState.Normal);
						}
					});
				}
			}
			if (sendButton != null) {
				if (this.chatEntry != null) {
					switch (sendButton.Mode) {
					case SendUIButton.SendUIButtonState.Record:
						mainColor.GetChatVoiceRecordingButtonResource ((UIImage image) => {
							if (sendButton != null) {
								sendButton.SetImage (image, UIControlState.Normal);
							}
						});
						break;
					case SendUIButton.SendUIButtonState.Send:
						mainColor.GetChatSendButtonResource ((UIImage image) => {
							if (sendButton != null) {
								sendButton.SetImage (image, UIControlState.Normal);
							}
						});
						break;
					case SendUIButton.SendUIButtonState.Disabled:
						mainColor.GetChatVoiceRecordingButtonResource ((UIImage image) => {
							if (sendButton != null) {
								sendButton.SetImage (image, UIControlState.Normal);
							}
						});
						break;
					}
				} else {
					mainColor.GetChatSendButtonResource ((UIImage image) => {
						if (sendButton != null) {
							sendButton.SetImage (image, UIControlState.Normal);
						}
					});
				}
			}
		}

		public override void ViewWillAppear (bool animated) {
			AppDelegate.Instance.SetAudioSessionToRespectSilence ();

			if (enterForeGroundObserver == null) {
				enterForeGroundObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)em.Constants.DID_ENTER_FOREGROUND, notification => {
					if (tableView != null)
						tableView.ReloadData ();
				});
			}

			if (applicationDidBecomeActiveObserver == null) {
				applicationDidBecomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)em.Constants.DID_BECOME_ACTIVE, (NSNotification obj) => {
					if (this != null && this.IsViewLoaded) {
						this.View.SetNeedsLayout ();
					}
				});
			}

			if (statusBarChangedObserver == null) {
				statusBarChangedObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)AppDelegate.STATUS_BAR_CHANGED_NOTIFICATION, (NSNotification obj) => {
					if (this != null && this.IsViewLoaded) {
						this.View.SetNeedsLayout ();
					}

					textEntryTextField.ResignFirstResponder ();
				});
			}

			#region copy + paste related
			// We need to check for null because we only want to instantiate the observer once.
			// If we don't check for null, we'll leave dangling observers around which won't be able to be removed.
			if (remoteDeleteSelectedObserver == null) {
				remoteDeleteSelectedObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)iOS_Constants.NOTIFICATION_REMOTE_DELETE_SELECTED, HandleNotificationRemoteDeleteSelected);
			}

			if (chatCopySelectedObserver == null) {
				chatCopySelectedObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)iOS_Constants.NOTIFICATION_CHAT_COPY_SELECTED, HandleNotificationCopySelected);
			}

			if (chatTextViewTappedObserver == null) {
				chatTextViewTappedObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)iOS_Constants.NOTIFICATION_CHAT_TEXTVIEW_TAPPED, HandleNotificationTableViewRowTextViewTapped);
			}
			#endregion
				
			#region keyboard callbacks

			if (keyboardWillShowCallback == null) {
				keyboardWillShowCallback = UIKeyboard.Notifications.ObserveWillShow ((ShowKeyboardNotification));
			}

			if (keyboardWillHideCallback == null) {
				keyboardWillHideCallback = UIKeyboard.Notifications.ObserveWillHide ((HideKeyboardNotification));
			}
				
			#endregion

			if (this.textViewBecameFirstResponderObserver == null) {
				this.textViewBecameFirstResponderObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)iOS_Constants.NOTIFICATION_TEXTVIEW_BECAME_FIRST_RESPONDER, HandleTextViewBecameFirstResponder);
			}

			UIApplication.SharedApplication.SetStatusBarStyle (UIStatusBarStyle.LightContent, false);

			sharedChatController.UpdateToContactsView ();
			sharedChatController.chatList.ObtainUnreadCountAsync (sharedChatController.DidChangeTotalUnread);

			// The reason why we set DisposeOnDisappear here is because of the slide out behavior to pop a controller off the Navigation stack.
			// If you press the 'back' button on the navigation controller, the lifecycle is ViewWillDisappear -> ViewDidDisappear
			// If you slide the controller, the lifecycle can be either
			// ViewWillDisappear -> ViewDidDisappear (Completed Slide so you pop the controller off the stack) or
			// ViewWillDisappear -> ViewWillAppear (Incomplete Slide so you stay on the controller)
			// In the second case, we don't want to dispose of the shared controller.
			DisposeOnDisappear = false;


			SharedChatController shared = this.sharedChatController;
			// If we have a tapped media message (that means we entered the media gallery, lets scroll to the last media message we looked at instead of our saved scroll position.
			if (shared.TappedMediaMessage != null) {
				int index = shared.viewModel.IndexOf (shared.TappedMediaMessage);
				this.tableView.ScrollToRow (NSIndexPath.FromRowSection (index, 0), UITableViewScrollPosition.Top, false);
				shared.TappedMediaMessage = null;
			}
		}

		#region copy + paste + remote delete
		private void HandleNotificationRemoteDeleteSelected (NSNotification notification) {
			UITableViewCell cell = notification.Object as UITableViewCell;
			if (cell != null) {
				cell.ResignFirstResponder ();
				NSIndexPath path = this.ChatTableView.IndexPathForCell (cell);
				if (path != null) {
					this.sharedChatController.InitiateRemoteTakeBack (path.Row);
				}
			}
		}

		private void HandleNotificationCopySelected (NSNotification notification) {
			UITableViewCell cell = notification.Object as UITableViewCell;
			if (cell != null) {
				cell.ResignFirstResponder ();
				NSIndexPath path = this.ChatTableView.IndexPathForCell (cell);
				if (path != null) {
					this.sharedChatController.CopyTextToClipboard (path.Row);
				}
			}
		}

		private void HandleNotificationTableViewRowTextViewTapped (NSNotification notification) {
			UITableViewCell cell = notification.Object as UITableViewCell;
			if (cell != null) {
				UIMenuController menuController = UIMenuController.SharedMenuController;

				bool showMenu = false;
				if (this.textEntryTextField.IsFirstResponder) {
					this.textEntryTextField.OverrideNextResponder = cell;
					menuDidHideObserver = NSNotificationCenter.DefaultCenter.AddObserver (UIMenuController.DidHideMenuNotification, HandleMenuControllerDidHideNotification);
					showMenu = true;
				} else {
					if (cell.IsFirstResponder) {
						cell.ResignFirstResponder ();
						menuController.SetMenuVisible (false, true);
					} else {
						showMenu = true;
						cell.BecomeFirstResponder ();
					}
				}

				if (showMenu) {
					UIMenuItem remoteDeleteMenuItem = new UIMenuItem ("REMOTE_DELETE_BUTTON".t (), new ObjCRuntime.Selector (iOS_Constants.RemoteDeleteSelector));
					menuController.MenuItems = new UIMenuItem[] { remoteDeleteMenuItem };
					menuController.Update ();
					menuController.SetTargetRect (cell.Frame, this.ChatTableView);
					menuController.SetMenuVisible (true, true);
				}
			}
		}

		public void HandleMenuControllerDidHideNotification (NSNotification n) {
			this.textEntryTextField.OverrideNextResponder = null;
			NSNotificationCenter.DefaultCenter.RemoveObserver (menuDidHideObserver);
		}
		#endregion

		/* 
		 * iOS9 related.
		 * Function that handles when a NotifyOnResponderTextView posts a notification that it has become first responder.
		 * We're handling this because iOS9 does not trigger keyboard notification events when we switch between responders.
		 * This code programatically triggers this keyboard events by flipping between first responders
		 */
		private NotifyOnFirstResponderTextView PreviousFirstResponder { get; set; }
		protected void HandleTextViewBecameFirstResponder (NSNotification n) {
			if (!UIDevice.CurrentDevice.IsIos9Later ()) return;

			// The text view that is or has become first responder.
			NotifyOnFirstResponderTextView respondingTextView = null;

			// Here, we're just finding which textView had posted the notification.
			if (this.textEntryTextField.IsFirstResponder) {
				respondingTextView = this.textEntryTextField;
			}

			if (this.ContactSearchTextView.IsFirstResponder) {
				respondingTextView = this.ContactSearchTextView;
			}

			if (this.FromAliasTextView.IsFirstResponder) {
				respondingTextView = this.FromAliasTextView;
			}

			// We don't need to do any flipping of show/hide keyboards because the responders are still the same.
			// Use case: User tapping on the same UITextView over and over again.
			if (this.PreviousFirstResponder == respondingTextView) {
				return;
			}

			this.PreviousFirstResponder = respondingTextView;
				
			if (respondingTextView != null) {
				// We resign first responder here, to dismiss the keyboard.
				respondingTextView.ResignFirstResponder ();

				// The block here is to prevent an infinite loop (notification posting and looping into this function again).
				respondingTextView.BlockNextNotify = true;

				// Bring it back to the original responding text view.
				respondingTextView.BecomeFirstResponder ();
			}
		}

		public void ShowKeyboardNotification (object sender, UIKeyboardEventArgs args) {
//			Debug.WriteLine ("Handle ShowKeyboardNotification");
			UpdateViewFramesBasedOnKeyboardUpdate (sender, args);
		}

		public void HideKeyboardNotification (object sender, UIKeyboardEventArgs args) {
//			Debug.WriteLine ("Handle HideKeyboardNotification");
			UpdateViewFramesBasedOnKeyboardUpdate (sender, args);
		}

		public void UpdateViewFramesBasedOnKeyboardUpdate (object sender, UIKeyboardEventArgs args) {
			// convertedFrame - Gets the correct frame back by converting frames to the window. This is needed when rotating.
			// option - This is the animation curve the keyboard uses. It's bitshifted (by 16) to convert from an AnimationCurve to an AnimationOption.

			CGRect convertedFrame = View.ConvertRectFromView (args.FrameEnd, View.Window);

			nfloat keyboardY = convertedFrame.Y;

			this.KeyboardOrigin = keyboardY;
			UIViewAnimationOptions option = UIViewAnimationOptions.BeginFromCurrentState;

			// This if/else is handling the case where the textentrybar/bottombar is taller than it should be (past the topbar's y origin, or past the top layout guide), it sizes it down before animating
			// This prevents a choppy up -> down animation from doing the animation below, and then laying out subviews to another frame size.
			if (topBarHidden) {
				float chattyBottomBarY = (float)(keyboardY - chattyBottomBar.Frame.Height);
				float chattyBottomBarHeightDifference = 0;
				float displacement_y = (float)this.TopLayoutGuide.Length;
				float minYValue = displacement_y + 1;
				if (chattyBottomBarY < minYValue) {
					// The bottom bar has grown past (above) the top bar.
					float oldChattyBottomBarY = chattyBottomBarY;
					chattyBottomBarY = minYValue; // Set the chattyTopBar as the max it can grow to.
					chattyBottomBarHeightDifference = chattyBottomBarY - oldChattyBottomBarY;
					reachedTopEdge = true;
				} 

				expectedBottomBarFrame = new CGRect (chattyBottomBar.Frame.X, chattyBottomBarY, View.Frame.Width, chattyBottomBar.Frame.Height - chattyBottomBarHeightDifference);
			} else {
				float minYValue = (float) (chattyTopBar.Frame.Y + chattyTopBar.Frame.Height);
				float chattyBottomBarY = (float) (keyboardY - chattyBottomBar.Frame.Height);
				float chattyBottomBarHeightDifference = 0;
				if (chattyBottomBarY < minYValue) {
					// The bottom bar has grown past (above) the top bar.
					float oldChattyBottomBarY = chattyBottomBarY;
					chattyBottomBarY = minYValue; // Set the chattyTopBar as the max it can grow to.
					chattyBottomBarHeightDifference = chattyBottomBarY - oldChattyBottomBarY;
					reachedTopEdge = true;
				}

				// If the FromAliasTextView is first responder, we don't want the bottom bar to be above the FromAliasPicker toolbar.
				if (this.FromAliasTextView.IsFirstResponder) {
					expectedBottomBarFrame = new CGRect (chattyBottomBar.Frame.X, this.View.Frame.Height - chattyBottomBar.Frame.Height, View.Frame.Width, chattyBottomBar.Frame.Height - chattyBottomBarHeightDifference);
				} else {
					expectedBottomBarFrame = new CGRect (chattyBottomBar.Frame.X, chattyBottomBarY, View.Frame.Width, chattyBottomBar.Frame.Height - chattyBottomBarHeightDifference);
				}
			}

			if (topBarHidden) {
				if (sharedChatController.IsGroupConversation) {
					expectedTableViewFrame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height + groupNameBar.Frame.Height, View.Frame.Width, expectedBottomBarFrame.Y - (lineView.Frame.Y + lineView.Frame.Height + groupNameBar.Frame.Height));
				} else {
					expectedTableViewFrame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height, View.Frame.Width, expectedBottomBarFrame.Y - (lineView.Frame.Y + lineView.Frame.Height));
				}
			} else {
				expectedTableViewFrame = new CGRect (0, chattyTopBar.Frame.Y + chattyTopBar.Frame.Height, View.Frame.Width, expectedBottomBarFrame.Y - chattyTopBar.Frame.Y - chattyTopBar.Frame.Height);
			}

			if (isRotating) {
				chattyBottomBar.Frame = expectedBottomBarFrame;
				tableView.Frame = expectedTableViewFrame;
				contactSearchController.ContactSearchTableView.Frame = tableView.Frame;
				this.TypingView.Frame = new CGRect (0, chattyBottomBar.Frame.Y - TYPING_VIEW_HEIGHT, this.View.Frame.Width, TYPING_VIEW_HEIGHT);
				this.View.SetNeedsLayout ();
			} else {
				UIView.AnimateNotify (args.AnimationDuration, 0, option, () => {
					UIView.SetAnimationCurve (args.AnimationCurve);
					chattyBottomBar.Frame = expectedBottomBarFrame;
					tableView.Frame = expectedTableViewFrame;
					contactSearchController.ContactSearchTableView.Frame = tableView.Frame;

					if (this.sharedChatController.CanScrollToBottom) {
						ScrollChatToBottom (animate: true);
					}

					this.TypingView.Frame = new CGRect (0, chattyBottomBar.Frame.Y - TYPING_VIEW_HEIGHT, this.View.Frame.Width, TYPING_VIEW_HEIGHT);
				}, (bool finished) => {
					EMTask.DispatchMain (() => {
						this.View.SetNeedsLayout ();
					});
				});
			}	
		}

		public override void ViewDidAppear(bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			sharedChatController.ViewBecameVisible ();
			//if new message, give To: textbox focus and bring up the keyboard
			ContactSearchTextView.BecomeFirstResponder ();

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Chat View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());

			sharedChatController.PossibleFromBarVisibilityChange ();

			sharedChatController.UpdateSendingMode ();
		}

		bool DisposeOnDisappear = false;
		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			NSNotificationCenter.DefaultCenter.RemoveObserver (enterForeGroundObserver);

			if (UINavigationControllerHelper.IsViewControllerBeingPopped (this))
				DisposeOnDisappear = true;

			if (this.sharedChatController != null) {
				this.sharedChatController.SoundRecordingInlineController.Stop ();
			}
		}

		public override void ViewDidDisappear(bool animated) {
			ClearInProgressRemoteActionMessages ();

			sharedChatController.ViewBecameHidden ();
			this.Visible = false;

			if (chatEntry.isPersisted) {
				chatEntry.underConstruction = textEntryTextField.Text;
				chatEntry.SaveAsync ();
			}

			if ( DisposeOnDisappear ) {
				sharedChatController.Dispose ();

				keyboardWillHideCallback.Dispose ();
				keyboardWillShowCallback.Dispose ();

				NSNotificationCenter.DefaultCenter.RemoveObserver (this);
				NSNotificationCenter.DefaultCenter.RemoveObserver (remoteDeleteSelectedObserver);
				NSNotificationCenter.DefaultCenter.RemoveObserver (chatCopySelectedObserver);
				NSNotificationCenter.DefaultCenter.RemoveObserver (chatTextViewTappedObserver);
				NSNotificationCenter.DefaultCenter.RemoveObserver (statusBarChangedObserver);
				NSNotificationCenter.DefaultCenter.RemoveObserver (enterForeGroundObserver);
				NSNotificationCenter.DefaultCenter.RemoveObserver (applicationDidBecomeActiveObserver);
				NSNotificationCenter.DefaultCenter.RemoveObserver (this.textViewBecameFirstResponderObserver);

				this.PopupController.Dispose ();
			}
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			scrollToBottomAfterLayout = true;

			SetDefaultKeyboardOrigin ();

			#region contact search related

			AddressBookArgs args = AddressBookArgs.From (false, true, false, this.sharedChatController.sendingChatEntry);
			contactSearchController = new ContactSearchController (this, this.View.Frame, args);
			contactSearchController.ContactSourceCallback = (Contact selectedContact) => {
				DidAddNewContact (selectedContact);
				ThemeController (this.InterfaceOrientation);
				UpdateExclusionPathOnContactSearch ();
			};
			#endregion

			#region setting up the ui

			AutomaticallyAdjustsScrollViewInsets = false;
			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;

			View.Add (lineView);

			#region group name bar 
			groupNameBar = new UIView (new CGRect(0, 0, View.Frame.Width, GROUP_NAMEBAR_HEIGHT));
			groupNameBar.BackgroundColor = iOS_Constants.WHITE_COLOR;
			groupNameBar.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;

			groupNameLabel = new UILabel(new CGRect(0, 0, View.Frame.Width, groupNameBar.Frame.Height));
			groupNameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			groupNameLabel.Font = FontHelper.DefaultFontForLabels ();
			groupNameLabel.TextColor = iOS_Constants.BLACK_COLOR;
			groupNameLabel.TextAlignment = UITextAlignment.Center;
			groupNameLabel.AdjustsFontSizeToFitWidth = true;
			groupNameLabel.LineBreakMode = UILineBreakMode.TailTruncation;
			groupNameLabel.Lines = 1; // 0 means unlimited
			groupNameBar.Add(groupNameLabel);

			View.Add(groupNameBar);

			#endregion

			#region top bar
			detailsButton = new UIBarButtonItem ("DETAILS".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapDetailsButton).HandleEvent<object,EventArgs>);
			detailsButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			sharedChatController.ShowDetailsOption(true, false);

			chattyTopBar = new UIView (new CGRect (0, 0, View.Frame.Width, TOP_BAR_HEIGHT_IPHONE));
			chattyTopBar.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			chattyTopBar.BackgroundColor = UIColor.FromRGBA (Constants.RGB_TOOLBAR_COLOR [0], Constants.RGB_TOOLBAR_COLOR[1], Constants.RGB_TOOLBAR_COLOR[2], 255);
			chattyTopBar.Layer.BorderWidth = 0.5f;
			View.Add (chattyTopBar);

			toLabel = new UILabel (new CGRect (0, 0, UI_CONSTANTS.TO_LABEL_SIZE, UI_CONSTANTS.TO_LABEL_SIZE));
			toLabel.Text = "TO".t ();
			toLabel.Font = FontHelper.DefaultFontForLabels (16f);
			chattyTopBar.Add (toLabel);

			addContactsButton = new UIButton (UIButtonType.Custom);
			if(UIDevice.CurrentDevice.IsRightLeftLanguage())
				addContactsButton.Frame = new CGRect (addContactsButton.Frame.Width + PADDING, chattyTopBar.Frame.Height / 2 - addContactsButton.Frame.Height / 2, UI_CONSTANTS.ADD_BUTTON_IMAGE_SIZE, UI_CONSTANTS.ADD_BUTTON_IMAGE_SIZE);
			else
				addContactsButton.Frame = new CGRect (chattyTopBar.Frame.Width - addContactsButton.Frame.Width - PADDING, chattyTopBar.Frame.Height / 2 - addContactsButton.Frame.Height / 2, UI_CONSTANTS.ADD_BUTTON_IMAGE_SIZE, UI_CONSTANTS.ADD_BUTTON_IMAGE_SIZE);
			addContactsButton.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			chattyTopBar.Add (addContactsButton);

			chattyTopBar.Add (this.ContactSearchTextView);

			chattyTopBar.SendSubviewToBack (this.ContactSearchTextView);
			#endregion

			#region bottom bar
			chattyBottomBar = new UIView (new CGRect (0, View.Frame.Height - CHAT_TOOLBAR_HEIGHT_IPHONE, View.Frame.Width, CHAT_TOOLBAR_HEIGHT_IPHONE));
			chattyBottomBar.BackgroundColor = UIColor.FromRGBA (Constants.RGB_TOOLBAR_COLOR [0], Constants.RGB_TOOLBAR_COLOR[1], Constants.RGB_TOOLBAR_COLOR[2], 255);
			chattyBottomBar.Layer.BorderColor = UIColor.Gray.ColorWithAlpha (0.5f).CGColor;
			chattyBottomBar.Layer.BorderWidth = 0.5f;
			chattyBottomBar.AutoresizingMask = UIViewAutoresizing.FlexibleBottomMargin;
			View.Add (chattyBottomBar);

			BackgroundColor backgroundColor = sharedChatController.backgroundColor;
			int margin = -40;
			UIEdgeInsets imageEdgeInsets = new UIEdgeInsets (margin, margin, margin, margin);
			attachmentsButton = new UIButton (UIButtonType.Custom);
			backgroundColor.GetChatAttachmentsResource ( (UIImage image) => {
				if (attachmentsButton != null) {
					attachmentsButton.SetImage (image, UIControlState.Normal);
				}
			});
			attachmentsButton.ImageView.ContentMode = UIViewContentMode.Center;
			attachmentsButton.ImageEdgeInsets = imageEdgeInsets;
			attachmentsButton.Frame = new CGRect (0, 0, ATTACHMENT_BUTTON_SIZE, ATTACHMENT_BUTTON_SIZE);
			chattyBottomBar.Add (attachmentsButton);

			sendButton = new SendUIButton (UIButtonType.Custom);
			sendButton.SetImage (ImageSetter.GetResourceImage ("chat/iconSendDisabled.png"), UIControlState.Disabled);
			sendButton.ImageView.ContentMode = UIViewContentMode.Center;
			sendButton.ImageEdgeInsets = imageEdgeInsets;
			sendButton.Frame = new CGRect (0, 0, SEND_BUTTON_SIZE, SEND_BUTTON_SIZE);
			sendButton.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			sendButton.TitleLabel.Font = UIFont.BoldSystemFontOfSize (17.5f); // trying to get the size close to iMessage app
			sendButton.SetTitleColor (UIColor.Gray, UIControlState.Normal);
			chattyBottomBar.Add (sendButton);

			nfloat textEntryFieldHeight = chattyBottomBar.Frame.Height - (CHAT_TEXTFIELD_PADDING * 2); // padding times 2 because we want padding on the top and bottom of the text entry field
			nfloat pixelsToSubtractToGetTextEntryWidth = attachmentsButton.Frame.Width + sendButton.Frame.Width;

			textEntryWidthPortrait = UIScreen.MainScreen.Bounds.Width - pixelsToSubtractToGetTextEntryWidth;
			textEntryWidthLandscape = UIScreen.MainScreen.Bounds.Height - pixelsToSubtractToGetTextEntryWidth;

			if (UIDevice.CurrentDevice.IsIos8Later ()) {
				textEntryWidthPortrait = UIScreen.MainScreen.Bounds.Width - pixelsToSubtractToGetTextEntryWidth;
				textEntryWidthLandscape = UIScreen.MainScreen.Bounds.Width - pixelsToSubtractToGetTextEntryWidth;
			}

			nfloat textEntryFieldWidth;
			if (InterfaceOrientation == UIInterfaceOrientation.Portrait)
				textEntryFieldWidth = textEntryWidthPortrait;
			else
				textEntryFieldWidth = textEntryWidthLandscape;

			textEntryTextField = new ResizableCaretTextView (new CGRect (attachmentsButton.Frame.X + attachmentsButton.Frame.Width + PADDING, chattyBottomBar.Frame.Height / 2 - textEntryFieldHeight / 2, textEntryFieldWidth, textEntryFieldHeight));
			textEntryTextField.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			textEntryTextField.ClipsToBounds = true;
			textEntryTextField.Delegate = new TextViewDelegate (this);
			textEntryTextField.Text = chatEntry.underConstruction;
			if (textEntryTextField.Text != null && textEntryTextField.Text.Length > 0)
				sharedChatController.IsStagingText = true;
			textEntryTextField.Font = FontHelper.DefaultFontForTextFields ();
			textEntryTextField.AutocorrectionType = UITextAutocorrectionType.Yes;
			textEntryTextField.BackgroundColor = UIColor.Clear;
			textEntryTextField.Opaque = false;
			textEntryTextField.ScrollEnabled = true;
			textEntryTextField.Editable = sharedChatController.Editable;
			textEntryTextField.Layer.AnchorPoint = new CGPoint (0f, 1f); // rotate around bottom left corner
			originalInsets = textEntryTextField.TextContainerInset;

			textFieldImage = new UIImageView (textEntryTextField.Frame);
			textFieldImage.Image = UIImage.FromFile ("chat/text-field.png").StretchableImage (UI_CONSTANTS.ONBOARDING_TEXT_FIELD_LEFT_CAP, UI_CONSTANTS.ONBOARDING_TEXT_FIELD_TOP_CAP);
			textFieldImage.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			chattyBottomBar.Add (textFieldImage);

			chattyBottomBar.Add (textEntryTextField);
			chattyBottomBar.Layer.AnchorPoint = new CGPoint (0f, 1f); // rotate around bottom left corner

			this.TypingView = new RemoteMessageTypingView (new CGRect (0, 0, this.View.Frame.Width, TYPING_VIEW_HEIGHT), BackgroundColor.Blue);
			this.View.Add (this.TypingView);

			#endregion

			#region table view
			tableView = new UITableView (new CGRect (0, chattyTopBar.Frame.Y + chattyTopBar.Frame.Height, View.Frame.Width, View.Frame.Height - chattyTopBar.Frame.Height - chattyBottomBar.Frame.Height));
			tableView.DataSource = new TableViewDataSource (this);
			tableView.Delegate = new TableViewDelegate (this);
			tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			tableView.AllowsSelection = false;
			tableView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin;
			tableView.BackgroundColor = UIColor.Clear;
			View.Add (tableView);

			View.SendSubviewToBack (tableView);

			View.Add (contactSearchController.ContactSearchTableView);
			#endregion

			#endregion

			if (chatEntry.underConstructionMediaPath != null)
				sharedChatController.AddStagedItem (chatEntry.underConstructionMediaPath);

			sendButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapSendButton).HandleEvent<object,EventArgs>;
			sendButton.TouchDown += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidHoldSendButton).HandleEvent<object,EventArgs>;
			sendButton.TouchUpOutside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidCancelTapSendButton).HandleEvent<object,EventArgs>;
			sendButton.TouchCancel += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidCancelTapSendButton).HandleEvent<object,EventArgs>;
			addContactsButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapAddContactsButton).HandleEvent<object,EventArgs>;

			UpdateTitle ();

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			attachmentsButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapAttachementsButton).HandleEvent<object,EventArgs>;

			this.PopupController = new PopupWindowController ();
			this.PopupController.ChatViewController = this;

			AddTapGesturesToDismissKeyboard ();

			if (!sharedChatController.ShowAddContacts ())
				sharedChatController.HideAddContactsOption (false);
			else {
				contactSearchController.LoadContactsAsync ();
			}


			ThemeController (InterfaceOrientation);
		}

		private void SetDefaultKeyboardOrigin () {
			CGSize screenSizeAccordingToOrientation = AppDelegate.Instance.MainController.ScreenSizeAccordingToOrientation;
			this.KeyboardOrigin = screenSizeAccordingToOrientation.Height;
		}

		public void ScrollChatToBottom (bool animate = false) {
			IList<Message> viewModel = this.sharedChatController.viewModel;
			if (viewModel != null && viewModel.Count > 0) {
				this.tableView.ScrollToRow (NSIndexPath.FromRowSection (viewModel.Count - 1, 0), UITableViewScrollPosition.Bottom, animate);
			}
		}

		void UpdateTitle () {
			this.Title = this.sharedChatController.Title;
		}

		void UpdateGroupNameBarText () {
			if (sharedChatController.IsGroupConversation)
				groupNameLabel.Text = chatEntry.contacts[0].displayName;
		}

		void AddTapGesturesToDismissKeyboard () {
			// Adds a basic way to dismiss the keyboard
			var onTap1 = new UITapGestureRecognizer ((Action)WeakDelegateProxy.CreateProxy (DidTapToDismissKeyboard).HandleEvent);

			onTap1.NumberOfTapsRequired = 1;
			onTap1.CancelsTouchesInView = false;
			tableView.AddGestureRecognizer (onTap1);
		}

		void DidTapToDismissKeyboard() {
			if (ContactSearchTextView.IsFirstResponder)
				ContactSearchTextView.ResignFirstResponder ();
			else if (FromAliasTextView.IsFirstResponder)
				FromAliasTextView.ResignFirstResponder ();
			else
				textEntryTextField.ResignFirstResponder ();
		}

		void ChangeAliasBarVisibility (bool showBar) {
			if (showBar && !this.FromAliasBarShowing) {
				this.FromAliasBar.Add (this.FromAliasToLabel);
				this.FromAliasBar.Add (this.FromAliasTextView);
				this.View.Add (this.FromAliasBar);
				this.FromAliasBarShowing = true;
			} else if (!showBar && this.FromAliasBarShowing) {
				this.FromAliasBar.RemoveFromSuperview ();
				this.FromAliasBarShowing = false;
			}

		}

		public void ChangeFromBarSelectionEnabled (bool shouldEnableAliasSelection) {
			this.FromAliasTextView.UserInteractionEnabled = shouldEnableAliasSelection;
		}
			
		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews ();
			nfloat displacement_y = this.TopLayoutGuide.Length;

			#region hack to get around recording videos increasing the size of the root controller
			CGSize screenSize = AppDelegate.Instance.MainController.ScreenSizeAccordingToOrientation;
			nfloat boundingHeight = screenSize.Height;
			CGRect mainControllerFrame = AppDelegate.Instance.MainController.View.Frame;
			if (mainControllerFrame.Height > boundingHeight) {
				mainControllerFrame.Height = boundingHeight;
				AppDelegate.Instance.MainController.View.Frame = mainControllerFrame; 
			}
			#endregion

			ThemeController (InterfaceOrientation);

			PossibleResizeTextEntryArea (textEntryTextField);

			lineView.Frame = new CGRect (0, (float)displacement_y, lineView.Frame.Width, lineView.Frame.Height);

			if (sharedChatController.IsGroupConversation) {
				groupNameBar.Frame = topBarHidden ? new CGRect (0, lineView.Frame.Y + lineView.Frame.Height, View.Frame.Width, GROUP_NAMEBAR_HEIGHT) : CGRect.Empty;
				UpdateGroupNameBarText ();
			} else {
				groupNameBar.Frame = CGRect.Empty;
			}

			nfloat chattyBottomBarHeight = textEntryTextField.Frame.Height + (TEXT_ENTRY_Y_ORIGIN * 2);

			if (sharedChatController.IsStagingMedia) {
				if (sharedChatController.IsStagingText) {
					(textEntryTextField as ResizableCaretTextView).UseLargeCaret = false;
					textEntryTextField.TextContainer.ExclusionPaths = new UIBezierPath[] { };

					chattyBottomBarHeight += sharedChatController.StagedMediaHeight;
				} else {
					(textEntryTextField as ResizableCaretTextView).UseLargeCaret = true;
					UIBezierPath exclusionPath = UIBezierPath.FromRect (new CGRect (0, 0, sharedChatController.StagedMediaWidth + sharedChatController.StagedMediaOffset * 2, sharedChatController.StagedMediaHeight));
					textEntryTextField.TextContainer.ExclusionPaths = new [] { exclusionPath };
					textEntryTextField.TextContainerInset = originalInsets;
					chattyBottomBarHeight += sharedChatController.StagedMediaHeight - textEntryTextField.ContentSize.Height + sharedChatController.StagedMediaOffset * 2;
				}
			} else {
				(textEntryTextField as ResizableCaretTextView).UseLargeCaret = false;
				textEntryTextField.TextContainer.ExclusionPaths = new UIBezierPath[] { };
				textEntryTextField.TextContainerInset = originalInsets;
				textEntryTextField.SizeToFit ();
				chattyBottomBarHeight = textEntryTextField.Frame.Height + (TEXT_ENTRY_Y_ORIGIN * 2);
			}
	
			reachedTopEdge = false;

			// View frames aren't correct in ViewDidLoad, so set the frames again in this function.
			if (!topBarHidden) {
				// when the top bar is up
				#region sizing the top bar

				var frameOfFirstBar = new CGRect (
					0, 
					lineView.Frame.Y + lineView.Frame.Height, 
					View.Frame.Width, 
					TOP_BAR_HEIGHT_IPHONE
				);

				if (this.FromAliasBarShowing) {
					this.FromAliasBar.Frame = frameOfFirstBar;

					chattyTopBar.Frame = new CGRect (
						0, 
						this.FromAliasBar.Frame.Y + this.FromAliasBar.Frame.Height, 
						View.Frame.Width, 
						TOP_BAR_HEIGHT_IPHONE
					);
				} else {
					chattyTopBar.Frame = frameOfFirstBar;
				}
				#endregion

				#region RTL + FromAlias subviews framing
				if (UIDevice.CurrentDevice.IsRightLeftLanguage ()) {
					this.FromAliasToLabel.Frame = new CGRect (
						this.FromAliasBar.Frame.Width - this.FromAliasToLabel.Frame.Width - UI_CONSTANTS.EXTRA_MARGIN,
						this.FromAliasBar.Frame.Height / 2 - this.FromAliasToLabel.Frame.Height / 2,
						this.FromAliasToLabel.Frame.Width,
						this.FromAliasToLabel.Frame.Height
					);

				} else {
					this.FromAliasToLabel.Frame = new CGRect (
						UI_CONSTANTS.SMALL_MARGIN, 
						this.FromAliasBar.Frame.Height / 2 - this.FromAliasToLabel.Frame.Height / 2,
						this.FromAliasToLabel.Frame.Width,
						this.FromAliasToLabel.Frame.Height
					);
				}


				this.FromAliasTextView.Frame = new CGRect (
					0, 
					0, 
					this.FromAliasBar.Frame.Width, 
					this.FromAliasBar.Frame.Height
				);
				#endregion

				#region RTL + Topbar subviews framing
				/* support LTR & RTL langauges */
				if (UIDevice.CurrentDevice.IsRightLeftLanguage ()) {
					addContactsButton.Frame = new CGRect (
						UI_CONSTANTS.SMALL_MARGIN,
						chattyTopBar.Frame.Height / 2 - addContactsButton.Frame.Height / 2,
						addContactsButton.Frame.Width,
						addContactsButton.Frame.Height
					);

					toLabel.Frame = new CGRect (
						chattyTopBar.Frame.Width - toLabel.Frame.Width - UI_CONSTANTS.EXTRA_MARGIN,
						chattyTopBar.Frame.Height / 2 - toLabel.Frame.Height / 2,
						toLabel.Frame.Width,
						toLabel.Frame.Height
					);

				} else {
					toLabel.Frame = new CGRect (
						UI_CONSTANTS.SMALL_MARGIN, 
						chattyTopBar.Frame.Height / 2 - toLabel.Frame.Height / 2,
						toLabel.Frame.Width,
						toLabel.Frame.Height
					);

					addContactsButton.Frame = new CGRect (
						chattyTopBar.Frame.Width - addContactsButton.Frame.Width - UI_CONSTANTS.SMALL_MARGIN,
						chattyTopBar.Frame.Height / 2 - addContactsButton.Frame.Height / 2,
						addContactsButton.Frame.Width,
						addContactsButton.Frame.Height
					);
				}


				this.ContactSearchTextView.Frame = new CGRect (
					0, 
					0, 
					chattyTopBar.Frame.Width, 
					chattyTopBar.Frame.Height
				);
				#endregion

				if (textEntryTextField.IsFirstResponder || this.ContactSearchTextView.IsFirstResponder) {
					// when the keyboard is on screen

					// note; there's a chance keyboardOrigin will be wrong (rotating the device with the keyboard up)
					// the second call if this function should get the correct origin
					// willrotate (keyboard is up) -> subviews are laid out (incorrect keyboardOrigin because keyboardwillhide is called), keyboardwillshow is called (keyboardOrigin is correct again) -> subviews are laid out again

					nfloat chattyBottomBarY = this.KeyboardOrigin - chattyBottomBarHeight;
					nfloat chattyBottomBarHeightDifference = 0;
					nfloat minYValue = chattyTopBar.Frame.Y + chattyTopBar.Frame.Height;
					if (chattyBottomBarY < minYValue) {
						// The bottom bar has grown past (above) the top bar.
						nfloat oldChattyBottomBarY = chattyBottomBarY;
						chattyBottomBarY = minYValue; // Set the chattyTopBar as the max it can grow to.
						chattyBottomBarHeightDifference = chattyBottomBarY - oldChattyBottomBarY;
						reachedTopEdge = true;
					}

					chattyBottomBar.Frame = new CGRect (chattyBottomBar.Frame.X, chattyBottomBarY, View.Frame.Width, chattyBottomBarHeight - chattyBottomBarHeightDifference);
					tableView.Frame = new CGRect (tableView.Frame.X, minYValue, View.Frame.Width, chattyBottomBar.Frame.Y - minYValue);
				} else {
					nfloat chattyBottomBarY = View.Frame.Height - chattyBottomBarHeight;
					nfloat chattyBottomBarHeightDifference = 0;
					nfloat minYValue = chattyTopBar.Frame.Y + chattyTopBar.Frame.Height;
					if (chattyBottomBarY < minYValue) {
						// The bottom bar has grown past (above) the top bar.
						nfloat oldChattyBottomBarY = chattyBottomBarY;
						chattyBottomBarY = minYValue; // Set the chattyTopBar as the max it can grow to.
						chattyBottomBarHeightDifference = chattyBottomBarY - oldChattyBottomBarY;
						reachedTopEdge = true;
					}

					chattyBottomBar.Frame = new CGRect (0, chattyBottomBarY, View.Frame.Width, chattyBottomBarHeight - chattyBottomBarHeightDifference);
					tableView.Frame = new CGRect (0, chattyTopBar.Frame.Y + chattyTopBar.Frame.Height, View.Frame.Width, chattyBottomBar.Frame.Y - (chattyTopBar.Frame.Y + chattyTopBar.Frame.Height));
				}

				UpdateExclusionPathOnContactSearch ();

			} else {
				// when the top bar is gone
				if (textEntryTextField.IsFirstResponder) {
					// when the keyboard is on screen

					// note; there's a chance keyboardOrigin will be wrong (rotating the device with the keyboard up)
					// the second call of this function should get the correct origin
					// willrotate (keyboard is up) -> subviews are laid out (incorrect keyboardOrigin because keyboardwillhide is called), keyboardwillshow is called (keyboardOrigin is correct again) -> subviews are laid out again
					nfloat chattyBottomBarY = this.KeyboardOrigin - chattyBottomBarHeight;
					nfloat chattyBottomBarHeightDifference = 0;
					nfloat minYValue = displacement_y + 1;
					if (chattyBottomBarY < minYValue) {
						// The bottom bar has grown past (above) the top bar.
						nfloat oldChattyBottomBarY = chattyBottomBarY;
						chattyBottomBarY = minYValue; // Set the chattyTopBar as the max it can grow to.
						chattyBottomBarHeightDifference = chattyBottomBarY - oldChattyBottomBarY;
						reachedTopEdge = true;
					} 

					chattyBottomBar.Frame = new CGRect (chattyBottomBar.Frame.X, chattyBottomBarY, View.Frame.Width, chattyBottomBarHeight - chattyBottomBarHeightDifference);
					if(sharedChatController.IsGroupConversation)
						tableView.Frame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height + groupNameBar.Frame.Height, View.Frame.Width, chattyBottomBar.Frame.Y - (lineView.Frame.Y + lineView.Frame.Height + groupNameBar.Frame.Height));
					else
						tableView.Frame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height, View.Frame.Width, chattyBottomBar.Frame.Y - (lineView.Frame.Y + lineView.Frame.Height));
				} else {
					// when the keyboard is off screen
					nfloat chattyBottomBarY = View.Frame.Height - chattyBottomBarHeight;
					nfloat chattyBottomBarHeightDifference = 0;
					nfloat minYValue = displacement_y + 1;
					if (chattyBottomBarY < minYValue) {
						// The bottom bar has grown past (above) the top bar.
						nfloat oldChattyBottomBarY = chattyBottomBarY;
						chattyBottomBarY = minYValue; // Set the chattyTopBar as the max it can grow to.
						chattyBottomBarHeightDifference = chattyBottomBarY - oldChattyBottomBarY;
						reachedTopEdge = true;
					}

					chattyBottomBar.Frame = new CGRect (0, chattyBottomBarY, View.Frame.Width, chattyBottomBarHeight - chattyBottomBarHeightDifference);
					if(sharedChatController.IsGroupConversation)
						tableView.Frame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height + groupNameBar.Frame.Height, View.Frame.Width, chattyBottomBar.Frame.Y - (lineView.Frame.Y + lineView.Frame.Height + groupNameBar.Frame.Height));
					else
						tableView.Frame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height, View.Frame.Width, chattyBottomBar.Frame.Y - (lineView.Frame.Y + lineView.Frame.Height));
				}

			}

			sendButton.Frame = new CGRect (chattyBottomBar.Frame.Width - sendButton.Frame.Width, chattyBottomBar.Frame.Height - sendButton.Frame.Height, sendButton.Frame.Width, sendButton.Frame.Height);
			attachmentsButton.Frame = new CGRect (0, sendButton.Frame.Y + (sendButton.Frame.Height - attachmentsButton.Frame.Height)/2, attachmentsButton.Frame.Width, attachmentsButton.Frame.Height);

			textEntryTextField.Frame = new CGRect (attachmentsButton.Frame.X + attachmentsButton.Frame.Width, chattyBottomBar.Frame.Height / 2 - textEntryTextField.Frame.Height / 2, chattyBottomBar.Frame.Width - attachmentsButton.Frame.Width - sendButton.Frame.Width, textEntryTextField.Frame.Height);

			CGRect textViewFrame = textEntryTextField.Frame;
			// Setting a minimum amount of padding between the textView and the chat bottom bar.
			if (textViewFrame.Y != TEXT_ENTRY_Y_ORIGIN) {
				textViewFrame.Y = TEXT_ENTRY_Y_ORIGIN;
				textViewFrame.Height = chattyBottomBar.Frame.Height - (textViewFrame.Y * 2);
				textEntryTextField.Frame = textViewFrame;
			}

			textEntryTextField.ScrollRangeToVisible (new NSRange (textEntryTextField.Text.Length, 0));

			// make the text field image slightly larger than the text entry so that text isn't out of the image frame
			CGRect textFieldImageFrame = textEntryTextField.Frame;
			textFieldImageFrame.Y -= 1.5f;
			textFieldImageFrame.Height += 3f;
			textFieldImage.Frame = textFieldImageFrame;

			contactSearchController.ContactSearchTableView.Frame = tableView.Frame;
			this.TypingView.Frame = new CGRect (0, chattyBottomBar.Frame.Y - TYPING_VIEW_HEIGHT, this.View.Frame.Width, TYPING_VIEW_HEIGHT);
		}
			
		#region exclusion paths
		void UpdateExclusionPathOnContactSearch () {
			UpdateExclusionPathForTextView (this.ContactSearchTextView, toLabel, addContactsButton, chattyTopBar);

			ContactSearchTextViewDelegate del = this.ContactSearchTextView.Delegate as ContactSearchTextViewDelegate;
			if (del == null) return;
			del.MatchPlaceHolderLabelWithExclusionPath (this.ContactSearchTextView);
		}

		void UpdateExclusionPathOnFromBar () {
			UpdateExclusionPathForTextView (this.FromAliasTextView, this.FromAliasToLabel, null, this.FromAliasBar);
		}

		static void UpdateExclusionPathForTextView (UITextView textView, UIView leftView, UIView rightView, UIView container) {
			CGRect leftViewFrame = textView.ConvertRectFromView (leftView.Bounds, leftView);
			leftViewFrame.X -= textView.TextContainerInset.Left;
			leftViewFrame.Height = container.Frame.Height + textView.ContentOffset.Y;
			leftViewFrame.Y = 0;

			if (!UIDevice.CurrentDevice.IsIos8Later ()) {
				leftViewFrame.X = 0;
				leftViewFrame.Width += UI_CONSTANTS.TINY_MARGIN;
			}

			// Written this way to account for the from bar not having a button on the right.
			UIBezierPath rightViewExclusion = UIBezierPath.FromRect (new CGRect (0, 0, 0, 0));
			if (rightView != null) {
				CGRect rightViewFrame = textView.ConvertRectFromView (rightView.Bounds, rightView);
				rightViewFrame.X -= textView.TextContainerInset.Left;
				rightViewFrame.Height = container.Frame.Height + textView.ContentOffset.Y;
				rightViewFrame.Y = 0;
				rightViewFrame.Width = textView.Frame.Width - rightView.Frame.X;
				rightViewExclusion = UIBezierPath.FromRect (rightViewFrame);
			}

			UIBezierPath leftViewExclusion = UIBezierPath.FromRect (leftViewFrame);
			textView.TextContainer.ExclusionPaths = new [] { leftViewExclusion, rightViewExclusion };
		}
		#endregion

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			if (scrollToBottomAfterLayout) {
				if (sharedChatController.viewModel != null && sharedChatController.viewModel.Count > 0) {
					ScrollChatToBottom ();
				}
				scrollToBottomAfterLayout = false;
			}
		}

		public void PossibleResizeTextEntryArea (UITextView textView) {

			if (finishedSending) {
				finishedSending = false;
				return;
			}

			if (sharedChatController.IsStagingMediaAndText) {
				var stagedMediaInset = new UIEdgeInsets (sharedChatController.StagedMediaHeight + UI_CONSTANTS.TINY_MARGIN, 0, 0, 0);
				if (!stagedMediaInset.Equals (textEntryTextField.TextContainerInset)) {
					textView.TextContainerInset = stagedMediaInset;
					textView.SizeToFit ();
				}
			}
				
			// Getting the size of the textView.
			nfloat textEntryFieldWidth;
			textEntryFieldWidth = InterfaceOrientation == UIInterfaceOrientation.Portrait ? textEntryWidthPortrait : textEntryWidthLandscape;

			nfloat heightForText = textView.Text.HeightFromWidthAndFont (textEntryFieldWidth, textView.Font);

			CGSize newSize = textView.SizeThatFits (new CGSize (textEntryFieldWidth, heightForText));
			CGRect newFrame = textView.Frame;
			nfloat newHeight = newSize.Height;

			// If we're staging media, lets remove its height from the equation.
			if (sharedChatController.IsStagingMediaAndText)
				newHeight -= sharedChatController.StagedMediaHeight;

			newFrame.Size = new CGSize (textEntryFieldWidth, newHeight);
			textView.Frame = newFrame;
		}

		public void PossibleResizeTextEntryAreaFromTyping (UITextView textView) {
			nfloat fixedWidth = textView.Frame.Width;
			CGSize newSize = textView.SizeThatFits (new CGSize (fixedWidth, 1000));
			CGRect newFrame = textView.Frame;
			newFrame.Size = new CGSize (fixedWidth, newSize.Height);
			nfloat difference = newFrame.Height - textView.Frame.Height;
			nfloat heightForText = textView.Text.HeightFromWidthAndFont (fixedWidth, textView.Font);

			// If the difference is less than zero, there's a chance for the text entry to be smaller than it currently is.
			// So we'd want a chance at laying out subviews.
			if (sharedChatController.IsStagingMediaAndText && difference > 0) {
				nfloat textHeightVeryTextEntryHeightDifference = (nfloat)Math.Abs (newFrame.Height - sharedChatController.StagedMediaOffset - heightForText);
				textHeightVeryTextEntryHeightDifference -= sharedChatController.StagedMediaHeight;

				// We're checking against less than 1 because if there is no need to resize the text entry layout, the height difference between the new frame and the original frame should be minimal.
				if (textHeightVeryTextEntryHeightDifference < 1)
					return;
			}

			// The case where you have an image, and you backspaced the last letter.
			// The difference being zero still qualifies for the layout to be renewed if we happen to be staging media only.
			if (sharedChatController.IsStagingMedia && !sharedChatController.IsStagingText) {
				if (difference <= 0) {
					View.SetNeedsLayout ();
					return;
				}
			}
				
			// difference != 0 && !reachedTopEdge is the case where the text entry is growing (bigger or smaller) but hasn't reached its limit.
			// reachedTopEdge && difference < 0 is the case where the text entry has reached the top limit and is growing smaller.
			if (difference != 0 && !reachedTopEdge || reachedTopEdge && difference < 0)
				View.SetNeedsLayout ();
		}

		public void ResetTextEntryHeight () {
			CGRect frame = textEntryTextField.Frame;
			frame.Height = IPHONE_TEXT_ENTRY_HEIGHT;
			textEntryTextField.Frame = frame;
		}

		public void ShowMainTable () {
			this.ChatTableView.Alpha = 1;
			contactSearchController.ContactSearchTableView.Alpha = 0;
			sharedChatController.ShowDetailsOption (true, false);
			if (this.NavigationItem.RightBarButtonItem == this.CancelContactSearchNavigationButton)
				this.NavigationItem.RightBarButtonItem = null;
		}

		public void ShowContactSearchTable () {
			this.ChatTableView.Alpha = 0;
			contactSearchController.ContactSearchTableView.Frame = this.ChatTableView.Frame;
			contactSearchController.ContactSearchTableView.ReloadData ();
			contactSearchController.ContactSearchTableView.Alpha = 1;
			this.NavigationItem.RightBarButtonItem = this.CancelContactSearchNavigationButton;
		}

		public void ReloadSearchContacts () {
			if (!sharedChatController.ShowAddContacts ()) {
				sharedChatController.HideAddContactsOption (false);
			} else {
				contactSearchController.LoadContactsAsync (true);
			}
		}

		public void ClearInProgressRemoteActionMessages () {
			// Clearing any data that has to do with remotely clicked messages so that we can refresh the ui state of those rows.
			TableViewDataSource dataSource = this.tableView.DataSource as TableViewDataSource;
			if (dataSource != null) {
				dataSource.ClearRemotelyClickedMessagesData ();
			}
		}

		class TableViewDelegate : UITableViewDelegate {
			readonly WeakReference chatViewControllerRef;

			public TableViewDelegate(ChatViewController controller) {
				chatViewControllerRef = new WeakReference(controller);

				UIMenuItem remoteDeleteMenuItem = new UIMenuItem ("REMOTE_DELETE_BUTTON".t (), new ObjCRuntime.Selector (iOS_Constants.RemoteDeleteSelector));
				UIMenuController.SharedMenuController.MenuItems = new UIMenuItem[] { remoteDeleteMenuItem };
				UIMenuController.SharedMenuController.Update ();
			}

			#region managing refresh and pull of new messages
			private System.Timers.Timer TimerBeforePullingOldMessages { get; set; }
			private nfloat ScrollOffset { get; set; }
			private const int OffsetBeforePullingOldMessagse = 20;
			private bool InitializedRefreshingTableViewIndicator { get; set; }

			public override void Scrolled (UIScrollView scrollView) {
				ChatViewController chatViewController = chatViewControllerRef.Target as ChatViewController;
				if (chatViewController != null) {

					// Don't bother worrying about offsets if this is a new chat. We aren't going to be loading previous messages.
					SharedChatController shared = chatViewController.sharedChatController;
					if (shared.IsNewMessage || !shared.CanLoadMorePreviousMessages) return;
					
					this.ScrollOffset = scrollView.ContentOffset.Y;

					// What we're doing here is keeping track of the scroll offset and using a timer.
					// If the timer elapses, we check the scroll offset. If it's still near the top, we continue with pulling old messages.
					// The reason we do this is to eliminate an instantaneous refresh that causes a jerky animation. (Mimic iMessage's behaviour)
					if (this.TimerBeforePullingOldMessages == null && this.ScrollOffset < OffsetBeforePullingOldMessagse) {
						this.TimerBeforePullingOldMessages = new System.Timers.Timer (Constants.TIMER_INTERVAL_BEFORE_RETRIEVING_OLD_MESSAGES);
						this.TimerBeforePullingOldMessages.AutoReset = false;
						this.TimerBeforePullingOldMessages.Elapsed += WeakDelegateProxy.CreateProxy<object, ElapsedEventArgs> (HandleTimerElapsed).HandleEvent<object, ElapsedEventArgs>;
						this.TimerBeforePullingOldMessages.Start ();

						// Add a loading indicator to the top of the table view to indicate to the user that it's loading previous messages.
						// If there are no more messages, the loading indicator will be removed elsewhere.
						UITableView tableView = chatViewController.ChatTableView;

						if (tableView.TableHeaderView == null && !this.InitializedRefreshingTableViewIndicator) {
							UIView view = new UIView (new CGRect (0, 0, tableView.Frame.Width, 50));
							UIActivityIndicatorView ac = new UIActivityIndicatorView (new CGRect (0, 0, 40, 40));
							ac.StartAnimating ();
							view.Add (ac);
							tableView.TableHeaderView = ac;
							this.InitializedRefreshingTableViewIndicator = true;
						}
					}

					if (this.ScrollOffset >= OffsetBeforePullingOldMessagse) {
						DisposeOfTimer ();
					}
				}
			}

			private void HandleTimerElapsed (object sender, ElapsedEventArgs e) {
				ChatViewController chatViewController = chatViewControllerRef.Target as ChatViewController;
				if (chatViewController == null) return;

				SharedChatController shared = chatViewController.sharedChatController;
				if (this.ScrollOffset < OffsetBeforePullingOldMessagse && shared.CanLoadMorePreviousMessages) {
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


			#region copy + paste related
			// This is just one entry into the UIMenuController to get the contextual copy or remote delete.
			// The other entry into the menu is through the textview in an individual cell posting notifications.
			public override bool ShouldShowMenu (UITableView tableView, NSIndexPath rowAtindexPath) {
				return true;
			}

			public override bool CanPerformAction (UITableView tableView, ObjCRuntime.Selector action, NSIndexPath indexPath, NSObject sender) {
				return true;
			}

			public override void PerformAction (UITableView tableView, ObjCRuntime.Selector action, NSIndexPath indexPath, NSObject sender) {
				ChatViewController chatViewController = chatViewControllerRef.Target as ChatViewController;
				if (chatViewController != null) {
					if (action == new ObjCRuntime.Selector ("copy:")) {
						UITableViewCell cell = chatViewController.ChatTableView.CellAt (indexPath);
						NSNotificationCenter.DefaultCenter.PostNotificationName (iOS_Constants.NOTIFICATION_CHAT_COPY_SELECTED, cell);
					} else {
						base.PerformAction (tableView, action, indexPath, sender);
					}
				}
			}
			#endregion

			Dictionary<int, nfloat> heightCache;
			public Dictionary<int, nfloat> HeightCache {
				get { 
					if (heightCache == null) {
						heightCache = new Dictionary<int, nfloat> ();
					}

					return heightCache; 
				}
				set { heightCache = value; }
			}

			public void InvalidateCachedRowHeights () {
				Debug.Assert (ApplicationModel.SharedPlatform.OnMainThread, "Calling invalidate height on a thread other than main.");
				this.HeightCache.Clear ();
			}

			public void InvalidateCachedHeightAtIndex (int position) {
				if (this.HeightCache.ContainsKey (position))
					this.HeightCache.Remove (position);
			}

			[DllImport (ObjCRuntime.Constants.UIKitLibrary, EntryPoint="objc_msgSend")]
			static extern int int_objc_msgSend_IntPtr (IntPtr target, IntPtr selector);

			static readonly IntPtr c_rowSelector = ObjCRuntime.Selector.GetHandle ("row");

			[Export ("tableView:heightForRowAtIndexPath:")]
			public nfloat HeightForRowAtIndexPath (IntPtr tableView, IntPtr indexPath) {
				ChatViewController chatViewController = chatViewControllerRef.Target as ChatViewController;
				if (chatViewController != null) {
					int row = int_objc_msgSend_IntPtr (indexPath, c_rowSelector);
					if (this.HeightCache.ContainsKey (row))
						return this.HeightCache [row];

					var size = new CGSize ();
					Message message = chatViewController.sharedChatController.viewModel [row];

					if (message.HasRemoteAction) {
						IncomingRemoteActionTableViewCell.SizeWithMessage (message, ref size);
						this.HeightCache.Add (row, size.Height);
						return size.Height;
					}

					if (message.HasMedia ()) {
						if (message.IsInbound ()) {
							IncomingMediaTableViewCell.SizeWithMessage (message, ref size);
							this.HeightCache.Add (row, size.Height);
							return size.Height;
						}
						// falls through to outgoing text table view
					}

					if (message.IsInbound ()) {
						IncomingTextTableViewCell.SizeWithMessage (message, ref size);
						this.HeightCache.Add (row, size.Height);
						return size.Height;
					} else {
						OutgoingTextTableViewCell.SizeWithMessage (message, chatViewController.View.Bounds.Width, ref size);
						this.HeightCache.Add (row, size.Height);
						return size.Height;
					}
				}

				return 0;
			}

			public override nfloat GetHeightForFooter (UITableView tableView, nint section) {
				return ChatViewController.TYPING_VIEW_HEIGHT;
			}

			UIView footer;
			protected UIView Footer {
				get { 
					if (footer == null) {
						footer = new UIView ();
						footer.Alpha = 0;
					}
					return footer; 
				}
			}

			public override UIView GetViewForFooter (UITableView tableView, nint section) {
				return this.Footer;
			}
		}

		class TableViewDataSource : UITableViewDataSource {
			readonly WeakReference chatViewControllerRef;

			public IList<em.Message> RemotelyClickedMessages { get; set; }

			public TableViewDataSource(ChatViewController controller) {
				chatViewControllerRef = new WeakReference (controller);
				this.RemotelyClickedMessages = new List<em.Message> ();
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				var chatViewController = chatViewControllerRef.Target as ChatViewController;
				if (chatViewController != null) {
					int rows = chatViewController.sharedChatController.viewModel == null ? 0 : chatViewController.sharedChatController.viewModel.Count;
					return rows;
				}

				return 0;
			}

			/*
		 	 * Function to clear the list containing all Message objects that have had their remote button clicked.
		 	 * This allows the UI to refresh and clear any loading indicators.
		 	 */ 
			public void ClearRemotelyClickedMessagesData () {
				var chatViewController = chatViewControllerRef.Target as ChatViewController;
				if (chatViewController == null) return;

				if (this.RemotelyClickedMessages.Count > 0) {
					this.RemotelyClickedMessages.Clear ();
					chatViewController.tableView.ReloadData ();
				}
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				var chatViewController = chatViewControllerRef.Target as ChatViewController;
				WeakReference controllerRef = new WeakReference (chatViewController); // Using a local weak ref of chat controller for thumbnail click callbacks.
				if (chatViewController != null && chatViewController.IsViewLoaded && chatViewController.sharedChatController.HasNotDisposed() ) {
					AbstractChatViewTableCell cell;
					Message message = chatViewController.sharedChatController.viewModel [indexPath.Row];

					nfloat height = tableView.Delegate.GetHeightForRow (tableView, indexPath);

					if (message.HasRemoteAction) {
						IncomingRemoteActionTableViewCell c;
						NSString cellReuseKey = IncomingRemoteActionTableViewCell.ReuseKeyForMessage (message);
						c = tableView.DequeueReusableCell (cellReuseKey) as IncomingRemoteActionTableViewCell;

						if (c == null) {
							c = IncomingRemoteActionTableViewCell.Create (cellReuseKey);

							c.RemoteActionButton.TouchUpInside += (object sender, EventArgs e) =>  {
								ChatViewController controller = controllerRef.Target as ChatViewController;
								if (controller == null) return;

								var button = sender as UIButton;
								nint indexOfMessageTapped = button.Tag - TAG_BUTTON_BASE;
								IList<Message> messages = controller.sharedChatController.viewModel;
								Message _message = messages [ (int) indexOfMessageTapped];  // TODO NPE check?

								if (!this.RemotelyClickedMessages.Contains (_message)) {
									this.RemotelyClickedMessages.Add (_message);
									c.RemoteActionButton.ShowProgress = true;
									controller.DidTapRemoteActionButton (sender, e);
								} 
							};
						}


						if (this.RemotelyClickedMessages.Contains (message)) {
							c.RemoteActionButton.ShowProgress = true;
						} else {
							c.RemoteActionButton.ShowProgress = false;
						}

						c.thumbnailClickCallback = () => {
							ChatViewController chatController = controllerRef.Target as ChatViewController;
							if (chatController != null && !message.fromContact.me) {
								chatController.NavigationController.PushViewController (new ProfileViewController (message.fromContact, false), true);
							}
						};

						c.RemoteActionButton.Tag = TAG_BUTTON_BASE + indexPath.Row;
						cell = c;
					} else if (message.HasMedia ()) {
						if (message.IsInbound ()) {
							IncomingMediaTableViewCell c;

							NSString cellReuseKey = IncomingMediaTableViewCell.ReuseKeyForMessage (message, height);

							c = tableView.DequeueReusableCell (cellReuseKey) as IncomingMediaTableViewCell;
							if (c == null) {
								c = new IncomingMediaTableViewCell (UITableViewCellStyle.Default, cellReuseKey);
								c.button.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs> (chatViewController.DidTapOpenMediaButton).HandleEvent<object, EventArgs>;
							}

							c.thumbnailClickCallback = () => {
								ChatViewController chatController = controllerRef.Target as ChatViewController;
								if (chatController != null && !message.fromContact.me) {
									chatController.NavigationController.PushViewController (new ProfileViewController (message.fromContact, false), true);
								}
							};
							c.button.Tag = TAG_BUTTON_BASE + indexPath.Row;
							cell = c;
						}
						else {
							OutgoingTextTableViewCell c;

							NSString cellReuseKey = OutgoingTextTableViewCell.ReuseKeyForMessage (message, height);

							c = (OutgoingTextTableViewCell)tableView.DequeueReusableCell (cellReuseKey);

							if (c == null) {
								c = OutgoingTextTableViewCell.Create (cellReuseKey);
								c.button.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs> (chatViewController.DidTapOpenMediaButton).HandleEvent<object, EventArgs>;;
							}
							c.button.Tag = TAG_BUTTON_BASE + indexPath.Row;
							cell = c;
						}
					}
					else {
						if (message.IsInbound ()) {
							NSString cellReuseKey = IncomingTextTableViewCell.ReuseKeyForHeight (height);

							cell = (AbstractChatViewTableCell)tableView.DequeueReusableCell (cellReuseKey);
							if (cell == null) {
								cell = IncomingTextTableViewCell.Create (cellReuseKey);
							}

							cell.thumbnailClickCallback = () => {
								ChatViewController chatController = controllerRef.Target as ChatViewController;
								if (chatController != null && !message.fromContact.me) {
									chatController.NavigationController.PushViewController (new ProfileViewController (message.fromContact, false), true);
								}
							};
						} else {
							OutgoingTextTableViewCell c;
							NSString cellReuseKey = OutgoingTextTableViewCell.ReuseKeyForHeight (height);

							c = (OutgoingTextTableViewCell)tableView.DequeueReusableCell (cellReuseKey);
							if (c == null) {
								c = OutgoingTextTableViewCell.Create (cellReuseKey);
								c.button.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs> (chatViewController.DidTapOpenMediaButton).HandleEvent<object, EventArgs>;
							}
							c.button.Tag = TAG_BUTTON_BASE + indexPath.Row;
							c.UpdateColorTheme (message.chatEntry.SenderColorTheme);
							cell = c;
						}
					}

					cell.Position = indexPath.Row;
					cell.message = message;

					cell.SelectionStyle = UITableViewCellSelectionStyle.None;
					return cell;
				}

				return null;
			}
		}

		class TextViewDelegate : UITextViewDelegate {
			private WeakReference chatViewControllerRef;
			private ChatViewController Controller {
				get { return chatViewControllerRef.Target as ChatViewController; }
				set {
					chatViewControllerRef = new WeakReference (value);
				}
			}

			public TextViewDelegate (ChatViewController controller) {
				this.Controller = controller;
			}

			public override void EditingEnded (UITextView textView) {

			}

			public override void EditingStarted (UITextView textView) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.ShowMainTable ();
				}
			}

			public override bool ShouldBeginEditing (UITextView textView) {
				return true;
			}

			public override bool ShouldChangeText (UITextView textView, NSRange range, string replacementString) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					SharedChatController sharedChatController = chatViewController.sharedChatController;
					if (sharedChatController.IsStagingMedia) {
						if (textView.Text.Length == 0 && string.IsNullOrEmpty (replacementString))
							chatViewController.sharedChatController.RemoveStagedItem ();
					}
				}

				return true;
			}

			public override bool ShouldEndEditing (UITextView textView) {
				return true;
			}

			public override void Changed (UITextView textView) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.sharedChatController.UpdateUnderConstructionText (textView.Text);

					bool isStagingText = (textView.Text.Length != 0);
					SharedChatController sharedChatController = chatViewController.sharedChatController;
					sharedChatController.IsStagingText = isStagingText;
					chatViewController.PossibleResizeTextEntryAreaFromTyping (textView);
				}
			}
		}

		class SharedChatController : AbstractChatController {
			WeakReference chatControllerRef;
			private ChatViewController Controller {
				get { return chatControllerRef.Target as ChatViewController; }
				set { chatControllerRef = new WeakReference (value); }
			}

			public override void UpdateTextEntryArea (string text) {
				throw new NotImplementedException ();
			}

			public int StagedMediaOffset {
				get {
					return 5;
				}
			}

			public SharedChatController(ApplicationModel appModel, ChatEntry sendingChatEntry, ChatEntry displayedChatEntry, ChatViewController controller) : base(appModel, sendingChatEntry, displayedChatEntry) {
				this.Controller = controller;
			}

			public override void ClearInProgressRemoteActionMessages () {
				EMTask.DispatchMain (() => {
					ChatViewController self = this.Controller;
					if (self == null) return;	
					self.ClearInProgressRemoteActionMessages ();
				});
			}

			public override void ReloadContactSearchContacts () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.ReloadSearchContacts ();
				}
			}

			public override void PreloadImages (IList<Message> messages) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					ImageSetter.PreloadMediaListAsync (messages);
				}
			}

			public override void ShowDetailsOption (bool animaged, bool forceShow) {
				//show only for already initiated chats (not new message or ad-hoc group message)
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if ((!IsNewMessage || forceShow) && !IsDeletedGroupConversation && (chatViewController.NavigationItem.RightBarButtonItems == null || chatViewController.NavigationItem.RightBarButtonItems.Length == 0)) {
						chatViewController.NavigationItem.SetRightBarButtonItem (chatViewController.detailsButton, true);
					}
				}
			}

			public override void UpdateFromAliasPickerInteraction (bool shouldAllowInteraction) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.ChangeFromBarSelectionEnabled (shouldAllowInteraction);
				}
			}

			public override void UpdateFromBarVisibility (bool showFromBar) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.ChangeAliasBarVisibility (showFromBar);
				}
			}

			public override void UpdateAliasText (string text) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.FromAliasTextView.Text = text;
					chatViewController.UpdateExclusionPathOnFromBar ();
				}
			}

			public override void DidFinishLoadingMessages() {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (chatViewController.IsViewLoaded) {
						// When we load new messages, invalidate all the cached heights so we don't get mashed up messages using wrong heights.
						TableViewDelegate tableViewDeleagate = chatViewController.tableView.Delegate as TableViewDelegate;
						if (tableViewDeleagate != null) {
							tableViewDeleagate.InvalidateCachedRowHeights ();
						}

						chatViewController.tableView.ReloadData ();
						chatViewController.ScrollChatToBottom ();
					}
				}
			}

			public override void DidFinishLoadingPreviousMessages (int count) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (chatViewController.IsViewLoaded) {
						UITableView tableView = chatViewController.ChatTableView;
						if (!chatViewController.sharedChatController.CanLoadMorePreviousMessages) {
							// No more previous messages, hide header view.
							tableView.TableHeaderView = null;
						}

						// When we load new messages, invalidate all the cached heights so we don't get mashed up messages using wrong heights.
						TableViewDelegate tableViewDeleagate = chatViewController.tableView.Delegate as TableViewDelegate;
						if (tableViewDeleagate != null) {
							tableViewDeleagate.InvalidateCachedRowHeights ();
						}

						// Try to get the original offset by calculating the heights of all the new rows coming in.
						CGPoint tableViewOffset = tableView.ContentOffset;

						nint heightForNewRows = 0;

						NSIndexPath[] arr = new NSIndexPath[count];
						for (int i = 0; i < count; i++) {
							NSIndexPath indexPath = NSIndexPath.FromRowSection (i, 0);
							arr [i] = indexPath;

							heightForNewRows += (nint)tableView.Delegate.GetHeightForRow (tableView, indexPath);
						}

						tableViewOffset.Y += heightForNewRows;

						tableView.ReloadData ();
						tableView.SetContentOffset (tableViewOffset, animated: false);
					}
				}
			}

			public override void HideAddContactsOption (bool animated) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (chatViewController.chattyTopBar != null) {
						chatViewController.addContactsButton.RemoveFromSuperview ();
						chatViewController.addContactsButton = null;
						chatViewController.chattyTopBar.RemoveFromSuperview ();
						chatViewController.chattyTopBar = null;

						chatViewController.topBarHidden = true;

						chatViewController.View.SetNeedsLayout ();
						chatViewController.UpdateTitle ();
					}

					if (chatViewController.FromAliasBarShowing) {
						chatViewController.FromAliasBar.RemoveFromSuperview ();
						chatViewController.FromAliasBarShowing = false;
					}
				}
			}

			public override void ClearTextEntryArea() {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.textEntryTextField.Text = "";
					this.IsStagingText = false;
					chatViewController.ResetTextEntryHeight ();
					chatViewController.View.SetNeedsLayout ();
				}
			}

			#region STAGING MEDIA
			public override void StagedMediaBegin () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.PauseUI ();
				}
			}

			public override void StagedMediaAddedToStagingAndPreload () {
				if (ContentTypeHelper.IsAudio (StagedMedia.GetPathForUri (ApplicationModel.SharedPlatform))) {
					var waveform = StagedMedia.PreloadThumbnailAsync ((UIImage image) => {
							StagedMedia.DelegateDidFinisLoadMedia ();
						});
					if (waveform == null)
						return;
				}

				StagedMedia.DelegateDidFinisLoadMedia ();
			}

			public override void StagedMediaEnd () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.ResumeUI ();
				}
			}

			public override float StagedMediaGetAspectRatio() {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					UIImageView imgView = CreateViewFromStagedMedia ();

					// this also adds the staged media view to the text entry
					chatViewController.ShowMediaInStagingArea (imgView);
					this.IsStagingMedia = true;

					CGSize size = imgView.Frame.Size;
					float heightToWidth = (float)size.Height / (float)size.Width;
					return heightToWidth;
				}

				return 0;
			}

			public override float StagedMediaGetSoundRecordingDurationSeconds () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (this.StagedMedia == null) {
						return 0;
					}

					string soundPath = this.StagedMedia.GetPathForUri (ApplicationModel.SharedPlatform);

					if (!ContentTypeHelper.IsAudio (soundPath)) {
						return 0;
					}

					NSUrl soundUrl = new NSUrl (soundPath);

					try {
						AVAudioPlayer player = AVAudioPlayer.FromUrl(soundUrl);
						double duration = player.Duration;
						float roundedDuration = Convert.ToSingle (duration);
						return roundedDuration;
					} catch (Exception e) {
						Debug.WriteLine ("StagedMediaGetSoundRecordingDurationSeconds: cannot get duration for sound recording {0}", e);
					}
				}

				return 0;
			}

			private UIImageView CreateViewFromStagedMedia () { 
				UIImage img = this.StagedMedia.LoadThumbnail ();
				if (ContentTypeHelper.IsAudio (this.StagedMedia.GetPathForUri (ApplicationModel.SharedPlatform))) {
					BackgroundColor color = this.backgroundColor;
					img = ImageSetter.UseSoundWaveFormMask (color, img);
				}
				return new UIImageView (img);
			}

			public override void StagedMediaRemovedFromStaging () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					UIView stagedItem = chatViewController.textEntryTextField.ViewWithTag (TAG_STAGED_ITEM);
					if (this.IsStagingMedia) {
						chatViewController.finishedSending = true;
						chatViewController.ResetTextEntryHeight ();
						this.IsStagingMedia = false;
						chatViewController.View.SetNeedsLayout ();
					}

					if (stagedItem != null)
						stagedItem.RemoveFromSuperview ();
				}
			}
			#endregion

			public override void UpdateToContactsView () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					((ContactSearchTextViewDelegate)chatViewController.ContactSearchTextView.Delegate).UpdateContactSearchTextView ();
				}
			}

			public override void GoToBottom () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (chatViewController.IsViewLoaded) {
						chatViewController.ScrollChatToBottom (animate: true);
					}
				}
			}

			public override bool CanScrollToBottom {
				get {
					ChatViewController chatViewController = this.Controller;
					if (chatViewController != null) {
						UITableView tableView = chatViewController.ChatTableView;
						NSIndexPath[] visibleIndexPaths = tableView.IndexPathsForVisibleRows;
						int visibleRows = visibleIndexPaths.Length;

						if (visibleRows > 0) {
							NSIndexPath path = visibleIndexPaths [visibleRows - 1]; 
							int lastVisiblePosition = path.Row;

							int messageToBeAdded = this.viewModel.Count - 1;
							int numberOfRowsAwayFromBottom = messageToBeAdded - lastVisiblePosition;

							if ((numberOfRowsAwayFromBottom) > visibleRows) {
								return false;
							} else {
								return true;
							}
						}
					}

					return false;
				}
			}

			public override void ConversationContainsActive (bool active, InactiveConversationReason reason) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController == null) return;
				if (chatViewController.Visible && !active) {
					switch (reason) {
					default:
					case InactiveConversationReason.FromAliasInActive: 
						{
							UIAlertView alert = new UIAlertView ("ALIAS_DELETED_TITLE".t (), "ALIAS_DELETED_CHAT_HISTORY_MESSAGE".t (), null, "OK_BUTTON".t ());
							alert.Show ();
							break;
						}
					case InactiveConversationReason.Other:
						{
							UIAlertView alert = new UIAlertView ("SEND_MESSAGE_FAILED_TITLE".t (), "SEND_MESSAGE_FAILED_REASON".t (), null, "OK_BUTTON".t ());
							alert.Show ();
							break;
						}
					}
				}

				UpdateSendingMode ();
			}

			public override void WarnLeftAdhoc() {
				var alert = new UIAlertView ("LEFT_CONVERSATION".t (), "LEFT_CONVERSATION_EXPLAINATION".t (), null, "OK_BUTTON".t ());
				alert.Show ();
			}

			public override void PrepopulateToWithAKA (string toAka) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController == null) return;
				chatViewController.ContactSearchTextView.InsertText (toAka);
			}

			public override void HandleMessageUpdates (IList<ModelStructureChange<Message>> structureChanges, IList<ModelAttributeChange<Message,object>> attributeChanges, bool animated, Action doneAnimatingCallback) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (!chatViewController.IsViewLoaded)
						doneAnimatingCallback ();
					else {
						List<NSIndexPath> rowsToAdd = new List<NSIndexPath>();
						List<NSIndexPath> rowsToDelete = new List<NSIndexPath>();
						if (structureChanges != null) {
							foreach (ModelStructureChange<Message> change in structureChanges) {
								NSIndexPath indexPath = NSIndexPath.FromRowSection (viewModel.IndexOf (change.ModelObject), 0);
								if (change.Change == ModelStructureChange.added)
									rowsToAdd.Add (indexPath);
								else if (change.Change == ModelStructureChange.deleted)
									rowsToDelete.Add (indexPath);
							}
						}

						if (rowsToAdd.Count == 0 && rowsToDelete.Count == 0) {
							HandleAttributeChanges (attributeChanges);
							doneAnimatingCallback ();
						}
						else {
							CATransaction.Begin ();

							chatViewController.tableView.BeginUpdates ();

							CATransaction.CompletionBlock = delegate {
								EMTask.DispatchMain (() => {
									HandleAttributeChanges (attributeChanges);
									doneAnimatingCallback ();
								});
							};

							if (rowsToAdd.Count > 0) {
								NSIndexPath[] indexPaths = rowsToAdd.ToArray ();
								chatViewController.tableView.InsertRows (indexPaths, UITableViewRowAnimation.Fade);
							}

							if (rowsToDelete.Count > 0) {
								NSIndexPath[] indexPaths = rowsToDelete.ToArray ();
								chatViewController.tableView.DeleteRows (indexPaths, UITableViewRowAnimation.Fade);
							}

							Message message = viewModel [viewModel.Count - 1];
							if (message.IsInbound ())
								ClearTypingMessage ();

							chatViewController.tableView.EndUpdates ();
							CATransaction.Commit ();

							if (!AppDelegate.Instance.applicationModel.IsHandlingMissedMessages && this.CanScrollToBottom) {
								chatViewController.ScrollChatToBottom (animate: true);
							}
						}
					}
				}
			}

			protected void HandleAttributeChanges(IList<ModelAttributeChange<Message,object>> attributeChanges) {
				if (attributeChanges == null)
					return;

				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (chatViewController.IsViewLoaded) {
						foreach (ModelAttributeChange<Message,object> attrChange in attributeChanges) {
							int position = viewModel.IndexOf (attrChange.ModelObject);
							NSIndexPath indexPath = NSIndexPath.FromRowSection (position, 0);
							NSIndexPath[] visible = chatViewController.tableView.IndexPathsForVisibleRows;
							if (ContainsIndexPath (visible, indexPath)) {
								if (indexPath.Row < viewModel.Count) {
									Message message = attrChange.ModelObject;
									if (attrChange.AttributeName.Equals (MESSAGE_ATTRIBUTE_MESSAGE_STATUS)) {
										if (!message.IsInbound ()) {
											AbstractOutgoingChatViewTableCell outgoingCell = (AbstractOutgoingChatViewTableCell)chatViewController.tableView.CellAt (indexPath);
											if (outgoingCell != null)
												outgoingCell.setMessageStatus (message, true);
										}
									}
									else if (attrChange.AttributeName.Equals (MESSAGE_ATTRIBUTE_TAKEN_BACK)) {
										(chatViewController.tableView.Delegate as TableViewDelegate).InvalidateCachedHeightAtIndex (position);
										if (chatViewController.IsViewLoaded)
											chatViewController.tableView.ReloadRows (new NSIndexPath[] { NSIndexPath.FromRowSection (position, 0) }, UITableViewRowAnimation.Automatic);
									}
								}
							}
						}
					}
				}
			}
				
			public override void ShowContactIsTyping (string typingMessage) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					BackgroundColor color = chatViewController.sharedChatController.backgroundColor;
					if (chatViewController.IsViewLoaded && !(new EqualsBuilder<string> (typingMessage, displayedMessage).Equals ())) {
						chatViewController.TypingView.UpdateRemoteTypingMessage (typingMessage, true, color);
						displayedMessage = typingMessage;
					}
				}
			}

			public override void HideContactIsTyping () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (chatViewController.IsViewLoaded) {
						chatViewController.TypingView.HideRemoteTypingMessage (true);
						displayedMessage = null;
					}
				}
			}

			public void UpdateSendingMode () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null && chatViewController.IsViewLoaded) {
					SetSendButtonMode (sendingChatEntry.MessageSendingAllowed, sendingChatEntry.SoundRecordingAllowed);

					chatViewController.attachmentsButton.Enabled = this.Editable;
					chatViewController.textEntryTextField.UserInteractionEnabled = this.Editable;
					if (!this.Editable) {
						chatViewController.textEntryTextField.ResignFirstResponder ();
					}
				}
			}

			public override void SetSendButtonMode (bool messageSendingAllowed, bool soundRecordingAllowed) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					BackgroundColor mainColor = this.backgroundColor;
					string iconResource = null;
					bool enableButton = false;

					if (this.Editable) {
						if (messageSendingAllowed) {
							enableButton = true;
							chatViewController.sendButton.Mode = SendUIButton.SendUIButtonState.Send;
							chatViewController.AllowMediaPickerController = true;
						} else if (soundRecordingAllowed) {
							enableButton = true;
							chatViewController.sendButton.Mode = SendUIButton.SendUIButtonState.Record;
							chatViewController.AllowMediaPickerController = true;
						} else {
							enableButton = false;
							chatViewController.sendButton.Mode = SendUIButton.SendUIButtonState.Disabled;
							chatViewController.AllowMediaPickerController = false;
						}
					} else {
						enableButton = false;
						chatViewController.sendButton.Mode = SendUIButton.SendUIButtonState.Disabled;
						chatViewController.AllowMediaPickerController = false;
					}

					chatViewController.sendButton.Enabled = enableButton;

					chatViewController.ThemeController (chatViewController.InterfaceOrientation);
				}
			}

			public override void DidChangeColorTheme () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.ThemeController (chatViewController.InterfaceOrientation);
					chatViewController.tableView.ReloadData ();
				}
			}

			public override void DidChangeDisplayName () {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.ThemeController (chatViewController.InterfaceOrientation); // probably don't need to theme the controller when the display name changes
					chatViewController.tableView.ReloadData ();
				}
			}

			public override void UpdateChatRows (Message message) {
				EMTask.DispatchMain (() => {
					AbstractChatViewTableCell cell = FindCellFromMessageIfVisible (message);
					if (cell != null)
						cell.UpdateCellFromMediaState (message);
				});
			}

			AbstractChatViewTableCell FindCellFromMessageIfVisible (Message message) {
				AbstractChatViewTableCell cell;
				if (message.IsInbound ())
					cell = FindIncomingMediaTableCellIfVisible (message);
				else
					cell = FindOutgoingTextTableCellIfVisible (message);
				return cell;
			}

			public override void CounterpartyPhotoDownloaded (CounterParty counterparty) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null && chatViewController.IsViewLoaded) {
					// messages can be null at this point, ex: adding a contact from search bar that then downloads a thumbnail, triggering this delegate
					if (viewModel == null)
						return;
					NSIndexPath[] visibleIndexes = chatViewController.tableView.IndexPathsForVisibleRows;
					foreach (NSIndexPath indexPath in visibleIndexes) {
						if (indexPath.Row < viewModel.Count) {
							Message message = viewModel [indexPath.Row];
							bool needsToUpdate = false;
							if (message.IsInbound ()) {
								// inbound messages fromContact is set so if this
								// download is from the contact related to the message
								// update it
								if (message.fromContact.Equals (counterparty))
									needsToUpdate = true;
							}
							else {
								// outgoing message the update only applies if
								// its our account info or our alias info
								var acct = counterparty as AccountInfo;
								if (acct != null && message.chatEntry.fromAlias == null)
									needsToUpdate = true;
								else {
									var aliasInfo = counterparty as AliasInfo;
									if (aliasInfo != null && message.chatEntry.fromAlias != null && aliasInfo.serverID.Equals (message.chatEntry.fromAlias))
										needsToUpdate = true;
								}
							}

							if (needsToUpdate) {
								UITableViewCell cell = chatViewController.tableView.CellAt (indexPath);
								var chatViewTableCell = cell as AbstractChatViewTableCell;
								chatViewTableCell.UpdateThumbnailImage (counterparty);
							}
						}
					}
				}
			}

			public override void ContactSearchPhotoUpdated (Contact c) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					chatViewController.ContactSearchController.UpdateContact (c);
				}
			}

			public override void DidChangeTotalUnread (int unreadCount) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null && chatViewController.IsViewLoaded) {
					if (chatViewController.ChatNavigationItem != null) {
						UINavigationBarUtil.SetBackButtonWithUnreadCount (chatViewController.ChatNavigationItem, unreadCount);
					}
				}
			}

			public override void ConfirmRemoteTakeBack (int index) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					(chatViewController.tableView.Delegate as TableViewDelegate).InvalidateCachedHeightAtIndex (index);
					var alert = new UIAlertView ("REMOTE_DELETE_BUTTON".t (), "REMOTE_DELETE_EXPLAINATION".t (),
						           null,
						           "CANCEL_BUTTON".t (),
						           new string[] { "DELETE_BUTTON".t () });
					alert.Show ();
					alert.Clicked += (sender, buttonArgs) => { 
						switch (buttonArgs.ButtonIndex) {
						case 1:
							ContinueRemoteTakeBack (index);
							break;

						default:
							break;
						}
					};
				}
			}

			public override void ConfirmMarkHistorical (int index) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					(chatViewController.tableView.Delegate as TableViewDelegate).InvalidateCachedHeightAtIndex (index);
					var alert = new UIAlertView ("DELETE_BUTTON".t (), "DELETE_EXPLAINATION".t (),
						           null,
						           "CANCEL_BUTTON".t (),
						           new string[] { "DELETE_BUTTON".t () });
					alert.Show ();
					alert.Clicked += (sender, buttonArgs) => { 
						switch (buttonArgs.ButtonIndex) {
						case 1:
							ContinueMarkHistorical (index);
							break;

						default:
							break;
						}
					};
				}
			}

			protected IncomingMediaTableViewCell FindIncomingMediaTableCellIfVisible(Message message) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (chatViewController.IsViewLoaded && chatViewController.sharedChatController.viewModel != null) {
						int position = chatViewController.sharedChatController.viewModel.IndexOf (message);
						if (position != -1) {
							NSIndexPath indexPath = NSIndexPath.FromRowSection (position, 0);
							NSIndexPath[] visible = chatViewController.tableView.IndexPathsForVisibleRows;
							if (ContainsIndexPath (visible, indexPath)) {
								var cell = chatViewController.tableView.CellAt (indexPath) as IncomingMediaTableViewCell;
								return cell;
							}
						}
					}
				}

				return null;
			}

			protected OutgoingTextTableViewCell FindOutgoingTextTableCellIfVisible(Message message) {
				ChatViewController chatViewController = this.Controller;
				if (chatViewController != null) {
					if (chatViewController.IsViewLoaded && chatViewController.sharedChatController.viewModel != null) {
						int position = chatViewController.sharedChatController.viewModel.IndexOf (message);
						if (position != -1) {
							NSIndexPath indexPath = NSIndexPath.FromRowSection (position, 0);
							NSIndexPath[] visible = chatViewController.tableView.IndexPathsForVisibleRows;
							if (ContainsIndexPath (visible, indexPath)) {
								var cell = chatViewController.tableView.CellAt (indexPath) as OutgoingTextTableViewCell;
								return cell;
							}
						}
					}
				}

				return null;
			}

			protected bool ContainsIndexPath( NSIndexPath[] visible, NSIndexPath indexPath ) {
				foreach ( NSIndexPath ip in visible )
					if ( ip.Equals(indexPath))
						return true;

				return false;
			}
		}
	}

	public abstract class AbstractChatViewTableCell : UITableViewCell {
		protected static readonly UIFont TimestampFont = FontHelper.DefaultFontWithSize (UIFont.SmallSystemFontSize);

		protected UILabel timestampLabel;

		public abstract BasicThumbnailView Thumbnail { get; }

		public Action thumbnailClickCallback;

        private int _position = -1;
        public int Position { get { return this._position; } set { this._position = value; } }

		public static void TimestampSizeWithMessage(Message m, ref CGRect sendDateRect) {
			// sent date
			sendDateRect.Location = new CGPoint (0, m.showSentDate ? 3 : 0);
			if (!m.showSentDate)
				sendDateRect.Size = CGSize.Empty;
			else {
				CGSize shrinkWrapped = m.FormattedSentDate.SizeOfTextWithFontAndLineBreakMode (TimestampFont, new CGSize (320, 50), UILineBreakMode.Clip);
				sendDateRect.Size = new CGSize (shrinkWrapped.Width, shrinkWrapped.Height + 10);
			}
		}

		public AbstractChatViewTableCell(IntPtr handle) : base(handle) {}

		public AbstractChatViewTableCell(UITableViewCellStyle style, string identifier) : base(style, identifier) {
			UIView backgroundView = new UIView (Frame);
			backgroundView.AutoresizingMask = UIViewAutoresizing.All;
			backgroundView.BackgroundColor = UIColor.Clear;
			BackgroundView = backgroundView;
			BackgroundColor = UIColor.Clear;

			timestampLabel = new UILabel ();
			timestampLabel.Font = TimestampFont;
			timestampLabel.TextAlignment = UITextAlignment.Center;
			timestampLabel.TextColor = iOS_Constants.WHITE_COLOR;
			timestampLabel.Tag = 0xA;
			ContentView.AddSubview (timestampLabel);
		}

		public override void SetSelected (bool selected, bool animated) {
			if (selected) {
				this.Alpha = 0.5f;
			} else {
				this.Alpha = 1f;
			}
		}

		public void DidTapThumbnail (object sender, EventArgs e) {
			if (thumbnailClickCallback != null) {
				thumbnailClickCallback ();
			}
		}
	
		Message m;
		public Message message { 
			get {
				return m;
			}
			set {
				Message oldMessage = m;
				this.m = value;

				if (oldMessage != null && oldMessage != this.m && oldMessage.HasMedia ()) {
					em.NotificationCenter.DefaultCenter.RemoveObserverAction (oldMessage.media, Media.MEDIA_DID_UPDATE_IMAGE_RESOURCE, MediaDidUpdateImageResource);
				}

				if (m != null && m.HasMedia ()) {
					em.NotificationCenter.DefaultCenter.AddWeakObserver (m.media, Media.MEDIA_DID_UPDATE_IMAGE_RESOURCE, MediaDidUpdateImageResource);
				}

				m = value;
				DidSetMessage (m);
			}
		}

		public abstract void DidSetMessage (Message m);
		public abstract void UpdateThumbnailImage (CounterParty c);
		public abstract void SetThumbnailImage (UIImage image);
		public abstract void UpdateColorTheme (BackgroundColor c);
		public abstract void UpdateCellFromMediaState (Message m, bool duringSetMessage = false);
		public abstract void MediaDidUpdateImageResource (Notification notif);

		#region copy paste + additional functionality

		// We make the ChatTableViewCell a Responder so that it can handle custom Edit actions.
		// When we handle these selector/actions, we just post a notification so that the listening controller can decide what to do.
		public override bool CanPerform (ObjCRuntime.Selector action, NSObject withSender) {
			if (action == new ObjCRuntime.Selector ("copy:")) {
				return base.CanPerform (action, withSender);
			} else if (action == new ObjCRuntime.Selector (iOS_Constants.RemoteDeleteSelector)) {
				bool remoteDeleteOkay = message.messageLifecycle != MessageLifecycle.deleted && !message.IsInbound () && message.messageChannel == MessageChannel.em;
				return remoteDeleteOkay;
			} else {
				return false;
			}
		}

		public override bool CanBecomeFirstResponder {
			get {
				return true;
			}
		}

		[Export (iOS_Constants.RemoteDeleteSelector)]
		public void HandleRemoteDeleteSelector (NSObject anObject) {
			NSNotificationCenter.DefaultCenter.PostNotificationName (iOS_Constants.NOTIFICATION_REMOTE_DELETE_SELECTED, this);
		}

		public override void Copy (NSObject sender) {
			NSNotificationCenter.DefaultCenter.PostNotificationName (iOS_Constants.NOTIFICATION_CHAT_COPY_SELECTED, this);
		}
		#endregion
	}

	public abstract class AbstractOutgoingChatViewTableCell : AbstractChatViewTableCell {
		public AbstractOutgoingChatViewTableCell(IntPtr handle) : base(handle) {
		}

		public AbstractOutgoingChatViewTableCell(UITableViewCellStyle style, string identifier) : base(style, identifier) {
		}

		public abstract void setMessageStatus(Message m, bool animated);
	}
}
