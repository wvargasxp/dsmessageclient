using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Com.Ortiz.Touch;
using EMXamarin;
using em;
using AndroidHUD;
using System.Collections.Generic;

namespace Emdroid {
	public abstract class AbstractGalleryItemFragment : Fragment {

		protected em.Message Message { get; set; }

		private HiddenReference<SharedPhotoVideoItemController> _shared;
		private SharedPhotoVideoItemController Shared { 
			get { return this._shared != null ? this._shared.Value : null; } 
			set { this._shared = new HiddenReference<SharedPhotoVideoItemController> (value); }
		}

        private bool _okayToSetupMedia = false;
		protected bool OkayToSetupMedia { get { return this._okayToSetupMedia; } set { this._okayToSetupMedia = value; } }

        private bool _detachedFromActivity = true;
		private bool DetachedFromActivity { get { return this._detachedFromActivity; } set { this._detachedFromActivity = value; } }

        private int _position = -1; // The position of the item/fragment relative to the list of media messages.
        protected int Position { get { return this._position; } set { this._position = value; } }

		protected int CurrentPosition { get; set; }
		protected bool SelfVisible {
			get {
				return this.Position == this.CurrentPosition;
			}
		}

		#region UI
		public ProgressBar ProgressBar {
			get;
			set;
		}

		public TouchImageView Image {
			get;
			set;
		}

		public VideoView Video {
			get;
			set;
		}

		public RelativeLayout VideoLayout {
			get;
			set;
		}

		public ImageButton PlayButton {
			get;
			set;
		}
		#endregion

		public AbstractGalleryItemFragment () {}

		protected void SetUIProperties (View view) {
			this.ProgressBar = view.FindViewById<ProgressBar> (Resource.Id.indeterminateProgressBar);
			this.VideoLayout = view.FindViewById<RelativeLayout> (Resource.Id.video_frame);
			this.Image = (TouchImageView)view.FindViewById<TouchImageView> (Resource.Id.galleryItemImageView);
			this.Video = (VideoView)view.FindViewById<VideoView> (Resource.Id.mediaPlaybackView);
			this.PlayButton = (ImageButton)view.FindViewById<ImageButton> (Resource.Id.play_button);
		}

		#region lifecycle - sorted order
		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
			this.DetachedFromActivity = false;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
			this.Shared = new SharedPhotoVideoItemController (this, EMApplication.Instance.appModel, this.Message.chatEntry);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View view = (RelativeLayout)inflater.Inflate (Resource.Layout.image_gallery_item, container, false);
			SetUIProperties (view);
			return view;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);
			// Check if media is on the filesystem.
			// Set the flag indicating this and inherited classes can use the flag to determine if it can draw its contents.
			Media media = this.Message.media;
			ApplicationModel appModel = EMApplication.Instance.appModel;
			MediaManager mediaManager = appModel.mediaManager;
			if (mediaManager.MediaOnFileSystem (media)) {
				this.OkayToSetupMedia = true;
			} else {
				this.OkayToSetupMedia = false;
				media.DownloadMedia (appModel);
			}
		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnResume () {
			base.OnResume ();
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.MediaGallery_PageChangedNotification, HandleNotificationPageChanged);
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.MediaGallery_Paused, HandleNotificationMediaGalleryPaused);
		}

		public override void OnPause () {
			base.OnPause ();
			NotificationCenter.DefaultCenter.RemoveObserverAction (HandleNotificationPageChanged);
			NotificationCenter.DefaultCenter.RemoveObserverAction (HandleNotificationMediaGalleryPaused);
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
			this.DetachedFromActivity = true;
		}
		#endregion

		private void ShowProgress () {
			WeakReference weakSelf = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractGalleryItemFragment self = weakSelf.Target as AbstractGalleryItemFragment;
				if (self != null && !self.DetachedFromActivity) {
					if (self.SelfVisible) {
						AndHUD.Shared.Show (self.Activity, null, -1, MaskType.None, default(TimeSpan?), null, true, null);
					}

				}	
			});
		}

		private void HideProgress () {
			WeakReference weakSelf = new WeakReference (this);
			EMTask.DispatchMain (() => {
				AbstractGalleryItemFragment self = weakSelf.Target as AbstractGalleryItemFragment;
				if (self != null && !self.DetachedFromActivity) {
					if (self.SelfVisible) {
						AndHUD.Shared.Dismiss (self.Activity);
					}
				}
			});
		}

		public abstract void PagedChanged ();
		private void HandleNotificationPageChanged (em.Notification g) {
			Dictionary<string, int> extra = g.Extra as Dictionary<string, int>;
			if (extra != null) {
				this.CurrentPosition = extra [MediaGalleryFragment.CurrentPageNotificationKey];
			}

			if (!this.SelfVisible) {
				HideProgress ();
			}

			PagedChanged ();
		}

		public abstract void MediaGalleryPaused ();
		private void HandleNotificationMediaGalleryPaused (em.Notification g) {
			MediaGalleryPaused ();
		}

		public abstract void SetupMedia ();
		public void SetupMediaAfterDownload () {
			SetupMedia ();
		}

		public class SharedPhotoVideoItemController : AbstractPhotoVideoItemController {
			private WeakReference weakRef;
			protected AbstractGalleryItemFragment Fragment {
				get { return weakRef != null ? weakRef.Target as AbstractGalleryItemFragment : null; }
				set { weakRef = new WeakReference (value); }
			}

			public SharedPhotoVideoItemController (AbstractGalleryItemFragment c, ApplicationModel appModel, ChatEntry ce) 
				: base (appModel, ce) {
				this.Fragment = c;
			}

			public override void MediaStartedDownload (em.Message message) {
				AbstractGalleryItemFragment f = this.Fragment;
				if (f != null) {
					if (f.Message == message && f.SelfVisible) {
						f.ShowProgress ();
					}
				}
			}

			public override void MediaPercentDownloadUpdated (em.Message message, double percentComplete) {
				AbstractGalleryItemFragment f = this.Fragment;
				if (f != null) {}
			}

			public override void MediaCompletedDownload (em.Message message) {
				EMTask.DispatchMain (() => {
					AbstractGalleryItemFragment f = this.Fragment;
					if (f != null && !f.DetachedFromActivity) {
						if (f.Message == message) {
							f.OkayToSetupMedia = true;
							if (f.SelfVisible) {
								f.HideProgress ();
								f.SetupMediaAfterDownload ();
							}
						}
					}
				});
			}
		}
	}
}

