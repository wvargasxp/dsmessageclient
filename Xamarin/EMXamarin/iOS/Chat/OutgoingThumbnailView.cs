using System.Diagnostics;
using CoreGraphics;
using em;
using UIKit;

namespace iOS {
	public class OutgoingThumbnailView : BasicThumbnailView {

		UIImageView backgroundImageView;
		public UIImageView BackgroundImageView {
			get { return backgroundImageView; }
			set { backgroundImageView = value; }
		}

		UIImageView thumbnailImageView;
		public UIImageView ThumbnailImageView {
			get { return thumbnailImageView; }
			set { thumbnailImageView = value; }
		}

		public UIImage Image {
			get { return ThumbnailImageView.Image; }
			set {
				ThumbnailImageView.Image = value;
				SetNeedsLayout ();
			}
		}

		UIActivityIndicatorView spinner;
		protected UIActivityIndicatorView Spinner {
			get { return spinner; }
			set { spinner = value; }
		}

		public OutgoingThumbnailView () {
			backgroundImageView = new UIImageView (Bounds);
			backgroundImageView.Tag = 0x21;
			backgroundImageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			AddSubview (backgroundImageView);

			ThumbnailImageView = new UIImageView (Bounds);
			ThumbnailImageView.Tag = 0x22;
			ThumbnailImageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			ThumbnailImageView.Layer.CornerRadius = 19f;
			ThumbnailImageView.Layer.MasksToBounds = true;
			AddSubview (ThumbnailImageView);
		}

		public void CreatePhotoFrame (BackgroundColor c) {
			Debug.Assert (backgroundImageView != null, "**OutgoingThumbnailView: CreatePhotoFrame: backgroundImageView is null");
			c.GetPhotoFrameRightResource ((UIImage image) => {
				if (backgroundImageView != null) {
					backgroundImageView.Image = image;
				}
			});
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			CGRect frame = Bounds;
			backgroundImageView.Frame = frame;

			frame.Size = new CGSize (38, 38);
			frame.Location = new CGPoint (13,3.5f);
			ThumbnailImageView.Frame = frame;
		}

		#region basic thumbnail view implementation 
		public override CGPoint ProgressIndicatorLocation {
			get {
				return new CGPoint (13, 4);
			}
		}

		public override void UpdateVisibility (bool showThumbnail) {
			if (showThumbnail) {
				this.ThumbnailImageView.Alpha = 1;
				this.BackgroundImageView.Alpha = 1;
			} else {
				this.ThumbnailImageView.Alpha = 0;
				this.BackgroundImageView.Alpha = 0;
			}
		}
		#endregion
	}
}