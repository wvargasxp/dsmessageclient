using System;
using Foundation;
using UIKit;
using CoreGraphics;
using System.Diagnostics;

namespace iOS {
	using UIDevice_Extension;
	public class ZoomingImageView : UIScrollView {
		private UIImageView imageView;
		public nfloat baseZoom;
		nfloat MIN_VALUE = 2.5f;
		nfloat MAX_ZOOM_SCALE = 3.5f;
		nfloat IPAD_SCALE = 0.65f;

		public bool ScrollIsEnabled {
			get {
				return this.ScrollEnabled;
			}
			set {
				this.ScrollEnabled = value;
			}
		}

		public ZoomingImageView (CGRect frame) : base (frame) {
			imageView = null;
			baseZoom = 1.0f;
			this.BackgroundColor = iOS_Constants.BLACK_COLOR;
			this.MaximumZoomScale = MAX_ZOOM_SCALE;
			this.ShowsVerticalScrollIndicator = false;
			this.ShowsHorizontalScrollIndicator = false;
			this.ScrollIsEnabled = false;
		}

		public void SetImage (UIImage image) {
			if (imageView != null)
				imageView.RemoveFromSuperview ();

			imageView = new UIImageView (image);
			this.Add (imageView);

			CGSize size = imageView.Bounds.Size;
			nfloat heightToWidth = size.Height / size.Width;

			CGRect bounds = this.Bounds;
			nfloat wrapperHeightToWidth = bounds.Size.Height / bounds.Size.Width;

			UIEdgeInsets insets = new UIEdgeInsets (0, 0, 0, 0);

			if (heightToWidth > wrapperHeightToWidth) {
				this.baseZoom = (nfloat)Math.Min (MIN_VALUE, bounds.Size.Height / size.Height);
				size.Height = (nfloat)Math.Min (MIN_VALUE * size.Height, bounds.Size.Height);
				size.Width = size.Height / heightToWidth;
			} else if (heightToWidth < wrapperHeightToWidth) {
				this.baseZoom = (nfloat)Math.Min (MIN_VALUE, bounds.Size.Width / size.Width);
				size.Width = (nfloat)Math.Min (MIN_VALUE * size.Width, bounds.Size.Width);
				size.Height = size.Width * heightToWidth;
			} else if (heightToWidth == wrapperHeightToWidth) {
				this.baseZoom = (nfloat)Math.Min (MIN_VALUE, bounds.Size.Height / size.Height);
				size.Width = (nfloat)Math.Min (MIN_VALUE * size.Width, bounds.Size.Width);
				size.Height = (nfloat)Math.Min (MIN_VALUE * size.Height, bounds.Size.Height);
			}

			// Adjust the image so that the initial size is smaller
			if (UIDevice.CurrentDevice.IsPad ()) {
				this.baseZoom *= IPAD_SCALE;
				size.Width *= IPAD_SCALE;
				size.Height *= IPAD_SCALE;
			}

			insets.Top = (bounds.Size.Height - size.Height) / 2;
			insets.Left = (bounds.Size.Width - size.Width) / 2;
			this.MinimumZoomScale = this.baseZoom;
			this.ZoomScale = this.baseZoom;
			this.ContentInset = insets;
		}

		public void DoRotate (UIInterfaceOrientation orientation) {
			if (imageView == null)
				return;

			CGSize size = imageView.Bounds.Size;
			nfloat heightToWidth = size.Height / size.Width;

			CGRect bounds = this.Bounds;
			nfloat wrapperHeightToWidth = bounds.Height / bounds.Width;

			UIEdgeInsets insets = new UIEdgeInsets (0, 0, 0, 0);

			if ( heightToWidth > wrapperHeightToWidth ) {
				this.baseZoom = bounds.Height / size.Height;
				// letter box width
				size.Height = bounds.Height;
				size.Width = size.Height / heightToWidth;
			} else if ( heightToWidth < wrapperHeightToWidth ) {
				this.baseZoom = bounds.Width / size.Width;
				// letter box height
				size.Width = bounds.Width;
				size.Height = size.Width * heightToWidth;
			} else if ( heightToWidth == wrapperHeightToWidth ) {
				this.baseZoom = bounds.Height / size.Height;
				size.Width = bounds.Width;
				size.Height = bounds.Height;
			}

			if (UIDevice.CurrentDevice.IsPad ()) {
				this.baseZoom *= IPAD_SCALE;
				size.Width *= IPAD_SCALE;
				size.Height *= IPAD_SCALE;
			}

			insets.Top = (bounds.Height - size.Height) / 2;
			insets.Left = (bounds.Width - size.Width) / 2;

			this.MinimumZoomScale = this.baseZoom;
			this.ZoomScale = this.baseZoom;
			this.ContentInset = insets;
		}
	}
}

