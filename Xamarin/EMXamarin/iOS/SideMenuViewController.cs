using System;
using System.Collections.Generic;
using CoreGraphics;
using em;
using Foundation;
using String_UIKit_Extension;
using UIKit;

namespace iOS {
	public class SideMenuViewController : UIViewController {

		readonly int NOTIFICATION_CELL_INDEX_PATH = 2;


		List<string> names;
		static bool UserInterfaceIdiomIsPhone {
			get { return UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone; }
		}

		public UINavigationController navigationController;
		UIImageView appIcon;
		UITableView tableView;
		UIView statusbarView;
		UIView lineView;
		UILabel copyrightLabel;

		public SideMenuViewController (UINavigationController navigation) {
			navigationController = navigation;
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			string[] sideMenuList = appDelegate.applicationModel.GetSideMenuList ();
			names = new List<string> (sideMenuList);
		}

		public SideMenuViewController () {}

		private void HandleNotificationNotificationEntryCountChanged (em.Notification notif) {
			// TODO It's more efficient to use the newCount here and update the cell instead of doing a full reload on one cell.
			int newCount = Convert.ToInt32 (notif.Extra);

			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				SideMenuViewController self = thisRef.Target as SideMenuViewController;
				if (self == null || !self.IsViewLoaded || self.tableView == null) return;
				NSIndexPath[] rowsToReload = {
					NSIndexPath.FromRowSection (NOTIFICATION_CELL_INDEX_PATH, 0) // points to the notification cell
				};
				tableView.ReloadRows(rowsToReload, UITableViewRowAnimation.Fade);
			});
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			View.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			appIcon = new UIImageView (new CGRect (0, 0, 45, 45));
			appIcon.Image = UIImage.FromFile ("Icon.png");
			appIcon.Layer.CornerRadius = 7;
			appIcon.Layer.MasksToBounds = true;
			View.Add (appIcon);

			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			lineView.BackgroundColor = iOS_Constants.BLACK_COLOR;
			View.Add (lineView);

			#region table view
			tableView = new UITableView (new CGRect (0, 0, View.Frame.Width, View.Frame.Height));
			tableView.DataSource = new TableViewDataSource (this);
			tableView.Delegate = new TableViewDelegate (this);
			tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			tableView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin;
			tableView.BackgroundColor = UIColor.Clear;
			View.Add (tableView);
			View.SendSubviewToBack (tableView);
			#endregion

