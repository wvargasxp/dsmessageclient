using System;
using System.IO;
using CoreGraphics;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using MBProgressHUD;
using String_UIKit_Extension;
using UIKit;

namespace iOS {
	using Media_iOS_Extension;

	public class EditAliasViewController : AbstractAcquiresImagesController, IPopListener {

		public MTMBProgressHUD progressHud;
		public string aliasServerID;

		NSData ThumbnailData, IconData;

		#region UI
		public UIView LineView;

		UIView BackgroundView;

		public UIButton ThumbnailButton, IconButton, ColorThemeButton;
		public EMButton DeleteButton;
		public UIImage Thumbnail, Icon;
		public UILabel NameLabel, ColorThemeLabel, IconButtonLabel, NameDescriptionLabel;
		public UITextField NameTextField;
		#endregion

		readonly SharedEditAliasController sharedEditAliasController;
		public AbstractEditAliasController SharedController { get { return sharedEditAliasController; }}

		readonly ApplicationModel appModel;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		public EditAliasViewController (string sid) {
			aliasServerID = sid;

			ThumbnailData = null;
			IconData = null;

			appModel = ((AppDelegate)UIApplication.SharedApplication.Delegate).applicationModel;
		
			this.sharedEditAliasController = new SharedEditAliasController (appModel, this);
			this.sharedEditAliasController.SetInitialAlias (aliasServerID);

			this.AllowVideoOnImageSelection = false;
			this.AllowImageEditingOnImageSelection = true;
		}

