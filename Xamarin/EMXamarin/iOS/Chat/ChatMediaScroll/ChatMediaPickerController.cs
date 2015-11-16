using System;
using Foundation;
using UIKit;
using CoreGraphics;
using System.Collections.Generic;
using AssetsLibrary;
using em;
using EMXamarin;
using UIDevice_Extension;

namespace iOS {
	public class ChatMediaPickerController {
		//INSTANCE VARIABLES
		public UITableView TableView { get; set; }
		private UIButton BackgroundView { get; set; }
		public static NSString SendAfterStagingKey = new NSString ("SendAfterStaging");

		private ALAssetsLibrary AssetsLibrary { get; set; } 
		public List<ALAsset> Selected { get; set; }
		public bool NoPermission { get; set; }
		public bool EmptyAssets { get; set; }
		public bool AllowVideo { get; set; }
		public bool CanAccessCamera { get; set; }

		public ImageScrollController ScrollView { get; set; }

		private WeakReference ControllerRef;
		public AbstractAcquiresImagesController Controller {
			get {
				return this.ControllerRef != null ? this.ControllerRef.Target as AbstractAcquiresImagesController : null;
			}
			set {
				this.ControllerRef = new WeakReference (value);
			}
		}

		//CONSTRUCTOR
		public ChatMediaPickerController (AbstractAcquiresImagesController aaic, bool allowMessageSending) {
			this.AssetsLibrary = new ALAssetsLibrary ();
			this.Controller = aaic;
			this.AllowVideo = aaic.AllowVideoOnImageSelection;
			this.CanAccessCamera = aaic.CameraAvailable;
			this.Selected = new List<ALAsset> ();
			this.EmptyAssets = !allowMessageSending; // If no messages allowed, treat as if no photos so that the ScrollView doesn't show
		}

		private void Initialize (List<ALAsset> assets) {
			MediaPickerDelegate dlgt = new MediaPickerDelegate (this);
			MediaPickerDataSource source = new MediaPickerDataSource (this, assets);

			int height = source.OptimalHeight;

			this.TableView = new UITableView (new CGRect ( new CGPoint (0, UIScreen.MainScreen.Bounds.Height), new CGSize (this.Controller.View.Frame.Width, height)));
			this.TableView.AlwaysBounceVertical = false;
			this.BackgroundView = new UIButton (UIScreen.MainScreen.Bounds);
			this.BackgroundView.TouchUpInside += WeakDelegateProxy.CreateProxy<object, EventArgs> ((object o, EventArgs e) => {
				this.Hide ();
			}).HandleEvent<object, EventArgs>;
			this.TableView.WeakDelegate = dlgt;
			this.TableView.WeakDataSource = source;
		}

		public void InitializeAndShow () {
			WeakDelegateProxy handleProxy = WeakDelegateProxy.CreateProxy<List<ALAsset>> ((List<ALAsset> assets) => {
				Initialize (assets);
				Show ();
			});
			GetAllItemsFromCameraRoll (handleProxy.HandleEvent<List<ALAsset>>);
		}
			
		public void GetAllItemsFromCameraRoll (Action<List<ALAsset>> completionHandler) {
			List<ALAsset> list = new List<ALAsset> ();
			ALAssetsLibraryGroupsEnumerationResultsDelegate enumerateDelegate = 
				new ALAssetsLibraryGroupsEnumerationResultsDelegate (delegate (ALAssetsGroup assetsGroup, ref bool stop) {
					if (assetsGroup != null && assetsGroup.Count > 0) {
						assetsGroup.SetAssetsFilter (ALAssetsFilter.AllPhotos);
						ALAssetsEnumerator groupEnumerator = new ALAssetsEnumerator (delegate(ALAsset result, nint index, ref bool shouldStop) {
							if (result != null && result.AssetType != ALAssetType.Video) {
								list.Add (result);
							}
						});
						int lastPhoto = (int)assetsGroup.Count - 1;
						int firstPhoto = lastPhoto - 19 >= 0 ? lastPhoto - 19 : 0;
						NSIndexSet setOfPhotos = NSIndexSet.FromNSRange (new NSRange (firstPhoto, lastPhoto - firstPhoto + 1));
						assetsGroup.Enumerate (setOfPhotos, NSEnumerationOptions.Reverse, groupEnumerator);
					} else { //The list has finished constructing
						if (list.Count == 0) {
							this.EmptyAssets = true;
						}
						completionHandler (list);
					}
				});

			this.AssetsLibrary.Enumerate (ALAssetsGroupType.SavedPhotos, enumerateDelegate, (NSError error) => {
				System.Diagnostics.Debug.WriteLine (error);
				NoPermission = true;
				completionHandler (list);
			});
		}

