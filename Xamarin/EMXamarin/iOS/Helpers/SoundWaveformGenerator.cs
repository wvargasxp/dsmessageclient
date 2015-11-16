using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UIKit;
using EZAudio;
using CoreGraphics;
using Foundation;
using em;
using EMXamarin;

namespace iOS {
	public class SoundWaveformGenerator {

		private static readonly int HEIGHT = 72;
		private static readonly int WIDTH = 439;
		private static readonly float WAVEFORM_VERTICAL_STRETCH = 6.0f;

		private static SoundWaveformGenerator sharedInstance = null;

		private UIWindow Window { get; set; }
		private UIView View { get; set; }
		private EZAudioPlot Plot { get; set; }
		private EZAudioFile File { get; set; }
		private WorkQueue GenerationQueue { get; set; }
		private UIImageView ImageView { get; set; }

		public static SoundWaveformGenerator SharedInstance {
			get {
				if (SoundWaveformGenerator.sharedInstance == null) {
					SoundWaveformGenerator.sharedInstance = new SoundWaveformGenerator ();
				}

				return SoundWaveformGenerator.sharedInstance;
			}
		}

		public SoundWaveformGenerator () {
			this.View = new UIView (new CGRect (0, 0, WIDTH, HEIGHT));
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			this.Window = appDelegate.Window;
			this.View.Center = new CGPoint (this.Window.Center.X, this.Window.Center.Y);
			this.ImageView = new UIImageView (new CGRect (0, 0, WIDTH, HEIGHT));
			this.GenerationQueue = new WorkQueue ();
		}

		public void GenerateThumbOfWaveForm (string soundFilePath, Action<UIImage> callback) {
			this.GenerationQueue.Add (new WaveFormWork (soundFilePath, this, callback));
		}

		private void DrawOnView (string soundFilePath, Action<UIImage> callback) {
			this.Plot = new EZAudioPlot (new CGRect (0, 0, WIDTH, HEIGHT));
			this.Plot.BackgroundColor = UIColor.White;
			this.Plot.Color = UIColor.Black;
			this.Plot.PlotType = EZPlotType.Buffer;
			this.Plot.ShouldFill = true;
			this.Plot.ShouldMirror = true;
			this.Plot.Gain = WAVEFORM_VERTICAL_STRETCH;


			this.View.Add (this.Plot);
			this.View.Add (this.ImageView);
			this.View.BringSubviewToFront (this.ImageView);
			this.View.Hidden = true;
			this.Window.AddSubview (this.View);
			this.Window.BringSubviewToFront (this.View);

			try {
				this.File = EZAudioFile.AudioFileWithURL (new NSUrl (soundFilePath));
				this.File.GetWaveformDataWithCompletionBlock ((ref float waveformData, UInt32 bufferLength) => {
					try {
						HandleWaveFormData (soundFilePath, callback, ref waveformData, bufferLength);
					} catch (Exception e) {
						Debug.WriteLine ("Failed to process waveform display data for sound file " + soundFilePath + ". Aborting waveform generation ", e.Message);
						HandleUnsuccessfulGeneration (soundFilePath, callback);
					} finally {
						this.GenerationQueue.Done ();
					}
				});
			} catch (Exception e) {
				Debug.WriteLine ("Failed to read sound file " + soundFilePath + " for reading. Aborting waveform generation ", e.Message);
				HandleUnsuccessfulGeneration (soundFilePath, callback);
				this.GenerationQueue.Done ();
			}
		}

		private void HandleWaveFormData (string soundFilePath, Action<UIImage> callback, ref float waveformData, UInt32 bufferLength) {
			this.Plot.UpdateBuffer (ref waveformData, bufferLength);
			CGRect rect = new CGRect (0, 0, WIDTH, HEIGHT);
			UIGraphics.BeginImageContext (rect.Size);
			CGContext context = UIGraphics.GetCurrentContext ();
			this.Plot.Layer.RenderInContext (context);
			UIImage thumb = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();
			DismissView ();

			this.ImageView.Image = thumb;
			callback (thumb);
		}

		private void HandleUnsuccessfulGeneration (string soundFilePath, Action<UIImage> callback) {
			UIImage thumb = ImageSetter.GetResourceImage ("sound-recording-inline-waveform-gray.png");
			callback (thumb);
		}

		private void DismissView () {
			this.View.RemoveFromSuperview ();
			if (this.Plot != null) {
				this.Plot.Dispose ();
				this.Plot = null;
			}
		}

		private class WaveFormWork : Work {
			public string Id { get; set; }
			public string SoundFilePath { get; set; }
			public SoundWaveformGenerator Generator { get; set; }
			public Action<UIImage> Callback { get; set; }

			public WaveFormWork (string soundFilePath, SoundWaveformGenerator generator, Action<UIImage> callback) {
				this.SoundFilePath = soundFilePath;
				this.Generator = generator;
				this.Callback = callback;
				this.Id = Path.GetRandomFileName ();

			}

			public void Do () {
				this.Generator.DrawOnView (this.SoundFilePath, this.Callback);
			}

			public void Done () {
				this.SoundFilePath = null;
				this.Generator = null;
				this.Callback = null;
			}
		}
	}
}

