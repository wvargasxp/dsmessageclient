using System;

namespace em {
	public class BingSearchImagePhoto : AbstractSearchImage {

		public override string UrlOfImageAsString () {
			return this.imageItem.MediaUrl;
		}

		public override string UrlOfThumbnailAsString () {
			return this.imageItem.Thumbnail.MediaUrl;
		}

		BingImageResult imageItem;
		protected BingImageResult ImageItem {
			get { return imageItem; }
			set { imageItem = value; }
		}

		public BingSearchImagePhoto (BingImageResult input, EMAccount eAccount) {
			this.ImageItem = input;
			this.Account = eAccount;
		}
	}
}

