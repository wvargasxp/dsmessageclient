using System;
using em;
using UIKit;
using EMXamarin;
using CoreGraphics;
using Foundation;
using GoogleAnalytics.iOS;

namespace iOS {
	public class AggregateContactSelectionViewController : UIViewController {

		readonly ApplicationModel appModel;

		TableViewDelegate tableDelegate;
		TableViewDataSource tableDataSource;

		Action<Contact> contactSelectionCallback;
		protected Action<Contact> ContactSelectionCallback {
			get { return contactSelectionCallback; }
		}

		AggregateContact aggregateContact;
		protected AggregateContact AggregateContact {
			get { return aggregateContact; }
		}

		#region UI
		public UIView LineView;
		public UIView BackgroundView;
		public UIImage Thumbnail;
		public UILabel NameLabel;
		public UIButton ThumbnailBackground;
		public UIView BlackLineView;
		public UITableView TableView;
		#endregion

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		public AggregateContactSelectionViewController (AggregateContact contact, Action<Contact> callback) {
			contactSelectionCallback = callback;
			aggregateContact = contact;
			appModel = ((AppDelegate)UIApplication.SharedApplication.Delegate).applicationModel;
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			BackgroundColor mainColor = appModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && LineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					LineView.BackgroundColor = mainColor.GetColor ();
				}
			});


			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);

			BlackLineView.BackgroundColor = mainColor.GetColor ();

			UpdateThumbnail ();
		}

		public override void LoadView () {
			base.LoadView ();

			LineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (LineView);

			BackgroundView = new UIView(new CGRect (0, 0, View.Frame.Width, View.Frame.Height));
			BackgroundView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			BackgroundView.BackgroundColor = iOS_Constants.WHITE_COLOR;

			ThumbnailBackground = new UIButton (new CGRect (0, 20, 100, 100));
			ThumbnailBackground.ClipsToBounds = true;
			ThumbnailBackground.Layer.CornerRadius = 50f;
			UpdateThumbnail ();
			BackgroundView.Add (ThumbnailBackground);

			NameLabel = new UILabel (new CGRect (0, 135, 200, 25));
			NameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			NameLabel.Font = FontHelper.DefaultFontForTextFields ();
			NameLabel.ClipsToBounds = true;
			NameLabel.TextColor = iOS_Constants.BLACK_COLOR;
			NameLabel.TextAlignment = UITextAlignment.Center;
			NameLabel.Text = AggregateContact.DisplayName;
			BackgroundView.Add (NameLabel);

			BlackLineView = new UINavigationBarLine (new CGRect (0, 160, View.Frame.Width, 1));
			BlackLineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			BackgroundView.Add (BlackLineView);

			View.Add (BackgroundView);

			TableView = new UITableView ();
			TableView.BackgroundColor = UIColor.Clear;
			TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			View.Add (TableView);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);

			Title = AggregateContact.DisplayName;

			tableDelegate = new TableViewDelegate (this);
			tableDataSource = new TableViewDataSource (this);

			TableView.Delegate = tableDelegate;
			TableView.DataSource = tableDataSource;

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			View.AutosizesSubviews = true;

			TableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			TableView.ReloadData ();
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Contact Selection View");
			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, LineView.Frame.Width, LineView.Frame.Height);

			ThumbnailBackground.Frame = new CGRect ((View.Frame.Width / 2) - (100 / 2), 25, 100, 100);
			NameLabel.Frame = new CGRect ((View.Frame.Width - 200)/2, 135, 200, 25);
			BlackLineView.Frame = new CGRect (0, 160 + UI_CONSTANTS.SMALL_MARGIN, BlackLineView.Frame.Width, BlackLineView.Frame.Height);
			BackgroundView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height, View.Frame.Width, 171);

			TableView.Frame = new CGRect (0, BackgroundView.Frame.Y + BackgroundView.Frame.Height, View.Frame.Width, View.Frame.Height - (BackgroundView.Frame.Y + BackgroundView.Frame.Height));

			if (this.Spinner != null)
				this.Spinner.Frame = new CGRect (this.View.Frame.Width / 2 - this.Spinner.Frame.Width / 2, displacement_y + 35 /*25 is the thumbnail's y from the background view + additional margin*/, this.Spinner.Frame.Width, this.Spinner.Frame.Height);
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
			base.Dispose (disposing);
		}

		public void UpdateThumbnail () {
			AggregateContact.ContactForDisplay.colorTheme.GetBlankPhotoAccountResource ((UIImage image) => {
				if (AggregateContact != null && AggregateContact.ContactForDisplay != null) {
					ImageSetter.SetAccountImage (AggregateContact.ContactForDisplay, image, (UIImage loadedImage) => {
						if (loadedImage != null)
							ThumbnailBackground.SetBackgroundImage (loadedImage, UIControlState.Normal);
					});
				}
			});

			UpdateThumbnailBorder ();
			UpdateProgressIndicatorVisibility (ImageSetter.ShouldShowProgressIndicator(AggregateContact.ContactForDisplay));
		}

		public void UpdateThumbnailBorder () {
			if (ImageSetter.ShouldShowAccountThumbnailFrame (AggregateContact.ContactForDisplay)) {
				ThumbnailBackground.Layer.BorderWidth = 3f;
				ThumbnailBackground.Layer.BorderColor = appModel.account.accountInfo.colorTheme.GetColor ().CGColor;
			} else {
				ThumbnailBackground.Layer.BorderWidth = 0f;
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
				this.ThumbnailBackground.Alpha = 0;

			} else {
				if (this.Spinner != null) {
					this.Spinner.StopAnimating ();
					this.Spinner.RemoveFromSuperview ();
					this.Spinner = null;
				}

				this.ThumbnailBackground.Alpha = 1;
			}
		}
		#endregion

		class TableViewDataSource : UITableViewDataSource {
			readonly WeakReference controllerRef;

			public TableViewDataSource (AggregateContactSelectionViewController g) {
				controllerRef = new WeakReference (g);
			}

			public override nint NumberOfSections (UITableView tableView) {
				return 1;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				var controller = (AggregateContactSelectionViewController)controllerRef.Target;
				return controller != null ? controller.AggregateContact.Contacts.Count : 0;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				var cell = (AggregateContactTableViewCell)tableView.DequeueReusableCell (AggregateContactTableViewCell.Key) ?? AggregateContactTableViewCell.Create ();

				var controller = (AggregateContactSelectionViewController)controllerRef.Target;
				if (controller != null) {
					Contact contact = controller.AggregateContact.Contacts [indexPath.Row];
					cell.AggContact = contact;
					cell.SetEvenRow (indexPath.Row % 2 == 0);
				}

				return cell;
			}
		}

		class TableViewDelegate : UITableViewDelegate {
			readonly WeakReference controllerRef;

			public TableViewDelegate (AggregateContactSelectionViewController c) {
				controllerRef = new WeakReference (c);
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return iOS_Constants.APP_CELL_ROW_HEIGHT;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				var controller = (AggregateContactSelectionViewController)controllerRef.Target;
				if (controller != null) {
					Contact contact = controller.AggregateContact.Contacts [indexPath.Row];

					controller.NavigationController.PopViewController (true);
					controller.ContactSelectionCallback (contact);
				}
			}
		}
	}
}