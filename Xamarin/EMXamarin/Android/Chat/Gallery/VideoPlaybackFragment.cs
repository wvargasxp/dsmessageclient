using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using em;
using EMXamarin;

namespace Emdroid {
	public class VideoPlaybackFragment : AbstractGalleryItemFragment {

		private bool CreatedThumbnailPreview { get; set; }

		public static VideoPlaybackFragment NewInstance (em.Message message, int position, int currentPosition) {
			VideoPlaybackFragment f = new VideoPlaybackFragment ();
			f.Message = message;
			f.Position = position;
			f.CurrentPosition = currentPosition;
			return f;
		}

		public VideoPlaybackFragment () {}

		#region lifecycle - sorted order
		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			return base.OnCreateView (inflater, container, savedInstanceState);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState); 
			SetEventHandlers ();
			this.ProgressBar.Visibility = ViewStates.Invisible;
			CreateThumbnailPreview ();
		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnResume () {
			base.OnResume ();
			InitializePlayback ();
		}

		public override void OnPause () {
			base.OnPause ();
			this.CreatedThumbnailPreview = false;
		}

		public override void OnStop () {
			base.OnStop ();
		}

		public override void OnDestroyView () {
			base.OnDestroyView ();
		}

		public override void OnDestroy () {
			base.OnDestroy ();
			RemoveEventHandlers ();
		}

		public override void OnDetach () {
			base.OnDetach ();
		}
		#endregion

		private void SetEventHandlers () {
			this.PlayButton.Click += PlayButtonTapped;
			this.Video.Completion += DidFinishPlaying;
		}

		private void RemoveEventHandlers () {
			this.PlayButton.Click -= PlayButtonTapped;
			this.Video.Completion -= DidFinishPlaying;
		}

		private void InitializePlayback () {
			if (this.OkayToSetupMedia) {
				em.Message mediaMessage = this.Message;
				string localMediaPath = EMApplication.Instance.appModel.uriGenerator.GetFilePathForChatEntryUri (mediaMessage.media.uri, mediaMessage.chatEntry);
				Android.Net.Uri videoUri = Android.Net.Uri.Parse (localMediaPath);
				this.Video.SetVideoURI (videoUri);
				this.Image.Visibility = ViewStates.Invisible;
				this.PlayButton.Visibility = ViewStates.Visible;

				if (!this.CreatedThumbnailPreview) {
					CreateThumbnailPreview ();
				}

				this.OkayToSetupMedia = false;
			}
		}

		private void CreateThumbnailPreview () {
			em.Message mediaMessage = this.Message;
			Media media = mediaMessage.media;
			string localMediaPath = EMApplication.Instance.appModel.uriGenerator.GetFilePathForChatEntryUri (mediaMessage.media.uri, mediaMessage.chatEntry);
			BitmapSetter.SetImageVideoView (null, media, this.Resources, this.Video, localMediaPath);
			this.CreatedThumbnailPreview = true;
		}

		#region event handlers
		private void PlayButtonTapped (object sender, EventArgs e) {
			// Hides the playbutton on click play media.
			EMTask.DispatchMain (() => {
				this.PlayButton.Visibility = ViewStates.Invisible;
				BitmapSetter.SetBackground (this.Video, null);
				this.Video.Start ();
			});
		}

		private void DidFinishPlaying (object sender, EventArgs e) {
			// Reshow the playbutton so user can replay video.
			this.PlayButton.Visibility = ViewStates.Visible;
		}
		#endregion

		public override void PagedChanged () {
			this.Video.Pause ();
			if (this.SelfVisible) {
				InitializePlayback ();
			} else {
				DidFinishPlaying (null, null);
			}
		}

		public override void MediaGalleryPaused () {
			this.Video.Pause ();
			DidFinishPlaying (null, null);
		}

		public override void SetupMedia () {
			InitializePlayback ();
		}
	}
}

