using System;
using em;
using Android.Media;
using Java.IO; 
using Com.Enc.Emencoding;

namespace Emdroid {
	public class NativeVideoConverter : IVideoConverter  {

		private static NativeVideoConverter _shared = null;
		public static NativeVideoConverter Shared {
			get { 
				if (_shared == null) {
					_shared = new NativeVideoConverter ();
				}

				return _shared;
			}
		}

		public NativeVideoConverter () {}

		#region IVideoConverter implementation

		public void ConvertVideo (ConvertVideoInstruction instruction) {
			// Not supporting below API level 18.
			if (EMApplication.SDK_VERSION < Android.OS.BuildVersionCodes.JellyBeanMr2) {
				instruction.DidFinishInstruction (false);
				return;
			}

			EMTask.Dispatch (() => {
				VideoResolutionChanger changer = new VideoResolutionChanger ();
				changer.SetExpectedWidth (1280);
				changer.SetExpectedHeight (640);
				changer.SetExpectedBitRate (1024 * 1024);
				changer.SetExpectedIFrameInterval (10);

				Java.IO.File f = new Java.IO.File (instruction.InputPath);
				try {
					changer.ChangeResolution (f, OnEncodingCompletionHandler.From (instruction));
				} catch (Exception e) {
					instruction.DidFinishInstruction (false);
				}
			}, EMTask.VIDEO_ENCODING);
		}

		#endregion
	}

	class OnEncodingCompletionHandler : Java.Lang.Object, IEncodingCompletion {
		private ConvertVideoInstruction Instruction { get; set; }

		public static OnEncodingCompletionHandler From (ConvertVideoInstruction instruction) {
			OnEncodingCompletionHandler g = new OnEncodingCompletionHandler ();
			g.Instruction = instruction;
			return g;
		}

		#region IEncodingCompletion implementation
		public void OnEncodingSuccess (string p0) {
			string outputPath = p0;
			ApplicationModel.SharedPlatform.GetFileSystemManager ().MoveFileAtPath (outputPath, this.Instruction.InputPath);
			this.Instruction.DidFinishInstruction (true);
		}
		#endregion
	}
}

