using System;
using em;
using EMXamarin;
using Foundation;
using UIKit;
using System.Collections.Generic;
using AssetsLibrary;
using CoreGraphics;

namespace iOS{

	public class ImageScrollController {

		public List<ALAsset> Assets { get; set; }
		public UICollectionView CollectionView { get; set; }
		private WeakReference _ref;
		public ChatMediaPickerController MediaPickerController {
			get { return _ref == null ? null : _ref.Target as ChatMediaPickerController; }
			set { _ref = new WeakReference (value); }
		}

		public static string CELL_IDENTIFIER = "imageScrollCell";

		public static int MAX_IMAGE_HEIGHT = 145;
		public static int IMAGE_MARGIN = 5;

		public ImageScrollController (List<ALAsset> assets, ChatMediaPickerController controller) {
			this.Assets = assets;
			this.MediaPickerController = controller;
			this.CollectionView = new UICollectionView (new CGRect (0, 0, UIScreen.MainScreen.Bounds.Size.Width, MediaPickerDelegate.MediaScrollHeight), new ImageScrollLayout (this));
			this.CollectionView.BackgroundColor = UIColor.White;
			this.CollectionView.RegisterClassForCell (typeof(ImageScrollCell), CELL_IDENTIFIER);
			this.CollectionView.DataSource = new ImageScrollDataSource (assets, this);
			this.CollectionView.Delegate = new ImageScrollDelegate (this);
		}

		public void HandleRowSelected (NSIndexPath indexPath) {
			ChatMediaPickerController controller = this.MediaPickerController;
			if (controller == null)
				return;
			controller.HandleRowSelected (indexPath);
		}

		public void HandleIncrementTap () {
			ChatMediaPickerController controller = this.MediaPickerController;
			if (controller == null)
				return;
			controller.HandleIncrementTap ();
		}

		public void AddToSelected (ALAsset asset) {
			ChatMediaPickerController controller = this.MediaPickerController;
			if (controller == null)
				return;
			controller.Selected.Add (asset);
		}

		public void RemoveFromSelected (ALAsset asset) {
			ChatMediaPickerController controller = this.MediaPickerController;
			if (controller == null)
				return;
			controller.Selected.Remove (asset);
		}

		public bool IsSelected (ALAsset asset) {
			ChatMediaPickerController controller = this.MediaPickerController;
			if (controller == null)
				return false;
			return controller.Selected.Contains (asset);
		}

		private static Dictionary<string, WeakReference> imageRefs;
		public static Dictionary<string, WeakReference> ImageRefs{
			get {
				if (imageRefs == null) {
					imageRefs = new Dictionary<string, WeakReference> ();
				}
				return imageRefs;
			}
		}

		private static Dictionary<NSIndexPath, int> widthOffsets;
		public static Dictionary<NSIndexPath, int> WidthOffsets {
			get {
				if (widthOffsets == null) {
					widthOffsets = new Dictionary<NSIndexPath, int> ();
				}
				return widthOffsets;
			}
		}

		public int GetWidthOffsetForIndexPath (NSIndexPath indexPath) {
			int offset;
			if (indexPath.Row == 0) {
				offset = 0;
			}
			else if (!WidthOffsets.TryGetValue (indexPath, out offset)) {
				NSIndexPath prevIndexPath = NSIndexPath.FromRowSection (indexPath.Row - 1, 0);
				offset = 5 + (int)GetImageForIndexPath (prevIndexPath).Size.Width + GetWidthOffsetForIndexPath (prevIndexPath);
				WidthOffsets [indexPath] = offset;
			}
			return offset;
		}

		public UIImage GetImageForIndexPath (NSIndexPath indexPath) {
			if (indexPath.Row >= this.Assets.Count)
				return null;
			UIImage image;
			ALAsset asset = this.Assets [indexPath.Row];
			string refKey = asset.AssetUrl.AbsoluteString;
			if (ImageRefs.ContainsKey (refKey)) {
				image = ImageRefs [refKey].Target as UIImage;
				if (image == null) {
					image = ScaleImageToImageScroll(UIImage.FromImage (asset.AspectRatioThumbnail ()));
					ImageRefs [refKey] = new WeakReference (image);
				}
			} else {
				image = ScaleImageToImageScroll (UIImage.FromImage (asset.AspectRatioThumbnail ()));
				ImageRefs [refKey] = new WeakReference (image);
			}
			return image;
		}

