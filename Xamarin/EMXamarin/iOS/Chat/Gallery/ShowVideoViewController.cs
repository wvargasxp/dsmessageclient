using System;
using em;
using UIKit;
using Foundation;
using MediaPlayer;
using System.Diagnostics;
using CoreGraphics;
using CoreMedia;
using System.Threading;
using AVFoundation;
using System.Collections.Generic;
using EMXamarin;

namespace iOS {
	public class ShowVideoViewController : PhotoVideoItemController {

		NSObject toolbarShowingObserver;
		protected NSObject Observer {
			get { return toolbarShowingObserver; }
			set { toolbarShowingObserver = value; }
		}

		EMVideoView vv = null;
		protected EMVideoView VideoView {
			get { return vv; }
			set { vv = value; }
		}

		private NSObject EnterForeGroundObserver { get; set; }

		#region playback
		readonly int TOOLBAR_HEIGHT = 44;
		readonly int SLIDER_WIDTH = 250;
		readonly int SLIDER_HEIGHT = 30;
		readonly int TIME_TO_UPDATE_SLIDER = 1000; // 1 second in milliseconds

		UIToolbar tb;
		protected UIToolbar Toolbar {
			get { return tb; }
			set { tb = value; }
		}

		UIBarButtonItem togglePlay;
		protected UIBarButtonItem TogglePlay {
			get { return togglePlay; }
			set { togglePlay = value; }
		}

		UISlider sl;
		protected UISlider SliderControl {
			get { return sl; }
			set { sl = value; }
		}

		bool trackingSlider;
		protected bool TrackingSlider {
			get { return trackingSlider; }
			set { trackingSlider = value; }
		}

		Timer timer;
		protected Timer Timer {
			get { return timer; }
			set { timer = value; }
		}

		UIBarButtonItem slBar;
		protected UIBarButtonItem SliderAsBarButton {
			get { return slBar; }
			set { slBar = value; }
		}

		#endregion

		public ShowVideoViewController (int index, Message message) : base (index, message) {}
			
