using System;
using CoreGraphics;
using UIKit;
using UIDevice_Extension;

namespace iOS {
	
	public class DHImageScrollView : UIScrollView {

		DHScrollViewDelegate scrollViewDelegate;

		CGSize imageSize;

		CGPoint pointToCenterAfterResize;
		nfloat scaleToRestoreAfterResize;

		UIImage image;

		public UIImageView ZoomView { get; set; }

		public DHImageScrollView (CGRect frame, UIImage img, nfloat maxZoomScale) : base (frame) {
			scrollViewDelegate = new DHScrollViewDelegate (this);
			InitWithFrame (frame);
			image = img;
			imageSize = image.Size;
			this.MaximumZoomScale = maxZoomScale;

			DisplayImage (image);
		}

		public DHImageScrollView (CGRect frame) {
			InitWithFrame (frame);
		}

		void InitWithFrame (CGRect frame) {
			base.Frame = frame;

			this.ShowsVerticalScrollIndicator = false;
			this.ShowsHorizontalScrollIndicator = false;
			this.BouncesZoom = true;
			this.DecelerationRate = UIScrollView.DecelerationRateFast;
			this.Delegate = scrollViewDelegate;
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();

			// ;; - note - james
			// ;; - centering code found here - https://stackoverflow.com/questions/794294/how-do-i-center-a-uiimageview-within-a-full-screen-uiscrollview
			// ;; - this code only fills our needs by 90%, it doesn't center the imageview within the context of the screen itself
			// center the zoom view as it becomes smaller than the size of the screen
			CGSize boundsSize = this.Bounds.Size;
			CGRect frameToCenter = ZoomView.Frame;
			// center horizontally
			if (frameToCenter.Size.Width < boundsSize.Width) 
				frameToCenter.X = (boundsSize.Width - frameToCenter.Size.Width) / 2f;
			else
				frameToCenter.X = 0f;

			// center vertically
			if (frameToCenter.Size.Height < boundsSize.Height)
				frameToCenter.Y = (boundsSize.Height - frameToCenter.Size.Height) / 2f;
			else
				frameToCenter.Y = 0f;

			ZoomView.Frame = frameToCenter;
		}

		public void SetFrame (CGRect frame) {
			var sizeChanging = !frame.Size.Equals (this.Frame.Size);

			if (sizeChanging)
				PrepareToResize ();

			base.Frame = frame; //[super setFrame:frame];

			if (sizeChanging)
				RecoverFromResizing ();
		}

		public void DisplayImage (UIImage img) {
			image = img;
			imageSize = image.Size;

			// clear the previous image
			if (ZoomView != null) {
				ZoomView.RemoveFromSuperview ();
				ZoomView = null;
			}

			// reset our zoomScale to 1.0 before doing any further calculations
			this.ZoomScale = 1.0f;

			// make a new UIImageView for the new image
			ZoomView = new UIImageView (image);
			this.AddSubview (ZoomView);

			ConfigureForImageSize (image.Size);
		}

		public void ConfigureForImageSize (CGSize imgSize) {
			imageSize = imgSize;
			ContentSize = imageSize;

			SetMaxMinZoomScalesForCurrentBounds ();
			ZoomScale = InitialZoomForImage ();
			CenterImageWithinScreen (imageSize);
		}

		public void CenterImageWithinScreen (CGSize imageSize) {
			// ;; - 
			// Because the scrollview's bounds is smaller than the screen and it is centered within the screen.
			// An image set at origin 0 will not be centered within the screen.
			// This only matters when the image's height is larger than the scrollview's bounds.
			// Calculate the difference in height and half of it is the offset we'd want initially.
			// ;; -

			nfloat height = imageSize.Height * this.ZoomScale; // We need the scaled image's height, not the actual height for measurement.
			nfloat width = imageSize.Width * this.ZoomScale; // ^
			if (height > this.Bounds.Height) {
				nfloat differenceInHeight = height - this.Bounds.Height;
				nfloat offsetY = (differenceInHeight / 2);
				this.ContentOffset = new CGPoint (0, offsetY);
			}
				
			if (width > this.Bounds.Width) {
				nfloat differenceInWidth = width - this.Bounds.Width;
				nfloat offsetX = (differenceInWidth / 2);
				this.ContentOffset = new CGPoint (offsetX, this.ContentOffset.Y);
			}
		}

