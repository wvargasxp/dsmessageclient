using System;
using System.Collections.Generic;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using GoogleAnalytics.iOS;
using UIKit;

namespace iOS {
	public class GroupsViewController : UIViewController {

		TableViewDelegate tableDelegate;
		TableViewDataSource tableDataSource;
		readonly SharedGroupController sharedGroupController;

		NSObject willEnterForegroundObserver = null;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		#region UI
		UIView LineView;
		UITableView GroupsTableView;
		#endregion

		public GroupsViewController() {
			var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
			sharedGroupController = new SharedGroupController (appDelegate.applicationModel, this);
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			var mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && LineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					LineView.BackgroundColor = mainColor.GetColor ();
				}
			});

			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		public override void LoadView () {
			base.LoadView ();

			LineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (LineView);
			View.BringSubviewToFront (LineView);

			GroupsTableView = new UITableView ();
			GroupsTableView.BackgroundColor = UIColor.Clear;
			GroupsTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			View.Add (GroupsTableView);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);

			Title = "GROUPS_TITLE".t ();

			tableDelegate = new TableViewDelegate (this);
			tableDataSource = new TableViewDataSource (this);

			GroupsTableView.Delegate = tableDelegate;
			GroupsTableView.DataSource = tableDataSource;

			var leftButton = new UIBarButtonItem ("CLOSE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( (sender, args) => DismissViewController (true, null)).HandleEvent<object,EventArgs>);
			leftButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			NavigationItem.SetLeftBarButtonItem(leftButton, true);

			var addGroupButton = new UIButton (UIButtonType.ContactAdd);
			addGroupButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs>( (sender, e) => {
				UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);
				NavigationController.PushViewController (new EditGroupViewController (false, null, true), true);
			}).HandleEvent<object,EventArgs>;
			var rightButton = new UIBarButtonItem ();
			rightButton.CustomView = addGroupButton;
			NavigationItem.RightBarButtonItem = rightButton;

			View.AutosizesSubviews = true;
			GroupsTableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			GroupsTableView.ReloadData ();

			willEnterForegroundObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)Constants.DID_ENTER_FOREGROUND, notification => {
				if (GroupsTableView != null)
					GroupsTableView.ReloadData ();
			});
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			Visible = true;

			// This screen name value will remain set on the tracker and sent with hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Groups View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, View.Frame.Width, LineView.Frame.Height);

			GroupsTableView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height, View.Frame.Width, View.Frame.Height - (displacement_y + LineView.Frame.Height));
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			NSNotificationCenter.DefaultCenter.RemoveObserver (willEnterForegroundObserver);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			Visible = false;
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose(bool disposing) {
			NSNotificationCenter.DefaultCenter.RemoveObserver (this);
			base.Dispose (disposing);
		}

		public void TransitionToChatController (ChatEntry chatEntry) {
			var chatViewController = new ChatViewController (chatEntry);
			chatViewController.NEW_MESSAGE_INITIATED_FROM_NOTIFICATION = true;


			MainController mainController = AppDelegate.Instance.MainController;
			UINavigationController navController = mainController.ContentController as UINavigationController;
			navController.PushViewController(chatViewController, true);
		}

		class TableViewDelegate : UITableViewDelegate {

			WeakReference groupsViewControllerRef;
			GroupsViewController groupsViewController {
				get { return groupsViewControllerRef.Target as GroupsViewController; }
				set { groupsViewControllerRef = new WeakReference (value); }
			}

			public TableViewDelegate(GroupsViewController controller) {
				groupsViewController = controller;
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return iOS_Constants.APP_CELL_ROW_HEIGHT;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				UINavigationBarUtil.SetBackButtonToHaveNoText (groupsViewController.NavigationItem);
				Group group = groupsViewController.sharedGroupController.Groups [indexPath.Row];
				var controller = new EditGroupViewController (true, group, true);
				groupsViewController.NavigationController.PushViewController (controller, true);

				tableView.DeselectRow (indexPath, true);
			}
		}

		class TableViewDataSource : UITableViewDataSource {
			WeakReference groupsViewControllerRef;
			GroupsViewController GroupsViewController {
				get { return groupsViewControllerRef.Target as GroupsViewController; }
				set { groupsViewControllerRef = new WeakReference (value); }
			}

			public TableViewDataSource(GroupsViewController controller) {
				GroupsViewController = controller;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				return GroupsViewController.sharedGroupController.Groups == null ? 0 : GroupsViewController.sharedGroupController.Groups.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				var cell = (GroupTableViewCell)tableView.DequeueReusableCell (GroupTableViewCell.Key) ?? GroupTableViewCell.Create ();
				Group g = GroupsViewController.sharedGroupController.Groups [indexPath.Row];

				cell.Group = g;
				cell.SetEvenRow (indexPath.Row % 2 == 0);
				cell.SendButtonClickCallback = () => GroupsViewController.DismissViewController (true, () => {
					GroupsViewController groupsViewController = this.GroupsViewController;
					if (groupsViewController != null) {
						groupsViewController.sharedGroupController.GoToNewOrExistingChatEntry (g);
					}
				});

				return cell;
			}
		}

		class SharedGroupController : AbstractGroupsController {
			WeakReference groupsViewControllerRef;
			GroupsViewController groupsViewController {
				get { return groupsViewControllerRef.Target as GroupsViewController; }
				set { groupsViewControllerRef = new WeakReference (value); }
			}

			public SharedGroupController(ApplicationModel appModel, GroupsViewController controller) : base(appModel) {
				groupsViewController = controller;
			}

			public override void GroupsValuesDidChange() {
				if (groupsViewController != null && groupsViewController.IsViewLoaded)
					groupsViewController.GroupsTableView.ReloadData();
			}

			public override void ReloadGroup(Contact group) {
				if (groupsViewController != null && groupsViewController.IsViewLoaded) {
					int index = -1;

					for(int i=0; i < Groups.Count; i++) {
						if(Groups[i].serverID.Equals(group.serverID)) {
							index = i;
							break;
						}
					}

					if (index != -1)
						groupsViewController.GroupsTableView.ReloadRows (new [] { NSIndexPath.FromItemSection (index, 0) }, UITableViewRowAnimation.Automatic);
				}
			}

			public override void DidChangeColorTheme () {
				if (groupsViewController != null && groupsViewController.IsViewLoaded) {
					groupsViewController.ThemeController (groupsViewController.InterfaceOrientation);
				}
			}

			public override void TransitionToChatController (ChatEntry chatEntry) {
				EMTask.DispatchMain (() => {
					if (groupsViewController != null && groupsViewController.IsViewLoaded)
						groupsViewController.TransitionToChatController (chatEntry);
				});
			}
		}
	}
}