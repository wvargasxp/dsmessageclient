using System;
using Android.Widget;
using Android.Views;

namespace Emdroid {
	public class ImageSearchItemViewHolder : EMBitmapViewHolder {
		RelativeLayout imageWrapper;

		public RelativeLayout ImageWrapper {
			get { return imageWrapper; }
			set { imageWrapper = value; }
		}

		ImageView mediaView;
		public ImageView MediaView {
			get { return mediaView; }
			set { mediaView = value; }
		}

		public ImageSearchItemViewHolder () {
		}
	}
}