		public nfloat InitialZoomForImage () {
			CGSize size = ScreenSizeAccordingToOrientation ();
			// We're using the bounds of the scroll view to determine the width to scale but the screen's height to determine the scale for height.
			// Reasoning is that we might be disabling rotations to landscape (meaning the scale doesn't have to be too precise.)
			// 2 is that we usually want to scale the image to the screen's height and not to its width.
			nfloat xScale = this.Bounds.Size.Width / imageSize.Width;
			nfloat yScale = size.Height / imageSize.Height;
			nfloat scale = NMath.Max (xScale, yScale);

			ZoomScale = scale;
			return ZoomScale;
		}

		public CGSize ScreenSizeAccordingToOrientation () {
			return AppDelegate.Instance.MainController.ScreenSizeAccordingToOrientation;
		}
			
		public void SetMaxMinZoomScalesForCurrentBounds () {
			CGSize boundsSize = Bounds.Size;

			// calculate min/max zoomscale
			nfloat xScale = boundsSize.Width  / imageSize.Width;    // the scale needed to perfectly fit the image width-wise
			nfloat yScale = boundsSize.Height / imageSize.Height;   // the scale needed to perfectly fit the image height-wise

			// fill width if the image and phone are both portrait or both landscape; otherwise take smaller scale
			var imagePortrait = imageSize.Height > imageSize.Width;
			var phonePortrait = boundsSize.Height > boundsSize.Width;
			nfloat minScale = imagePortrait == phonePortrait ? xScale : NMath.Min (xScale, yScale);

			nfloat maxScale = NMath.Max(this.MaximumZoomScale, NMath.Max(xScale, yScale));

			// don't let minScale exceed maxScale. (If the image is smaller than the screen, we don't want to force it to be zoomed.)
			if (minScale > maxScale)
				minScale = maxScale;

			MaximumZoomScale = maxScale;
			MinimumZoomScale = minScale;
		}

		#region rotation support

		public void PrepareToResize () {
			var boundsCenter = new CGPoint(this.Bounds.GetMidX(), this.Bounds.GetMidY());
			pointToCenterAfterResize =  this.ConvertPointFromView (boundsCenter, ZoomView); //[self convertPoint:boundsCenter toView:_zoomView];

			scaleToRestoreAfterResize = ZoomScale;

			// If we're at the minimum zoom scale, preserve that by returning 0, which will be converted to the minimum
			// allowable scale when the scale is restored.
			if (scaleToRestoreAfterResize <= this.MinimumZoomScale + nfloat.Epsilon)
				scaleToRestoreAfterResize = 0;
		}

		public void RecoverFromResizing () {
			this.SetMaxMinZoomScalesForCurrentBounds ();

			// Step 1: restore zoom scale, first making sure it is within the allowable range.
			nfloat maxZoomScale = NMath.Max(this.MinimumZoomScale, scaleToRestoreAfterResize);
			this.ZoomScale = NMath.Min (this.MaximumZoomScale, maxZoomScale);

			// Step 2: restore center point, first making sure it is within the allowable range.

			// 2a: convert our desired center point back to our own coordinate space
			CGPoint boundsCenter = this.ConvertPointFromView (pointToCenterAfterResize, ZoomView); //[self convertPoint:_pointToCenterAfterResize fromView:_zoomView];

			// 2b: calculate the content offset that would yield that center point
			var offset = new CGPoint(boundsCenter.X - this.Bounds.Size.Width / 2.0f, boundsCenter.Y - this.Bounds.Size.Height / 2.0f);

			// 2c: restore offset, adjusted to be within the allowable range
			CGPoint maxOffset = MaximumContentOffset();
			CGPoint minOffset = MinimumContentOffset();

			nfloat realMaxOffset = NMath.Min(maxOffset.X, offset.X);
			offset.X = NMath.Max(minOffset.X, realMaxOffset);

			realMaxOffset = NMath.Min(maxOffset.Y, offset.Y);
			offset.Y = NMath.Max(minOffset.Y, realMaxOffset);

			this.ContentOffset = offset;
		}

		public CGPoint MaximumContentOffset () {
			CGSize contentSize = this.ContentSize;
			CGSize boundsSize = this.Bounds.Size;

			return new CGPoint (contentSize.Width - boundsSize.Width, contentSize.Height - boundsSize.Height);
		}

		public CGPoint MinimumContentOffset () {
			return CGPoint.Empty;
		}
			
		public override UIView HitTest (CGPoint point, UIEvent e) {
			// So we can pan from the overlay view
			return this;
		}

		#endregion

		class DHScrollViewDelegate : UIScrollViewDelegate {
			readonly DHImageScrollView view;
			protected DHImageScrollView View {
				get { return view; }
			}

			public DHScrollViewDelegate (DHImageScrollView v) {
				view = v;
			}

			public override UIView ViewForZoomingInScrollView (UIScrollView scrollView) {
				return this.View.ZoomView;
			}
		}
	}
}