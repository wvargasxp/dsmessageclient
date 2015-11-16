using System;
using em;
using UIKit;
using Media_iOS_Extension;
using CoreGraphics;
using Foundation;
using UIDevice_Extension;

namespace iOS {
	public class ShowPhotoViewController : PhotoVideoItemController {

		private ZoomingImageView zoomingImageView;
		public ZoomingImageView ZoomingImageView {
			get {
				return zoomingImageView;
			}

			set {
				zoomingImageView = value;
			}
		}

		private UIImage Image { get; set; }

		public ShowPhotoViewController (int index, Message message) : base (index, message) {}

		#region ViewWill
		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			this.ZoomingImageView = new ZoomingImageView (new CGRect (0, 0, this.View.Frame.Width, this.View.Frame.Height));
			this.ZoomingImageView.Scrolled += WeakDelegateProxy.CreateProxy<object, EventArgs>(ScrollViewDidScroll).HandleEvent<object, EventArgs>;
			this.ZoomingImageView.ViewForZoomingInScrollView += ViewForZoomingInScrollView;
			this.ZoomingImageView.ZoomingEnded += WeakDelegateProxy.CreateProxy<object, ZoomingEndedEventArgs>(ScrollViewDidEndZooming).HandleEvent<object, ZoomingEndedEventArgs>;
			this.ZoomingImageView.DidZoom += WeakDelegateProxy.CreateProxy<object, EventArgs>(ScrollViewDidZoom).HandleEvent<object, EventArgs>;
			this.View.Add (this.ZoomingImageView);

			LoadImageIntoZoomingView ();
		}

		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews ();
			nfloat displacement_y = this.TopLayoutGuide.Length;
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			LoadImageIntoZoomingView ();
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.ZoomingImageView.ViewForZoomingInScrollView -= ViewForZoomingInScrollView;
		}

		#endregion

		private void LoadImageIntoZoomingView () {
			if (this.OkayToSetupMedia) {
				if (this.Image == null) {
					this.Image = this.Message.media.LoadFullSizeImage ();

					if (this.Image != null) {
						this.ZoomingImageView.SetImage (this.Image);
					}
				}

				this.OkayToSetupMedia = false;
			}
		}

		#region rotation
		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			CGRect frame = this.View.Frame;
			this.ZoomingImageView.Frame = new CGRect (0, 0, frame.Width, frame.Height);
			this.ZoomingImageView.DoRotate (toInterfaceOrientation);
		}

		public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation) {
			base.DidRotate (fromInterfaceOrientation);
		}
		#endregion


		#region scroll

		private void ScrollViewDidZoom (object sender, EventArgs e) {
			UIScrollView scrollView = (UIScrollView)sender;
			scrollView.SetNeedsLayout ();
			scrollView.LayoutIfNeeded ();
		}

		private void ScrollViewDidScroll (object sender, EventArgs e) {}

		private UIImageView ViewForZoomingInScrollView (UIScrollView sv) {
			UIImageView imageView = sv.Subviews [0] as UIImageView;
			return imageView; // possibly null;
		}

		private void ScrollViewDidEndZooming (object sender, ZoomingEndedEventArgs args) {
			ZoomingImageView zoomingView = sender as ZoomingImageView;
			if (zoomingView != null) {
				bool scrollingEnabled = args.AtScale != zoomingView.baseZoom;
				zoomingView.ScrollIsEnabled = scrollingEnabled;
			}
		}

		[Export("_finishScrollingSelector")]
		private void ScrollViewFinishedScrolling () {
			//NSObject.CancelPreviousPerformRequest (this);
		}
		#endregion

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		#region overriding downloaded
		public override void OnStartDownload (Message message) {
			
		}

		public override void OnPercentedDownload (Message message, double percentComplete) {}

		public override void OnDownloadedMedia (Message message) {
			LoadImageIntoZoomingView ();
		}
		#endregion
	}
}

