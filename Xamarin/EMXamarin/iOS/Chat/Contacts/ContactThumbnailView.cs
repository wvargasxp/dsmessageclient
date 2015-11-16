using CoreGraphics;
using em;
using UIKit;

// Basically copied and pasted from GroupThumbnailView.

namespace iOS {
	public class ContactThumbnailView : BasicThumbnailView {

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
		public BackgroundColor ColorTheme {
			set {
				value.GetPhotoFrameLeftResource ((UIImage image) => {
					if (BackgroundImageView != null) {
						BackgroundImageView.Image = image;
					}	
				});
			}
		}

		public ContactThumbnailView () {
			BackgroundImageView = new UIImageView (Bounds);
			BackgroundImageView.Tag = 0x21;
			BackgroundImageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			AddSubview (BackgroundImageView);

			ThumbnailImageView = new UIImageView (Bounds);
			ThumbnailImageView.Tag = 0x22;
			ThumbnailImageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			ThumbnailImageView.Layer.CornerRadius = 25f;
			ThumbnailImageView.Layer.MasksToBounds = true;
			AddSubview (ThumbnailImageView);
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();

			CGRect frame = Bounds;
			BackgroundImageView.Frame = frame;

			frame.Size = new CGSize (50, 50);
			frame.Location = new CGPoint (5.5f, 5f);
			ThumbnailImageView.Frame = frame;
		}

		#region basic thumbnail view implementation 
		public override CGPoint ProgressIndicatorLocation {
			get {
				return new CGPoint (8, 8);
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