		public UIImageView CreateImageViewForIndexPath (NSIndexPath indexPath, bool selected) {
			if (indexPath.Row >= this.Assets.Count)
				return null;
			UIImage image = GetImageForIndexPath (indexPath);
			UIImageView imageView = new UIImageView (new CGRect (0, 5, image.Size.Width, ImageScrollController.MAX_IMAGE_HEIGHT));
			imageView.Image = image;
			nfloat checkboxSize = 2 * CheckboxView.DefaultCheckBoxSize;
			CheckboxView checkbox = new CheckboxView (new CGRect (imageView.Frame.Size.Width - checkboxSize / 1.5, imageView.Frame.Size.Height - checkboxSize / 1.5, checkboxSize, checkboxSize));
			checkbox.ShouldDrawBorder = true;
			if (selected) {
				checkbox.IsOn = true;
				checkbox.SetBoundedRectFill (this.CollectionView.TintColor);
			}
			imageView.Add (checkbox);
			return imageView;
		}

		public static UIImage ScaleImageToImageScroll (UIImage image) {
			nfloat screenWidth = (nfloat)UIScreen.MainScreen.Bounds.Size.Width;
			nfloat originalWidth = image.Size.Width;
			nfloat originalHeight = image.Size.Height;
			nfloat targetHeight = (nfloat)(MediaPickerDelegate.MediaScrollHeight);
			nfloat scale = targetHeight / originalHeight;
			if (originalWidth * scale > screenWidth) {
				scale = screenWidth / originalWidth;
			}
			return image.Scale (new CGSize (originalWidth * scale, originalHeight * scale));
		}
	}

	public class ImageScrollDelegate : UICollectionViewDelegateFlowLayout {

		private WeakReference _ref;
		public ImageScrollController ImageScrollController {
			get {
				return _ref == null ? null : _ref.Target as ImageScrollController;
			} 
			set {
				_ref = new WeakReference (value);
			}
		}

		public ImageScrollDelegate (ImageScrollController controller) : base () {
			this.ImageScrollController = controller;
		}

		public override void ItemSelected (UICollectionView collectionView, NSIndexPath indexPath){
			ImageScrollController controller = this.ImageScrollController;
			if (controller == null) return;
			ALAsset asset = controller.Assets [indexPath.Row];

			if (controller.IsSelected (asset)) {
				controller.RemoveFromSelected (asset);
			} else {
				controller.AddToSelected (asset);
			}
			controller.HandleIncrementTap ();
			collectionView.ReloadItems (new NSIndexPath[]{ indexPath });
		}

		public override CGSize GetSizeForItem (UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath) {
			return new CGSize (ImageScrollController.GetImageForIndexPath (indexPath).Size.Width, MediaPickerDelegate.MediaScrollHeight);
		}

		public override void WillDisplayCell (UICollectionView collectionView, UICollectionViewCell cell, NSIndexPath indexPath) {
		}
	}

	public class ImageScrollDataSource : UICollectionViewDataSource {

		private WeakReference _ref;
		public ImageScrollController Controller {
			get {
				return _ref == null ? null : _ref.Target as ImageScrollController;
			} 
			set {
				_ref = new WeakReference (value);
			}
		}

		public ImageScrollDataSource (List<ALAsset> assets, ImageScrollController controller) : base () {
			this.Controller = controller;
			CellKeys = new List<string> ();
		}

		public override nint NumberOfSections (UICollectionView collectionView) {
			return 1;
		}

		private List<string> CellKeys { get; set; }

		public override UICollectionViewCell GetCell (UICollectionView collectionView, NSIndexPath indexPath) {
			ImageScrollController controller = this.Controller;
			if (controller == null)
				return null;
			CGSize size = ((ImageScrollDelegate)collectionView.WeakDelegate).GetSizeForItem (collectionView, collectionView.CollectionViewLayout, indexPath);
			string cellStartKey = ImageScrollController.CELL_IDENTIFIER;
			if (!CellKeys.Contains (cellStartKey + size.Width)) {
				collectionView.RegisterClassForCell (typeof(ImageScrollCell), cellStartKey + size.Width);
				CellKeys.Add (cellStartKey + size.Width);
			}
			ImageScrollCell cell = (ImageScrollCell)collectionView.DequeueReusableCell (cellStartKey + size.Width, indexPath);
			ALAsset asset = controller.Assets [indexPath.Row];
			cell.Selected = controller.IsSelected (asset);
			cell.Asset = asset;
			return cell;
		}

