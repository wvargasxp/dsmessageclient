using CoreGraphics;
using em;
using UIKit;

namespace iOS {
	public class IncomingThumbnailView : BasicThumbnailView {

		UIButton backgroundImageButton;
		public UIButton BackgroundImageButton {
			get { return backgroundImageButton; }
			set { backgroundImageButton = value; }
		}

		UIImageView thumbnailImageView;
		public UIImageView ThumbnailImageView {
			get { return thumbnailImageView; }
			set { thumbnailImageView = value; }
		}

		public UIImage Image {
			get { return thumbnailImageView.Image; }
			set {
				thumbnailImageView.Image = value;
				SetNeedsLayout ();
			}
		}

		public IncomingThumbnailView () {
			backgroundImageButton = new UIButton (Bounds);
			backgroundImageButton.Tag = 0x21;
			backgroundImageButton.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			AddSubview (backgroundImageButton);

			thumbnailImageView = new UIImageView (Bounds);
			thumbnailImageView.Tag = 0x22;
			thumbnailImageView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
			thumbnailImageView.Layer.CornerRadius = 19f;
			thumbnailImageView.Layer.MasksToBounds = true;
			AddSubview (thumbnailImageView);
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			CGRect frame = Bounds;
			backgroundImageButton.Frame = frame;

			frame.Size = new CGSize (38, 38);
			frame.Location = new CGPoint (5.5f, 4.5f);
			thumbnailImageView.Frame = frame;
		}

		public void SetFromContact(Contact contact) {
			contact.colorTheme.GetPhotoFrameLeftResource ((UIImage image) => {
				if (backgroundImageButton != null) {
					backgroundImageButton.SetBackgroundImage (image, UIControlState.Normal);
				}
			});
		}
			
		#region basic thumbnail view implementation 
		public override CGPoint ProgressIndicatorLocation {
			get {
				return new CGPoint (4, 6);
			}
		}

		public override void UpdateVisibility (bool showThumbnail) {
			if (showThumbnail) {
				this.ThumbnailImageView.Alpha = 1;
				this.BackgroundImageButton.Alpha = 1;
			} else {
				this.ThumbnailImageView.Alpha = 0;
				this.BackgroundImageButton.Alpha = 0;
			}
		}
		#endregion

	}
}