using System;
using System.Collections.Generic;
using System.Diagnostics;
using UIKit;

namespace iOS{
	public class QueuedThumbnailAnimationStrategy : ThumbnailAnimationStrategy {

		private UIView uiView;
		private UIImageView thumbnailView;

		private Queue<SetThumbnailAction> animationQueue = new Queue<SetThumbnailAction> ();

		private bool isAnimating = false;

		/**
		 * @param uiView			this UIView's SetNeedsLayout() is called when we update the thumbnailView.
		 *                      	Typically, this is the UIView holding the thumbnailView
		 * @param thumbnailView 	the thumbnail to animate
		 */
		public QueuedThumbnailAnimationStrategy (UIView uiView, UIImageView thumbnailView) {
			this.uiView = uiView;
			this.thumbnailView = thumbnailView;
		}

		public void AnimateThumbnail(UIImage image, bool animated) {
			animationQueue.Enqueue (new SetThumbnailAction (image, animated));

			AnimateThumbnail ();
		}

		private void AnimateThumbnail () {
			if (isAnimating)
				return;

			if (animationQueue.Count == 0)
				return;

			SetThumbnailAction thumbnailToAnimate = null;
			while (animationQueue.Count > 0) {
				thumbnailToAnimate = animationQueue.Dequeue ();
			}

			isAnimating = true;

			UIImage image = thumbnailToAnimate.Image;
			bool animated = thumbnailToAnimate.Animated;

			if (!animated) {
				thumbnailView.Image = image;
				uiView.SetNeedsLayout ();
				isAnimating = false;
			} else {
				UIView.Animate(0.2,
					() => {
						thumbnailView.Alpha = 0;
					},
					() => {
						thumbnailView.Image = image;
						uiView.SetNeedsLayout ();
						UIView.Animate(0.2,
							() => {
								thumbnailView.Alpha = 1;
							},
							() => {
								isAnimating = false;
								AnimateThumbnail ();
							});
					});
			}
		}

		private class SetThumbnailAction {

			private UIImage image;

			private bool animated;

			public SetThumbnailAction (UIImage image, bool animated) {
				this.image = image;
				this.animated = animated;
			}

			public bool Animated {
				get {
					return animated;
				}
			}

			public UIImage Image {
				get {
					return image;
				}
			}
		}

	}
}