		public void ThemeController (UIInterfaceOrientation orientation) {
			BackgroundColor colorTheme = sharedEditAliasController.Alias != null ? sharedEditAliasController.Alias.colorTheme : BackgroundColor.Default;
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
					UpdateThumbnail ();
					UpdateIcon ();
				}
			});
			if(DeleteButton != null)
				DeleteButton.Layer.BorderColor = colorTheme.GetColor ().CGColor;
		}

		public override void TouchesBegan (NSSet touches, UIEvent evt) {
			// hide the keyboard when there are touches outside the keyboard view
			base.TouchesBegan (touches, evt);
			View.EndEditing (true);
		}

		public override void LoadView () {
			base.LoadView ();

			LineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (LineView);
			View.BringSubviewToFront (LineView);

			BackgroundView = new UIView(new CGRect (0, 0, View.Frame.Width, View.Frame.Height));
			BackgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			BackgroundView.BackgroundColor = iOS_Constants.WHITE_COLOR;

			ThumbnailButton = new UIButton (new CGRect (0, 20, 100, 100));
			ThumbnailButton.ClipsToBounds = true;
			ThumbnailButton.Layer.CornerRadius = 50f;
			UpdateThumbnail ();
			BackgroundView.Add (ThumbnailButton);

			ColorThemeButton = new UIButton (new CGRect (0, 0, 35, 35));
			BackgroundView.Add (ColorThemeButton);

			ColorThemeLabel = new UILabel (new CGRect(0, 0, View.Frame.Width, 20));
			ColorThemeLabel.Font = FontHelper.DefaultItalicFont (9f);
			ColorThemeLabel.Text = "COLOR_THEME".t ();
			ColorThemeLabel.TextColor = iOS_Constants.BLACK_COLOR;
			BackgroundView.Add (ColorThemeLabel);

			IconButton = new UIButton (new CGRect (0, 0, 35, 35));
			IconButton.ClipsToBounds = true;
			IconButton.Layer.CornerRadius = iOS_Constants.EDIT_ALIAS_VIEW_CORNER_RADIUS;
			IconButton.Layer.MasksToBounds = true;
			UpdateIcon ();
			BackgroundView.Add (IconButton);

			IconButtonLabel = new UILabel (new CGRect(0, 0, View.Frame.Width, 20));
			IconButtonLabel.Font = FontHelper.DefaultItalicFont (9f);
			IconButtonLabel.Text = "CHOOSE_ICON".t ();
			IconButtonLabel.TextColor = iOS_Constants.BLACK_COLOR;
			BackgroundView.Add (IconButtonLabel);

			if (aliasServerID == null) {
				NameTextField = new EMTextField (new CGRect (0, 140, 200, 34), "ALIAS".t (), UITextAutocorrectionType.No, UITextAutocapitalizationType.None, UIReturnKeyType.Done);
				NameTextField.Text = sharedEditAliasController.Alias != null && !string.IsNullOrEmpty (sharedEditAliasController.Alias.displayName) ? sharedEditAliasController.Alias.displayName : "";
				NameTextField.ShouldReturn += textField => { 
					// Dismiss the keyboard when pressing the done key
					textField.ResignFirstResponder ();
					return true; 
				};
				NameTextField.EditingChanged += (object sender, EventArgs e) => {
					sharedEditAliasController.TextInDisplayNameField = NameTextField.Text;
				};
				// to validate after the user has entered text and moved away from that text field, use EditingDidEnd
				NameTextField.EditingDidEnd += (sender, e) => {
					// perform validation; removed for now per Bryant
				};
				NameTextField.BecomeFirstResponder ();
				BackgroundView.Add (NameTextField);

				sharedEditAliasController.TextInDisplayNameField = NameTextField.Text;

				NameDescriptionLabel = new UILabel (new CGRect(0, 180, View.Frame.Width, 20));
				NameDescriptionLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
				NameDescriptionLabel.TextAlignment = UITextAlignment.Center;
				NameDescriptionLabel.Font = FontHelper.DefaultItalicFont (9f);
				NameDescriptionLabel.Text = "ALIAS".t ();
				NameDescriptionLabel.TextColor = iOS_Constants.BLACK_COLOR;
				BackgroundView.Add (NameDescriptionLabel);
			} else {
				NameLabel = new UILabel (new CGRect (0, 140, 200, 34));
				NameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
				NameLabel.Font = FontHelper.DefaultFontForTextFields ();
				NameLabel.ClipsToBounds = true;
				NameLabel.TextColor = iOS_Constants.BLACK_COLOR;
				NameLabel.TextAlignment = UITextAlignment.Center;
				NameLabel.Text = sharedEditAliasController.Alias.displayName;
				BackgroundView.Add (NameLabel);

				sharedEditAliasController.TextInDisplayNameField = NameLabel.Text;

				var color = sharedEditAliasController.Alias != null ? sharedEditAliasController.Alias.colorTheme.GetColor () : BackgroundColor.Default.GetColor ();
				DeleteButton = new EMButton (new CGRect (0, 185, 200, 34), color, "DELETE_ALIAS_BUTTON".t ());
				DeleteButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapDeleteButton).HandleEvent<object,EventArgs>;
				BackgroundView.Add (DeleteButton);
			}

			View.Add (BackgroundView);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			Title = aliasServerID == null ? "ADD_ALIAS_BUTTON".t () : "EDIT_ALIAS_TITLE".t ();

			if (aliasServerID == null) {
				UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

				var saveButton = new UIBarButtonItem ("SAVE_BUTTON".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapSaveButton).HandleEvent<object,EventArgs>);
				saveButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
				NavigationItem.SetRightBarButtonItem(saveButton, true);
			} else {
				var doneButton = new UIBarButtonItem ("DONE_BUTTON".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapSaveButton).HandleEvent<object,EventArgs>);
				doneButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
				NavigationItem.SetLeftBarButtonItem(doneButton, true);
			}

			ThumbnailButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapImageButton).HandleEvent<object,EventArgs>;
			IconButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapIconButton).HandleEvent<object,EventArgs>;
			ColorThemeButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapColorThemeButton).HandleEvent<object,EventArgs>;

			View.AutosizesSubviews = true;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			//NameTextField.Selected = true;
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Edit Alias View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, View.Frame.Width, LineView.Frame.Height);

			BackgroundView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height, View.Frame.Width, View.Frame.Height);
			ThumbnailButton.Frame = new CGRect ((View.Frame.Width / 2) - (100 / 2), 25, 100, 100);

			if (aliasServerID == null) {
				NameTextField.Frame = new CGRect ((View.Frame.Width - 200) / 2, 140, 200, 34);

				CGSize sizeNL = NameDescriptionLabel.Text.SizeOfTextWithFontAndLineBreakMode (NameDescriptionLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
				sizeNL = new CGSize ((float)((int)(sizeNL.Width + 1.5)), (float)((int)(sizeNL.Height + 1.5)));
				var xCoordNL = (View.Frame.Width - sizeNL.Width) / 2;
				NameDescriptionLabel.Frame = new CGRect (new CGPoint(xCoordNL, NameTextField.Frame.Y + NameTextField.Frame.Height + 5), sizeNL);
			} else {
				NameLabel.Frame = new CGRect ((View.Frame.Width - 200) / 2, 140, 200, 34);
				DeleteButton.Frame = new CGRect ((View.Frame.Width - 200) / 2, 185, 200, 34);
			}

			var xCoordCTB = ((View.Frame.Width / 2) - (ThumbnailButton.Frame.Width / 2) - ColorThemeButton.Frame.Width) / 2;
			ColorThemeButton.Frame = new CGRect (xCoordCTB, ((ThumbnailButton.Frame.Height - ColorThemeButton.Frame.Height)/2) - 10, ColorThemeButton.Frame.Width, ColorThemeButton.Frame.Height);

			CGSize sizeCTL = ColorThemeLabel.Text.SizeOfTextWithFontAndLineBreakMode (ColorThemeLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
			sizeCTL = new CGSize ((float)((int)(sizeCTL.Width + 1.5)), (float)((int)(sizeCTL.Height + 1.5)));
			var xCoordCTL = ((View.Frame.Width / 2) - (ThumbnailButton.Frame.Width / 2) - sizeCTL.Width) / 2;
			ColorThemeLabel.Frame = new CGRect (new CGPoint(xCoordCTL, ColorThemeButton.Frame.Y + ColorThemeButton.Frame.Height + 5), sizeCTL);

			var XCoordIB = View.Frame.Width - ((View.Frame.Width - ((View.Frame.Width) / 2 + (ThumbnailButton.Frame.Width / 2) - IconButton.Frame.Width)) / 2);
			IconButton.Frame = new CGRect (XCoordIB, ((ThumbnailButton.Frame.Height - IconButton.Frame.Height)/2) - 10, IconButton.Frame.Width, IconButton.Frame.Height);

			CGSize sizeIBL = IconButtonLabel.Text.SizeOfTextWithFontAndLineBreakMode (IconButtonLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
			sizeIBL = new CGSize ((float)((int)(sizeIBL.Width + 1.5)), (float)((int)(sizeIBL.Height + 1.5)));
			var xCoordIBL = View.Frame.Width - ((View.Frame.Width - ((View.Frame.Width) / 2 + (ThumbnailButton.Frame.Width / 2) - sizeIBL.Width)) / 2);
			IconButtonLabel.Frame = new CGRect (new CGPoint(xCoordIBL, IconButton.Frame.Y + IconButton.Frame.Height + 5), sizeIBL);


			if (this.Spinner != null) {
				this.Spinner.Frame = new CGRect (this.View.Frame.Width / 2 - this.Spinner.Frame.Width / 2, displacement_y + 35 /*25 is the thumbnail's y from the background view + additional margin*/, this.Spinner.Frame.Width, this.Spinner.Frame.Height);
				this.View.BringSubviewToFront (this.Spinner);
			}
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			if (NameTextField != null && NameTextField.IsFirstResponder)
				NameTextField.ResignFirstResponder ();
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
		}

		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose(bool disposing) {
			sharedEditAliasController.Dispose ();

			base.Dispose (disposing);
		}

		#region IPopListener 
		public bool ShouldPopController () {
			return !this.sharedEditAliasController.ShouldStopUserFromExiting;
		}

		public void RunNonPopAction () {
			string title = "ALERT_ARE_YOU_SURE".t ();
			string message = "UNSAVED_CHANGES".t ();
			string action = "EXIT".t ();

			UserPrompter.PromptUserWithAction (title, message, action, Exit);
		}

		private void Exit () {
			this.sharedEditAliasController.UserChoseToLeaveUponBeingAsked = true;
			this.NavigationController.PopViewController (true);
		}

		#endregion

		public void UpdateThumbnail () {
			sharedEditAliasController.Alias.colorTheme.GetBlankPhotoAccountResource ((UIImage image) => {
				if (sharedEditAliasController != null && sharedEditAliasController.Alias != null) {
					ImageSetter.SetAccountImage (sharedEditAliasController.Alias, image, (UIImage loadedImage) => {
						if (loadedImage != null)
							ThumbnailButton.SetBackgroundImage (loadedImage, UIControlState.Normal);
					});
				}
			});

			UpdateThumbnailBorder ();
			UpdateProgressIndicatorVisibility (ImageSetter.ShouldShowProgressIndicator(sharedEditAliasController.Alias));
		}

		public void UpdateThumbnailBorder () {
			if (ImageSetter.ShouldShowAccountThumbnailFrame (sharedEditAliasController.Alias)) {
				ThumbnailButton.Layer.BorderWidth = 3f;
				ThumbnailButton.Layer.BorderColor = sharedEditAliasController.Alias.colorTheme.GetColor ().CGColor;
			} else {
				ThumbnailButton.Layer.BorderWidth = 0f;
			}
		}

		public void UpdateIcon(bool expectingMediaToDownload = false) {
			if (sharedEditAliasController.Alias.MediaForIcon != null) {
				Icon = sharedEditAliasController.Alias.MediaForIcon.LoadThumbnail ();
				if (Icon != null)
					IconButton.SetBackgroundImage (Icon, UIControlState.Normal);
			}

			if (Icon == null && !expectingMediaToDownload)
				IconButton.SetBackgroundImage (UIImage.FromFile(NSBundle.MainBundle.PathForResource ("Icon", "png")), UIControlState.Normal);
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
			var picker = new ColorThemePickerViewControllerController ();
			picker.DidPickColorDelegate += WeakDelegateProxy.CreateProxy<BackgroundColor>( DidPickColor).HandleEvent<BackgroundColor>;
			picker.Title = "COLOR_THEME_PLURAL".t ();
			NavigationController.PushViewController (picker, true);
		}

		protected void DidPickColor(BackgroundColor updatedColor) {
			sharedEditAliasController.Alias.colorTheme = updatedColor; // will kick off a delegate call that will update theme
			NavigationController.PopViewController (true);
		}

		protected void DidTapSaveButton(object sender, EventArgs e) {
			progressHud = new MTMBProgressHUD (View) {
				LabelText = "WAITING".t (),
				LabelFont = FontHelper.DefaultFontForLabels(),
				RemoveFromSuperViewOnHide = true
			};

			this.View.Add (progressHud);
			progressHud.Show (animated: true);

			sharedEditAliasController.SaveOrUpdateAliasAsync ();
		}

		protected void DidTapDeleteButton(object sender, EventArgs e) {
			sharedEditAliasController.DeleteAliasAsync (aliasServerID, true);
		}

		#region media selection
		public override string ImageSearchSeedString {
			get {
				string nameLabelText = NameLabel != null ? NameLabel.Text : string.Empty;
				string nameTextField = NameTextField != null ? NameTextField.Text : string.Empty;
				if (!string.IsNullOrWhiteSpace (nameTextField)) {
					return nameTextField;
				} else if (!string.IsNullOrWhiteSpace (nameLabelText)) {
					return nameLabelText;
				} else {
					return string.Empty;
				}
			}
		}

		bool selectingImage;
		protected bool SelectingImage {
			get { return selectingImage; }
			set { selectingImage = value; }
		}

		protected void DidTapImageButton(object sender, EventArgs eventArgs) {
			this.UsingSquareCropper = false;
			this.SelectingImage = true;
			this.StartAcquiringImage ();
		}

		protected override void HandleImageSelected (object sender, UIImagePickerMediaPickedEventArgs e, bool isImage) {
			// get the original image
			var editedImage = e.Info [UIImagePickerController.EditedImage] as UIImage;
			if (this.SelectingImage) {
				ScaleImageAndSave (editedImage);
			} else {
				ScaleIconAndSave (editedImage);
			}

			// dismiss the picker
			this.NavigationController.DismissViewController (true, null);
		}

		protected override void HandleSearchImageSelected (UIImage originalImage) {
			if (this.SelectingImage) {
				ScaleImageAndSave (originalImage);
			} else {
				ScaleIconAndSave (originalImage);
			}
		}

		protected void DidTapIconButton(object sender, EventArgs eventArgs) {
			this.UsingSquareCropper = true;
			this.SelectingImage = false;
			this.StartAcquiringImage ();
		}

		protected void ScaleImageAndSave (UIImage editedImage) {
			if(editedImage != null) {
				Thumbnail = ScaleImage (editedImage, 400);
				ThumbnailData = Thumbnail.AsJPEG (0.90f);
				byte[] updatedMedia = ThumbnailData != null ? ThumbnailData.ToByteArray () : null;
				if (updatedMedia != null) {
					string aliasThumbnailPath = sharedEditAliasController.GetStagingFilePathForAliasThumbnail ();
					appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (aliasThumbnailPath);
					appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (aliasThumbnailPath, updatedMedia, null);
					sharedEditAliasController.Alias.UpdateThumbnailUrlAfterMovingFromCache (aliasThumbnailPath);
					sharedEditAliasController.UpdatedThumbnail = updatedMedia;
					UpdateThumbnail ();
				}
			}
		}

		protected void ScaleIconAndSave (UIImage editedImage) {
			if(editedImage != null) {
				Icon = editedImage.ScaleImage (100);
				IconData = Icon.AsJPEG (0.90f);
				byte[] updatedIcon = IconData != null ? IconData.ToByteArray () : null;
				if (updatedIcon != null) {
					string aliasIconPath = sharedEditAliasController.GetStagingFilePathForAliasIconThumbnail ();
					appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (aliasIconPath);
					appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (aliasIconPath, updatedIcon, null);
					sharedEditAliasController.Alias.UpdateIconUrlAfterMovingFromCache (aliasIconPath);
					sharedEditAliasController.UpdatedIcon = updatedIcon;
					UpdateIcon ();
				}
			}
		}
		#endregion
	}

	class SharedEditAliasController : AbstractEditAliasController {
		readonly WeakReference editAliasViewControllerRef;
		readonly ApplicationModel _appModel;

		public SharedEditAliasController(ApplicationModel appModel, EditAliasViewController eavc) : base (appModel) {
			_appModel = appModel;
			editAliasViewControllerRef = new WeakReference(eavc);
		}

		public override void ThumbnailUpdated () {
			var editAliasViewController = editAliasViewControllerRef.Target as EditAliasViewController;
			if (editAliasViewController == null) 
				return;

			EMTask.DispatchMain (() => {
				editAliasViewController.ThemeController (editAliasViewController.InterfaceOrientation);
			});
		}

		public override void DidAliasActionFail (string message) {
			EMTask.DispatchMain (() => {
				var editAliasViewController = editAliasViewControllerRef.Target as EditAliasViewController;
				if (editAliasViewController != null && editAliasViewController.IsViewLoaded) {
					editAliasViewController.progressHud.Hide (animated: true, delay: 1);

					var alert = new UIAlertView ("APP_TITLE".t (), message, null, "OK_BUTTON".t (), null);
					alert.Clicked += (s, b) => { };
					alert.Show ();
				}
			});
		}

		public override void DidSaveAlias(bool saved) {
			EMTask.DispatchMain (() => {
				var editAliasViewController = editAliasViewControllerRef.Target as EditAliasViewController;
				if (editAliasViewController != null && editAliasViewController.IsViewLoaded) {
					if (saved) {
						EMTask.DispatchBackground (() => {
							#region saving intagram photo screenshot to share
							byte[] thumbnailBytes = null;
							if(UpdatedThumbnail != null) {
								thumbnailBytes = new byte[UpdatedThumbnail.Length];
								UpdatedThumbnail.CopyTo(thumbnailBytes, 0);
							}

							ShareHelper.GenerateInstagramSharableFile (_appModel, Alias, thumbnailBytes, -1, (int index) => { });

							// clear images in memory on success only
							UpdatedThumbnail = null;
							UpdatedIcon = null;
							#endregion
						});
					}

					editAliasViewController.progressHud.Hide (animated: true, delay: 1);
					editAliasViewController.NavigationController.PopViewController (true);
				}
			});
		}

		public override void DidDeleteAlias() {
			EMTask.DispatchMain (() => {
				var editAliasViewController = editAliasViewControllerRef.Target as EditAliasViewController;
				if (editAliasViewController != null && editAliasViewController.IsViewLoaded) {
					editAliasViewController.progressHud.Hide (animated: true, delay: 1);
					editAliasViewController.NavigationController.PopViewController (true);
				}
			});
		}

		public override void DidChangeColorTheme() {
			var editAliasViewController = editAliasViewControllerRef.Target as EditAliasViewController;
			if (editAliasViewController != null && editAliasViewController.IsViewLoaded)
				editAliasViewController.ThemeController (editAliasViewController.InterfaceOrientation);
		}

		public override void ConfirmWithUserDelete (String serverID, Action<bool> onCompletion) {
			var alert = new UIAlertView ("ALERT_ARE_YOU_SURE".t (), "ALIAS_DELETE_CONFIRMATION_MESSAGE".t (), null, "CANCEL_BUTTON".t (), "DELETE_BUTTON".t ());
			alert.Clicked += (sender, buttonArgs) => {
				bool willDelete = buttonArgs.ButtonIndex == 1;

				if (willDelete) {
					var vc = editAliasViewControllerRef.Target as EditAliasViewController;
					if ( vc != null ) {
						vc.progressHud = new MTMBProgressHUD (vc.View) {
							LabelText = "WAITING".t (),
							LabelFont = FontHelper.DefaultFontForLabels(),
							RemoveFromSuperViewOnHide = true
						};

						vc.View.Add (vc.progressHud);
						vc.progressHud.Show (animated: true);
					}
				}

				onCompletion(willDelete);
			};
			alert.Show ();
		}
	}
}