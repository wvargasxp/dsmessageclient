using System;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using em;
using EMXamarin;
using Android.App;

namespace Emdroid {
	public abstract class BasicAccountFragment : AbstractAcquiresImagesFragment {

		bool uiInPendingState;
		public bool PendingState {
			get { return uiInPendingState; }
			set { uiInPendingState = value; }
		}

		ProgressBar progressBar;
		public ProgressBar ProgressBar {
			get { return progressBar; }
			set { progressBar = value; }
		}

		ImageView colorThemeButton;
		protected ImageView ColorThemeButton {
			get { return colorThemeButton; }
			set { colorThemeButton = value; }
		}

		ImageView thumbnailButton;
		protected ImageView ThumbnailButton {
			get { return thumbnailButton; }
			set { thumbnailButton = value; }
		}

		ImageView thumbnailBackgroundView;
		protected ImageView ThumbnailBackgroundView {
			get { return thumbnailBackgroundView; }
			set { thumbnailBackgroundView = value; }
		}

		Spinner colorThemeSpinner;
		protected Spinner ColorThemeSpinner {
			get { return colorThemeSpinner; }
			set { colorThemeSpinner = value; }
		}

		EditText nameEditText;
		protected EditText NameEditText {
			get { return nameEditText; }
			set { nameEditText = value; }
		}

		TextView colorThemeText;
		protected TextView ColorThemeText {
			get { return colorThemeText; }
			set { colorThemeText = value; }
		}

		RelativeLayout titlebarLayout;
		protected RelativeLayout TitleBarLayout {
			get { return titlebarLayout; }
			set { titlebarLayout = value; }
		}

		TextView titleTextView;
		protected TextView TitleTextView {
			get { return titleTextView; }
			set { titleTextView = value; }
		}

		Button leftBarButton;
		protected Button LeftBarButton {
			get { return leftBarButton; }
			set { leftBarButton = value; }
		}

		Button rightBarButton;
		protected Button RightBarButton {
			get { return rightBarButton; }
			set { rightBarButton = value; }
		}

		public int ThumbnailSizePixels { get; set; }

		public abstract BackgroundColor ColorTheme { get; }
		public abstract CounterParty CounterParty { get; }
		public abstract string TextInDisplayField { get; set; }
		public abstract void LeftBarButtonClicked (object sender, EventArgs e);
		public abstract void RightBarButtonClicked (object sender, EventArgs e);
		public abstract void ColorThemeSpinnerItemClicked (object sender, AdapterView.ItemSelectedEventArgs e);
		public abstract void AdditionalUIChangesOnResume ();
		public abstract void AdditionalThemeController ();

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
			// Create your fragment here
			DisplayMetrics displayMetrics = Resources.DisplayMetrics;
			this.ThumbnailSizePixels = (int) (Android_Constants.ROUNDED_THUMBNAIL_SIZE / displayMetrics.Density);
		}

		public override void OnResume () {
			base.OnResume ();

			//if (this.UpdateUIWhenActive) {
				ThemeController ();
				UpdateThumbnailPicture ();
				AdditionalUIChangesOnResume ();
			//}
		}

		public override void OnPause () {
			base.OnPause ();
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			if (this.NameEditText != null) {
				this.NameEditText.Focusable = true;
				this.NameEditText.Typeface = FontHelper.DefaultFont;
				this.NameEditText.Text = this.CounterParty.displayName;
				this.NameEditText.RequestFocus ();

				this.TextInDisplayField = this.NameEditText.Text;
			}

			if (this.LeftBarButton != null) {
				this.LeftBarButton.Click += LeftBarButtonClicked;
				ViewClickStretchUtil.StretchRangeOfButton (this.LeftBarButton);
			}

			if (this.RightBarButton != null) {
				this.RightBarButton.Click += RightBarButtonClicked;
			}

			if (this.ColorThemeSpinner != null) {
				this.ColorThemeSpinner.Visibility = ViewStates.Invisible;
				this.ColorThemeSpinner.Focusable = true;
				this.ColorThemeSpinner.Adapter = new BackgroundColorThemeArrayAdapter (Activity.BaseContext, BackgroundColor.AllColors);
				BackgroundColor color = this.ColorTheme;
				this.ColorThemeSpinner.SetSelection( Array.IndexOf (BackgroundColor.AllColors, this.ColorTheme));
				this.ColorThemeSpinner.ItemSelected += ColorThemeSpinnerItemClicked;
			}

			if (this.ColorThemeButton != null) {
				this.ColorThemeButton.Focusable = true;
				this.ColorThemeButton.Click += (sender, e) => {
					this.ColorThemeSpinner.RequestFocus();
					this.ColorThemeSpinner.PerformClick ();
				};
			}

		}