		public override nint GetItemsCount (UICollectionView collectionView, nint section) {
			ImageScrollController controller = this.Controller;
			if (controller == null)
				return 0;
			return controller.Assets.Count;
		}
	}

	public class ImageScrollLayout : UICollectionViewFlowLayout {
		private WeakReference _ref;
		public ImageScrollController Controller {
			get {
				return _ref == null ? null : _ref.Target as ImageScrollController;
			} 
			set {
				_ref = new WeakReference (value);
			}
		}
			
		public ImageScrollLayout (ImageScrollController controller) : base () {
			this.MinimumInteritemSpacing = 5;
			this.Controller = controller;
			this.ScrollDirection = UICollectionViewScrollDirection.Horizontal;
		}

		public override CGSize CollectionViewContentSize {
			get {
				ImageScrollController controller = this.Controller;
				if (controller == null) {
					return new CGSize (0, 0);
				}

				NSIndexPath lastIndex = NSIndexPath.FromRowSection (Controller.Assets.Count - 1, 0);
				int widthOffset = controller.GetWidthOffsetForIndexPath (lastIndex);
				int newWidth = widthOffset + (int)controller.GetImageForIndexPath (lastIndex).Size.Width;
				return new CGSize (newWidth, MediaPickerDelegate.MediaScrollHeight);
			}
		}

		public override UICollectionViewLayoutAttributes InitialLayoutAttributesForAppearingItem (NSIndexPath itemIndexPath) {
			ImageScrollController controller = this.Controller;
			if (controller == null) {
				return null;
			}

			UICollectionViewLayoutAttributes attrs = new UICollectionViewLayoutAttributes ();
			CGSize layoutSize = new CGSize (controller.GetImageForIndexPath (itemIndexPath).Size.Width, MediaPickerDelegate.MediaScrollHeight);
			int widthOffset = controller.GetWidthOffsetForIndexPath (itemIndexPath);
			CGPoint layoutPoint = new CGPoint (widthOffset, 0);
			attrs.Frame = new CGRect (layoutPoint, layoutSize);
			return attrs;
		}
	}

	public class ImageScrollCell : UICollectionViewCell {
		private UIImageView ImageView { get; set; }
		private ALAsset asset;
		public ALAsset Asset { 
			get { 
				return asset;
			}
			set { 
				this.asset = value;
				UpdateCellWithAsset (value);
			}
		}

		public ImageScrollCell (IntPtr ptr) : base (ptr) {
			this.ImageView = new UIImageView ();
			this.ImageView.ContentMode = UIViewContentMode.Center;
			this.ContentView.Add (ImageView);
		}

		public ImageScrollCell (CGRect frame) : base (frame) {
			this.ImageView = new UIImageView ();
			this.ImageView.ContentMode = UIViewContentMode.Center;
			this.ContentView.Add (ImageView);
		}

		public void UpdateCellWithAsset (ALAsset asset) {
			foreach (UIView view in this.ImageView) {
				view.RemoveFromSuperview ();
			}
			string refKey = asset.AssetUrl.AbsoluteString;
			UIImage image;
			Dictionary<string, WeakReference> refs = ImageScrollController.ImageRefs;
			if (refs.ContainsKey (refKey)) {
				image = refs [refKey].Target as UIImage;
				if (image == null) {
					image = ImageScrollController.ScaleImageToImageScroll(UIImage.FromImage (asset.AspectRatioThumbnail ()));
					refs [refKey] = new WeakReference (image);
				}
			} else {
				image = ImageScrollController.ScaleImageToImageScroll (UIImage.FromImage (asset.AspectRatioThumbnail ()));
				refs [refKey] = new WeakReference (image);
			}
			this.ImageView.Frame = new CGRect (0, 5, image.Size.Width, ImageScrollController.MAX_IMAGE_HEIGHT);
			this.ImageView.Image = image;
			nfloat checkboxSize = 2 * CheckboxView.DefaultCheckBoxSize;
			CheckboxView checkbox = new CheckboxView (new CGRect (ImageView.Frame.Size.Width - checkboxSize / 1.5, ImageView.Frame.Size.Height - checkboxSize / 1.5, checkboxSize, checkboxSize));
			checkbox.ShouldDrawBorder = true;
			if (this.Selected) {
				checkbox.IsOn = true;
				checkbox.SetBoundedRectFill (this.TintColor);
			}
			this.ImageView.AddSubview (checkbox);
		}
	}
}

