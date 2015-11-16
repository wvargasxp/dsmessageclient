using CoreGraphics;
using em;
using UIKit;

namespace iOS
{
	public sealed class ProfileThumbnailView : UIView {

		public UIImageView BackgroundImageView;
		public UIImageView ThumbnailImageView;
		public UIImage Image {
			get {
				return ThumbnailImageView.Image;
			}

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

		public ProfileThumbnailView () {
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

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			CGRect frame = Bounds;
			BackgroundImageView.Frame = frame;

			frame.Size = new CGSize (50, 50);
			frame.Location = new CGPoint (5.5f, 5f);
			ThumbnailImageView.Frame = frame;
		}
	}
}