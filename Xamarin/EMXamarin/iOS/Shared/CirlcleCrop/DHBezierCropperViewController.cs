
using System;

using Foundation;
using UIKit;
using System.Drawing;
using ObjCRuntime;
using CoreGraphics;
using CoreImage;
using EMXamarin;

namespace iOS {
	public partial class DHBezierCropperViewController : UIViewController {
		static readonly float kMargin = 10.0f;

		public Action<UIImage> Completion { get; set; }

		public UIBezierPath CropShapePath { get; set; }
		public CGRect CropFrame { get; set; }
		public UIImage ImageToCrop { get; set; }

		DHImageScrollView ScrollView;
		DHImageCropperOverlayView OverlayView;

		float MaxRequestedZoomSize;

		bool StatusBarOriginalyHidden;
		bool NavBarButtonOriginallyHidden;
		UIStatusBarStyle StatusBarOriginalStyle;

		public void Styling () {
			View.BackgroundColor = UIColor.FromWhiteAlpha(0f, 1f);
			View.ClipsToBounds = true;
		}

		public void SetupScrollView () {
			ScrollView = new DHImageScrollView (CropFrame, ImageToCrop, MaxRequestedZoomSize);
			ScrollView.ClipsToBounds = false;
			ScrollView.BackgroundColor = UIColor.FromRGBA (0f, 0f, 0f, 1f);
			View.AddSubview (ScrollView);
		}

		public void SetupOverlayView () {
			OverlayView = new DHImageCropperOverlayView (View.Bounds, CropShapePath);
			View.AddSubview(OverlayView);

			WeakReference thisRef = new WeakReference (this);
			OverlayView.Completion = (bool didCrop) => {
				DHBezierCropperViewController self = thisRef.Target as DHBezierCropperViewController;

				if (self != null) {
					if (didCrop) {
						if (self.Completion != null) {
							UIImage croppedImage = self.CropImage();
							self.Completion (croppedImage);
						}
					} else if (self.Completion != null) {
						self.Completion (null);
					}
				}
			};
		}

		public void CommonSetup () {
			if (RespondsToSelector(new Selector("edgesForExtendedLayout"))) {
				EdgesForExtendedLayout = UIRectEdge.None;
				AutomaticallyAdjustsScrollViewInsets = false;
			}

			Styling ();
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad();

			CommonSetup ();
			SetupScrollView ();
			SetupOverlayView ();
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear(animated);

			CacheOriginalSettings ();
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear(animated);

			RestoreOriginalSettings ();
		}

		public DHBezierCropperViewController (UIImage cropImage, bool requestingSquareCropper = true) {
			CGRect inRect = new CGRect (0f, 0f, 10f, 10f);
			UIBezierPath cropSharePath;
			if (requestingSquareCropper) {
				cropSharePath = UIBezierPath.FromRect (inRect);
			} else {
				cropSharePath = UIBezierPath.FromOval (inRect);
			}

			CropShapePath = cropSharePath;
			CropFrame = CGRect.Empty;
			ImageToCrop = cropImage;
			MaxRequestedZoomSize = 1;
		}
			
		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews();

			UpdateMaskFrame ();
			LayoutScrollView ();
			LayoutOverlayView ();
			View.SetNeedsUpdateConstraints ();
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews();

			ScrollView.DisplayImage (ImageToCrop);
		}

		void UpdateMaskFrame() {
			nfloat width = this.View.Frame.Width;
			nfloat height = this.View.Frame.Height;

			nfloat diameter = ((nfloat) Math.Min (width, height)) - kMargin * 2;

			CGSize maskSize = new CGSize (diameter, diameter);

			CGRect cgRect = RectangleFExtensions.Integral (new CGRect ((width - maskSize.Width) * 0.5f,
					(height - maskSize.Height) * 0.5f, maskSize.Width, maskSize.Height));
			CropFrame = new CGRect ((float) cgRect.X, (float) cgRect.Y, (float) cgRect.Size.Width, (float) cgRect.Size.Height);
		}

		void LayoutScrollView () {
			ScrollView.Frame = CropFrame;
		}

		void LayoutOverlayView () {
			OverlayView.Frame = this.View.Bounds;
		}

		void CacheOriginalSettings() {
			StatusBarOriginalyHidden = UIApplication.SharedApplication.StatusBarHidden;
			UIApplication.SharedApplication.StatusBarHidden = true;

			StatusBarOriginalStyle = UIApplication.SharedApplication.StatusBarStyle;
			UIApplication.SharedApplication.SetStatusBarStyle (UIStatusBarStyle.LightContent, false);

			if (NavigationController != null) {
				NavBarButtonOriginallyHidden = NavigationController.NavigationBarHidden;
				NavigationController.SetNavigationBarHidden (true, false);
			}
		}
			
		void RestoreOriginalSettings () {
			UIApplication.SharedApplication.StatusBarHidden = StatusBarOriginalyHidden;
			UIApplication.SharedApplication.StatusBarStyle = StatusBarOriginalStyle;
			if ( NavigationController != null )
				NavigationController.SetNavigationBarHidden(NavBarButtonOriginallyHidden, false);
		}

		CGRect CropRect () {
			CGRect rect = RectangleF.Empty;
			nfloat scale = ((nfloat) 1.0) / ScrollView.ZoomScale;

			rect.X = ScrollView.ContentOffset.X * scale;
			rect.Y = ScrollView.ContentOffset.Y * scale;
			rect.Width = ScrollView.Bounds.Width * scale;
			rect.Height = ScrollView.Bounds.Height * scale;

			var imageSize = ImageToCrop.Size;
			nfloat x = RectangleFExtensions.GetMinX(rect);
			nfloat y = RectangleFExtensions.GetMinY(rect);
			nfloat width = rect.Width;
			nfloat height = rect.Height;

			UIImageOrientation orientation = ImageToCrop.Orientation;
			if (orientation == UIImageOrientation.Right || orientation == UIImageOrientation.RightMirrored) {
				rect.X = y;
				rect.Y = imageSize.Width - rect.Width - x;
				rect.Width = height;
				rect.Height = width;
			} else if (orientation == UIImageOrientation.Left || orientation == UIImageOrientation.LeftMirrored) {
				rect.X = imageSize.Height - rect.Height - y;
				rect.Y = x;
				rect.Width = height;
				rect.Height = width;
			} else if (orientation == UIImageOrientation.Down || orientation == UIImageOrientation.DownMirrored) {
				rect.X = imageSize.Width - rect.Width - x;
				rect.Y = imageSize.Height - rect.Height - y;
			}

			return rect;
		}
			
		UIImage CropImage () {
			UIImage image = null;
			using (CGImage cgImage = this.ImageToCrop.CGImage.WithImageInRect (CropRect ())) {
				image = UIImage.FromImage (cgImage, 1.0f, this.ImageToCrop.Orientation);
			}

			return image;
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}
	}
}
