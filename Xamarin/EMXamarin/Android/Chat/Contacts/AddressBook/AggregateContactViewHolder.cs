using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class AggregateContactViewHolder : EMBitmapViewHolder {

		ImageView photoFrame;
		ImageView thumbnailView;
		TextView descriptionTextView;
		ImageView sendButton;
		View colorTrimView;
		ProgressBar progressbar;

		public ImageView PhotoFrame {
			get { return photoFrame; }
			set { photoFrame = value; }
		}

		public ImageView ThumbnailView {
			get { return thumbnailView; }
			set { thumbnailView = value; }
		}

		public TextView DescriptionTextView {
			get { return descriptionTextView; }
			set { descriptionTextView = value; }
		}

		public ImageView SendButton {
			get { return sendButton; }
			set { sendButton = value; }
		}

		public View ColorTrimView {
			get { return colorTrimView; }
			set { colorTrimView = value; }
		}

		public ProgressBar ProgressBar {
			get { return progressbar; }
			set { progressbar = value; }
		}

		Contact c;
		public Contact C {
			get { return c; }
			set {
				c = value;
				if (c != null) {
					descriptionTextView.Text = string.IsNullOrEmpty (c.label) ? c.description : string.Format ("{0} ({1})", c.description, c.label);
					descriptionTextView.Typeface = FontHelper.DefaultFont;
					descriptionTextView.SetTextColor (c.colorTheme.GetColor ());
					c.colorTheme.GetPhotoFrameLeftResource ((string file) => {
						if (photoFrame != null && EMApplication.Instance != null) {
							BitmapSetter.SetBackgroundFromFile (photoFrame, EMApplication.Instance.Resources, file);
						}
					});
					c.colorTheme.GetChatSendButtonResource ((string filepath) => {
						if (SendButton != null) {
							BitmapSetter.SetBackgroundFromFile (SendButton, EMApplication.Instance.Resources, filepath);
						}
					});

					ColorTrimView.SetBackgroundColor (c.colorTheme.GetColor());
				}
			}
		}

		public void PossibleShowProgressIndicator (CounterParty c) {
			if (BitmapSetter.ShouldShowProgressIndicator (c)) {
				this.ProgressBar.Visibility = ViewStates.Visible;
				this.PhotoFrame.Visibility = ViewStates.Invisible;
				this.ThumbnailView.Visibility = ViewStates.Invisible;
			} else {
				this.ProgressBar.Visibility = ViewStates.Gone;
				this.PhotoFrame.Visibility = ViewStates.Visible;
				this.ThumbnailView.Visibility = ViewStates.Visible;
			}
		}
	}
}