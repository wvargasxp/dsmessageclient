using System;
using Android.Views;
using Android.Widget;
using Android.Content;
using Com.Ortiz.Touch;
using Android.Util;

namespace Emdroid {
	public class GalleryItemHolder : EMBitmapViewHolder {
		private ProgressBar progressBar;
		private TouchImageView image;
		private VideoView video;
		private RelativeLayout videoLayout;
		private ImageButton playButton;

		public ProgressBar ProgressBar {
			get { return progressBar; }
			set { progressBar = value; }
		}

		public TouchImageView Image {
			get { return image; }
			set { image = value; }
		}

		public VideoView Video {
			get { return video; }
			set { video = value; }
		}

		public RelativeLayout VideoLayout {
			get { return videoLayout; }
			set { videoLayout = value; }
		}

		public ImageButton PlayButton {
			get { return playButton; }
			set { playButton = value; }
		}

		public GalleryItemHolder () {

		}
	}
}

