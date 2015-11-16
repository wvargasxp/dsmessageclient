using System;
using AVFoundation;
using CoreGraphics;
using Foundation;
using CoreVideo;
using CoreMedia;
using CoreFoundation;
using System.Diagnostics;
using EMXamarin;
using em;

namespace iOS {
	public class VideoConverter : IVideoConverter {

		private static VideoConverter shared;
		public static VideoConverter Shared {
			get {
				if (shared == null) {
					shared = new VideoConverter ();
				}

				return shared;
			}
		}
			
		#region IVideoConverter implementation
		public void ConvertVideo (ConvertVideoInstruction instruction) {
			// Currently, we convert the video after selecting it on iOS.
			// So ignore encoding instructions from the outgoing queue.
			if (instruction.FromOutgoingQueue) {
				instruction.DidFinishInstruction (true);
				return;
			}

			NSUrl inputUrl = new NSUrl ("file://" + Uri.EscapeUriString (instruction.InputPath));
			NSUrl outputUrl = new NSUrl ("file://" + Uri.EscapeUriString (instruction.OutputPath));

			// This is where converting the video starts.
			AVAsset avAsset = AVAsset.FromUrl (inputUrl);
			string[] compatiblePresets = AVAssetExportSession.ExportPresetsCompatibleWithAsset (avAsset);
			foreach (string preset in compatiblePresets) {
				if (preset.Equals ("AVAssetExportPresetLowQuality")) {
					AVAssetExportSession exportSession = new AVAssetExportSession (avAsset, "AVAssetExportPresetMediumQuality");
					//AVAssetExportSession exportSession = new AVAssetExportSession (avAsset, "AVAssetExportPreset640x480");
					exportSession.OutputUrl = outputUrl;
					exportSession.OutputFileType = AVFileType.Mpeg4;
					exportSession.ShouldOptimizeForNetworkUse = true;
					exportSession.ExportAsynchronously (() => {
						switch ( exportSession.Status ) {
						default:
						case AVAssetExportSessionStatus.Cancelled:
						case AVAssetExportSessionStatus.Failed:
							{
								Debug.WriteLine(string.Format("Export of mov file failed {0}", exportSession.Status));
								instruction.DidFinishInstruction (false);
								break;
							}
						case AVAssetExportSessionStatus.Completed:
							{
								ApplicationModel.SharedPlatform.GetFileSystemManager ().MoveFileAtPath (instruction.OutputPath, instruction.InputPath);
								instruction.DidFinishInstruction (true);
								break;
							}
						}

						// TODO Release seems to cause this to crash.
						//exportSession.Release();
					});
					break;
				}
			}
		}
		#endregion

		static AVAssetWriter videoWriter;
		static AVAsset videoAsset;
		static AVAssetTrack videoTrack;
		static AVAssetWriterInput videoWriterInput;
		static AVAssetReaderTrackOutput videoReaderOutput;
		static AVAssetReader videoReader;
		static AVAssetWriterInput audioWriterInput;
		static AVAssetTrack audioTrack;
		static AVAssetReaderOutput audioReaderOutput;
		static AVAssetReader audioReader;

		static CMTime lastPresentationTimeStamp;

