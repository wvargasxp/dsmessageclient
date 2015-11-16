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
	public partial class InboxViewController : UIViewController {
		TableViewDelegate tableDelegate;
		TableViewDataSource tableDatasource;
		protected ChatList chatList;
		CommonInbox commonInbox;

		UIToolbar bottomToolbar;
		int BOTTOM_TOOLBAR_HEIGHT = 44;

		UIView lineView;
		NSObject didEnterForegroundObserver = null;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		#region hiding and showing toolbar
		bool toolbarInHidingState;
		public bool ToolbarInHidingState {
			get { return toolbarInHidingState; }
			set { toolbarInHidingState = value; }
		}

		bool showingToolbarInProgress = false;
		public bool ShowingToolBarInProgress {
			get { return showingToolbarInProgress; }
			set { showingToolbarInProgress = value; }
		}
		#endregion

		private BurgerButton BurgerButton { get; set; }

		public InboxViewController () : base ("InboxViewController", null) {
			chatList = null;
			loadChatList ();

			commonInbox = new CommonInbox (chatList, this);
		}
		protected override void Dispose(bool disposing) {
			NSNotificationCenter.DefaultCenter.RemoveObserver (this);
			commonInbox.Dispose ();
			base.Dispose (disposing);
		}

		void loadChatList() {
			var appDelegate = (AppDelegate) UIApplication.SharedApplication.Delegate;
			chatList = appDelegate.applicationModel.chatList;
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			AutomaticallyAdjustsScrollViewInsets = false; // added so that the table view doesn't add extra padding to the top

			#region navigationbar UI
			Title = "INBOX_TITLE".t ();
			EditButtonItem.Title = "EDIT_BUTTON".t ();
			UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (this.NavigationController.NavigationBar);
			UINavigationBarUtil.SetBackButtonToHaveNoText (this.NavigationItem);

			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (lineView);
			#endregion

			tableDelegate = new TableViewDelegate(this);
			tableView.Delegate = tableDelegate;

			tableDatasource = new TableViewDataSource(this);
			tableView.DataSource = tableDatasource;

			this.BurgerButton = new BurgerButton ();
			this.BurgerButton.TouchUpInside += DidTapSettingsButton;

			this.NavigationItem.SetLeftBarButtonItem (new UIBarButtonItem (this.BurgerButton), true);

			UIBarButtonItem editButton = EditButtonItem;
			editButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			NavigationItem.RightBarButtonItem = editButton;
			editButton.Clicked += (sender, e) => {
				tableView.SetEditing (!tableView.Editing, true);
				EditButtonItem.Title = tableView.Editing ? "DONE_BUTTON".t () : "EDIT_BUTTON".t ();
			};

			View.AutosizesSubviews = true;
			tableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			tableView.BackgroundColor = UIColor.Clear;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			ShowToolbar (true, false); // Always show toolbar when inbox is about to appear.
			UIApplication.SharedApplication.SetStatusBarStyle (UIStatusBarStyle.LightContent, false);

			ThemeController (this.InterfaceOrientation);

			didEnterForegroundObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)em.Constants.DID_ENTER_FOREGROUND, (NSNotification notification) => {
				if (tableView != null)
					tableView.ReloadData ();

				if (this.ToolbarInHidingState) {
					ShowToolbar (true, true);
				}
			});

			if (tableView != null)
				tableView.ReloadData ();


			AppDelegate.Instance.applicationModel.notificationList.ObtainUnreadCountAsync ((int newCount) => {
				this.commonInbox.UpdateBurgerUnreadCount (newCount);
			});
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			NSNotificationCenter.DefaultCenter.RemoveObserver (didEnterForegroundObserver);
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			CGSize screenSize = UIScreen.MainScreen.Bounds.Size;
			// Background image might not be ready yet. Load this image.
			AppDelegate.Instance.ScreenScale = UIScreen.MainScreen.Scale;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && lineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					lineView.BackgroundColor = mainColor.GetColor ();
				}
			});
			UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Inbox View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
			chatList.DidBecomeVisible ();
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();
			// We theme the controller in this call instead of ViewWillAppear and ViewDidAppear.
			// ViewWillAppear will sometimes get the wrong bounds for the navigationbar and ViewDidAppear is too late to update the UI.
			// ViewDidLayoutSubviews gets called between the two and is the best place to update the controller's looks.
			ThemeController (this.InterfaceOrientation);


			nfloat displacement_y = this.TopLayoutGuide.Length;

			lineView.Frame = new CGRect (0, displacement_y, lineView.Frame.Width, lineView.Frame.Height);
			CGRect bottomToolbarBounds = bottomToolbar == null ? CGRect.Empty : bottomToolbar.Bounds;
			tableView.Frame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height, tableView.Frame.Width, View.Frame.Height - lineView.Frame.Y - lineView.Frame.Height - bottomToolbarBounds.Height);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
		}

		#region Rotation
		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}
		#endregion

		#region updating unread count
		void UpdateBackButtonWithUnreadCount (int unreadCount) {
			UINavigationBarUtil.SetBackButtonWithUnreadCount (this.NavigationItem, unreadCount);
		}
		#endregion

		public void HandleNotificationBanner (ChatEntry chatEntry) {
			this.commonInbox.ShowNotificationBanner (chatEntry);
		}

		protected void OnComposeClicked(object sender, EventArgs e) {
			var appDelegate = (AppDelegate) UIApplication.SharedApplication.Delegate;

			ChatEntry chatEntry = ChatEntry.NewUnderConstructionChatEntry (appDelegate.applicationModel, DateTime.Now.ToEMStandardTime(appDelegate.applicationModel));
			chatList.underConstruction = chatEntry;
			var chatView = new ChatViewController (chatEntry);
			NavigationController.PushViewController (chatView, true);
		}

		UIToolbar removed;

		void ShowToolbar(bool enabled, bool animated) {
			if (this.ShowingToolBarInProgress)
				return;
				
			if (enabled) {
				if (bottomToolbar == null) {
					CGRect initialPosition = animated ? new CGRect (0, View.Frame.Size.Height, View.Frame.Size.Width, BOTTOM_TOOLBAR_HEIGHT) : new CGRect (0, View.Frame.Size.Height - BOTTOM_TOOLBAR_HEIGHT, View.Frame.Size.Width, BOTTOM_TOOLBAR_HEIGHT);
					bottomToolbar = new UIToolbar (initialPosition);
					bottomToolbar.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleWidth;
					bottomToolbar.BackgroundColor = UIColor.FromRGBA (Constants.RGB_TOOLBAR_COLOR [0], Constants.RGB_TOOLBAR_COLOR [1], Constants.RGB_TOOLBAR_COLOR [2], 255);

					var doneBtn = new UIBarButtonItem (UIBarButtonSystemItem.Compose, OnComposeClicked);
					bottomToolbar.Items = new [] {
						new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace),
						doneBtn,
						new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace)
					};
					View.Add (bottomToolbar);

					if (animated) {
						UIView.Animate (0.1, () => {
							this.ShowingToolBarInProgress = true;
							bottomToolbar.Frame = new CGRect (0, View.Frame.Size.Height - BOTTOM_TOOLBAR_HEIGHT, View.Frame.Size.Width, BOTTOM_TOOLBAR_HEIGHT);
						}, () => {
							this.ShowingToolBarInProgress = false;
						});
					}

					CGRect tableViewFrame = tableView.Frame;
					CGPoint oldOffset = tableView.ContentOffset;
					tableView.Frame = new CGRect (tableViewFrame.Location.X, tableViewFrame.Location.Y, tableViewFrame.Size.Width, tableViewFrame.Size.Height - BOTTOM_TOOLBAR_HEIGHT);
					tableView.ContentOffset = oldOffset;
				}
			}
			else {
				if (bottomToolbar != null) {
					if (animated) {
						removed = bottomToolbar;
						UIView.Animate (0.1, () => {
							this.ShowingToolBarInProgress = true;
							bottomToolbar.Frame = new CGRect (0, View.Frame.Size.Height, View.Frame.Size.Width, BOTTOM_TOOLBAR_HEIGHT);
						}, () => {
							removed.RemoveFromSuperview ();
							removed.Dispose ();
							removed = null;
							this.ShowingToolBarInProgress = false;
						});
					} else {
						bottomToolbar.RemoveFromSuperview ();
						bottomToolbar.Dispose ();
					}
						
					bottomToolbar = null;

					CGRect tableViewFrame = tableView.Frame;
					// Sometimes the offset goes back to zero, so keep the old one before setting the frame.
					CGPoint oldOffset = tableView.ContentOffset;
					tableView.Frame = new CGRect (tableViewFrame.Location.X, tableViewFrame.Location.Y, tableViewFrame.Size.Width, tableViewFrame.Size.Height + BOTTOM_TOOLBAR_HEIGHT);
					tableView.ContentOffset = oldOffset;
				}
			}
		}
			
		void DidTapSettingsButton (object sender, EventArgs e) {
			MainController mainController = (UIApplication.SharedApplication.Delegate as AppDelegate).MainController;
			mainController.Root.ShowLeftPanelAnimated (true);
		}

		private UIActivityIndicatorView TitleSpinner { get; set; }
		public void UpdateTitleProgress () {
			if (!this.commonInbox.ShowingProgressIndicator) {
				this.NavigationItem.TitleView = null;
				if (this.TitleSpinner != null) {
					this.TitleSpinner.StopAnimating ();
					this.TitleSpinner = null;
				}
			} else {
				if (this.TitleSpinner == null) {
					this.TitleSpinner = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.White);
				}

				this.NavigationItem.TitleView = this.TitleSpinner;
				this.TitleSpinner.StartAnimating ();
			}
		}

		class TableViewDelegate : UITableViewDelegate {
			InboxViewController inBoxViewController;
			CGPoint oldOffset;

			public TableViewDelegate(InboxViewController controller) {
				inBoxViewController = controller;
				oldOffset = CGPoint.Empty;
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return iOS_Constants.APP_CELL_ROW_HEIGHT;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				ChatEntry chatEntry = inBoxViewController.commonInbox.viewModel [indexPath.Row];
				var chatView = new ChatViewController (chatEntry);
				tableView.DeselectRow (indexPath, true);
				inBoxViewController.NavigationController.PushViewController (chatView, true);
			}
				
			public override void Scrolled (UIScrollView scrollView) {
				CGPoint point = inBoxViewController.tableView.ContentOffset;
				if (!inBoxViewController.ShowingToolBarInProgress) {
					bool showScrollbar = ShouldShowToolBar (scrollView);
					inBoxViewController.ShowToolbar (showScrollbar, true);
					oldOffset = point;
				}
			}

			public override UITableViewRowAction[] EditActionsForRow (UITableView tableView, NSIndexPath indexPath) {
				UITableViewRowAction deleteAction = UITableViewRowAction.Create (UITableViewRowActionStyle.Destructive, "DELETE_BUTTON".t (), (UITableViewRowAction a, NSIndexPath ip) => {
					inBoxViewController.chatList.RemoveChatEntryAtAsync (indexPath.Row, true);
				});

				ChatEntry chatEntry = inBoxViewController.chatList.entries [indexPath.Row];
				if (chatEntry.IsAdHocGroupWeCanLeave()) {
					UITableViewRowAction leaveAction = UITableViewRowAction.Create(UITableViewRowActionStyle.Normal, "LEAVE".t (), (UITableViewRowAction a, NSIndexPath ip) => {
						tableView.SetEditing(false, true);
						var alert = new UIAlertView("LEAVE_CONVERSATION".t (), 
							"LEAVE_CONVERSATION_EXPAINATION".t (),
							null,
							"CANCEL_BUTTON".t (),
							new string[] { "LEAVE".t () });
						alert.Show();
						alert.Clicked += (sender, buttonArgs) => { 
							switch (buttonArgs.ButtonIndex) {
							case 1:
								chatEntry.LeaveConversationAsync();
								break;

							default:
								break;
							}
						};
					});

					return new UITableViewRowAction[] { deleteAction, leaveAction };
				}

				return new UITableViewRowAction[] { deleteAction };
			}

			public bool ShouldShowToolBar(UIScrollView scrollView) {
				// rules for hiding tool bar
				// tableview longer than screen (table view would scroll)
				// table scrolling down

				// rules for showing toolbar
				// tableview is shorter than screen
				// tableview is near the top (anything less than 5)
				// table is scrolling up.
				CGPoint point = inBoxViewController.tableView.ContentOffset;
				int height = inBoxViewController.commonInbox.viewModel.Count * iOS_Constants.APP_CELL_ROW_HEIGHT;
				CGRect tableViewBounds = inBoxViewController.tableView.Bounds;

				bool showScrollbar;
				// We're keeping track of the toolbarhidingstate because we want to accurate determine if we should show the scrollbar based on the height of the tableview.
				// The tableview's frame's height is getting resized (subtracted) in the ShowToolbar function if we're hiding the toolbar.
				// This will clash with this function because we should be comparing the tableview's height before ShowToolbar's animation is done, but it won't work because we're setting the tableView's frame immediately (inside ShowToolbar).
				// By accounting for the tableView's preemtive (tableView's frame's height is getting set before animation finishes) height increase (the height increase to fill in the the space where the bottom toolbar would be hidden from),
				// We fix the issue where showScrollbar's value goes from false -> true -> false -> true when it should be false -> false -> false -> false.
				if (inBoxViewController.ToolbarInHidingState)
					showScrollbar = height <= tableViewBounds.Size.Height - inBoxViewController.BOTTOM_TOOLBAR_HEIGHT;
				else
					showScrollbar = height <= tableViewBounds.Size.Height;

				if (!showScrollbar) {
					showScrollbar = point.Y < 5;
					// if not showing and we aren't past the end of scrolling (which causes a rubber band rebound we want to ignore).
					if (!showScrollbar && point.Y < (inBoxViewController.tableView.ContentSize.Height - inBoxViewController.tableView.Bounds.Height))
						showScrollbar = oldOffset.Y > point.Y;
				}

				if (showScrollbar)
					inBoxViewController.ToolbarInHidingState = false;
				else
					inBoxViewController.ToolbarInHidingState = true;

				return showScrollbar;
			}
		}
			
		class TableViewDataSource : UITableViewDataSource {
			InboxViewController inBoxViewController;

			public TableViewDataSource(InboxViewController controller) {
				inBoxViewController = controller;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				return inBoxViewController.commonInbox == null ? 1 : inBoxViewController.commonInbox.viewModel.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				var cell = (InboxTableViewCell)tableView.DequeueReusableCell (InboxTableViewCell.Key) ?? InboxTableViewCell.Create ();

				ChatEntry chatEntry = inBoxViewController.commonInbox.viewModel[indexPath.Row];
				cell.chatEntry = chatEntry;
				cell.SetEvenRow (indexPath.Row % 2 == 0);

				return cell;
			}

			public override void CommitEditingStyle (UITableView tableView, UITableViewCellEditingStyle editingStyle, NSIndexPath indexPath) {
				switch (editingStyle) {
				case UITableViewCellEditingStyle.Delete:
					inBoxViewController.chatList.RemoveChatEntryAtAsync (indexPath.Row, true);
					break;

				default:
				case UITableViewCellEditingStyle.None:
					Debug.WriteLine ("CommitEditingStyle:None called");
					break;
				}
			}
			public override bool CanEditRow (UITableView tableView, NSIndexPath indexPath) {
				return true;
			}
		}

		class CommonInbox : AbstractInBoxController {
			InboxViewController inboxViewController;

			public CommonInbox (ChatList chatList, InboxViewController adapter):base(chatList) {
				inboxViewController = adapter;
			}

			public override void HandleUpdatesToChatList (IList<ModelStructureChange<ChatEntry>> repositionChatItems, IList<ModelAttributeChange<ChatEntry,object>> previewUpdates, bool animated, Action callback) {
				if (!inboxViewController.IsViewLoaded)
					callback ();
				else {
					// TODO why on earth is this here
					//chatList.ObtainUnreadCountAsync (inboxViewController.UpdateBackButtonWithUnreadCount);

					if (!animated) {
						inboxViewController.tableView.ReloadData ();
						callback ();
					}
					else {
						try {
							if ( repositionChatItems == null || repositionChatItems.Count == 0 ) {
								HandleAttributeUpdates(previewUpdates, callback);
							}
							else {
								CATransaction.Begin ();

								inboxViewController.tableView.BeginUpdates ();

								CATransaction.CompletionBlock = delegate {
									EMTask.DispatchMain (() => {
										callback ();
										StartBackgroundCorrectionAnimations ();
									});
								};

								NSIndexPath[] visibleRows = inboxViewController.tableView.IndexPathsForVisibleRows;

								foreach (ModelStructureChange<ChatEntry> ins in repositionChatItems) {
									if (ins.Change == ModelStructureChange.added )
										inboxViewController.tableView.InsertRows (new [] { NSIndexPath.FromRowSection (viewModel.IndexOf(ins.ModelObject), 0) }, UITableViewRowAnimation.Automatic);
									else if (ins.Change == ModelStructureChange.deleted)
										inboxViewController.tableView.DeleteRows (new [] { NSIndexPath.FromRowSection (ins.Index, 0) }, UITableViewRowAnimation.Automatic);
									else
										inboxViewController.tableView.MoveRow (NSIndexPath.FromRowSection (ins.Index, 0), NSIndexPath.FromRowSection (viewModel.IndexOf(ins.ModelObject), 0));
								}

								inboxViewController.tableView.EndUpdates ();

								CATransaction.Commit ();
							}

							HandleAttributeUpdates(previewUpdates, callback);
						}
						catch (Exception e) {
							Debug.WriteLine ("Exception HandleUpdatesToChatList: " + e.Message + "\n" + e.StackTrace);
							UITableView brokenTableView = inboxViewController.tableView;
							// replace TableView
							var replacementTableView = new UITableView (brokenTableView.Frame, brokenTableView.Style);
							replacementTableView.Tag = brokenTableView.Tag;
							replacementTableView.Delegate = brokenTableView.Delegate;
							replacementTableView.DataSource = brokenTableView.DataSource;
							replacementTableView.AutoresizingMask = brokenTableView.AutoresizingMask;
							replacementTableView.AutosizesSubviews = brokenTableView.AutosizesSubviews;
							replacementTableView.BackgroundColor = UIColor.Clear;
							replacementTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;

							UIView superview = brokenTableView.Superview;
							brokenTableView.RemoveFromSuperview ();
							superview.AddSubview (replacementTableView);
							inboxViewController.tableView = replacementTableView;

							inboxViewController.tableView.ReloadData ();
							inboxViewController.ThemeController (inboxViewController.InterfaceOrientation);
							callback ();
						}
					}
				}
			}

			protected void HandleAttributeUpdates(IList<ModelAttributeChange<ChatEntry,object>> previewUpdates, Action callback) {
				if (previewUpdates != null && previewUpdates.Count > 0) {
					foreach (ModelAttributeChange<ChatEntry,object> changeInstruction in previewUpdates) {

						ChatEntry chatEntry = changeInstruction.ModelObject;
						int position = viewModel.IndexOf (chatEntry);
						NSIndexPath rowToUpdate = NSIndexPath.FromRowSection (position, 0);
						NSIndexPath[] visibleRows = inboxViewController.tableView.IndexPathsForVisibleRows;

						if (ContainsRow (visibleRows, rowToUpdate)) {
							UITableViewCell cell = inboxViewController.tableView.CellAt (rowToUpdate);
							InboxTableViewCell inCell = cell as InboxTableViewCell;

							if (inCell != null) {
								if (changeInstruction.AttributeName.Equals (CHATENTRY_THUMBNAIL) && !chatEntry.IsAdHocGroupChat ())
									inCell.UpdateThumbnailImage (chatEntry.FirstContactCounterParty);

								if (changeInstruction.AttributeName.Equals (CHATENTRY_NAME))
									inCell.SetContactsLabel (chatEntry.ContactsLabel, true);

								if (changeInstruction.AttributeName.Equals (CHATENTRY_PREVIEW)) {
									inCell.SetPreviewLabel (chatEntry.preview, chatEntry.FormattedPreviewDate, true);
									inCell.SetHasUnread (chatEntry.hasUnread, true);
								}

								if (changeInstruction.AttributeName.Equals (CHATENTRY_COLOR_THEME)) {
									inCell.SetColorBand (chatEntry.IncomingColorTheme, true);
									inCell.UpdateAKAMask (chatEntry.IncomingColorTheme);
								}
							}
						}
					}
				}

				callback();
			}

			public override void GoToChatEntry (ChatEntry chatEntry) {
				if (this.inboxViewController == null) {
					return;
				}
				UIViewController[] viewControllers = inboxViewController.NavigationController.ViewControllers;
				UIViewController lastController = viewControllers [viewControllers.Length - 1];
				bool didFindChatEntry = false;
				try {
					ChatViewController chatController = (ChatViewController)lastController;
					if (chatEntry.chatEntryID == chatController.ChatEntry.chatEntryID) {
						return;
					}
					didFindChatEntry = AppDelegate.Instance.PopToChatEntry (chatEntry);
					if (didFindChatEntry) {
						return;
					}
					ChatViewController chatView = new ChatViewController (chatEntry);
					inboxViewController.NavigationController.PushViewController (chatView, true);
				} catch (InvalidCastException e) {
					didFindChatEntry = AppDelegate.Instance.PopToChatEntry (chatEntry);
					if (didFindChatEntry) {
						return;
					}
					ChatViewController chatView = new ChatViewController (chatEntry);
					inboxViewController.NavigationController.PushViewController (chatView, true);
				}
			}

			public override void DidChangeColorTheme () {
				if (inboxViewController.IsViewLoaded)
					inboxViewController.ThemeController (inboxViewController.InterfaceOrientation);
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
			protected void StartBackgroundCorrectionAnimations () {
				NSIndexPath[] visibleIndexes = inboxViewController.tableView.IndexPathsForVisibleRows;
				if (visibleIndexes.Length > 0) {
					ContinueBackgroundCorrectionAnimations (visibleIndexes, 0);
				}
			}

			protected void ContinueBackgroundCorrectionAnimations (NSIndexPath[] visibleIndexes, int index) {
				UITableViewCell cell = inboxViewController.tableView.CellAt (visibleIndexes[index]);
				InboxTableViewCell inCell = cell as InboxTableViewCell;
				if ( inCell != null )
					inCell.SetEvenRow( visibleIndexes[index].Row % 2 == 0 );
				if (++index < visibleIndexes.Length) {
					NSTimer.CreateScheduledTimer (iOS_Constants.ODD_EVEN_BACKGROUND_COLOR_CORRECTION_PAUSE, (NSTimer t) => ContinueBackgroundCorrectionAnimations (visibleIndexes, index));
				}
			}

			public override void UpdateTitleProgressIndicatorVisibility () {
				EMTask.DispatchMain (() => {
					inboxViewController.UpdateTitleProgress ();
				});
			}

			public override void UpdateBurgerUnreadCount (int unreadCount) {
				InboxViewController self = this.inboxViewController;
				self.BurgerButton.UnreadCount = unreadCount;
			}

			private static List<InboxTableViewCell> VisibleBanners = new List<InboxTableViewCell> ();
			private static object visibleBannersLock = new object ();

			public override void ShowNotificationBanner (ChatEntry entry) {
				InboxViewController self = this.inboxViewController;
				if (self == null || self.Visible) // No notifications in inbox (that would be a little redundant)
					return;
				try {
					UIViewController[] controllers = self.NavigationController.ViewControllers;
					ChatViewController chatController = (ChatViewController)controllers [controllers.Length - 1];
					if (chatController != null) {
						if (entry.chatEntryID.Equals (chatController.ChatEntry.chatEntryID)) {
							return;
						} else {
							AnimateNotificationBannerAppear (entry);
						}
					}
				} catch (InvalidCastException e) {
					AnimateNotificationBannerAppear (entry);
				}
			}

			private void AnimateNotificationBannerDisappear (InboxTableViewCell banner, float duration, bool navigating) {
				bool bannerStillVisible = true;
				lock (visibleBannersLock) {
					bannerStillVisible = VisibleBanners.Contains (banner);
				}
				nfloat screenWidth = UIScreen.MainScreen.Bounds.Size.Width;
				if (bannerStillVisible) {
					if (navigating) {
						UIApplication.SharedApplication.StatusBarHidden = false;
					}
					UIView.Animate (duration, () => {
						banner.Frame = new CGRect (0, -1 * iOS_Constants.APP_CELL_ROW_HEIGHT, screenWidth, iOS_Constants.APP_CELL_ROW_HEIGHT);
					}, () => {
						lock (visibleBannersLock) {
							if (VisibleBanners.Count <= 1) {
								UIApplication.SharedApplication.StatusBarHidden = false;
							}
							VisibleBanners.Remove (banner);
						}
					});
				}
			}

			private void AnimateNotificationBannerAppear(ChatEntry entry) {
				nfloat screenWidth = UIScreen.MainScreen.Bounds.Size.Width;
				InboxTableViewCell banner = new InboxTableViewCell ();
				banner.chatEntry = entry;
				banner.IsNotificationBanner = true;
				banner.ContentView.BackgroundColor = iOS_Constants.EVEN_COLOR;
				banner.Frame = new CGRect(0, -iOS_Constants.APP_CELL_ROW_HEIGHT, screenWidth, iOS_Constants.APP_CELL_ROW_HEIGHT);
				lock (visibleBannersLock) {
					VisibleBanners.Add (banner);
				}
				UIApplication.SharedApplication.KeyWindow.Add (banner);
				UIApplication.SharedApplication.StatusBarHidden = true;
				UITapGestureRecognizer tapHandler = new UITapGestureRecognizer (
					WeakDelegateProxy.CreateProxy<UITapGestureRecognizer> (HandleBannerTap).HandleEvent<UITapGestureRecognizer>
				);
				UISwipeGestureRecognizer swipeHandler = new UISwipeGestureRecognizer (
					WeakDelegateProxy.CreateProxy<UISwipeGestureRecognizer> (HandleBannerSwipe).HandleEvent<UISwipeGestureRecognizer>
				);
				swipeHandler.Direction = UISwipeGestureRecognizerDirection.Up;
				banner.AddGestureRecognizer (tapHandler);
				banner.AddGestureRecognizer (swipeHandler);
				NSTimer timer = NSTimer.CreateScheduledTimer (5, (NSTimer bleh) => {
					AnimateNotificationBannerDisappear (banner, 0.3f, false);
				});
				NSRunLoop.Current.AddTimer (timer, NSRunLoopMode.Common);
				UIView.Animate (0.3, () => {
					banner.Frame = new CGRect (0, 0, screenWidth, iOS_Constants.APP_CELL_ROW_HEIGHT);
				});
			}

			private void HandleBannerTap (UITapGestureRecognizer recognizer) {
				InboxViewController self = this.inboxViewController;
				if (self == null)
					return;
				InboxTableViewCell view = RemoveCurrentBannersAndReturnFocused (recognizer);
				AnimateNotificationBannerDisappear (view, 0.3f, true);
				self.commonInbox.GoToChatEntry (view.chatEntry);
			}

			private void HandleBannerSwipe (UISwipeGestureRecognizer recognizer) {
				InboxViewController self = this.inboxViewController;
				if (self == null)
					return;
				InboxTableViewCell view = RemoveCurrentBannersAndReturnFocused (recognizer);
				AnimateNotificationBannerDisappear (view, 0.1f, false);
			}

			private InboxTableViewCell RemoveCurrentBannersAndReturnFocused (UIGestureRecognizer recognizer) {
				InboxTableViewCell view = (InboxTableViewCell)recognizer.View;
				lock (visibleBannersLock) {
					for (int i = 0; i < VisibleBanners.Count; i++) {
						InboxTableViewCell cell = VisibleBanners [i];
						if (!(cell.Equals (view))) {
							cell.RemoveFromSuperview ();
							VisibleBanners.Remove (cell);
							i--;
						}
					}
				}
				return view;
			}
		}
	}
}