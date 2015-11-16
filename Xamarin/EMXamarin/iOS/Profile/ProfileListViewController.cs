using System;
using System.Collections.Generic;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using GoogleAnalytics.iOS;
using UIKit;

namespace iOS {
	public class ProfileListViewController : UIViewController {

		TableViewDelegate tableDelegate;
		TableViewDataSource tableDataSource;

		readonly SharedProfileListController sharedProfileListController;

		public ChatEntry chatEntry;
		public IList<Contact> contacts;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		#region UI
		public UIView LineView;
		UIBarButtonItem RemoveMeButton;
		UITableView ContactsTableView;
		#endregion

		public ProfileListViewController (ChatEntry ce) {
			var appDelegate = (AppDelegate) UIApplication.SharedApplication.Delegate;
			sharedProfileListController = new SharedProfileListController (appDelegate.applicationModel, ce);
			chatEntry = ce;
			contacts = ce.contacts;
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

			RemoveMeButton = new UIBarButtonItem ("LEAVE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( DidTapRemoveMeButton).HandleEvent<object,EventArgs>);
			RemoveMeButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			RemoveMeButton.Enabled = chatEntry.IsAdHocGroupWeCanLeave();
			NavigationItem.SetRightBarButtonItem(RemoveMeButton, true);

			ContactsTableView = new UITableView ();
			ContactsTableView.BackgroundColor = UIColor.Clear;
			ContactsTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			View.Add (ContactsTableView);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);

			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			Title = "PROFILES_TITLE".t ();

			tableDelegate = new TableViewDelegate (this);
			tableDataSource = new TableViewDataSource (this);

			ContactsTableView.Delegate = tableDelegate;
			ContactsTableView.DataSource = tableDataSource;

			View.AutosizesSubviews = true;
			ContactsTableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			ContactsTableView.ReloadData ();
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Profiles View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, View.Frame.Width, LineView.Frame.Height);
			ContactsTableView.Frame = new CGRect (0, displacement_y + LineView.Frame.Height, View.Frame.Width, View.Frame.Height - (displacement_y + LineView.Frame.Height));
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose(bool disposing) {
			sharedProfileListController.Dispose ();
			base.Dispose (disposing);
		}

		protected void DidTapRemoveMeButton(object sender, EventArgs e) {
			var alert = new UIAlertView("LEAVE_CONVERSATION".t (), 
				"LEAVE_CONVERSATION_EXPAINATION".t (),
				null,
				"CANCEL_BUTTON".t (),
				new string[] { "LEAVE".t () });
			alert.Show();
			alert.Clicked += (s, args) => { 
				switch (args.ButtonIndex) {
				case 1:
					sharedProfileListController.RemoveFromAdHocGroupAsync ();

					// Back to Inbox.
					UIViewController[] viewControllers = this.NavigationController.ChildViewControllers;
					foreach (UIViewController viewController in viewControllers) {
						if (viewController is InboxViewController) {
							this.NavigationController.PopToViewController (viewController, true);
							return;
						}
					}

					break;
				default:
					break;
				}
			};
		}

		class TableViewDelegate : UITableViewDelegate {
			WeakReference Ref { get; set; }
			public ProfileListViewController profileListViewController {
				get {
					return (Ref == null ? null : Ref.Target as ProfileListViewController);
				}
				set {
					Ref = new WeakReference (value);
				}
			}

			public TableViewDelegate(ProfileListViewController controller) {
				profileListViewController = controller;
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return iOS_Constants.APP_CELL_ROW_HEIGHT;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				CounterParty cp = profileListViewController.sharedProfileListController.Profiles [indexPath.Row];
				var controller = new ProfileViewController (cp, false);
				profileListViewController.NavigationController.PushViewController (controller, true);

				tableView.DeselectRow (indexPath, true);
			}
		}

		class TableViewDataSource : UITableViewDataSource {
			WeakReference Ref { get; set; }
			public ProfileListViewController profileListViewController {
				get {
					return (Ref == null ? null : Ref.Target as ProfileListViewController);
				}
				set {
					Ref = new WeakReference (value);
				}
			}

			public TableViewDataSource(ProfileListViewController controller) {
				profileListViewController = controller;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				ProfileListViewController controller = this.profileListViewController;
				if (controller == null)
					return ((nint)0);
				return controller.sharedProfileListController.Profiles == null ? 0 : controller.sharedProfileListController.Profiles.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				ProfileListViewController controller = this.profileListViewController;
				if (controller == null)
					return null;
				var cell = (ProfileTableViewCell)tableView.DequeueReusableCell (ProfileTableViewCell.Key) ?? ProfileTableViewCell.Create ();
				CounterParty cp = controller.sharedProfileListController.Profiles [indexPath.Row];

				cell.counterparty = cp;
				cell.SetEvenRow (indexPath.Row % 2 == 0);

				return cell;
			}
		}

		class SharedProfileListController : AbstractProfileController {

			public SharedProfileListController(ApplicationModel appModel, ChatEntry ce) : base (appModel, ce) {
				
			}

			public override void DidChangeTempProperty () {

			}

			public override void DidChangeBlockStatus (Contact c) {
				
			}

			public override void TransitionToChatController (ChatEntry chatEntry) {
				
			}
		}
	}
}