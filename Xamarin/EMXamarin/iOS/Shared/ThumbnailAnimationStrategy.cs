using System;
using UIKit;

namespace iOS {
	public interface ThumbnailAnimationStrategy {
		void AnimateThumbnail (UIImage image, bool animated);
	}
}

