using System;
using System.Collections.Generic;
using System.IO;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using GoogleAnalytics.iOS;
using MBProgressHUD;
using String_UIKit_Extension;
using UIKit;

namespace iOS {

	public class EditGroupViewController : AbstractAcquiresImagesController, IPopListener {

		readonly ApplicationModel appModel;
		readonly SharedEditGroupController sharedEditGroupController;
		public AbstractEditGroupController SharedController { get { return sharedEditGroupController; } }
		readonly bool shouldDismissController;

		public bool EditMode, ViewLoaded, UILoaded;

		MTMBProgressHUD progressHud;

		BackgroundColor colorTheme {
			get {
				return SharedController.ColorTheme;
			}
			set {
				SharedController.ColorTheme = value;
			}
		}
		TableViewDelegate tableDelegate;
		TableViewDataSource tableDataSource;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		public Group Group { get { return sharedEditGroupController.Group; } set { sharedEditGroupController.Group = value; } }

		NSData ImageData;
		NSObject willEnterForegroundObserver;

		#region UI
		UIBarButtonItem ActionButton;

		UIView LineView;

		UIView TopView;

		UIButton ThumbnailButton;
		UIImage Thumbnail;

		UITextField GroupNameTextField;
		UILabel GroupNameLabel;

		UIButton ColorThemeButton;
		UILabel ColorThemeLabel;

		UIButton AddMembersButton;
		UILabel AddMembersLabel;

		EMButton SendMessageButton, RemoveButton;

		UIView MembersView;
		UILabel MembersLabel;

		UITableView TableView;
		#endregion


		#region picking from alias
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

					var doneButton = new UIBarButtonItem("DONE_BUTTON".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>((s, e) => fromAliasTextView.ResignFirstResponder ()).HandleEvent<object,EventArgs>);

					doneButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
					toolbar.SetItems (new []{ doneButton }, true); 
					fromAliasPickerToolbar = toolbar;
				}
				return fromAliasPickerToolbar; 
			}
			set { fromAliasPickerToolbar = value; }
		}

		BlockContextMenuTextView fromAliasTextView;
		protected BlockContextMenuTextView FromAliasTextView {
			get {
				if (fromAliasTextView == null) {
					fromAliasTextView = new BlockContextMenuTextView (new CGRect (0, 200, 70, 50));
					fromAliasTextView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
					fromAliasTextView.Font = FontHelper.DefaultFontForTextFields ();
					fromAliasTextView.ClipsToBounds = true;
					fromAliasTextView.AutocorrectionType = UITextAutocorrectionType.No;
					fromAliasTextView.BackgroundColor = UIColor.Green;

					fromAliasTextView.TintColor = UIColor.Clear;

					FromAliasPicker = new UIPickerView ();
					IList<AliasInfo> aliases = AppDelegate.Instance.applicationModel.account.accountInfo.aliases;
					FromAliasPickerViewModel = new FromAliasPickerViewModel (aliases);
					FromAliasPickerViewModel.PickerChanged += WeakDelegateProxy.CreateProxy<object,AliasInfoPickerChangedEventArgs>((sender, e) => sharedEditGroupController.UpdateFromAlias (e.SelectedValue)).HandleEvent<object,AliasInfoPickerChangedEventArgs>;

					FromAliasPicker.Model = FromAliasPickerViewModel;
					FromAliasPicker.ShowSelectionIndicator = true;
					fromAliasTextView.InputView = FromAliasPicker;
					fromAliasTextView.InputAccessoryView = FromAliasPickerToolbar;

					picker.Select (sharedEditGroupController.CurrentRowForFromAliasPicker (), 0, true);

					#region selection
					var tap = new UITapGestureRecognizer ( (Action) WeakDelegateProxy.CreateProxy(() => fromAliasTextView.BecomeFirstResponder ()).HandleEvent);
					fromAliasTextView.AddGestureRecognizer (tap);
					#endregion
				}
				return fromAliasTextView;
			}
		}
		#endregion

		public EditGroupViewController (bool edit, Group g, bool dismiss) {
			EditMode = edit;
			shouldDismissController = dismiss;

			ImageData = null;

			appModel = ((AppDelegate)UIApplication.SharedApplication.Delegate).applicationModel;
			sharedEditGroupController = new SharedEditGroupController (this, appModel, g);

			colorTheme = g != null ? g.colorTheme : appModel.account.accountInfo.colorTheme;

			// image selection
			AllowVideoOnImageSelection = false;
			AllowImageEditingOnImageSelection = true;
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			colorTheme.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && LineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					LineView.BackgroundColor = colorTheme.GetColor ();
				}
			});
			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);

			/* set dynamic color properties for this view */
			colorTheme.GetColorThemeSelectionImageResource ((UIImage image) => {
				if (ColorThemeButton != null) {
					ColorThemeButton.SetBackgroundImage (image, UIControlState.Normal);
				}
			});
			colorTheme.GetAddImageResource ((UIImage image) => {
				if (AddMembersButton != null) {
					AddMembersButton.SetBackgroundImage (image, UIControlState.Normal);
				}
			});
			MembersView.BackgroundColor = colorTheme.GetColor ();
			SendMessageButton.Layer.BorderColor = colorTheme.GetColor ().CGColor;
			RemoveButton.Layer.BorderColor = colorTheme.GetColor ().CGColor;

			UpdateThumbnail ();
		}

		public override void TouchesBegan (NSSet touches, UIEvent evt) {
			// hide the keyboard when there are touches outside the keyboard view
			base.TouchesBegan (touches, evt);
			View.EndEditing (true);
		}

		public override void LoadView () {
			base.LoadView ();

			UILoaded = false;

			// show loading indicator while we retreive group info from the server
			progressHud = new MTMBProgressHUD (View) {
				LabelText = "LOADING".t (),
				LabelFont = FontHelper.DefaultFontForLabels(),
				RemoveFromSuperViewOnHide = true
			};
			View.AddSubview (progressHud);
			progressHud.Show (true);

			LineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (LineView);

			TopView = new UIView(new CGRect (0, 0, View.Frame.Width, 164));
			TopView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			TopView.BackgroundColor = iOS_Constants.WHITE_COLOR;

			ThumbnailButton = new UIButton (new CGRect (0, 10, 100, 100));
			ThumbnailButton.ClipsToBounds = true;
			ThumbnailButton.Layer.CornerRadius = 50f;
			TopView.Add (ThumbnailButton);

			View.Add (TopView);

			ColorThemeButton = new UIButton (new CGRect (0, 0, 35, 35));
			ColorThemeButton.Alpha = 0;
			TopView.Add (ColorThemeButton);

			ColorThemeLabel = new UILabel (new CGRect(0, 0, View.Frame.Width, 20));
			ColorThemeLabel.Font = FontHelper.DefaultItalicFont (9f);
			ColorThemeLabel.Text = "COLOR_THEME".t ();
			ColorThemeLabel.TextColor = iOS_Constants.BLACK_COLOR;
			ColorThemeLabel.Alpha = 0;
			TopView.Add (ColorThemeLabel);

			AddMembersButton = new UIButton (new CGRect (0, 0, 35, 35));
			AddMembersButton.Alpha = 0;
			TopView.Add (AddMembersButton);

			AddMembersLabel = new UILabel (new CGRect(0, 0, View.Frame.Width, 20));
			AddMembersLabel.Font = FontHelper.DefaultItalicFont (9f);
			AddMembersLabel.Text = "ADD_MEMBERS".t ();
			AddMembersLabel.TextColor = iOS_Constants.BLACK_COLOR;
			AddMembersLabel.Alpha = 0;
			TopView.Add (AddMembersLabel);

			GroupNameTextField = new EMTextField (new CGRect (0, 120, 200, 34), "GROUP_NAME".t (), UITextAutocorrectionType.Yes, UITextAutocapitalizationType.Words, UIReturnKeyType.Done);
			GroupNameTextField.ShouldReturn += textField => {
				// Dismiss the keyboard when pressing the done key
				textField.ResignFirstResponder ();
				return true; 
			};
			// to validate after the user has entered text and moved away from that text field, use EditingDidEnd
			GroupNameTextField.EditingDidEnd += (sender, e) => {
				// perform validation; removed for now per Bryant
			};
			GroupNameTextField.Alpha = 0;
			TopView.Add (GroupNameTextField);

			GroupNameLabel = new UILabel (new CGRect (0, 120, View.Frame.Width, 24));
			GroupNameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			GroupNameLabel.Font = FontHelper.DefaultFontForLabels ();
			GroupNameLabel.TextColor = iOS_Constants.BLACK_COLOR;
			GroupNameLabel.TextAlignment = UITextAlignment.Center;
			GroupNameLabel.AdjustsFontSizeToFitWidth = true;
			GroupNameLabel.LineBreakMode = UILineBreakMode.TailTruncation;
			GroupNameLabel.Lines = 1; // 0 means unlimited
			GroupNameLabel.Alpha = 0;
			TopView.Add (GroupNameLabel);

			SendMessageButton  = new EMButton (new CGRect (0, 164, 200, 30), colorTheme.GetColor(), "SEND_MESSAGE_TEXT".t ());
			SendMessageButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapSendMessageButton ).HandleEvent<object,EventArgs>;
			TopView.Add (SendMessageButton);

			RemoveButton = new EMButton (new CGRect (0, 200, 200, 30), colorTheme.GetColor(), "");
			RemoveButton.Alpha = 0;
			TopView.Add (RemoveButton);

			MembersView = new UIView (new CGRect (0, 0, View.Frame.Width, 25));
			MembersView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			MembersView.BackgroundColor = colorTheme.GetColor ();

			MembersLabel = new UILabel (new CGRect (0, 0, View.Frame.Width, 15));
			MembersLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			MembersLabel.Font = FontHelper.DefaultFontForLabels (15f);
			MembersLabel.TextColor = iOS_Constants.WHITE_COLOR;
			MembersLabel.TextAlignment = UITextAlignment.Center;
			MembersLabel.Text = "MEMBERS_TITLE".t ();
			MembersLabel.AdjustsFontSizeToFitWidth = true;
			MembersLabel.LineBreakMode = UILineBreakMode.TailTruncation;
			MembersLabel.Lines = 1; // 0 means unlimited

			MembersView.Add (MembersLabel);

			View.Add (MembersView);

			TableView = new UITableView ();
			TableView.BackgroundColor = UIColor.Clear;
			TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			TableView.AllowsSelection = true;
			TableView.AllowsSelectionDuringEditing = true;

			View.Add (TableView);

			View.BringSubviewToFront (progressHud);

			ViewLoaded = true;

			if (Group != null && Group.serverID == null && !UILoaded)
				FinalizeUI ();
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			SharedController.OriginalColorTheme = this.colorTheme;
			ThemeController (InterfaceOrientation);
			
			tableDelegate = new TableViewDelegate (this);
			tableDataSource = new TableViewDataSource (this, shouldDismissController);

			TableView.Delegate = tableDelegate;
			TableView.DataSource = tableDataSource;

			View.AutosizesSubviews = true;
			TableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			TableView.ReloadData ();

			willEnterForegroundObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)Constants.DID_ENTER_FOREGROUND, notification => {
				if (TableView != null)
					TableView.ReloadData ();
			});
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			Visible = true;
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			if (Group != null) {
				ThemeController (InterfaceOrientation);

				nfloat displacement_y = TopLayoutGuide.Length;

				LineView.Frame = new CGRect (0, displacement_y, View.Frame.Width, LineView.Frame.Height);

				var topViewHeight = EditMode && !Group.isUserGroupOwner ? 220 : 200;
				if (EditMode && Group.isUserGroupOwner)
					topViewHeight = 240;
				
				TopView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height, View.Frame.Width, topViewHeight);

				ThumbnailButton.Frame = new CGRect ((View.Frame.Width / 2) - (100 / 2), 10, 100, 100);

				if (Group.isUserGroupOwner) {
					var xCoordCTB = ((View.Frame.Width / 2) - (ThumbnailButton.Frame.Width / 2) - ColorThemeButton.Frame.Width) / 2;
					ColorThemeButton.Frame = new CGRect (xCoordCTB, ((ThumbnailButton.Frame.Height - ColorThemeButton.Frame.Height)/2) - 15, ColorThemeButton.Frame.Width, ColorThemeButton.Frame.Height);

					CGSize sizeCTL = ColorThemeLabel.Text.SizeOfTextWithFontAndLineBreakMode (ColorThemeLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
					sizeCTL = new CGSize ((float)((int)(sizeCTL.Width + 1.5)), (float)((int)(sizeCTL.Height + 1.5)));
					var xCoordCTL = ((View.Frame.Width / 2) - (ThumbnailButton.Frame.Width / 2) - sizeCTL.Width) / 2;
					ColorThemeLabel.Frame = new CGRect (new CGPoint(xCoordCTL, ColorThemeButton.Frame.Y + ColorThemeButton.Frame.Height + 5), sizeCTL);

					var XCoordSB = View.Frame.Width - ((View.Frame.Width - ((View.Frame.Width) / 2 + (ThumbnailButton.Frame.Width / 2) - AddMembersButton.Frame.Width)) / 2);
					AddMembersButton.Frame = new CGRect (XCoordSB, ((ThumbnailButton.Frame.Height - AddMembersButton.Frame.Height)/2) - 15, AddMembersButton.Frame.Width, AddMembersButton.Frame.Height);

					CGSize sizeSBL = AddMembersLabel.Text.SizeOfTextWithFontAndLineBreakMode (AddMembersLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
					sizeSBL = new CGSize ((float)((int)(sizeSBL.Width + 1.5)), (float)((int)(sizeSBL.Height + 1.5)));
					var xCoordSBL = View.Frame.Width - ((View.Frame.Width - ((View.Frame.Width) / 2 + (ThumbnailButton.Frame.Width / 2) - sizeSBL.Width)) / 2);
					AddMembersLabel.Frame = new CGRect (new CGPoint(xCoordSBL, AddMembersButton.Frame.Y + AddMembersButton.Frame.Height + 5), sizeSBL);
				}

				if (!EditMode || Group.isUserGroupOwner) {
					GroupNameTextField.Frame = new CGRect ((View.Frame.Width - 200) / 2, 120, 200, 34);
					SendMessageButton.Frame = new CGRect ((View.Frame.Width - 200) / 2, 162, 200, 30);
				} else
					SendMessageButton.Frame = new CGRect ((View.Frame.Width - 200) / 2, 142, 200, 30);

				if (EditMode && !Group.isUserGroupOwner) {
					GroupNameLabel.Frame = new CGRect ((View.Frame.Width - 200) / 2, 113, 200, 24);
					RemoveButton.Frame = new CGRect ((View.Frame.Width - 200) / 2, 182, 200, 30);
				} else if(EditMode && Group.isUserGroupOwner)
					RemoveButton.Frame = new CGRect ((View.Frame.Width - 200) / 2, 200, 200, 30);

				MembersView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height + TopView.Frame.Height, View.Frame.Width, MembersView.Frame.Height);
				// center Members label
				CGSize size = MembersLabel.Text.SizeOfTextWithFontAndLineBreakMode (MembersLabel.Font, new CGSize (MembersView.Frame.Width, MembersView.Frame.Height), UILineBreakMode.Clip);
				size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
				MembersLabel.Frame = new CGRect (new CGPoint ((MembersView.Frame.Width - size.Width)/2, (MembersView.Frame.Height - size.Height)/2), size);

				TableView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height + TopView.Frame.Height + MembersView.Frame.Height, View.Frame.Width, View.Frame.Height - (displacement_y + LineView.Frame.Height + TopView.Frame.Height + MembersView.Frame.Height));

				if (this.Spinner != null)
					this.Spinner.Frame = new CGRect (this.View.Frame.Width / 2 - this.Spinner.Frame.Width / 2, displacement_y + 35 /*25 is the thumbnail's y from the background view + additional margin*/, this.Spinner.Frame.Width, this.Spinner.Frame.Height);
			}
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			NSNotificationCenter.DefaultCenter.RemoveObserver (willEnterForegroundObserver);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear(animated);
			Visible = false;
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose(bool disposing) {
			NSNotificationCenter.DefaultCenter.RemoveObserver (this);
			sharedEditGroupController.Dispose ();
			base.Dispose (disposing);
		}

		public void SwitchFromSaveToEdit() {
			EditMode = true;
			sharedEditGroupController.Changed = false;
			ImageData = null;

			TableView.ReloadData ();

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			Title = "EDIT_GROUP_TITLE".t ();

			SendMessageButton.Enabled = true;
			AnimateView (SendMessageButton);

			RemoveButton.Enabled = true;
			RemoveButton.SetTitle ("DELETE_GROUP_BUTTON".t (), UIControlState.Normal);
			RemoveButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapRemoveButton).HandleEvent<object,EventArgs>;
			AnimateView (RemoveButton);

			ActionButton = new UIBarButtonItem ("DONE_BUTTON".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapSaveButton).HandleEvent<object,EventArgs>);
			ActionButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			ActionButton.Enabled = true;
			NavigationItem.SetLeftBarButtonItem(ActionButton, true);

			NavigationItem.RightBarButtonItem = null;
		}

		public void FinalizeUI() {
			if (ViewLoaded) {

				UILoaded = true;
				colorTheme = Group.colorTheme;
				GroupNameTextField.Text = Group.displayName;
				GroupNameLabel.Text = Group.displayName;

				//hide the loading indicator now that we're finalizing the UI
				progressHud.Hide (true);

				if (Group.isUserGroupOwner) {
					AnimateView (ColorThemeButton);
					AnimateView (ColorThemeLabel);
					AnimateView (AddMembersButton);
					AnimateView (AddMembersLabel);
				}

				if (!EditMode || Group.isUserGroupOwner) {
					AnimateView (GroupNameTextField);

					if (Group.isUserGroupOwner && !EditMode)
						GroupNameTextField.BecomeFirstResponder ();
				} else
					AnimateView (GroupNameLabel);

				if (Group.serverID != null)
					UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);
				else {
					SendMessageButton.Enabled = false;
					SendMessageButton.Alpha = 0.6f;
				}

				if (EditMode) {
					var btnTitle = "LEAVE_GROUP_BUTTON".t ();
					if (Group.canUserRejoinGroup)
						btnTitle = "REJOIN_GROUP_BUTTON".t ();
					else if (Group.isUserGroupOwner)
						btnTitle = "DELETE_GROUP_BUTTON".t ();

					RemoveButton.SetTitle (btnTitle, UIControlState.Normal);
					AnimateView (RemoveButton);
				}

				if (Group.isUserGroupOwner && !EditMode)
					Title = "CREATE_GROUP_TITLE".t ();
				else
					Title = Group.isUserGroupOwner ? "EDIT_GROUP_TITLE".t () : "GROUP_DETAILS_TITLE".t ();

				TableView.SetEditing (EditMode && Group.isUserGroupOwner, true);

				if (!EditMode || (EditMode && !Group.isUserGroupOwner))
					UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);
				else if (EditMode && Group.isUserGroupOwner) {
					ActionButton = new UIBarButtonItem ("DONE_BUTTON".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapSaveButton).HandleEvent<object,EventArgs>);
					ActionButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
					ActionButton.Enabled = true;
					NavigationItem.SetLeftBarButtonItem(ActionButton, true);
				}

				if (!EditMode || Group.isUserGroupOwner) {
					var title = "SAVE_BUTTON".t ();
					if (EditMode && Group.isUserGroupOwner) {
						//no right bar
					} else {
						ActionButton = EditButtonItem;
						ActionButton.Title = title;
						ActionButton.Clicked += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapSaveButton).HandleEvent<object,EventArgs>;
						ActionButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes (), UIControlState.Normal);
						ActionButton.Enabled = true;
						NavigationItem.RightBarButtonItem = ActionButton;
					}
				}

				if (Group.isUserGroupOwner) {
					ThumbnailButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapImageButton).HandleEvent<object,EventArgs>;
					ColorThemeButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs> (DidTapColorThemeButton).HandleEvent<object,EventArgs>;
					AddMembersButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapAddContactToGroupButton).HandleEvent<object,EventArgs>;
				}

				if (Group.canUserRejoinGroup)
					RemoveButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapRejoinButton).HandleEvent<object,EventArgs>;
				else if (EditMode)
					RemoveButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>(DidTapRemoveButton).HandleEvent<object,EventArgs>;

				UpdateThumbnail ();

				TableView.ReloadData ();

				// This screen name value will remain set on the tracker and sent with hits until it is set to a new value or to null.
				var ViewName = "Create Group View";
				if (EditMode && Group.isUserGroupOwner)
					ViewName = "Edit Group View";
				else if (EditMode && !Group.isUserGroupOwner)
					ViewName = "Group Details View";

				GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, ViewName);
				GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
				//this.View.Add (this.FromAliasTextView);

				View.SetNeedsLayout ();
			}
		}

		static void AnimateView(UIView v) {
			UIView.Animate (0.2, () => {
				v.Alpha = 1;
			});
		}

		public void UpdateThumbnail () {
			colorTheme.GetBlankPhotoAccountResource ((UIImage image) => {
				if (this.Group != null) {
					ImageSetter.SetAccountImage (this.Group, image, (UIImage loadedImage) => {
						if (loadedImage != null) {
							ThumbnailButton.SetImage (loadedImage, UIControlState.Normal);
						}
					});
				}
			});

			UpdateProgressIndicatorVisibility (ImageSetter.ShouldShowProgressIndicator(this.Group));
			UpdateThumbnailBorder ();
		}

		public void UpdateThumbnailBorder () {
			if (ImageSetter.ShouldShowAccountThumbnailFrame (this.Group)) {
				ThumbnailButton.Layer.BorderWidth = 3f;
				ThumbnailButton.Layer.BorderColor = colorTheme.GetColor ().CGColor;
			} else {
				ThumbnailButton.Layer.BorderWidth = 0f;
			}
		}

		#region progress spinner
		// TODO: maybe we can refactor these account pages..
		UIActivityIndicatorView spinner;
		protected UIActivityIndicatorView Spinner {
			get { return spinner; }
			set { spinner = value; }
		}

		public void UpdateProgressIndicatorVisibility (bool showProgressIndicator) {
			if (showProgressIndicator) {
				if (this.Spinner == null) {
					this.Spinner = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.WhiteLarge);
					this.Spinner.Color = UIColor.Black;
					this.Add (this.Spinner);
				}

				CGRect f = this.Spinner.Frame;

				f.Location = new CGPoint (this.View.Frame.Width / 2 - this.spinner.Frame.Width / 2, 70);
				this.Spinner.Frame = f;

				if (!this.Spinner.IsAnimating)
					this.Spinner.StartAnimating ();

				this.View.BringSubviewToFront (this.Spinner);
				this.ThumbnailButton.Alpha = 0;

			} else {
				if (this.Spinner != null) {
					this.Spinner.StopAnimating ();
					this.Spinner.RemoveFromSuperview ();
					this.Spinner = null;
				}

				this.ThumbnailButton.Alpha = 1;
			}
		}
		#endregion

		protected void DidTapColorThemeButton(object sender, EventArgs eventArgs) {
			var p = new ColorThemePickerViewControllerController ();
			p.DidPickColorDelegate += WeakDelegateProxy.CreateProxy<BackgroundColor>( DidPickColor ).HandleEvent<BackgroundColor>;
			p.Title = "COLOR_THEME_PLURAL".t ();
			NavigationController.PushViewController (p, true);
		}

		protected void DidPickColor(BackgroundColor updatedColor) {
			colorTheme = updatedColor;
			this.sharedEditGroupController.ColorTheme = colorTheme;

			ThemeController (InterfaceOrientation);

			NavigationController.PopViewController (true);
		}

		protected void DidTapSaveButton(object sender, EventArgs e) {
			// perform validation
			if(string.IsNullOrEmpty(GroupNameTextField.Text)) {
				UserPrompter.PromptUser ("APP_TITLE".t (), "GROUP_NAME_REQUIRED".t ());
				return;
			}

			if (!this.sharedEditGroupController.HasSuitableNumberOfMembers) {
				UserPrompter.PromptUser ("APP_TITLE".t (), "GROUP_MEMBERS_REQUIRED".t ());
				return;
			}

			ActionButton.Enabled = false;

			byte[] thumbnail = ImageData != null ? ImageData.ToByteArray () : null;

			sharedEditGroupController.SaveOrUpdateAsync (GroupNameTextField.Text, colorTheme, thumbnail, !EditMode);
		}

		protected void DidTapSendMessageButton(object sender, EventArgs eventArgs) {
			this.DismissViewController (true, () => {
				sharedEditGroupController.SendMessageToGroup ();
			});
		}

		protected void DidTapRemoveButton(object sender, EventArgs eventArgs) {
			var title = "ALERT_ARE_YOU_SURE".t ();
			var message = Group.isUserGroupOwner ? "DELETE_GROUP_EXPLAINATION".t () : "LEAVE_GROUP_EXPLAINATION".t ();
			var action = Group.isUserGroupOwner ? "DELETE_GROUP_BUTTON".t () : "LEAVE_GROUP_BUTTON".t ();

			UserPrompter.PromptUserWithAction (title, message, action, DidConfirmRemove);
		}

		protected void DidTapRejoinButton(object sender, EventArgs eventArgs) {
			var title = "ALERT_ARE_YOU_SURE".t ();
			var message = "REJOIN_GROUP_EXPLAINATION".t ();
			var action = "REJOIN_GROUP_BUTTON".t ();

			UserPrompter.PromptUserWithAction (title, message, action, DidConfirmRejoin);
		}

		void DidConfirmRemove() {
			if (Group.isUserGroupOwner) {
				sharedEditGroupController.DeleteAsync (Group.serverID);
			} else
				sharedEditGroupController.LeaveAsync (Group.serverID);

			NavigationController.PopViewController (true);
		}

		void DidConfirmRejoin() {
			sharedEditGroupController.RejoinAsync (Group.serverID);

			NavigationController.PopViewController (true);
		}

		public void Exit() {
			progressHud.Hide (true);
			NavigationController.PopViewController (true);
		}

		public void EnableActionButton() {
			ActionButton.Enabled = true;
		}

		protected void DidTapAddContactToGroupButton(object sender, EventArgs e) {
			AddressBookArgs args = AddressBookArgs.From (true, false, false, this.sharedEditGroupController.ManageableListOfContacts, null);
			var controller = new AddressBookViewController (args);
			controller.DelegateContactSelected += WeakDelegateProxy.CreateProxy<AddressBookSelectionResult>( HandleAddressBookSelectionResult ).HandleEvent<AddressBookSelectionResult>;

			NavigationController.PushViewController (controller, true);
		}

		protected void HandleAddressBookSelectionResult (AddressBookSelectionResult result) {
			this.sharedEditGroupController.ManageContactsAfterAddressBookResult (result);
		}

		public void TransitionToChatController (ChatEntry chatEntry) {
			var chatViewController = new ChatViewController (chatEntry);
			chatViewController.NEW_MESSAGE_INITIATED_FROM_NOTIFICATION = true;

			MainController mainController = AppDelegate.Instance.MainController;
			UINavigationController navController = mainController.ContentController as UINavigationController;
			navController.PushViewController(chatViewController, true);
		}

		#region IPopListener 
		public bool ShouldPopController () {
			return !this.sharedEditGroupController.ShouldStopUserFromExiting;
		}

		public void RunNonPopAction () {
			string title = "ALERT_ARE_YOU_SURE".t ();
			string message = "UNSAVED_CHANGES".t ();
			string action = "EXIT".t ();

			UserPrompter.PromptUserWithAction (title, message, action, LeaveExit);
		}

		private void LeaveExit () {
			this.sharedEditGroupController.UserChoseToLeaveUponBeingAsked = true;
			this.NavigationController.PopViewController (true);
		}

		#endregion

		#region media selection

		public override string ImageSearchSeedString {
			get {
				string groupNameTextField = GroupNameTextField != null ? GroupNameTextField.Text : string.Empty;
				return groupNameTextField;
			}
		}

		protected void DidTapImageButton(object sender, EventArgs eventArgs) {
			StartAcquiringImage ();
		}

		protected override void HandleImageSelected (object sender, UIImagePickerMediaPickedEventArgs e, bool isImage) {
			// get the original image
			var editedImage = e.Info[UIImagePickerController.EditedImage] as UIImage;
			ScaleImageAndSave (editedImage);

			// dismiss the picker
			NavigationController.DismissViewController (true, null);
		}

		protected override void HandleSearchImageSelected (UIImage originalImage) {
			ScaleImageAndSave (originalImage);
		}

		public void ScaleImageAndSave (UIImage editedImage) {
			if(editedImage != null) {
				Thumbnail = ScaleImage (editedImage, 200);
				ImageData = Thumbnail.AsJPEG (0.75f);
				byte[] updatedMedia = ImageData != null ? ImageData.ToByteArray () : null;

				if (updatedMedia != null) {
					string thumbnailPath = sharedEditGroupController.GetStagingFilePathForGroupThumbnail ();
					appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (thumbnailPath);
					appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (thumbnailPath, updatedMedia, null);
					sharedEditGroupController.Group.UpdateThumbnailUrlAfterMovingFromCache (thumbnailPath);
					UpdateThumbnail ();
				}
			}
		}
		#endregion

		class TableViewDelegate : UITableViewDelegate {
			WeakReference editGroupViewControllerRef;
			EditGroupViewController editGroupViewController {
				get { return editGroupViewControllerRef.Target as EditGroupViewController; }
				set { editGroupViewControllerRef = new WeakReference (value); }
			}

			public TableViewDelegate(EditGroupViewController controller) {
				editGroupViewController = controller;
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return iOS_Constants.APP_CELL_ROW_HEIGHT;
			}

			public override UITableViewCellEditingStyle EditingStyleForRow (UITableView tableView, NSIndexPath indexPath) {
				return editGroupViewController.Group.isUserGroupOwner ? UITableViewCellEditingStyle.Delete : UITableViewCellEditingStyle.None;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				UINavigationBarUtil.SetBackButtonToHaveNoText (editGroupViewController.NavigationItem);

				Contact member = editGroupViewController.sharedEditGroupController.Group.members [indexPath.Row];
				if (!member.me)
					editGroupViewController.NavigationController.PushViewController (new ProfileViewController (member, editGroupViewController.shouldDismissController), true);

				tableView.DeselectRow (indexPath, true);
			}
		}

		class TableViewDataSource : UITableViewDataSource {
			WeakReference editGroupViewControllerRef;

			EditGroupViewController EditGroupViewController {
				get { return editGroupViewControllerRef.Target as EditGroupViewController; }
				set { editGroupViewControllerRef = new WeakReference (value); }
			}
			readonly bool shouldDismissController;

			public TableViewDataSource(EditGroupViewController controller, bool dismiss) {
				EditGroupViewController = controller;
				shouldDismissController = dismiss;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				if (EditGroupViewController.sharedEditGroupController.Group == null)
					return 0;

				return EditGroupViewController.sharedEditGroupController.Group.members == null || EditGroupViewController.sharedEditGroupController.Group.members.Count < 1 ? 0 : EditGroupViewController.sharedEditGroupController.Group.members.Count;
			}

			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath) {
				if (EditGroupViewController.Group.isUserGroupOwner) {
					if (EditGroupViewController.EditMode)
						return indexPath.Row != 0; //assume first row is group owner always per API design

					return true;
				}

				return false;
			}

			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath) {
				Contact toRemove = EditGroupViewController.sharedEditGroupController.Group.members [indexPath.Row];

				if(!toRemove.me) {
					EditGroupViewController.sharedEditGroupController.RemoveContact (toRemove);
					EditGroupViewController.TableView.DeleteRows (new [] { indexPath }, UITableViewRowAnimation.Fade);
				}
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {

				EditGroupViewController controller = this.EditGroupViewController;

				var cell = (GroupTableViewCell)tableView.DequeueReusableCell (GroupTableViewCell.Key) ?? GroupTableViewCell.Create ();

				if (controller != null) {
					Contact member = controller.sharedEditGroupController.Group.members [indexPath.Row];

					cell.Group = member;
					cell.SetEvenRow (indexPath.Row % 2 == 0);

					if (controller.Group.abandonedContacts.Contains (member.serverID))
						cell.SetAbandoned ();

					cell.SendButtonClickCallback = () => {
						if (!member.me) {
							if (shouldDismissController) {
								controller.DismissViewController (true, () => {
									controller.sharedEditGroupController.GoToNewOrExistingChatEntry (member);
								});
							} else {
								controller.sharedEditGroupController.GoToNewOrExistingChatEntry (member);
							}
						}
					};
				}

				return cell;
			}
		}

		class SharedEditGroupController : AbstractEditGroupController {
			WeakReference editGroupViewControllerRef;

			EditGroupViewController editGroupViewController {
				get { return editGroupViewControllerRef.Target as EditGroupViewController; }
				set { editGroupViewControllerRef = new WeakReference (value); }
			}

			public SharedEditGroupController(EditGroupViewController controller, ApplicationModel appModel, Group g) : base (appModel, g) {
				editGroupViewController = controller;
			}

			public override void ListOfMembersUpdated () {
				EMTask.DispatchMain (() => {
					EditGroupViewController self = this.editGroupViewController;
					if (self == null) return;
					self.TableView.ReloadData ();
				});
			}

			protected override void DidLoadGroup () {
				EMTask.DispatchMain (() => {
					if (editGroupViewController != null && editGroupViewController.IsViewLoaded)
						editGroupViewController.FinalizeUI();
				});
			}

			protected override void DidSaveGroup () {
				EMTask.DispatchMain (() => {
					if (editGroupViewController != null && editGroupViewController.IsViewLoaded)
						editGroupViewController.SwitchFromSaveToEdit ();
				});
			}

			protected override void DidUpdateGroup () {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						editGroupViewController.Exit ();
				});
			}

			public override void DidChangeColorTheme() {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						editGroupViewController.ThemeController (editGroupViewController.InterfaceOrientation);
				});
			}

			public override void UpdateAliasText (string text) {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						editGroupViewController.FromAliasTextView.Text = text;
				});
			}

			protected override void DidLoadGroupFailed () {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						UserPrompter.PromptUserWithActionNoNegative ("APP_TITLE".t (), "GROUP_LOAD_FAILED".t (), editGroupViewController.Exit);
				});
			}

			protected override void DidSaveGroupFailed () {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						UserPrompter.PromptUserWithActionNoNegative ("APP_TITLE".t (), "GROUP_SAVE_FAILED".t (), editGroupViewController.EnableActionButton);
				});
			}

			protected override void DidSaveOrUpdateGroupFailed () {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						UserPrompter.PromptUserWithActionNoNegative ("APP_TITLE".t (), "GROUP_SAVE_OR_UPDATE_FAILED".t (), editGroupViewController.EnableActionButton);
				});
			}

			protected override void DidUpdateGroupFailed () {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						UserPrompter.PromptUserWithActionNoNegative ("APP_TITLE".t (), "GROUP_UPDATE_FAILED".t (), editGroupViewController.EnableActionButton);
				});
			}

			protected override void DidLeaveOrRejoinGroup () {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						editGroupViewController.TableView.ReloadData ();
				});
			}

			protected override void DidLeaveGroupFailed () {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						UserPrompter.PromptUser ("APP_TITLE".t (), "GROUP_LEAVE_FAILED".t ());
				});
			}

			protected override void DidRejoinGroupFailed () {
				EMTask.DispatchMain (() => {
					if (editGroupViewController != null && editGroupViewController.IsViewLoaded)
						UserPrompter.PromptUser ("APP_TITLE".t (), "GROUP_REJOIN_FAILED".t ());
				});
			}

			protected override void ContactDidChangeThumbnail () {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						editGroupViewController.TableView.ReloadData ();
				});
			}

			public override void TransitionToChatController (ChatEntry chatEntry) {
				EMTask.DispatchMain (() => {
					if(editGroupViewController != null && editGroupViewController.IsViewLoaded)
						editGroupViewController.TransitionToChatController (chatEntry);
				});
			}

			public override string TextInDisplayField {
				get {
					string s = string.Empty;
					EditGroupViewController self = this.editGroupViewController;
					if (self != null) {
						s = self.GroupNameTextField.Text;
					}

					return s;
				}
			}
		}
	}
}