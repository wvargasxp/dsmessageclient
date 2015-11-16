using System;
using System.Collections.Generic;
using System.Diagnostics;
using CoreAnimation;
using CoreGraphics;
using em;
using Foundation;
using GoogleAnalytics.iOS;
using UIKit;

namespace iOS {
	public class NotificationsViewController : UIViewController {

		TableViewDelegate tableDelegate;
		TableViewDataSource tableDatasource;
		protected NotificationList NotificationList;
		readonly CommonNotification commonNotification;

		AppDelegate appDelegate;
		AccountInfo accountInfo;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		NSObject willEnterForegroundObserver = null;

		#region UI
		UIBarButtonItem editButton;
		UIView LineView;

		UIView TabButtonView;
		UIButton readButton;
		UIButton unreadButton;
		UIButton allButton;
		UIView SeperatorOne;
		UIView SeperatorTwo;
		UIView BlackLineView;

		UITableView NotificationsTableView;
		#endregion

		public NotificationsViewController () {
			appDelegate = (AppDelegate) UIApplication.SharedApplication.Delegate;
			accountInfo = appDelegate.applicationModel.account.accountInfo;

			NotificationList = appDelegate.applicationModel.notificationList;

			commonNotification = new CommonNotification (appDelegate.applicationModel, this);
		}
		
