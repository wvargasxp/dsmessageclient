using System;
using UIKit;
using CoreGraphics;
using em;

namespace iOS {
	public class InboxThumbnailView : BasicThumbnailView {
		UIImageView backgroundImageView;
		public UIImageView BackgroundImageView {
			get { return backgroundImageView; }
			set { backgroundImageView = value; }
		}

		public UIImageView ThumbnailImageView;
		public UIImage Image {
			get { return ThumbnailImageView.Image; }
			set {
				ThumbnailImageView.Image = value;
				SetNeedsLayout ();
			}
		}
		public BackgroundColor colorTheme {
			set {
				value.GetPhotoFrameLeftResource ((UIImage image) => {
					if (backgroundImageView != null) {
						backgroundImageView.Image = image;
					}
				});
			}
		}

		public InboxThumbnailView () {
			backgroundImageView = new UIImageView (Bounds);
			backgroundImageView.Tag = 0x21;
			backgroundImageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			AddSubview (backgroundImageView);

			ThumbnailImageView = new UIImageView (Bounds);
			ThumbnailImageView.Tag = 0x22;
			ThumbnailImageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			ThumbnailImageView.Layer.CornerRadius = 25f;
			ThumbnailImageView.Layer.MasksToBounds = true;
			AddSubview (ThumbnailImageView);
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			CGRect frame = Bounds;
			backgroundImageView.Frame = frame;

			frame.Size = new CGSize (50, 50);
			frame.Location = new CGPoint (5.5f, 5f);
			ThumbnailImageView.Frame = frame;
		}

		#region basic thumbnail view implementation 
		public override CGPoint ProgressIndicatorLocation {
			get {
				return new CGPoint (9, 9);
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