			copyrightLabel = new UILabel (new CGRect (0, 0, iOS_Constants.LEFT_DRAWER_WIDTH, View.Frame.Height));
			copyrightLabel.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			copyrightLabel.Font = FontHelper.DefaultFontForLabels (10f);
			copyrightLabel.Text = "COPYRIGHT".t ();
			copyrightLabel.TextColor = iOS_Constants.WHITE_COLOR;
			copyrightLabel.TextAlignment = UITextAlignment.Center;
			copyrightLabel.AdjustsFontSizeToFitWidth = false;
			copyrightLabel.LineBreakMode = UILineBreakMode.WordWrap;
			copyrightLabel.Lines = 0;
			View.Add (copyrightLabel);

			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, em.Constants.NotificationEntryDao_UnreadCountChanged, HandleNotificationNotificationEntryCountChanged);
		}

		const int STATUS_BAR_HEIGHT = 20;

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();
			nfloat displacement_y = this.TopLayoutGuide.Length + navigationController.NavigationBar.Frame.Height + STATUS_BAR_HEIGHT;
			appIcon.Frame = new CGRect (UI_CONSTANTS.EXTRA_MARGIN, displacement_y / 2 - appIcon.Frame.Height / 2 + UI_CONSTANTS.TINY_MARGIN, appIcon.Frame.Width, appIcon.Frame.Height);
			lineView.Frame = new CGRect (0, appIcon.Frame.Y + appIcon.Frame.Height + UI_CONSTANTS.SMALL_MARGIN, lineView.Frame.Width, lineView.Frame.Height);

			CGSize sizeCRL = copyrightLabel.Text.SizeOfTextWithFontAndLineBreakMode (copyrightLabel.Font, new CGSize (iOS_Constants.LEFT_DRAWER_WIDTH - 16, View.Frame.Height), UILineBreakMode.WordWrap);
			sizeCRL = new CGSize ((float)((int)(sizeCRL.Width + 1.5)), (float)((int)(sizeCRL.Height + 1.5)));
			copyrightLabel.Frame = new CGRect (new CGPoint(8, View.Frame.Height - sizeCRL.Height - 8), sizeCRL);

			tableView.Frame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height, tableView.Frame.Width, View.Frame.Height - (lineView.Frame.Y + lineView.Frame.Height + copyrightLabel.Frame.Height + 15));
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			BackgroundColor.Gray.GetBackgroundResourceForOrientation (InterfaceOrientation, (UIImage image) => {
				if (View != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
				}
			});
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			View.SetNeedsLayout ();
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
			em.NotificationCenter.DefaultCenter.RemoveObserver (this);
		}

		class TableViewDelegate : UITableViewDelegate {
			SideMenuViewController sideMenuViewController;
			readonly UINavigationController navigationController;
			readonly AppDelegate appDelegate;
			const int ROW_HEIGHT = 55;

			public TableViewDelegate(SideMenuViewController controller) {
				sideMenuViewController = controller;
				navigationController = sideMenuViewController.navigationController;
				appDelegate = (UIApplication.SharedApplication.Delegate as AppDelegate);
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return ROW_HEIGHT;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				tableView.DeselectRow (indexPath, false);
				var item = (SideMenuItems)indexPath.Row;
				switch (item) {
				case SideMenuItems.Account: {
						// Get a reference to the main controller and have it toggle the side menu away from the screen.
						// While the side menu is being animated away, the navigation controller will push the next controller without animation.
						// This results in the pushed controller piggybacking off the toggle's animation.
						appDelegate.MainController.Root.ShowCenterPanelAnimated (true);
						navigationController.PresentViewController(new UINavigationController(new AccountViewController(false)), true, null);
						break;
					}
				case SideMenuItems.Alias: {
						// Get a reference to the main controller and have it toggle the side menu away from the screen.
						// While the side menu is being animated away, the navigation controller will push the next controller without animation.
						// This results in the pushed controller piggybacking off the toggle's animation.
						appDelegate.MainController.Root.ShowCenterPanelAnimated (true);
						navigationController.PresentViewController(new EMNavigationController(new AliasViewController()), true, null);
						break;
					}
				case SideMenuItems.Notifications: {
						// Get a reference to the main controller and have it toggle the side menu away from the screen.
						// While the side menu is being animated away, the navigation controller will push the next controller without animation.
						// This results in the pushed controller piggybacking off the toggle's animation.
						appDelegate.MainController.Root.ShowCenterPanelAnimated (true);
						navigationController.PresentViewController(new UINavigationController(new NotificationsViewController()), true, null);
						break;
					}
				case SideMenuItems.Groups: {
						// Get a reference to the main controller and have it toggle the side menu away from the screen.
						// While the side menu is being animated away, the navigation controller will push the next controller without animation.
						// This results in the pushed controller piggybacking off the toggle's animation.
						appDelegate.MainController.Root.ShowCenterPanelAnimated (true);
						navigationController.PresentViewController(new EMNavigationController(new GroupsViewController()), true, null);
						break;
					}
				case SideMenuItems.Invite: {
						appDelegate.MainController.Root.ShowCenterPanelAnimated (true);
						AddressBookArgs args = AddressBookArgs.From (excludeGroups: true, exludeTemp: true, excludePreferred: true, entry: null);
						navigationController.PresentViewController (new UINavigationController (new InviteFriendsViewController (appDelegate.applicationModel, args)), true, null);
						break;
					}
				/*
				case SideMenuItems.Search: {
						// Get a reference to the main controller and have it toggle the side menu away from the screen.
						// While the side menu is being animated away, the navigation controller will push the next controller without animation.
						// This results in the pushed controller piggybacking off the toggle's animation.
						appDelegate.MainController.ShowCenterPanelAnimated (true);
						navigationController.PresentViewController(new UINavigationController(new SearchViewController()), true, null);
						break;
					}
				*/
				case SideMenuItems.Help: {
						// Get a reference to the main controller and have it toggle the side menu away from the screen.
						// While the side menu is being animated away, the navigation controller will push the next controller without animation.
						// This results in the pushed controller piggybacking off the toggle's animation.
						appDelegate.MainController.Root.ShowCenterPanelAnimated (true);
						navigationController.PresentViewController(new UINavigationController(new HelpViewController()), true, null);
						break;
					}
				case SideMenuItems.Settings:
					{
						appDelegate.MainController.Root.ShowCenterPanelAnimated (true);
						navigationController.PresentViewController (new UINavigationController (new SettingsViewController ()), true, null);
						break;
					}
				case SideMenuItems.About: {
						// Get a reference to the main controller and have it toggle the side menu away from the screen.
						// While the side menu is being animated away, the navigation controller will push the next controller without animation.
						// This results in the pushed controller piggybacking off the toggle's animation.
						appDelegate.MainController.Root.ShowCenterPanelAnimated (true);
						navigationController.PresentViewController(new UINavigationController(new AboutViewController()), true, null);
						break;
					}
				}
			}
		}

		class TableViewDataSource : UITableViewDataSource {
			SideMenuViewController sideMenuViewController;
			int rowsInTableview;

			public TableViewDataSource(SideMenuViewController controller) {
				sideMenuViewController = controller;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				rowsInTableview = sideMenuViewController.names.Count;
				return rowsInTableview;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				var cell = (SideMenuTableViewCell)tableView.DequeueReusableCell (SideMenuTableViewCell.Key) ?? SideMenuTableViewCell.Create ();
				int row = indexPath.Row;
				var sideMenuItem = (SideMenuItems)row;

				switch (sideMenuItem) {
				case SideMenuItems.Account: {
						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconAccount.png");
						cell.TextOnCell.Text = "MY_ACCOUNT_TITLE".t ();
						cell.HideAccessories ();
						break;
					}
				case SideMenuItems.Alias: {
						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconAlias.png");
						cell.TextOnCell.Text = "ALIAS_TITLE".t ();
						cell.HideAccessories ();
						break;
					}
				case SideMenuItems.Notifications: {
						var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;

						appDelegate.applicationModel.notificationList.ObtainUnreadCountAsync ((int count) => {
							cell.AccessoryLabel.Text = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (count), NSNumberFormatterStyle.Decimal);
						});

						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconNotify.png");
						cell.TextOnCell.Text = "NOTIFICATIONS_TITLE".t ();
						cell.ShowAccessories ();
						cell.AccessoryIcon.Image = UIImage.FromFile ("sidemenu/iconNotifyCounter.png");
						break;
					}
				case SideMenuItems.Groups: {
						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconGroup.png");
						cell.TextOnCell.Text = "GROUPS_TITLE".t ();
						cell.HideAccessories ();
						break;
					}
				case SideMenuItems.Invite: {
						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconInviteFriend.png");
						cell.TextOnCell.Text = "INVITE_FRIENDS_TITLE".t ();
						cell.HideAccessories ();
						break;
					}
				/*
				case SideMenuItems.Search: {
						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconFind.png");
						cell.TextOnCell.Text = "FIND_FRIENDS_TITLE".t ();
						cell.HideAccessories ();
						break;
					}
				*/
				case SideMenuItems.Help: {
						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconHelp.png");
						cell.TextOnCell.Text = "HELP_TITLE".t ();
						cell.HideAccessories ();
						break;
					}
				case SideMenuItems.Settings: 
					{
						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconSettings.png");
						cell.TextOnCell.Text = "SETTINGS_TITLE".t ();
						cell.HideAccessories ();
						break;
					}
				case SideMenuItems.About: {
						cell.IconView.Image = UIImage.FromFile ("sidemenu/iconInfo.png");
						cell.TextOnCell.Text = "ABOUT_TITLE".t ();
						cell.HideAccessories ();
						break;
					}
				}

				if (row != rowsInTableview - 1)
					cell.HideBottomLine ();

				cell.TextOnCell.Font = FontHelper.DefaultFontWithSize (15);
				cell.TextOnCell.TextColor = FontHelper.DefaultTextColor ();
				cell.BackgroundColor = UIColor.Clear;
				return cell;
			}
		}
	}
}