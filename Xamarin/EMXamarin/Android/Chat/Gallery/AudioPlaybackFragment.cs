using System;
using Android.Views;
using Android.OS;
using Android.App;
using Android.Widget;
using em;
using EMXamarin;

namespace Emdroid {
	public class AudioPlaybackFragment : AbstractGalleryItemFragment {

		private bool CreatedThumbnailPreview { get; set; }

		private SoundRecordingPlayer AudioPlayer { get; set; }

		public static AudioPlaybackFragment NewInstance (em.Message message, int position, int currentPosition) {
			AudioPlaybackFragment f = new AudioPlaybackFragment ();
			f.Message = message;
			f.Position = position;
			f.CurrentPosition = currentPosition;
			return f;
		}

		public AudioPlaybackFragment () {}

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
			this.AudioPlayer = ApplicationModel.SharedPlatform.GetSoundRecordingPlayer ();

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
			StopMedia ();
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
			this.AudioPlayer.DelegateDidFinishedPlaying += DidFinishPlaying;
		}

		private void RemoveEventHandlers () {
			this.PlayButton.Click -= PlayButtonTapped;
			this.AudioPlayer.DelegateDidFinishedPlaying -= DidFinishPlaying;
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

		private void PlayMedia () {
			em.Message mediaMessage = this.Message;
			Media media = mediaMessage.media;
			string localMediaPath = EMApplication.Instance.appModel.uriGenerator.GetFilePathForChatEntryUri (mediaMessage.media.uri, mediaMessage.chatEntry);
			this.AudioPlayer.Play (localMediaPath);
		}

		private void StopMedia () {
			this.AudioPlayer.Stop ();
		}

		#region event handlers
		private void PlayButtonTapped (object sender, EventArgs e) {
			// Hides the playbutton on click play media.
			EMTask.DispatchMain (() => {
				this.PlayButton.Visibility = ViewStates.Invisible;
				BitmapSetter.SetBackground (this.Video, null);
				PlayMedia ();
			});
		}

		private void DidFinishPlaying () {
			// Reshow the playbutton so user can replay video.
			this.PlayButton.Visibility = ViewStates.Visible;
		}
		#endregion

		public override void PagedChanged () {
			if (this.SelfVisible) {
				InitializePlayback ();
			} else {
				StopMedia ();
				DidFinishPlaying ();
			}
		}

		public override void MediaGalleryPaused () {
			StopMedia ();
			DidFinishPlaying ();
		}

		public override void SetupMedia () {
			InitializePlayback ();
		}
	}
}

