using System;
using UIKit;
using Foundation;
using CoreGraphics;
using em;
using System.Diagnostics;

namespace iOS {
	using UIImageExtensions;
	public class ImageSearchCollectionViewCell : UICollectionViewCell {
		public ImageSearchCollectionViewCell () {}

		UIImageView imageView;
		public UIImageView ImageView {
			get { return imageView; }
			set { imageView = value; }
		}

		int position;
		public int Position {
			get { return position; }
			set { position = value; }
		}

		AbstractSearchImage abImage;
		UIActivityIndicatorView spinner;

		public void SetAbstractSearchImage (int position, AbstractSearchImage sI) {
			abImage = sI;
			UIImage cachedImage = ImageSetter.SearchImageForKey (abImage.ThumbnailKeyForCache);
			if (cachedImage != null)
				imageView.Image = cachedImage;
			else {
				imageView.Image = null;
				imageView.BackgroundColor = UIColor.Clear;
				imageView.SetNeedsDisplay ();

				CGRect spinnerRect = new CGRect (imageView.Frame.Size.Width / 2 - 50 / 2, imageView.Frame.Size.Height / 2 - 50 / 2, 50, 50);
				if (spinner == null) {
					spinner = new UIActivityIndicatorView (spinnerRect);
					spinner.TintColor = UIColor.Blue;
					imageView.AddSubview (spinner);
				}

				spinner.Frame = spinnerRect;

				if (!spinner.IsAnimating)
					spinner.StartAnimating ();

				abImage.GetThumbnailAsBytesAsync (position, (int originalPosition, byte[] loadedImage) => {
					EMTask.DispatchMain (() => {
						if (spinner != null && spinner.IsAnimating) {
							spinner.StopAnimating ();
						}
					});

					if (loadedImage != null) {
						UIImage image = UIImageExtension.ByteArrayToImage (loadedImage);
						if (image == null)
							ImageLoadError ();
						else {
							ImageSetter.AddSearchImageToCache (abImage.ThumbnailKeyForCache, image);
							EMTask.DispatchMain (() => {
								if (originalPosition == this.Position) {
									imageView.Image = image;
								}
							});
						}
					} else {
						ImageLoadError ();
					}
				});
			}
		}

		public void ImageLoadError () {
			// TODO: failed to retrieve image; todo maybe blank out the image
			EMTask.DispatchMain (() => {
				if (imageView != null) {
					imageView.Alpha = .5f;
				}
			});
		}

		[Export ("initWithFrame:")]
		public ImageSearchCollectionViewCell (CGRect frame) : base (frame) {
			this.BackgroundView = new UIView { BackgroundColor = UIColor.Clear };

			this.SelectedBackgroundView = new UIView{ BackgroundColor = UIColor.Clear };

			this.ContentView.BackgroundColor = UIColor.Clear;

			CGRect cFrame = this.ContentView.Frame;
			int cFrameSize = (int)cFrame.Width;
			int size = (int)cFrame.Width - 10;
			imageView = new UIImageView (new CGRect ((cFrameSize - size) / 2, (cFrameSize - size) / 2, size, size));
			imageView.Center = ContentView.Center;
			imageView.ContentMode = UIViewContentMode.ScaleAspectFit;
			imageView.BackgroundColor = UIColor.Clear;

			this.ContentView.AddSubview (imageView);
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();
			CGRect cFrame = this.ContentView.Frame;
			CGRect sBViewFrame = this.SelectedBackgroundView.Frame;
			CGRect bVFrame = this.BackgroundView.Frame;

			int cFrameSize = (int)cFrame.Width;
			int size = (int)cFrame.Width - 10;
			imageView.Frame = new CGRect ((cFrameSize - size) / 2, (cFrameSize - size) / 2, size, size);
		}

	}
}