		public void Show () {
			AbstractAcquiresImagesController controller = this.Controller;
			if (controller == null)
				return;
			((AppDelegate)UIApplication.SharedApplication.Delegate).MainController.DisableRotation ();
			nfloat screenHeight = UIScreen.MainScreen.Bounds.Size.Height;
			controller.View.Add (this.BackgroundView);
			controller.View.Add (this.TableView);
			UIColor color = UIColor.Black;
			color = color.ColorWithAlpha (0.35f);
			UIView.Animate (0.3f, 0, UIViewAnimationOptions.CurveEaseInOut, () => {
				BackgroundView.BackgroundColor = color;
				CGSize tableSize = TableView.Frame.Size;
				TableView.Frame = new CGRect (new CGPoint (0, screenHeight - tableSize.Height), tableSize);
			}, () => {});
		}

		public void Hide () {
			nfloat screenHeight = UIScreen.MainScreen.Bounds.Size.Height;
			UIColor color = UIColor.Black;
			color = color.ColorWithAlpha (0.0f);
			UIView.Animate (0.3f, 0, UIViewAnimationOptions.CurveEaseInOut, () => {
				BackgroundView.BackgroundColor = color;
				CGSize tableSize = TableView.Frame.Size;
				TableView.Frame = new CGRect (new CGPoint (0, UIScreen.MainScreen.Bounds.Height), tableSize);
			}, () => {
				((AppDelegate)UIApplication.SharedApplication.Delegate).MainController.AllowRotation ();
				BackgroundView.RemoveFromSuperview ();
				TableView.WeakDelegate = null;
				TableView.WeakDataSource = null;
				TableView.RemoveFromSuperview ();
			});
		}