		void ThemeController (UIInterfaceOrientation orientation) {
			var mainColor = accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && LineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					LineView.BackgroundColor = mainColor.GetColor ();
				}
			});

			if (TabButtonView != null)
				TabButtonView.BackgroundColor = accountInfo.colorTheme.GetColor ();

			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		public override void LoadView () {
			base.LoadView ();

			LineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (LineView);
			View.BringSubviewToFront (LineView);

			TabButtonView = new UIView(new CGRect(0, 0, View.Frame.Width, 40));
			TabButtonView.BackgroundColor = accountInfo.colorTheme.GetColor ();
			TabButtonView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;

			readButton = new UIButton (UIButtonType.Custom);
			readButton.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			readButton.SetTitle ("READ_BUTTON".t (), UIControlState.Normal);
			readButton.Frame = new CGRect (0, 0, View.Frame.Width / 3, 40);
			readButton.Font = FontHelper.DefaultFontForButtons (FontHelper.DefaultFontSizeForLabels);
			readButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object, EventArgs> (HandleReadButtonTouchUpInside).HandleEvent<object, EventArgs>;
			TabButtonView.Add (readButton);

			SeperatorOne = new UINavigationBarLine (new CGRect (0, 0, 1, 24));
			SeperatorOne.BackgroundColor = iOS_Constants.WHITE_COLOR;
			TabButtonView.Add (SeperatorOne);
			TabButtonView.BringSubviewToFront (SeperatorOne);

			unreadButton = new UIButton (UIButtonType.Custom);
			unreadButton.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			unreadButton.SetTitle ("UNREAD_BUTTON".t (), UIControlState.Normal);
			unreadButton.Frame = new CGRect (0, 0, View.Frame.Width / 3, 40);
			unreadButton.Font = FontHelper.DefaultFontForButtons (FontHelper.DefaultFontSizeForLabels);
			unreadButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object, EventArgs> (HandleUnreadButtonTouchUpInside).HandleEvent<object, EventArgs>;
			TabButtonView.Add (unreadButton);

			SeperatorTwo = new UINavigationBarLine (new CGRect (0, 0, 1, 24));
			SeperatorTwo.BackgroundColor = iOS_Constants.WHITE_COLOR;
			TabButtonView.Add (SeperatorTwo);
			TabButtonView.BringSubviewToFront (SeperatorTwo);

			allButton = new UIButton (UIButtonType.Custom);
			allButton.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			allButton.SetTitle ("ALL_BUTTON".t (), UIControlState.Normal);
			allButton.Frame = new CGRect (0, 0, View.Frame.Width / 3, 40);
			allButton.Font = FontHelper.DefaultBoldFontForButtons (FontHelper.DefaultFontSizeForLabels);
			allButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object, EventArgs> (HandleAllButtonTouchUpInside).HandleEvent<object, EventArgs>;
			allButton.Selected = true;
			TabButtonView.Add (allButton);

			BlackLineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			BlackLineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			BlackLineView.BackgroundColor = iOS_Constants.BLACK_COLOR;
			TabButtonView.Add (BlackLineView);
			TabButtonView.BringSubviewToFront (BlackLineView);

			View.Add (TabButtonView);

			NotificationsTableView = new UITableView ();
			NotificationsTableView.AllowsMultipleSelectionDuringEditing = false;
			NotificationsTableView.AllowsSelectionDuringEditing = false;
			NotificationsTableView.BackgroundColor = UIColor.Clear;
			NotificationsTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			View.Add (NotificationsTableView);
		}

		private void HandleReadButtonTouchUpInside (object sender, EventArgs e) {
			if(!readButton.Selected) {
				//load read notifications
				readButton.Selected = true;
				readButton.Font = FontHelper.DefaultBoldFontForButtons(FontHelper.DefaultFontSizeForLabels);
			
				editButton.Title = "EDIT_BUTTON".t ();
				NotificationsTableView.Editing = false;
			
				unreadButton.Selected = false;
				unreadButton.Font = FontHelper.DefaultFontForButtons (FontHelper.DefaultFontSizeForLabels);
				allButton.Selected = false;
				allButton.Font = FontHelper.DefaultFontForButtons (FontHelper.DefaultFontSizeForLabels);

				NotificationsTableView.ReloadData ();
			}
		}

		private void HandleAllButtonTouchUpInside (object sender, EventArgs e) {
			if(!allButton.Selected) {
				//load all notifications
				allButton.Selected = true;
				allButton.Font = FontHelper.DefaultBoldFontForButtons (FontHelper.DefaultFontSizeForLabels);
				
				editButton.Title = "EDIT_BUTTON".t ();
				NotificationsTableView.Editing = false;

				readButton.Selected = false;
				readButton.Font = FontHelper.DefaultFontForButtons (FontHelper.DefaultFontSizeForLabels);
				unreadButton.Selected = false;
				unreadButton.Font = FontHelper.DefaultFontForButtons (FontHelper.DefaultFontSizeForLabels);

				NotificationsTableView.ReloadData ();
			}
		}

		private void HandleUnreadButtonTouchUpInside (object sender, EventArgs e) {
			if(!unreadButton.Selected) {
				//load unread notifications
				unreadButton.Selected = true;
				unreadButton.Font = FontHelper.DefaultBoldFontForButtons(FontHelper.DefaultFontSizeForLabels);
			
				editButton.Title = "EDIT_BUTTON".t ();
				NotificationsTableView.Editing = false;
			
				readButton.Selected = false;
				readButton.Font = FontHelper.DefaultFontForButtons (FontHelper.DefaultFontSizeForLabels);
				allButton.Selected = false;
				allButton.Font = FontHelper.DefaultFontForButtons (FontHelper.DefaultFontSizeForLabels);
			
				NotificationsTableView.ReloadData ();
			}
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);
			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			Title = "NOTIFICATIONS_TITLE".t ();

			tableDelegate = new TableViewDelegate(this);
			NotificationsTableView.Delegate = tableDelegate;

			tableDatasource = new TableViewDataSource(this);
			NotificationsTableView.DataSource = tableDatasource;

			var leftButton = new UIBarButtonItem ("CLOSE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object, EventArgs>((sender, args) => {
				DismissViewController (true, null);
			}).HandleEvent<object, EventArgs>);
			leftButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			NavigationItem.SetLeftBarButtonItem(leftButton, true);

			editButton = EditButtonItem;
			editButton.Title = "EDIT_BUTTON".t ();
			editButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			editButton.Clicked += WeakDelegateProxy.CreateProxy<object, EventArgs> (HandleEditButtonClicked).HandleEvent<object, EventArgs>;
			NavigationItem.RightBarButtonItem = editButton;

			View.AutosizesSubviews = true;
			NotificationsTableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		}

		private void HandleEditButtonClicked (object sender, EventArgs e) {
			NotificationsTableView.SetEditing( !NotificationsTableView.Editing, true);
			editButton.Title = NotificationsTableView.Editing == true ? "DONE_BUTTON".t () : "EDIT_BUTTON".t ();
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			NotificationsTableView.ReloadData ();

			willEnterForegroundObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)Constants.DID_ENTER_FOREGROUND, notification => {
				if (NotificationsTableView != null)
					NotificationsTableView.ReloadData ();
			});
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Notification View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, LineView.Frame.Width, LineView.Frame.Height);

			TabButtonView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height, View.Frame.Width, TabButtonView.Frame.Height);
			readButton.Frame = new CGRect (0, 0, (View.Frame.Width / 3) - 1, TabButtonView.Frame.Height);
			SeperatorOne.Frame = new CGRect (readButton.Frame.Width, 8, 1, 24);
			unreadButton.Frame = new CGRect (readButton.Frame.Width + SeperatorOne.Frame.Width, 0, (View.Frame.Width / 3) - 1, TabButtonView.Frame.Height);
			SeperatorTwo.Frame = new CGRect (readButton.Frame.Width + SeperatorOne.Frame.Width + unreadButton.Frame.Width, 8, 1, 24);
			allButton.Frame = new CGRect (readButton.Frame.Width + SeperatorOne.Frame.Width + unreadButton.Frame.Width + SeperatorTwo.Frame.Width, 0, View.Frame.Width / 3, TabButtonView.Frame.Height);
			BlackLineView.Frame = new CGRect (0, TabButtonView.Frame.Height, View.Frame.Width, BlackLineView.Frame.Height);

			NotificationsTableView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height + TabButtonView.Frame.Height + BlackLineView.Frame.Height, View.Frame.Width, View.Frame.Height - (displacement_y + LineView.Frame.Height + TabButtonView.Frame.Height));
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			NSNotificationCenter.DefaultCenter.RemoveObserver (willEnterForegroundObserver);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose (bool disposing) {
			NSNotificationCenter.DefaultCenter.RemoveObserver (this);
			commonNotification.Dispose ();
			base.Dispose (disposing);
		}

		class TableViewDelegate : UITableViewDelegate {
			private WeakReference controllerRef;

			public NotificationsViewController notificationsViewController {
				get {
					return (controllerRef == null ? null : controllerRef.Target as NotificationsViewController);
				}
				set {
					controllerRef = new WeakReference (value);
				}
			}

			public TableViewDelegate(NotificationsViewController controller) {
				notificationsViewController = controller;
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				if(notificationsViewController.unreadButton.Selected) {
					NotificationEntry notificationEntry = notificationsViewController.NotificationList.Entries [indexPath.Row];
					if (notificationEntry != null && notificationEntry.Read)
						return 0; //don't show read notifications
				} else if(notificationsViewController.readButton.Selected) {
					NotificationEntry notificationEntry = notificationsViewController.NotificationList.Entries [indexPath.Row];
					if (notificationEntry != null && !notificationEntry.Read)
						return 0; //don't show unread notifications
				}

				return iOS_Constants.APP_CELL_ROW_HEIGHT;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				NotificationEntry notificationEntry = notificationsViewController.NotificationList.Entries [indexPath.Row];

				notificationsViewController.NotificationList.MarkNotificationEntryReadAsync (notificationEntry);

				var webView = new NotificationsWebViewController (notificationEntry);
				notificationsViewController.NavigationController.PushViewController (webView, true);

				tableView.DeselectRow (indexPath, true);

				if (notificationsViewController.unreadButton.Selected) {
					notificationsViewController.NotificationsTableView.ReloadData ();
				}
			}
		}

		class TableViewDataSource : UITableViewDataSource {
			private WeakReference controllerRef;

			public NotificationsViewController notificationsViewController {
				get {
					return (controllerRef == null ? null : controllerRef.Target as NotificationsViewController);
				}
				set {
					controllerRef = new WeakReference (value);
				}
			}

			public TableViewDataSource(NotificationsViewController controller) {
				notificationsViewController = controller;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				NotificationsViewController controller = this.notificationsViewController;
				if (controller == null)
					return ((nint)0);
				return controller.NotificationList == null ? 0 : controller.NotificationList.Entries.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				NotificationsViewController controller = this.notificationsViewController;
				if (controller == null)
					return null;
				var cell = (NotificationTableViewCell)tableView.DequeueReusableCell (NotificationTableViewCell.Key) ?? NotificationTableViewCell.Create ();

				NotificationEntry entry = controller.NotificationList.Entries[indexPath.Row];

				cell.notificationEntry = entry;
				cell.SetEvenRow (indexPath.Row % 2 == 0);

				if (controller.unreadButton.Selected && entry.Read)
					cell.Hidden = true;
				else if (controller.readButton.Selected && !entry.Read)
					cell.Hidden = true;

				return cell;
			}

			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath) {
				NotificationsViewController controller = this.notificationsViewController;
				if (controller == null)
					return;
				switch (editingStyle) {
				case UITableViewCellEditingStyle.Delete:
					controller.NotificationList.RemoveNotificationEntryAtAsync (indexPath.Row);
					break;

				default:
					break;
				}
			}

			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath) {
				return true;
			}
		}

		class CommonNotification : AbstractNotificationController {
			private WeakReference controllerRef;

			public NotificationsViewController notificationsViewController {
				get {
					return (controllerRef == null ? null : controllerRef.Target as NotificationsViewController);
				}
				set {
					controllerRef = new WeakReference (value);
				}
			}

			public CommonNotification(ApplicationModel appModel, NotificationsViewController adapter) : base(appModel) {
				notificationsViewController = adapter;
			}

			public override void HandleUpdatesToNotificationList(IList<MoveOrInsertInstruction<NotificationEntry>> repositionNotificationItems, IList<ChangeInstruction<NotificationEntry>> previewUpdates, bool animated, Action<bool> callback) {
				NotificationsViewController controller = this.notificationsViewController;
				if (controller == null)
					return;
				if (controller.IsViewLoaded) {
					if (!animated) {
						controller.NotificationsTableView.ReloadData ();
						callback (animated);
					} else {
						try {
							CATransaction.Begin ();

							controller.NotificationsTableView.BeginUpdates ();

							CATransaction.CompletionBlock = delegate {
								EMTask.DispatchMain (() => {
									callback (animated);
									StartBackgroundCorrectionAnimations ();
								});
							};

							NSIndexPath[] visibleRows = controller.NotificationsTableView.IndexPathsForVisibleRows;

							foreach (MoveOrInsertInstruction<NotificationEntry> ins in repositionNotificationItems) {
								if (ins.FromPosition == -1)
									controller.NotificationsTableView.InsertRows (new [] { NSIndexPath.FromRowSection (ins.ToPosition, 0) }, UITableViewRowAnimation.Automatic);
								else if (ins.ToPosition == -1)
									controller.NotificationsTableView.DeleteRows (new [] { NSIndexPath.FromRowSection (ins.FromPosition, 0) }, UITableViewRowAnimation.Automatic);
								else
									controller.NotificationsTableView.MoveRow (NSIndexPath.FromRowSection (ins.FromPosition, 0), NSIndexPath.FromRowSection (ins.ToPosition, 0));
							}

							notificationsViewController.NotificationsTableView.EndUpdates ();

							CATransaction.Commit ();

							if (previewUpdates.Count > 0) {
								foreach (ChangeInstruction<NotificationEntry> changeInstruction in previewUpdates) {
									NotificationEntry notificationEntry = changeInstruction.Entry;
									int position = this.notificationList.Entries.IndexOf (notificationEntry);

									NSIndexPath rowToUpdate = NSIndexPath.FromRowSection (position, 0);
									if (ContainsRow (visibleRows, rowToUpdate)) {
										UITableViewCell cell = controller.NotificationsTableView.CellAt (rowToUpdate);
										NotificationTableViewCell nCell = cell as NotificationTableViewCell;
										if ( nCell != null ) {
											if ( changeInstruction.PhotoChanged ) {
												nCell.UpdateThumbnailImage (notificationEntry.counterparty);
											}

											if (changeInstruction.ColorThemeChanged) {
												nCell.SetColorBand (notificationEntry.counterparty.colorTheme, animated);
											}
										}
									}
								}
							}
						} catch (Exception e) {
							Debug.WriteLine (e.Message);

							var brokenTableView = controller.NotificationsTableView;
							// replace TableView
							var replacementTableView = new UITableView (brokenTableView.Frame, brokenTableView.Style);
							replacementTableView.Tag = brokenTableView.Tag;
							replacementTableView.Delegate = brokenTableView.Delegate;
							replacementTableView.DataSource = brokenTableView.DataSource;
							replacementTableView.AutoresizingMask = brokenTableView.AutoresizingMask;
							replacementTableView.AutosizesSubviews = brokenTableView.AutosizesSubviews;

							var superview = brokenTableView.Superview;
							brokenTableView.RemoveFromSuperview ();
							superview.AddSubview (replacementTableView);
							controller.NotificationsTableView = replacementTableView;

							controller.NotificationsTableView.ReloadData ();

							ResumeUpdates (false);
						}
					}
				}
			}

			public override void DidChangeColorTheme () {
				NotificationsViewController controller = this.notificationsViewController;
				if (controller == null)
					return;
				if (controller.IsViewLoaded) {
					controller.ThemeController (notificationsViewController.InterfaceOrientation);
					controller.View.SetNeedsDisplay ();
				}
			}

			protected bool ContainsRow(NSIndexPath[] visibleRows, NSIndexPath row) {
				foreach (NSIndexPath visibleRow in visibleRows) {
					if (visibleRow.Equals (row))
						return true;
				}
				return false;
			}

			// After moving rows around the odd/even backgrounds can be mismatched
			// we run down through the visible rows and correct the backgrounds
			protected void StartBackgroundCorrectionAnimations() {
				NotificationsViewController controller = this.notificationsViewController;
				if (controller == null)
					return;
				NSIndexPath[] visibleIndexes = controller.NotificationsTableView.IndexPathsForVisibleRows;
				if(visibleIndexes.Length > 0)
					ContinueBackgroundCorrectionAnimations(visibleIndexes, 0);
			}

			protected void ContinueBackgroundCorrectionAnimations(NSIndexPath[] visibleIndexes, int index) {
				NotificationsViewController controller = this.notificationsViewController;
				if (controller == null)
					return;
				UITableViewCell cell = controller.NotificationsTableView.CellAt (visibleIndexes [index]);
				var inCell = cell as NotificationTableViewCell;
				if (inCell != null)
					inCell.SetEvenRow (visibleIndexes [index].Row % 2 == 0);
				if (++index < visibleIndexes.Length)
					NSTimer.CreateScheduledTimer (0.2, (NSTimer t) => ContinueBackgroundCorrectionAnimations (visibleIndexes, index));
			}
		}
	}
}