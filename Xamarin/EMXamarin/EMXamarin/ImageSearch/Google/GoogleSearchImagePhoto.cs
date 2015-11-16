using System;

namespace em {
	public class GoogleSearchImagePhoto : AbstractSearchImage {

		public override string UrlOfImageAsString () {
			return this.imageItem.Link;
		}
			
		public override string UrlOfThumbnailAsString () {
			return this.ImageItem.Image.ThumbnailLink;
		}

		GoogleImageItem imageItem;
		protected GoogleImageItem ImageItem {
			get { return imageItem; }
			set { imageItem = value; }
		}

		public GoogleSearchImagePhoto (GoogleImageItem input, EMAccount eAccount) {
			this.ImageItem = input;
			this.Account = eAccount;
		}
	}
}