		public void ThemeController () {
			// View's memory can be released here if the fragment Paused but didn't dispose of the SharedChatController yet.
			// We keep track of an Active flag to avoid check the View directly.
			BackgroundColor colorTheme = this.ColorTheme;
			if(this.View != null) {
				colorTheme.GetBackgroundResource ((string file) => {
					if (this.View != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile (this.View, this.Resources, file);
					}
				});
				if (this.ColorThemeButton != null) {
					colorTheme.GetColorThemeSelectionImageResource ((string filepath) => {
						if (this.ColorThemeButton != null && this != null) {
							BitmapSetter.SetBackgroundFromFile (this.ColorThemeButton, this.Resources, filepath);
						}
					});
				}
				UpdateThumbnailLayout (this.CounterParty);
				colorTheme.GetBlankPhotoAccountResource ((string f) => {
					if (this != null) {
						BitmapSetter.SetAccountImage (this.CounterParty, this.Resources, this.thumbnailButton, f, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
						AdditionalThemeController ();
					}
				});
			}
		}

		public void UpdateThumbnailPicture () {
			if (View == null)
				return;

			BackgroundColor colorTheme = this.ColorTheme;

			colorTheme.GetBlankPhotoAccountResource ((string file) => {
				if (this != null && this.ThumbnailButton != null) {
					BitmapSetter.SetAccountImage ( this.CounterParty, this.Resources, this.ThumbnailButton, file, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
					UpdateThumbnailLayout (this.CounterParty);
				}
			});
		}

		public void UpdateThumbnailLayout (CounterParty c) {
			if (this.PendingState) {
				SetProgresBarVisibility (true);
				return;
			}

			MediaState currentMediaState = MediaState.Unknown;
			if (c == null || (c.media == null && c.PriorMedia == null)) {
				this.ProgressBar.Visibility = ViewStates.Invisible;
				this.ThumbnailButton.Visibility = ViewStates.Visible;
				this.ThumbnailBackgroundView.Visibility = ViewStates.Invisible;
				return;
			}

			Media media = c.media;
			Media priorMedia = c.PriorMedia;
			MediaManager mediaManager = EMApplication.Instance.appModel.mediaManager;

			if (media != null) {
				mediaManager.ResolveState (media);
				currentMediaState = media.MediaState;
			} else {
				if (priorMedia != null)
					currentMediaState = MediaState.Absent;
			}

			switch (currentMediaState) {
			case MediaState.Absent:
				{
					if (priorMedia != null) {
						if (mediaManager.MediaOnFileSystem (priorMedia)) {
							SetProgresBarVisibility (false);
							return;
						}
					}

					SetProgresBarVisibility (true);
					break;
				}
			case MediaState.Downloading:
				{
					if (priorMedia != null) {
						if (mediaManager.MediaOnFileSystem (priorMedia)) {
							SetProgresBarVisibility (false);
							return;
						}
					}

					SetProgresBarVisibility (true);
					break;
				}
			case MediaState.Present:
				{
					SetProgresBarVisibility (false);
					break;
				}
			case MediaState.FailedDownload:
				{
					SetProgresBarVisibility (false);
					this.ThumbnailBackgroundView.Visibility = ViewStates.Invisible;
					break;
				}
			default:
				{
					SetProgresBarVisibility (false);
					break;
				}

			}
		}

		protected override string ImageIntentMediaType () {
			return "image/*";
		}

		public void ShowBackgroundThumbnailView () {
			BackgroundColor colorTheme = this.ColorTheme;
			colorTheme.GetLargePhotoBackgroundResource ( (string file) => {
				if (this.Activity != null &&  this.ThumbnailBackgroundView != null && this.Resources != null) {
					BitmapSetter.SetBackgroundFromFile (this.ThumbnailBackgroundView, this.Resources, file);
					this.ThumbnailBackgroundView.Visibility = ViewStates.Visible;
				}
			});
		}

		public void SetProgresBarVisibility (bool shouldShowProgressBar) {
			if (shouldShowProgressBar) {
				this.ProgressBar.Visibility = ViewStates.Visible;
				this.ThumbnailBackgroundView.Visibility = ViewStates.Invisible;
				this.ThumbnailButton.Visibility = ViewStates.Invisible;
			} else {
				this.ProgressBar.Visibility = ViewStates.Invisible;
				this.ThumbnailButton.Visibility = ViewStates.Visible;
				ShowBackgroundThumbnailView ();
			}
		}

		#region acquires images, subclasses should override
		protected override int PopupMenuInflateResource () {
			return -1;
		}

		protected override View PopupMenuAnchorView () {
			return this.ThumbnailButton;
		}

		protected override void DidAcquireMedia (string mediaType, string path) {}
		protected override bool AllowsImageCropping () {
			return true;
		}
		#endregion
	}
}