		#region ViewWill
		public override void ViewDidLoad () {
			base.ViewDidLoad ();
			this.View.BackgroundColor = UIColor.Black;

			InitializePlayback ();

			if (this.EnterForeGroundObserver == null) {
				this.EnterForeGroundObserver = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)em.Constants.DID_ENTER_FOREGROUND, ResetVideoOnEnterForeground);
			}
		}

		private void ResetVideoOnEnterForeground (NSNotification notif) {
			EMVideoView videoView = this.VideoView;
			bool playing = false;
			if (videoView != null) {
				playing = this.VideoView.CurrentlyPlaying;
			} 

			CreateTogglePlayButton (playing);
			UpdateToolBar ();
		}

		#region slider event handlers
		public void SliderShouldFollowVideo (object o) {
			if (this.VideoView == null)
				return;
			if (this.VideoView.CurrentlyPlaying) {
				EMTask.DispatchMain (() => {
					this.SliderControl.Value = (float)this.VideoView.VideoPlayer.CurrentTime.Seconds;
				});
			}

			this.Timer = new Timer (SliderShouldFollowVideo, null, TIME_TO_UPDATE_SLIDER, Timeout.Infinite);
		}

		public void SliderValueChanged (object sender, EventArgs e) {
			if (!this.TrackingSlider)
				this.VideoView.Pause (false);
			this.VideoView.VideoPlayer.Seek (CMTime.FromSeconds (this.SliderControl.Value, 1));
			this.TrackingSlider = true;
		}

		public void SliderWillStopChangingValue (object sender, EventArgs e) {
			if (this.TrackingSlider)
				this.VideoView.Play (false);
			this.TrackingSlider = false;
		}
		#endregion

		public void TogglePlayClicked (object sender, EventArgs e) {
			if (this.VideoView != null) {
				bool playing = this.VideoView.CurrentlyPlaying;
				CreateTogglePlayButton (!playing);
				if (playing) {
					this.VideoView.Pause ();
					UpdateToolBar ();
				} else {
					this.VideoView.Play ();
					UpdateToolBar ();
				}
			}
		}

		#region ui updates
		private void CreateTogglePlayButton (bool playing) {
			if (this.TogglePlay != null) {
				this.TogglePlay.Clicked -= TogglePlayClicked;
			}

			this.TogglePlay = new UIBarButtonItem (playing ? UIBarButtonSystemItem.Pause :UIBarButtonSystemItem.Play);
			this.TogglePlay.TintColor = UIColor.Black;
			this.TogglePlay.Clicked += TogglePlayClicked;
		}

		public void UpdateToolBar () {
			UIBarButtonItem fS1 = new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace);
			UIBarButtonItem fS2 = new UIBarButtonItem (UIBarButtonSystemItem.FlexibleSpace);
			this.Toolbar.SetItems (new UIBarButtonItem[] { fS1, this.TogglePlay, fS1, this.SliderAsBarButton, fS2 }, true);
			this.View.BringSubviewToFront (this.Toolbar);
		}

		private void HandleNotificationToggleUI (NSNotification notification) {
			NSNumber boolVa = (NSNumber)notification.Object;
			bool showing = boolVa.BoolValue;
			UIView.AnimateAsync (.3, () => {
				if (this.Toolbar != null) {
					if (showing) {
						this.Toolbar.Alpha = 1.0f;
					} else {
						this.Toolbar.Alpha = 0f;
					}
				}
			});
		}
		#endregion

		public override void ViewWillLayoutSubviews () {
			base.ViewWillLayoutSubviews ();
			if (this.Toolbar != null) {
				this.Toolbar.Frame = new CGRect (0, this.View.Frame.Height - TOOLBAR_HEIGHT, this.View.Frame.Width, TOOLBAR_HEIGHT);
			}
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
			this.Observer = NSNotificationCenter.DefaultCenter.AddObserver ((NSString)PhotoVideoController.NOTIFICATION_TOGGLING_UI, HandleNotificationToggleUI);
			InitializePlayback ();
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
		}

		public override void ViewWillDisappear (bool animated) {
			base.ViewWillDisappear (animated);

			if (this.SliderControl != null) {
				this.SliderControl.ValueChanged -= SliderValueChanged;
				this.SliderControl.TouchUpInside -= SliderWillStopChangingValue;
				this.SliderControl.TouchUpOutside -= SliderWillStopChangingValue;
			}
			NSNotificationCenter.DefaultCenter.RemoveObserver (this.Observer);
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			if (this.VideoView != null) {
				bool playing = this.VideoView.CurrentlyPlaying;
				if (playing)
					this.VideoView.Pause ();
				this.VideoView.Dispose ();
				this.VideoView = null;
			}
		}

		#endregion

		#region rotation
		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
		}

		public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation) {
			base.DidRotate (fromInterfaceOrientation);
		}
		#endregion

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
			NSNotificationCenter.DefaultCenter.RemoveObserver (this.EnterForeGroundObserver);
		}

		#region overriding downloaded
		public override void OnStartDownload (Message message) {}

		public override void OnPercentedDownload (Message message, double percentComplete) {}

		public override void OnDownloadedMedia (Message message) {
			InitializePlayback ();
		}

		#endregion

		public void InitializePlayback () {
			if (this.OkayToSetupMedia) {
				if (this.VideoView == null) {
					AppDelegate appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
					string localpath = this.Message.media.GetPathForUri (appDelegate.applicationModel.platformFactory);
					WeakDelegateProxy EMVideoViewAction = WeakDelegateProxy.CreateProxy (() => {
						CreateTogglePlayButton (false);
						this.SliderControl.Value = 0;
						UpdateToolBar ();
					});
					this.VideoView = new EMVideoView (localpath, this.View.Frame, EMVideoViewAction.HandleEvent);
					this.VideoView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;
					this.VideoView.PlayButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object, EventArgs> (TogglePlayClicked).HandleEvent<object, EventArgs>;
					this.Add (this.VideoView);

					this.Timer = new Timer (SliderShouldFollowVideo, null, TIME_TO_UPDATE_SLIDER, Timeout.Infinite);
				}

				this.OkayToSetupMedia = false;

				this.Toolbar = new UIToolbar (new CGRect (0, this.View.Frame.Height - TOOLBAR_HEIGHT, this.View.Frame.Width, TOOLBAR_HEIGHT));
				this.Toolbar.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
				this.Toolbar.Translucent = true;
				this.Toolbar.Alpha = 1f;

				this.TogglePlay = new UIBarButtonItem (UIBarButtonSystemItem.Play);
				this.TogglePlay.TintColor = UIColor.Black;
				this.TogglePlay.Clicked += WeakDelegateProxy.CreateProxy<object, EventArgs> (TogglePlayClicked).HandleEvent<object, EventArgs>;

				#region slider
				this.SliderControl = new UISlider (new CGRect (0, 0, SLIDER_WIDTH, SLIDER_HEIGHT));
				this.SliderAsBarButton = new UIBarButtonItem (this.SliderControl);
				this.SliderAsBarButton.Width = SLIDER_WIDTH;

				float beginValue = 0;
				float endValue = (float)this.VideoView.Duration.Seconds;

				this.SliderControl.Value = beginValue;
				this.SliderControl.MaxValue = beginValue > endValue ? beginValue : endValue;

				this.SliderControl.ValueChanged += WeakDelegateProxy.CreateProxy<object, EventArgs> (SliderValueChanged).HandleEvent<object, EventArgs>;
				this.SliderControl.TouchUpInside += WeakDelegateProxy.CreateProxy<object, EventArgs> (SliderWillStopChangingValue).HandleEvent<object, EventArgs>;
				this.SliderControl.TouchUpOutside += WeakDelegateProxy.CreateProxy<object, EventArgs> (SliderWillStopChangingValue).HandleEvent<object, EventArgs>;

				#endregion
				this.View.Add (this.Toolbar);

				UpdateToolBar ();
			}
		}
	}
}

