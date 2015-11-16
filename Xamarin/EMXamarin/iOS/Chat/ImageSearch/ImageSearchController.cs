using System;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using MBProgressHUD;
using UIKit;
using System.Diagnostics;

namespace iOS {
	using UIImageExtensions;
	public class ImageSearchController : UICollectionViewController {
		UIToolbar toolbar;
		BlockEditMenuTextField textField;
		UIButton searchButton;
		UIView lineView;
		private MTMBProgressHUD ProgressHud { get; set; }

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		Action<UIImage> imageSelectedCallback;
		public Action<UIImage> ImageSelectedCallback {
			get { return imageSelectedCallback; }
			set { imageSelectedCallback = value;; }
		}

		readonly int SEND_BUTTON_SIZE = 30;
		static readonly NSString IMAGE_SEARCH_COLLETION_VIEW_CELL_ID = new NSString ("ImageSearchCollectionViewCellId");

		public SharedImageSearchController SharedController { get; set; }

		public ImageSearchController (UICollectionViewLayout layout, ImageSearchParty thirdParty, Action<UIImage> imgSelectedCB, string seedString) : base (layout) {
			SharedController = new SharedImageSearchController (this, thirdParty, seedString);
			this.CollectionView.RegisterClassForCell (typeof(ImageSearchCollectionViewCell), IMAGE_SEARCH_COLLETION_VIEW_CELL_ID);
			this.CollectionView.Delegate = new ImageSearchDelegate (this);
			this.CollectionView.DataSource = new ImageSearchDataSource (this);
			this.CollectionView.BackgroundColor = UIColor.Clear;
			this.CollectionView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;
			this.ImageSelectedCallback = imgSelectedCB;
			this.Title = "SEARCH_TITLE".t ();
		}

		readonly int TOOLBAR_HEIGHT = 40;

		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews ();
			nfloat displacement_y = this.TopLayoutGuide.Length;
			lineView.Frame = new CGRect (0, displacement_y, lineView.Frame.Width, lineView.Frame.Height);
			toolbar.Frame = new CGRect (0, lineView.Frame.Y + 1, this.View.Frame.Width, TOOLBAR_HEIGHT);
			searchButton.Frame = new CGRect (this.View.Frame.Width - searchButton.Frame.Width - UI_CONSTANTS.SMALL_MARGIN, toolbar.Frame.Height / 2 - searchButton.Frame.Height / 2, searchButton.Frame.Width, searchButton.Frame.Height);
			this.CollectionView.Frame = new CGRect (0, toolbar.Frame.Y + toolbar.Frame.Height + 1, this.CollectionView.Frame.Width, this.View.Frame.Height - (toolbar.Frame.Y + toolbar.Frame.Height) + 1);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			this.AutomaticallyAdjustsScrollViewInsets = false;
			lineView = new UINavigationBarLine (new CGRect (0, 0, this.View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			this.View.Add (lineView);

			toolbar = new UIToolbar (new CGRect (0, 0, this.View.Frame.Width, TOOLBAR_HEIGHT));
			this.View.Add (toolbar);

			textField = new BlockEditMenuTextField (new CGRect (10, 0, this.View.Frame.Width - 100, TOOLBAR_HEIGHT - 5));
			textField.Text = SharedController.BeginningQueryString;
			textField.AutocorrectionType = UITextAutocorrectionType.No;
			textField.AutocapitalizationType = UITextAutocapitalizationType.None;
			textField.ReturnKeyType = UIReturnKeyType.Search;

			searchButton = new UIButton (UIButtonType.RoundedRect);
			searchButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object, EventArgs> (SearchButtonTapped).HandleEvent<object, EventArgs>;

			AppDelegate.Instance.applicationModel.account.accountInfo.colorTheme.GetChatSendButtonResource ((UIImage image) => {
				if (searchButton != null) {
					searchButton.SetBackgroundImage (image, UIControlState.Normal);
				}
			});
			searchButton.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
			searchButton.Frame = new CGRect (0, 0, SEND_BUTTON_SIZE, SEND_BUTTON_SIZE);
			searchButton.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			searchButton.Enabled = true;

			var textFieldItem = new UIBarButtonItem (textField);
			var searchItem = new UIBarButtonItem (searchButton);
			var space = new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace);
			toolbar.Items = new UIBarButtonItem[] { textFieldItem, searchItem, space };

			SharedController.InitializeAndSearch ();
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			if (IsModal ()) {
				var leftButton = new UIBarButtonItem ("CLOSE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>(LeftButtonTapped).HandleEvent<object,EventArgs>);
				leftButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes (), UIControlState.Normal);
				NavigationItem.SetLeftBarButtonItem (leftButton, true);
			}

			ThemeController ();
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;
			textField.ShouldReturn += TextFieldShouldReturn;
			textField.SelectAll (textField);
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
			ImageSetter.ClearSearchImageCache ();
			textField.ShouldReturn -= TextFieldShouldReturn;
		}

		public void PauseUI () {
			EMTask.DispatchMain (() => {
				this.View.EndEditing (true);
				this.ProgressHud = new MTMBProgressHUD (View) {
					LabelText = "WAITING".t (),
					LabelFont = FontHelper.DefaultFontForLabels(),
					RemoveFromSuperViewOnHide = true
				};

				this.ProgressHud.UserInteractionEnabled = false; // This lets the user interact with the view behind the progress indicator.
				this.View.Add (ProgressHud);
				this.ProgressHud.Show (animated: true);
			});
		}

		public void ResumeUI () {
			EMTask.DispatchMain (() => {
				if (this.ProgressHud != null) {
					this.ProgressHud.Hide (animated: true, delay: 0);
				}
			});
		}

