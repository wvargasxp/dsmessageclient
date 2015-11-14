using System;
using Android.OS;
using Android.App;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class PhotoPlaybackFragment : AbstractGalleryItemFragment {
		public static PhotoPlaybackFragment NewInstance (em.Message message, int position, int currentPosition) {
			PhotoPlaybackFragment f = new PhotoPlaybackFragment ();
			f.Message = message;
			f.Position = position;
			f.CurrentPosition = currentPosition;
			return f;
		}

		public PhotoPlaybackFragment () {}

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
			this.ProgressBar.Visibility = ViewStates.Invisible;
			DisplayImage ();
		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnResume () {
			base.OnResume ();
		}

		public override void OnPause () {
			base.OnPause ();
		}

		public override void OnStop () {
			base.OnStop ();
		}

		public override void OnDestroyView () {
			base.OnDestroyView ();
		}

		public override void OnDestroy () {
			base.OnDestroy ();
		}

		public override void OnDetach () {
			base.OnDetach ();
		}
		#endregion

		private void DisplayImage () {
			this.VideoLayout.Visibility = ViewStates.Invisible;
			if (this.OkayToSetupMedia) {
				em.Message mediaMessage = this.Message;
				Media media = mediaMessage.media;
				this.Image.Visibility = ViewStates.Visible;
				string localMediaPath = EMApplication.Instance.appModel.uriGenerator.GetFilePathForChatEntryUri (mediaMessage.media.uri, mediaMessage.chatEntry);
				BitmapSetter.SetFullSizeImageView (null, media, this.Resources, this.Image, localMediaPath);
				this.OkayToSetupMedia = false;
			}
		}

		public override void PagedChanged () {
			if (this.SelfVisible) {
				DisplayImage ();
			}
		}

		public override void MediaGalleryPaused () {
			//
		}

		public override void SetupMedia () {
			DisplayImage ();
		}
	}
}