		//METHODS
		public void HandleIncrementTap () {
			int numTapped = this.Selected.Count;
			List<string> labelList = ((MediaPickerDataSource)this.TableView.DataSource).CellLabels;
			string libraryButtonText = this.AllowVideo ? "PHOTO_AND_VIDEO_LIBRARY_BUTTON".t () : "PHOTO_LIBRARY_BUTTON".t ();
			if (numTapped == 0) {
				labelList [1] = libraryButtonText;
			} else if (numTapped == 1) {
				string localizedNumber = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (numTapped), NSNumberFormatterStyle.Decimal);
				labelList [1] = string.Format("SEND_MEDIA_SINGULAR".t (), localizedNumber);
			}
			else {
				string localizedNumber = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (numTapped), NSNumberFormatterStyle.Decimal);
				labelList [1] = string.Format("SEND_MEDIA_MULTIPLE".t (), localizedNumber);
			}
			TableView.ReloadRows (new NSIndexPath[]{ NSIndexPath.FromRowSection (1, 0) }, UITableViewRowAnimation.None);
		}

		private void HandleSelectedAssets (List<ALAsset> assets) {
			ALAsset selectedAsset = assets [0];
			NSString type = null;
			NSMutableDictionary info = new NSMutableDictionary ();
			if (selectedAsset.AssetType == ALAssetType.Photo) {
				type = new NSString ("public.image");
				UIImage original = UIImage.FromImage (selectedAsset.DefaultRepresentation.GetFullScreenImage ());
				info.SetValueForKey (original, UIImagePickerController.OriginalImage);
			} else {
				type = new NSString ("Unknown type name " + selectedAsset.AssetType);
			}
			info.SetValueForKey (type, UIImagePickerController.MediaType);
			info.SetValueForKey (selectedAsset.AssetUrl, UIImagePickerController.ReferenceUrl);
			info.SetValueForKey (new NSString ("true"), ChatMediaPickerController.SendAfterStagingKey);
			UIImagePickerMediaPickedEventArgs args = new UIImagePickerMediaPickedEventArgs (info);
			UIImagePickerController spoofController = new UIImagePickerController ();
			spoofController.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
			this.Controller.HandleFinishPickingMedia (spoofController, args);
		}

		public void HandleRowSelected (NSIndexPath indexPath) {
			int rowSelected = indexPath.Row;
			List<ALAsset> selected = this.Selected;
			int numTapped = selected.Count;
			AbstractAcquiresImagesController controller = this.Controller;
			if (controller == null)
				return;

			int IMAGESCROLL = 0;
			int GALLERY = 1;
			int CAMERA = 2;
			int SEARCH = 3;

			if (this.EmptyAssets) {
				IMAGESCROLL = -1;
				GALLERY--;
				CAMERA--;
				SEARCH--;
			}
			if (!this.CanAccessCamera) {
				CAMERA = -1;
				SEARCH--;
			}

			if (rowSelected == GALLERY) {
				if (numTapped > 0) {
					controller.HandleBulkMedia (selected);
				} else {
					controller.ShowImagePicker (UIImagePickerControllerSourceType.PhotoLibrary);
				}
			} else if (rowSelected == CAMERA) {
				controller.ShowImagePicker (UIImagePickerControllerSourceType.Camera);
			} else if (rowSelected == SEARCH) {
				controller.ShowImageSearch ();
			} else if (rowSelected == IMAGESCROLL && this.NoPermission) {
				if (UIDevice.CurrentDevice.IsIos8Later ()) {
					UIApplication.SharedApplication.OpenUrl (new NSUrl (UIApplication.OpenSettingsUrlString));
				}
			}
			this.Hide ();
		}
	}

	public class MediaPickerDelegate : UITableViewDelegate {
		private WeakReference _ref;
		public ChatMediaPickerController MediaPickerController {
			get {
				return _ref == null ? null : _ref.Target as ChatMediaPickerController;
			} 
			set {
				_ref = new WeakReference (value);
			}
		}

		public static int MediaScrollHeight = 150;    // How tall the cell that holds the scroll view should be
		public static int ButtonHeight = 40;
		// CONSTRUCTOR
		public MediaPickerDelegate (ChatMediaPickerController mpc) : base () {
			this.MediaPickerController = mpc;
		}
		// METHODS
		public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
			var controller = this.MediaPickerController;
			if (controller == null) return;

			controller.HandleRowSelected (indexPath);
			tableView.DeselectRow (indexPath, true);
		}

		public override nfloat GetHeightForRow (UITableView tableView, NSIndexPath indexPath) {
			ChatMediaPickerController controller = this.MediaPickerController;
			if (controller == null)
				return 0;

			if (indexPath.Row == 0 && !controller.EmptyAssets) {
				return MediaScrollHeight;
			} else {
				return ButtonHeight;    
			}
		}
	}

	public class MediaPickerDataSource : UITableViewDataSource {
		//INSTANCE VARIABLES

		private WeakReference _ref;
		public ChatMediaPickerController MediaPickerController {
			get {
				return _ref == null ? null : _ref.Target as ChatMediaPickerController;
			} 
			set {
				_ref = new WeakReference (value);
			}
		}

		// Identifier for first row (horizontal scroll through 10 recent photos
		public static string ScrollCell = "MediaPickerScrollCell";
		// Identifier for every other row
		public static string ActionCell = "MediaPickerActionCell";

		public List<string> CellLabels { get; set; }

		private bool AllowVideo { get; set; }
		private List<ALAsset> Assets { get; set; }
		public int OptimalHeight { get; set; }

		public static Dictionary<string, WeakReference> imageRefs = new Dictionary<string, WeakReference> ();

		public override nint NumberOfSections (UITableView tableView) {
			return (nint)1;
		}

		public override nint RowsInSection (UITableView tableView, nint section) {
			return (nint)CellLabels.Count;
		}

		//CONSTRUCTOR
		public MediaPickerDataSource (ChatMediaPickerController mpc, List<ALAsset> assets) : base () {
			this.Assets = assets;
			this.CellLabels = new List<string> ();
			this.MediaPickerController = mpc;
			this.OptimalHeight = 0;
			// Placeholder so that the list plays nice with the index paths (1st cell is media scroller => no label)
			if (!mpc.EmptyAssets) {
				this.CellLabels.Add (""); 
				this.OptimalHeight += MediaPickerDelegate.MediaScrollHeight;
			}
			int numButtons = 0;
			string libraryButtonText = mpc.AllowVideo ? "PHOTO_AND_VIDEO_LIBRARY_BUTTON".t () : "PHOTO_LIBRARY_BUTTON".t ();
			CellLabels.Add (libraryButtonText);
			if (mpc.CanAccessCamera) {
				string cameraText = "CAMERA_BUTTON".t ();
				CellLabels.Add (cameraText);
				numButtons++;
			}
			string imageSearchText = "WEB_SEARCH".t ();
			CellLabels.Add (imageSearchText);
			string cancelText = "CANCEL_BUTTON".t ();
			CellLabels.Add (cancelText);
			numButtons += 3;
			OptimalHeight += (numButtons * MediaPickerDelegate.ButtonHeight);
		}

		//METHODS
		public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
			UITableViewCell cell = null;
			if (indexPath.Row == 0 && !this.MediaPickerController.EmptyAssets) {
				cell = tableView.DequeueReusableCell (ScrollCell);
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Default, ScrollCell);
				}
				cell.UserInteractionEnabled = true;
				try {
					AddScrollViewToParentView (this.Assets, cell);
				} catch (Exception e) {
					System.Diagnostics.Debug.WriteLine (e.StackTrace);
				}
			} else {
				cell = tableView.DequeueReusableCell (ActionCell);
				if (cell == null) {
					cell = new UITableViewCell (UITableViewCellStyle.Default, ActionCell);
				}
				cell.TextLabel.TextAlignment = UITextAlignment.Center;
				cell.TextLabel.Text = CellLabels [indexPath.Row];
				cell.TextLabel.TextColor = tableView.TintColor;
				cell.TextLabel.Font = UIFont.SystemFontOfSize (20);
			}
			cell.UserInteractionEnabled = true;
			return cell;
		}

		private void AddScrollViewToParentView (List<ALAsset> assets, UIView parent) {
			ChatMediaPickerController controller = this.MediaPickerController;
			if (controller == null)
				return;

			int scrollHeight = MediaPickerDelegate.MediaScrollHeight;
			if (controller.NoPermission) {
				UILabel turnOnPhotos = new UILabel (new CGRect (0, 0, controller.TableView.Frame.Width, scrollHeight));
				turnOnPhotos.TextAlignment = UITextAlignment.Center;
				turnOnPhotos.Font = UIFont.SystemFontOfSize (20);
				turnOnPhotos.TextColor = UIColor.Gray;
				turnOnPhotos.Lines = 3;
				turnOnPhotos.Text = "ALLOW_ACCESS_PHOTOS".t ();
				parent.Add (turnOnPhotos);
				return;
			}
			if (controller.ScrollView == null) {
				controller.ScrollView = new ImageScrollController (assets, controller);
				UICollectionView view = controller.ScrollView.CollectionView;
				parent.Add (view);
			}
		}
	}
} 
