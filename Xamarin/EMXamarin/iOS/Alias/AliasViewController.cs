using System;
using System.IO;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using GoogleAnalytics.iOS;
using Social;
using String_UIKit_Extension;
using Twitter;
using UIKit;
using System.Diagnostics;

namespace iOS {
	public class AliasViewController : UIViewController {

		AliasTableViewDelegate tableDelegate;
		AliasTableViewDataSource tableDataSource;
		readonly SharedAliasController sharedAliasController;

		ApplicationModel appModel;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		NSObject willEnterForegroundObserver = null;

		#region UI
		UIBarButtonItem rightButton;
		UIView LineView;
		UILabel AliasInfoLabel;
		UITableView AliasTableView;
		#endregion

		public AliasViewController() {
			var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
			appModel = appDelegate.applicationModel;
			sharedAliasController = new SharedAliasController (appModel, this);

			var leftButton = new UIBarButtonItem ("CLOSE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>((sender, args) => {
				DismissViewController (true, null);
			}).HandleEvent<object,EventArgs>);
			leftButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes (), UIControlState.Normal);
			NavigationItem.SetLeftBarButtonItem (leftButton, true);
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

			if (this.NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		public override void LoadView () {
			base.LoadView ();

			LineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			LineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (LineView);
			View.BringSubviewToFront (LineView);

			AliasInfoLabel = new UILabel (new CGRect (0, 1 + UI_CONSTANTS.LABEL_PADDING, View.Frame.Width, 60));
			AliasInfoLabel.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			AliasInfoLabel.Text = "SETUP_ALIAS".t ();
			AliasInfoLabel.Font = FontHelper.DefaultFontForLabels ();
			AliasInfoLabel.TextColor = FontHelper.DefaultTextColor ();
			AliasInfoLabel.LineBreakMode = UILineBreakMode.WordWrap;
			AliasInfoLabel.Lines = 0;
			AliasInfoLabel.TextAlignment = UITextAlignment.Center;
			AliasInfoLabel.ClipsToBounds = true;
			View.Add (AliasInfoLabel);

			AliasTableView = new UITableView ();
			AliasTableView.BackgroundColor = UIColor.Clear;
			AliasTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			View.Add (AliasTableView);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);
			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			Title = "ALIAS_TITLE".t ();

			tableDelegate = new AliasTableViewDelegate (this);
			tableDataSource = new AliasTableViewDataSource (this);

			AliasTableView.Delegate = tableDelegate;
			AliasTableView.DataSource = tableDataSource;

			var addAliasButton = new UIButton (UIButtonType.ContactAdd);
			addAliasButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object,EventArgs> ((sender, e) => {
				if(sharedAliasController.EnableAddNewAliasButton ()) {
					NavigationController.PushViewController (new EditAliasViewController (null), true);
				}
			}).HandleEvent<object,EventArgs>;

			rightButton = new UIBarButtonItem ();
			rightButton.CustomView = addAliasButton;
			rightButton.Enabled = sharedAliasController.EnableAddNewAliasButton ();
			NavigationItem.RightBarButtonItem = rightButton;

			View.AutosizesSubviews = true;
			AliasTableView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);

			AliasTableView.ReloadData ();

			rightButton.Enabled = sharedAliasController.EnableAddNewAliasButton ();

			willEnterForegroundObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)em.Constants.DID_ENTER_FOREGROUND, notification => {
				if (AliasTableView != null)
					AliasTableView.ReloadData ();
			});
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;

			// This screen name value will remain set on the tracker and sent with hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Alias View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;

			LineView.Frame = new CGRect (0, displacement_y, View.Frame.Width, LineView.Frame.Height);

			CGSize sizeCTL = AliasInfoLabel.Text.SizeOfTextWithFontAndLineBreakMode (AliasInfoLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, UIScreen.MainScreen.Bounds.Height), UILineBreakMode.Clip);
			sizeCTL = new CGSize ((float)((int)(sizeCTL.Width + 1.5)), (float)((int)(sizeCTL.Height + 1.5)));
			AliasInfoLabel.Frame = new CGRect (0, displacement_y + LineView.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN, sizeCTL.Width, sizeCTL.Height);

			AliasTableView.Frame = new CGRect (0, AliasInfoLabel.Frame.Y + AliasInfoLabel.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN, View.Frame.Width, View.Frame.Height - (AliasInfoLabel.Frame.Y + AliasInfoLabel.Frame.Height + UI_CONSTANTS.EXTRA_MARGIN));
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
			NSNotificationCenter.DefaultCenter.RemoveObserver (willEnterForegroundObserver);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		public override void ViewDidDisappear(bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
		}

		protected override void Dispose(bool disposing) {
			NSNotificationCenter.DefaultCenter.RemoveObserver (this);
			sharedAliasController.Dispose ();
			base.Dispose (disposing);
		}

		public void DidTapShareToInstagram(string actualPath, AliasInfo alias) {
			/*
			If your application creates photos and you'd like your users to share these photos using Instagram, you can use the Document Interaction API 
			to open your photo in Instagram's sharing flow.

			You must first save your file in PNG or JPEG (preferred) format and use the filename extension ".ig". Using the iOS Document Interaction APIs you 
			can trigger the photo to be opened by Instagram. The Identifier for our Document Interaction UTI is com.instagram.photo, and it conforms to the public/jpeg 
			and public/png UTIs. See the Apple documentation articles: Previewing and Opening Files and the UIDocumentInteractionController Class Reference for more information.

			Alternatively, if you want to show only Instagram in the application list (instead of Instagram plus any other public/jpeg-conforming apps) you can specify 
			the extension class igo, which is of type com.instagram.exclusivegram.

			When triggered, Instagram will immediately present the user with our filter screen. The image is preloaded and sized appropriately for Instagram. 
			For best results, Instagram prefers opening a JPEG that is 640px by 640px square. If the image is larger, it will be resized dynamically.

			To include a pre-filled caption with your photo, you can set the annotation property on the document interaction request to an NSDictionary containing 
			an NSString under the key "InstagramCaption". This feature is available on Instagram 2.1 and later.
			*/

			var imageURL = new NSUrl("file://" + actualPath);
			UIDocumentInteractionController docInteractionController = UIDocumentInteractionController.FromUrl(imageURL);
			//docInteractionController.Uti = "com.instagram.photo";
			docInteractionController.Uti = "com.instagram.exclusivegram";
			var values = new NSObject[] { new NSString (string.Format("INSTAGRAM_SHARE_DEFAULT_MESSAGE".t (), alias.displayName)) };
			var keys = new NSObject[] { new NSString ("InstagramCaption") };
			var options = NSDictionary.FromObjectsAndKeys(values, keys);
			docInteractionController.Annotation = options;
			docInteractionController.PresentOpenInMenu(View.Frame, View, true);
		}

		public void DidTapShareToTwitter(string actualPath, AliasInfo alias) {
			var tvc = new TWTweetComposeViewController();
			tvc.SetInitialText(string.Format("INSTAGRAM_SHARE_DEFAULT_MESSAGE".t (), alias.displayName));
			tvc.AddImage(UIImage.FromFile(actualPath));

			tvc.SetCompletionHandler((TWTweetComposeViewControllerResult r)=>{
				if (r == TWTweetComposeViewControllerResult.Cancelled){
					Debug.WriteLine("Tweet canceled");
				} else {
					Debug.WriteLine("Tweet sent");
				}

				tvc.DismissViewController (true, null);
			});

			NavigationController.PushViewController (tvc, true);
		}

		public void DidTapShare(string actualPath, AliasInfo alias, SLServiceKind type) {
			SLComposeViewController slComposer = SLComposeViewController.FromService (type);
			slComposer.SetInitialText (string.Format("INSTAGRAM_SHARE_DEFAULT_MESSAGE".t (), alias.displayName));
			slComposer.AddImage (UIImage.FromFile(actualPath));
			slComposer.CompletionHandler += (result) => {
				Debug.WriteLine("Result from share: " + result);
				slComposer.DismissViewController (true, null);
			};
			NavigationController.PushViewController (slComposer, true);
		}

		class AliasTableViewDelegate : UITableViewDelegate {
			WeakReference aliasViewControllerRef;
			AliasViewController aliasViewController {
				get { return aliasViewControllerRef.Target as AliasViewController; }
				set { aliasViewControllerRef = new WeakReference (value); }
			}

			public AliasTableViewDelegate(AliasViewController controller) {
				aliasViewController = controller;
			}

			public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
				return iOS_Constants.APP_CELL_ROW_HEIGHT;
			}

			public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
				AliasInfo alias = aliasViewController.sharedAliasController.Aliases [indexPath.Row];
				if (alias.lifecycle == ContactLifecycle.Active) {
					var controller = new EditAliasViewController (alias.serverID);
					aliasViewController.NavigationController.PushViewController (controller, true);
				} else {
					var alert = new UIAlertView ("ALIAS_DELETED_TITLE".t (), "ALIAS_DELETED_MESSAGE".t (), null, "OK_BUTTON".t (), "ALIAS_DELETED_REACTIVATE".t ());
					// TODO resave the alias to support reactivation.
					alert.Clicked += (s, b) => { 
						if ( b.ButtonIndex == 1 ) {
							aliasViewController.sharedAliasController.ReactivateAlias(alias);
						}
					};
					alert.Show ();
				}

				tableView.DeselectRow (indexPath, true);
			}
		}

		class AliasTableViewDataSource : UITableViewDataSource {
			WeakReference aliasViewControllerRef;
			AliasViewController aliasViewController {
				get { return aliasViewControllerRef.Target as AliasViewController; }
				set { aliasViewControllerRef = new WeakReference (value); }
			}

			public AliasTableViewDataSource(AliasViewController controller) {
				aliasViewController = controller;
			}

			public override nint RowsInSection (UITableView tableView, nint section) {
				return aliasViewController.sharedAliasController.Aliases == null ? 0 : aliasViewController.sharedAliasController.Aliases.Count;
			}

			public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
				var cell = (AliasTableViewCell)tableView.DequeueReusableCell (AliasTableViewCell.Key) ?? AliasTableViewCell.Create ();
				AliasInfo alias = aliasViewController.sharedAliasController.Aliases [indexPath.Row];

				cell.Alias = alias;
				cell.SetEvenRow (indexPath.Row % 2 == 0);
				cell.Tag = indexPath.Row;

				cell.ShareClickCallback = WeakDelegateProxy.CreateProxy<int>( ShareAliasTapped ).HandleEvent<int>;

				return cell;
			}

			void ShareAliasTapped(int index) {
				var ah = new AnalyticsHelper();
				ah.SendEvent(AnalyticsConstants.CATEGORY_UI_ACTION, AnalyticsConstants.ACTION_BUTTON_PRESS, AnalyticsConstants.SHARE_PROFILE, 0);

				AliasInfo alias = aliasViewController.sharedAliasController.Aliases [index];

				//first check to make sure screenshot exists
				string imagePath = aliasViewController.appModel.platformFactory.GetFileSystemManager ().GetFilePathForSharingAlias(alias);
				string actualPath = aliasViewController.appModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri(imagePath);
				if(File.Exists(actualPath)) {
					int i = 0, instagramIndex = -1, twitterIndex = -1, facebookIndex = -1;

					var actionSheet = new UIActionSheet ();

					//next, see if phone has any app(s) that EM supports (Instagram, Twitter, Facebook)
					var instagramUrl = new NSUrl("instagram://app");
					if(UIApplication.SharedApplication.CanOpenUrl(instagramUrl)) {
						instagramIndex = i++;
						actionSheet.AddButton("Instagram");
					}

					/*
					if(SLComposeViewController.IsAvailable (SLServiceKind.Twitter)) {
						twitterIndex = i++;
						actionSheet.AddButton("Twitter");
					}

					if(SLComposeViewController.IsAvailable(SLServiceKind.Facebook)) {
						facebookIndex = i++;
						actionSheet.AddButton("Facebook");
					}
					*/

					//if so, show action sheet and ask user which platform they'd like to share to
					if(i > 0) {
						actionSheet.AddButton("CANCEL_BUTTON".t ());
						actionSheet.CancelButtonIndex = i;
						actionSheet.Clicked += delegate(object sender, UIButtonEventArgs buttonEventArgs) {
							if(buttonEventArgs.ButtonIndex == instagramIndex)
								aliasViewController.DidTapShareToInstagram(actualPath, alias);
							else if(buttonEventArgs.ButtonIndex == twitterIndex)
								aliasViewController.DidTapShare(actualPath, alias, SLServiceKind.Twitter);
							else if(buttonEventArgs.ButtonIndex == facebookIndex)
								aliasViewController.DidTapShare(actualPath, alias, SLServiceKind.Facebook);
						};
						actionSheet.ShowInView (aliasViewController.View);
					}
					//if not, put up an error message
					else {
						var title = "SHARE_PROFILE".t ();
						var message = "INSTAGRAM_NOT_FOUND".t ();

						var alert = new UIAlertView (title, message, null, "CLOSE".t (), null);
						alert.Show ();
					}
				} else {
					EMTask.DispatchBackground (() => ShareHelper.GenerateInstagramSharableFile (aliasViewController.appModel, alias, null, index, ClickShareButton));
				}
			}

			void ClickShareButton(int index) {
				EMTask.DispatchMain (delegate {
					ShareAliasTapped(index);
				});
			}
		}

		class SharedAliasController : AbstractAliasController {
			readonly AliasViewController aliasViewController;

			public SharedAliasController(ApplicationModel appModel, AliasViewController controller) : base(appModel) {
				aliasViewController = controller;
			}

			public override void DidChangeColorTheme () {
				if (aliasViewController != null && aliasViewController.IsViewLoaded)
					aliasViewController.ThemeController (aliasViewController.InterfaceOrientation);
			}

			public override void DidChangeAliasList() {
				if (aliasViewController != null && aliasViewController.IsViewLoaded) {
					aliasViewController.AliasTableView.ReloadData ();
					aliasViewController.rightButton.Enabled = aliasViewController.sharedAliasController.EnableAddNewAliasButton ();
				}
			}

			public override void DidChangeThumbnailMedia () {
				if (aliasViewController != null && aliasViewController.IsViewLoaded)
					aliasViewController.AliasTableView.ReloadData ();
			}

			public override void DidDownloadThumbnail () {
				if (aliasViewController != null && aliasViewController.IsViewLoaded)
					aliasViewController.AliasTableView.ReloadData ();
			}

			public override void DidChangeIconMedia () {
				if (aliasViewController != null && aliasViewController.IsViewLoaded)
					aliasViewController.AliasTableView.ReloadData ();
			}

			public override void DidDownloadIcon () {
				if (aliasViewController != null && aliasViewController.IsViewLoaded)
					aliasViewController.AliasTableView.ReloadData ();
			}

			public override void DidUpdateLifecycle () {
				if (aliasViewController != null && aliasViewController.IsViewLoaded) {
					aliasViewController.AliasTableView.ReloadData ();
					aliasViewController.rightButton.Enabled = aliasViewController.sharedAliasController.EnableAddNewAliasButton ();
				}
			}
		}
	}
}