		public static void ConvertVideoToLowQuality (NSUrl inputUrl, NSUrl outputUrl, Action beginCallback, Action endCallback) {
			/* broken due to UNIFIED API changes, TODO: fix if needed
			NSError err = null;
			beginCallback ();

			videoAsset = AVAsset.FromUrl (inputUrl);

			videoTrack = videoAsset.TracksWithMediaType (AVMediaType.Video) [0];
			CGSize videoSize = videoTrack.NaturalSize;

			int bitRate = 125000;// 1250000;
			NSNumber compressionInt = new NSNumber (bitRate);

			NSDictionary videoWriterCompressionSettings = NSDictionary.FromObjectsAndKeys (
				new NSObject[]{compressionInt}, 
				new NSObject[]{AVVideo.AverageBitRateKey});

			NSNumber videoWidthFloat = new NSNumber (videoSize.Width);
			NSNumber videoHeightFloat = new NSNumber (videoSize.Height);

			NSDictionary videoWriterSettings = NSDictionary.FromObjectsAndKeys (
				new NSObject[]{AVVideo.CodecH264, videoWriterCompressionSettings, videoWidthFloat, videoHeightFloat}, 
				new NSObject[]{AVVideo.CodecKey, AVVideo.CompressionPropertiesKey, AVVideo.WidthKey, AVVideo.HeightKey});
				
			videoWriterInput = new AVAssetWriterInput (AVMediaType.Video, videoWriterSettings);

			videoWriterInput.ExpectsMediaDataInRealTime = true;
			videoWriterInput.Transform = videoTrack.PreferredTransform;

			videoWriter = new AVAssetWriter (outputUrl, AVFileType.QuickTimeMovie, out err);
			videoWriter.AddInput (videoWriterInput);

			if (err != null)
				Debug.WriteLine ("ConvertVideoToLowQuality: { videoWriter = new AVAssetWriter (outputUrl, AVFileType.QuickTimeMovie, out err); } " + err.Description);

			// setup video reader
			CVPixelFormatType pixelFormatType = CVPixelFormatType.CV420YpCbCr8BiPlanarVideoRange;
			NSDictionary videoReaderSettings = NSDictionary.FromObjectAndKey (NSNumber.FromObject (pixelFormatType), CVPixelBuffer.PixelFormatTypeKey);

			videoReaderOutput = new AVAssetReaderTrackOutput (videoTrack, videoReaderSettings);

			videoReader = new AVAssetReader (videoAsset, out err);
			if (err != null)
				Debug.WriteLine ("ConvertVideoToLowQuality: { videoReader = new AVAssetReader (videoAsset, out err); } " + err.Description);

			videoReader.AddOutput (videoReaderOutput);

			// setup audio writer
			audioWriterInput = new AVAssetWriterInput (AVMediaType.Audio, null);
			audioWriterInput.ExpectsMediaDataInRealTime = false;
			videoWriter.AddInput (audioWriterInput);

			// setup audio reader
			audioTrack = videoAsset.TracksWithMediaType (AVMediaType.Audio) [0];

			audioReaderOutput = new AVAssetReaderTrackOutput (audioTrack, null);
			audioReader = new AVAssetReader (videoAsset, out err);
			if (err != null)
				Debug.WriteLine ("ConvertVideoToLowQuality: { audioReader = new AVAssetReader (videoAsset, out err); " + err.Description);
			audioReader.AddOutput (audioReaderOutput);
			videoWriter.StartWriting ();

			//start writing from video reader
			videoReader.StartReading ();
			videoWriter.StartSessionAtSourceTime (CMTime.Zero);

			DispatchQueue processingQueue = new DispatchQueue ("processingQueue1");

			videoWriterInput.RequestMediaData (processingQueue, () => {
				while (videoWriterInput.ReadyForMoreMediaData) {
					CMSampleBuffer sampleBuffer;
					if (videoReader.Status == AVAssetReaderStatus.Reading) {
						sampleBuffer = videoReaderOutput.CopyNextSampleBuffer ();
						if (sampleBuffer != null && sampleBuffer.IsValid) {
							videoWriterInput.AppendSampleBuffer (sampleBuffer);
							sampleBuffer.Invalidate ();
						} else {
							VideoWriterFinished (endCallback);
						}
					} else {
						VideoWriterFinished (endCallback);
					}

				}
			});

		*/
				
		}

		private static void VideoWriterFinished (Action endCallback) {
			videoWriterInput.MarkAsFinished ();
			if (videoReader.Status == AVAssetReaderStatus.Completed) {
				audioReader.StartReading ();
				videoWriter.StartSessionAtSourceTime (CMTime.Zero);
				DispatchQueue processingQueue2 = new DispatchQueue ("processingQueue2");
				audioWriterInput.RequestMediaData (processingQueue2, () => {
					while (audioWriterInput.ReadyForMoreMediaData) {
						CMSampleBuffer sampleBuffer2;
						if (audioReader.Status == AVAssetReaderStatus.Reading) {
							sampleBuffer2 = audioReaderOutput.CopyNextSampleBuffer ();
							if (sampleBuffer2 != null && sampleBuffer2.IsValid) {
								audioWriterInput.AppendSampleBuffer (sampleBuffer2);
								lastPresentationTimeStamp = sampleBuffer2.PresentationTimeStamp;
								sampleBuffer2.Invalidate ();
							} else {
								AudioWriterFinished (endCallback);
							}
						} else {
							AudioWriterFinished (endCallback);
						}
					}
				});

			}
		}

		private static void AudioWriterFinished (Action endCallback) {
			audioWriterInput.MarkAsFinished ();
			if (audioReader.Status == AVAssetReaderStatus.Completed) {
				videoWriter.EndSessionAtSourceTime (lastPresentationTimeStamp);
				videoWriter.FinishWriting (() => {
					endCallback ();
					Dispose ();
				});
			}
		}

		private static void Dispose () {
			videoWriter.Dispose ();
			videoAsset.Dispose ();
			videoTrack.Dispose ();
			videoWriterInput.Dispose ();
			videoReaderOutput.Dispose ();
			videoReader.Dispose ();
			audioWriterInput.Dispose ();
			audioTrack.Dispose ();
			audioReaderOutput.Dispose ();
			audioReader.Dispose ();

			videoWriter = null;
			videoAsset = null;
			videoTrack = null;
			videoWriterInput = null;
			videoReaderOutput = null;
			videoReader = null;
			audioWriterInput = null;
			audioTrack = null;
			audioReaderOutput = null;
			audioReader = null;
		}

	}
}