		private void LeftButtonTapped (object sender, EventArgs args) {
			ResumeUI ();
			this.DismissViewController (true, null);
		}

		void SearchButtonTapped (object sender, EventArgs e) {
			ImageSetter.ClearSearchImageCache ();
			SharedController.SearchForImagesWithTerm (textField.Text);
		}

		bool TextFieldShouldReturn (UITextField tf) {
			// Dismiss the keyboard when pressing the go key
			tf.ResignFirstResponder ();
			// Press the search button
			searchButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
			return true; 
		}

		public void ReloadUI () {
			EMTask.DispatchMain (() => this.CollectionView.ReloadData ());
		}

		public bool IsModal () {
			return this.NavigationController.ViewControllers.Length <= 1;
		}

		protected override void Dispose(bool disposing) {
			//sharedController.Dispose ();
			base.Dispose (disposing);
		}

		void ThemeController () {
			ThemeController (this.InterfaceOrientation);
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;

			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && lineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					lineView.BackgroundColor = mainColor.GetColor ();
				}
			});


			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		void DisplayErrorMessage (string errorMessage) {
			EMTask.DispatchMain (() => {
				var alert = new UIAlertView ("APP_TITLE".t (), errorMessage, null, "OK_BUTTON".t (), null);
				alert.Show ();
			});
		}

		class ImageSearchDataSource : UICollectionViewDataSource {
			readonly WeakReference controllerRef;

			public ImageSearchDataSource (ImageSearchController c) {
				controllerRef = new WeakReference (c);
			}

			public override nint NumberOfSections (UICollectionView collectionView) {
				return 1;
			}

			public override nint GetItemsCount (UICollectionView collectionView, nint section) {
				var controller = controllerRef.Target as ImageSearchController;
				return controller != null ? controller.SharedController.SearchImages.Count : 0;
			}
				
			public override UICollectionViewCell GetCell (UICollectionView collectionView, NSIndexPath indexPath) {
				var controller = controllerRef.Target as ImageSearchController;
				if (controller != null) {
					var cell = (ImageSearchCollectionViewCell)collectionView.DequeueReusableCell (IMAGE_SEARCH_COLLETION_VIEW_CELL_ID, indexPath);
					cell.Position = indexPath.Row;
					cell.SetAbstractSearchImage (indexPath.Row, controller.SharedController.SearchImages [indexPath.Row]);
					return cell;
				}

				return null;
			}
		}

		class ImageSearchDelegate : UICollectionViewDelegate {
			private WeakReference controllerRef;

			private ImageSearchController Controller {
				get { return controllerRef != null ? controllerRef.Target as ImageSearchController : null; }
				set { controllerRef = new WeakReference (value); }
			}

			public ImageSearchDelegate (ImageSearchController c) {
				this.Controller = c;
			}

			public override void ItemHighlighted (UICollectionView collectionView, NSIndexPath indexPath) {
				UICollectionViewCell cell = collectionView.CellForItem (indexPath);
				cell.ContentView.BackgroundColor = UIColor.LightGray;
			}

			public override void ItemUnhighlighted (UICollectionView collectionView, NSIndexPath indexPath) {
				UICollectionViewCell cell = collectionView.CellForItem (indexPath);
				cell.ContentView.BackgroundColor = UIColor.Clear;
			}

			public override bool ShouldHighlightItem (UICollectionView collectionView, NSIndexPath indexPath) {
				return true;
			}

			public override void ItemSelected (UICollectionView collectionView, NSIndexPath indexPath) {
				ImageSearchController controller = this.Controller;

				if (controller != null) {
					controller.PauseUI ();
					WeakReference weakRef = new WeakReference (controller);
					AbstractSearchImage searchImage = controller.SharedController.SearchImages [indexPath.Row];
					searchImage.GetFullImageAsBytesAsync ((byte[] imageAsBytes) => {
						ImageSearchController weakController = weakRef.Target as ImageSearchController;
						if (weakController != null) {
							UIImage image = UIImageExtension.ByteArrayToImage (imageAsBytes);
							if (image != null) {
								weakController.ImageSelectedCallback (image);
							} else {
								weakController.DisplayErrorMessage ("IMAGE_SELECTION_ERROR".t ());
							}

							weakController.ResumeUI ();

							if (weakController.IsModal ()) {
								weakController.NavigationController.DismissViewControllerAsync (true);
							} else {
								weakController.NavigationController.PopViewController (true);
							}
						}
					});
				}
			}
		}

		public class SharedImageSearchController : AbstractImageSearchController {
			private WeakReference controllerRef;

			private ImageSearchController Controller {
				get { return controllerRef != null ? controllerRef.Target as ImageSearchController : null; }
				set { controllerRef = new WeakReference (value); }
			}

			public SharedImageSearchController (ImageSearchController c, ImageSearchParty thirdParty, string seedString) 
				: base (AppDelegate.Instance.applicationModel, AppDelegate.Instance.applicationModel.account, thirdParty, seedString) {
				this.Controller = c;
			}

			public override void PauseUI () {
				ImageSearchController controller = this.Controller;
				if (controller != null) {
					controller.PauseUI ();
				}
			}

			public override void ResumeUI () {
				ImageSearchController controller = this.Controller;
				if (controller != null) {
					controller.ResumeUI ();
				}
			}

			public override void ReloadUI () {
				ImageSearchController controller = this.Controller;
				if (controller != null) {
					controller.ReloadUI ();
				}
			}

			public override void DisplayError (string errorMessage) {
				ImageSearchController controller = this.Controller;
				if (controller != null) {
					controller.DisplayErrorMessage (errorMessage);
				}
			}
		}
	}
}