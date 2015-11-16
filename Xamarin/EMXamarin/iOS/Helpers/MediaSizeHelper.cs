using System;
using CoreGraphics;
using em;

namespace iOS {
	public class MediaSizeHelper {
		public MediaSizeHelper () {}

		private static MediaSizeHelper instance;
		public static MediaSizeHelper SharedInstance {
			get {
				if (instance == null) {
					instance = new MediaSizeHelper ();
				}

				return instance;
			}
		}

		public CGSize ThumbnailSizeForHeightToWidth (float heightToWidth) {
			CGSize mediaSize;
			if (heightToWidth < 0.01) {
				// zero == not provided
				mediaSize = new CGSize (90, 90);
			} else if (heightToWidth > 1) {
				// taller than it is wide
				mediaSize = new CGSize ((int)(Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT / heightToWidth), Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT);
			} else {
				mediaSize = new CGSize (Constants.LANDSCAPE_CHAT_THUMBNAIL_WIDTH, (int)(Constants.LANDSCAPE_CHAT_THUMBNAIL_WIDTH * heightToWidth));
			}

			return mediaSize;
		}
	}
}

