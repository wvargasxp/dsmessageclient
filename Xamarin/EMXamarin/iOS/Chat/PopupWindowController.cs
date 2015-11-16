using System;
using System.Diagnostics;
using Foundation;
using UIKit;
using CoreGraphics;
using EZAudio;
using System.Runtime.InteropServices;
using AVFoundation;
using em;

namespace iOS {
	public class PopupWindowController : EZMicrophoneDelegate, IDisposable {

		private static readonly int HEIGHT = 72;
		private static readonly int WIDTH = 439;
		private static readonly float WAVEFORM_VERTICAL_STRETCH = 6.0f;

		public EZMicrophone Mic { get; set; }
		public EZAudioPlotGL Plot { get; set; }

		private WeakReference chatViewControllerRef = null;
		public ChatViewController ChatViewController {
			get {
				return this.chatViewControllerRef.Target as ChatViewController;
			}
			set {
				this.chatViewControllerRef = new WeakReference (value);
			}
		}

		public PopupWindowController () {
			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, iOSSoundRecordingRecorder.RECORDING_STARTED, HandleNotificationRecordingStarted);
			em.NotificationCenter.DefaultCenter.AddWeakObserver (null, iOSSoundRecordingRecorder.RECORDING_STOPPED, HandleNotificationRecordingStopped);
		}

		private void HandleNotificationRecordingStarted (Notification notif) {
			EZMicrophone.SharedMicrophone ().WeakMicrophoneDelegate = this;
			NSError error = EZMicrophone.SharedMicrophone ().StartFetchingAudio ();
			if (error != null) {
				Debug.WriteLine ("Cannot start fetching audio {0}", error);
			}
		}

		private void HandleNotificationRecordingStopped (Notification notif) {
			EZMicrophone.SharedMicrophone ().StopFetchingAudio ();
		}

		public void Show () {
			if (this.ChatViewController == null) {
				return;
			}

			UIApplication.SharedApplication.IdleTimerDisabled = true;

			BackgroundColor color = this.ChatViewController.MainColor;

			this.Plot = new EZAudioPlotGL (new CGRect (0, 0, WIDTH, HEIGHT));
			this.Plot.BackgroundColor = iOS_Constants.NEUTRAL_COLOR;
			this.Plot.Color = color.GetColor ();
			this.Plot.ShouldMirror = true;
			this.Plot.ShouldFill = true;
			this.Plot.PlotType = EZPlotType.Rolling;
			this.Plot.Gain = WAVEFORM_VERTICAL_STRETCH;

			this.ChatViewController.ShowMediaInStagingArea (this.Plot);
		}

		public void Dismiss () {
			try {
				if (this.ChatViewController == null) {
					return;
				}

				if (this.Plot != null) {
					this.Plot.RemoveFromSuperview ();
					this.Plot.Dispose ();
					this.Plot = null;
				}
			} finally {
				UIApplication.SharedApplication.IdleTimerDisabled = false;
			}
		}

		public override void HasAudioReceived (EZMicrophone microphone, ref IntPtr buffer, UInt32 bufferSize, UInt32 numberOfChannels) {
			if (this.Plot == null) {
				return;
			}

			float[] waveformBuffer = MarshallToFloatArray (buffer, bufferSize);
			EMTask.DispatchMain (() => {
				if (this.Plot != null) {
					this.Plot.UpdateBuffer (ref waveformBuffer [0], bufferSize);
				}
			});
		}

		private float[] MarshallToFloatArray (IntPtr source, nuint bufferSize) {
			float[] array = new float[bufferSize];
			Marshal.Copy (source, array, 0, (int)bufferSize);
			return array;
		}

		public void Dispose () {
			em.NotificationCenter.DefaultCenter.RemoveObserverAction (HandleNotificationRecordingStarted);
			em.NotificationCenter.DefaultCenter.RemoveObserverAction (HandleNotificationRecordingStopped);
		}
	}
}

