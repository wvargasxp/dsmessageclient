using System;
using UIKit;
using em;
using MBProgressHUD;
using EMXamarin;

namespace iOS {
	public abstract class PhotoVideoItemController : UIViewController {

		int indexOfMedia;
		public int IndexOfContent {
			get { return indexOfMedia; }
			set { indexOfMedia = value; }
		}

		Message message;
		public Message Message {
			get { return message; }
			set { message = value; }
		}

		Action<int> onAppearAction;
		public Action<int> OnAppearAction {
			get { return onAppearAction; }
			set { onAppearAction = value; }
		}

		private MTMBProgressHUD progressHud;
		public MTMBProgressHUD ProgressHUD {
			get { return progressHud; }
			set { progressHud = value; }
		}

		private SharedPhotoVideoItemController sharedController;
		public SharedPhotoVideoItemController SharedController {
			get { return sharedController; }
			set { sharedController = value; }
		}

        private bool _okayToSetupMedia = false;
        protected bool OkayToSetupMedia { get { return this._okayToSetupMedia; } set { this._okayToSetupMedia = value; } }

		public PhotoVideoItemController (int index, Message message) {
			this.IndexOfContent = index;
			this.Message = message;
			AppDelegate appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
			this.SharedController = new SharedPhotoVideoItemController (this, appDelegate.applicationModel, this.Message.chatEntry);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			this.View.BackgroundColor = UIColor.White;

			MediaManager manager = AppDelegate.Instance.applicationModel.mediaManager;
			Media media = this.Message.media;
			if (manager.MediaOnFileSystem (media)) {
				this.OkayToSetupMedia = true;
			} else {
				this.OkayToSetupMedia = false;
				media.DownloadMedia (AppDelegate.Instance.applicationModel);
			}
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			if (OnAppearAction != null) {
				OnAppearAction (this.IndexOfContent);
			}
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);
		}

		public void ShowProgress () {
			WeakReference selfRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				PhotoVideoItemController self = selfRef.Target as PhotoVideoItemController;
				if (self != null) {
					self.View.EndEditing (true);
					if (self.ProgressHUD == null) {
						self.ProgressHUD = new MTMBProgressHUD (View) {
							LabelText = "LOADING".t (),
							LabelFont = FontHelper.DefaultFontForLabels(),
							RemoveFromSuperViewOnHide = true
						};

						self.View.Add (progressHud);
						self.ProgressHUD.Show (animated: true);
					}
				}
			});
		}

		public void HideProgress () {
			WeakReference selfRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				PhotoVideoItemController self = selfRef.Target as PhotoVideoItemController;
				if (self != null) {
					if (self.ProgressHUD != null) {
						self.ProgressHUD.Hide (animated: true, delay: 0);
						self.ProgressHUD = null;
					}
				}
			});
		}

		protected override void Dispose (bool disposing) {
			this.SharedController.Dispose ();
			base.Dispose (disposing);
		}
			
		public abstract void OnStartDownload (Message message);
		public abstract void OnPercentedDownload (Message message, double percentComplete);
		public abstract void OnDownloadedMedia (Message message);

		public class SharedPhotoVideoItemController : AbstractPhotoVideoItemController {

			WeakReference Ref { get; set; }

			public SharedPhotoVideoItemController (PhotoVideoItemController c, ApplicationModel appModel, ChatEntry ce) : base (appModel, ce) {
				this.Ref = new WeakReference (c);
			}

			public override void MediaStartedDownload (Message message) {
				PhotoVideoItemController photoVideoItem = this.Ref.Target as PhotoVideoItemController;
				if (photoVideoItem != null && photoVideoItem.Message == message) {
					photoVideoItem.ShowProgress ();
					photoVideoItem.OnStartDownload (message);
				}
			}

			public override void MediaPercentDownloadUpdated (Message message, double percentComplete) {
				PhotoVideoItemController photoVideoItem = this.Ref.Target as PhotoVideoItemController;
				if (photoVideoItem != null && photoVideoItem.Message == message) {
					photoVideoItem.OnPercentedDownload (message, percentComplete);
				}
			}

			public override void MediaCompletedDownload (Message message) {
				PhotoVideoItemController photoVideoItem = this.Ref.Target as PhotoVideoItemController;
				if (photoVideoItem != null && photoVideoItem.Message == message) {
					photoVideoItem.OkayToSetupMedia = true;
					photoVideoItem.HideProgress ();
					photoVideoItem.OnDownloadedMedia (message);
				}
			}
		}
	}
}

