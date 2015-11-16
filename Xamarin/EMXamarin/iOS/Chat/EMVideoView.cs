using System;
using UIKit;
using AVFoundation;
using Foundation;
using em;
using CoreMedia;
using CoreGraphics;
using System.Diagnostics;
using System.Threading.Tasks;

namespace iOS {
	public class EMVideoView : UIView {
		AVPlayer player;
		public AVPlayer VideoPlayer {
			get { return player; }
		}

		AVPlayerLayer playerLayer;
		AVAsset asset;
		AVPlayerItem playerItem;
		NSObject videoPlayingObserver;

		UIButton playButton;
		public UIButton PlayButton {
			get { return playButton; }
			set { playButton = value; }
		}

		readonly int PLAYBUTTON_SIZE = 70;
		readonly float ANIMATION_DURATION = 0.2f;
		public CMTime Duration {
			get {
				if (player != null && player.CurrentItem != null && player.CurrentItem.Asset != null) {
					return player.CurrentItem.Asset.Duration;
				} else {
					return CMTime.Zero;
				}
			}
		}

		public bool CurrentlyPlaying {
			get {
				if (player != null && player.Rate > 0 && player.Error == null) {
					return true;
				} else {
					return false;
				}
			}
		}

		Action finishPlayingCallback;

		public EMVideoView (string localpath, CGRect f, Action cb) : base (f) {

			NSUrl url = NSUrl.FromFilename (localpath);
			asset = AVAsset.FromUrl (url);
			playerItem = new AVPlayerItem (asset);
			if (playerItem.Error != null && playerItem.Error.Description != null)
				Debug.WriteLine ("EMVideoView:Constructor: playerItem Error : " + playerItem.Error.Description);

			player = new AVPlayer (playerItem);
			player.AddObserver (this, new NSString ("status"), NSKeyValueObservingOptions.Initial | NSKeyValueObservingOptions.New, IntPtr.Zero);

			playerLayer = AVPlayerLayer.FromPlayer (player);
			playerLayer.NeedsDisplayOnBoundsChange = true;
			playerLayer.VideoGravity = AVLayerVideoGravity.ResizeAspect; //AVPlayerLayer.GravityResizeAspect; TODO: This might not be the same UNIFIED

			playerLayer.Frame = f;

			this.Layer.AddSublayer (playerLayer);

			this.Layer.NeedsDisplayOnBoundsChange = true;

			this.PlayButton = new UIButton (UIButtonType.Custom);
			this.PlayButton.Frame = new CGRect (f.Width/2 - PLAYBUTTON_SIZE/2, f.Height/2 - PLAYBUTTON_SIZE/2, PLAYBUTTON_SIZE, PLAYBUTTON_SIZE);
			this.PlayButton.SetImage (ImageSetter.GetResourceImage ("video-icon.png"), UIControlState.Normal);

			this.Add (this.PlayButton);
			finishPlayingCallback = cb;
			videoPlayingObserver = NSNotificationCenter.DefaultCenter.AddObserver (AVPlayerItem.DidPlayToEndTimeNotification, HandleAVPlayerStop);
		}

		public override void ObserveValue (NSString keyPath, NSObject ofObject,
			NSDictionary change, IntPtr context) {
			if (player.Status == AVPlayerStatus.ReadyToPlay) {
				player.SeekAsync (CMTime.Zero);
			}
		}

		private void HandleAVPlayerStop (NSNotification notification) {
			AnimatePlayButtonVisibility (1f);
			player.Seek (CMTime.Zero);
			if (finishPlayingCallback != null)
				finishPlayingCallback ();
		}

		public void Play (bool animate = true) {
			player.Play ();
			if (animate)
				AnimatePlayButtonVisibility (0f);
		}

		public void Pause (bool animate = true) {
			player.Pause ();
			if (animate)
				AnimatePlayButtonVisibility (1f);
		}

		private void AnimatePlayButtonVisibility (float value) {
			UIView.AnimateAsync (ANIMATION_DURATION, () => {
				this.PlayButton.Alpha = value;
			});
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();
			CGRect f = this.Frame;
			this.PlayButton.Frame = new CGRect (f.Width/2 - PLAYBUTTON_SIZE/2, f.Height/2 - PLAYBUTTON_SIZE/2, PLAYBUTTON_SIZE, PLAYBUTTON_SIZE);
			if (playerLayer != null)
				playerLayer.Frame = f;
		}

		protected override void Dispose (bool disposing) {
			if (playerLayer != null) {
				playerLayer.RemoveFromSuperLayer ();
				playerLayer.Dispose ();
				playerLayer = null;
			}

			if (player != null) {
				player.RemoveObserver (this, new NSString ("status"));
				player.Dispose ();
				player = null;
			}

			if (playerItem != null) {
				playerItem.Dispose ();
				playerItem = null;
			}

			if (asset != null) {
				asset.Dispose ();
				asset = null;
			}

			NSNotificationCenter.DefaultCenter.RemoveObserver (videoPlayingObserver);
			if (videoPlayingObserver != null) {
				videoPlayingObserver.Dispose ();
				videoPlayingObserver = null;
			}

			base.Dispose (disposing);
		}

	}
}

