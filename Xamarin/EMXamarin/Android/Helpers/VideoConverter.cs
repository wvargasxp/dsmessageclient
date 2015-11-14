using System;
using Com.Github.Hiteshsondhi88.Libffmpeg;
using Com.Github.Hiteshsondhi88.Libffmpeg.Exceptions;
using em;
using EMXamarin;
using System.Globalization;
using System.Collections.Generic;

namespace Emdroid {
	public class VideoConverter : IVideoConverter {

		private const string Tag = "VideoConverter: ";

		private static VideoConverter shared;
		public static VideoConverter Shared {
			get {
				if (shared == null) {
					shared = new VideoConverter ();
				}

				return shared;
			}
		}

        private bool _initialized = false;
		public bool Initialized { get { return this._initialized; } set { this._initialized = value; } }

        private bool _canConvertVideo = false;
		public bool CanConvertVideo { get { return this._canConvertVideo; } set { this._canConvertVideo = value; } }

		private FFmpeg instance;
		private FFmpeg Instance { 
			get {
				if (this.instance == null) {
					this.instance = FFmpeg.GetInstance (EMApplication.Instance.BaseContext);
					this.instance.SetCanRunMultipleCommands (true); // Allow FFmpeg to run multiple commands.
					this.instance.SetExecuteOnExecutor (true); // Don't block async tasks.
				}

				return this.instance;
			}
		}

		public VideoConverter () {
			Init ();
		}

		private void Init () {
			try {
				this.Instance.LoadBinary (FFmpegLoadBinaryResponseHandler.From (this));
			} catch (FFmpegNotSupportedException e) {
				System.Diagnostics.Debug.WriteLine ("{0}Init: {1}", Tag, e.LocalizedMessage);

				// If we're catching an exception, just consider it initialized.
				this.Initialized = true;
				this.CanConvertVideo = false;
			}
		}

        private bool _convertWhenReady = false;
		public bool ConvertWhenReady { get { return this._convertWhenReady; } set { this._convertWhenReady = value; } }

        private Action<bool> _onFinishCallback = null;
		private Action<bool> OnFinishCallback { get { return this._onFinishCallback; } set { this._onFinishCallback = value; } }

        private IList<ConvertVideoInstruction> _inMemoryInstructions = new List<ConvertVideoInstruction> ();
        public IList<ConvertVideoInstruction> InMemoryInstructions { get { return this._inMemoryInstructions; } set { this._inMemoryInstructions = value; } } 

		public void ConvertVideo (ConvertVideoInstruction instruction) {
			if (!this.Initialized) {
				this.ConvertWhenReady = true;
				this.InMemoryInstructions.Add (instruction);
			} else {
				// vcodec - x264
				// crf - constant rate factor, lowers bitrate by constant factor (23 is default), higher is lower quality
				// scale - set the size 640 depending on aspect ratio
				// acodec - copy, don't encode
				// preset/tune - optimize for performance
				//-vcodec libx264 
//				string command = string.Format ("-i {0} -vcodec libx264 -crf 26 -vf scale='if(gt(iw,ih),640,trunc(oh*a/2)*2)':'if(gt(iw,ih),trunc(ow/a/2)*2,640)' -acodec copy -preset ultrafast -tune fastdecode -tune zerolatency {1}", input, output);

				ApplicationModel.SharedPlatform.GetFileSystemManager ().RemoveFileAtPath (instruction.OutputPath);
				// This one seems to be faster.
				string command = string.Format ("-i {0} -vcodec mpeg4 -qscale:v 15 -acodec copy {1}", instruction.InputPath, instruction.OutputPath);

				Execute (command, instruction);

			}
		}

		public void Execute (string command, ConvertVideoInstruction instruction) {
			System.Diagnostics.Debug.Assert (this.Initialized == true, "Called execute, but VideoConverter has not initialized yet.");
			if (!this.CanConvertVideo) {
				System.Diagnostics.Debug.WriteLine ("{0}Execute {1}", Tag, "Can't convert video.");
				instruction.DidFinishInstruction (false);
			} else {
				try {
					string[] commandAsArray = command.Split (new string[] { " " }, StringSplitOptions.None);
					this.Instance.Execute (commandAsArray, FFmpegExecuteResponseHandler.From (this, instruction));
				} catch (FFmpegCommandAlreadyRunningException e) {
					System.Diagnostics.Debug.WriteLine ("{0}Execute: {1}", Tag, e.LocalizedMessage);
					instruction.DidFinishInstruction (false);
				}
			}
		}
	}

	public class FFmpegExecuteResponseHandler : Java.Lang.Object, IFFmpegExecuteResponseHandler {
		private VideoConverter This { get; set; }
		private ConvertVideoInstruction Instruction { get; set; }
		private const string Tag = "FFmpegExecuteResponseHandler: ";
		private long Start { get; set; }

		public static FFmpegExecuteResponseHandler From (VideoConverter self, ConvertVideoInstruction instruction) {
			FFmpegExecuteResponseHandler h = new FFmpegExecuteResponseHandler ();
			h.This = self;
			h.Instruction = instruction;
			return h;
		}

		public void OnFailure (string p0) {
			System.Diagnostics.Debug.WriteLine ("{0}OnFailure: {1}", Tag, p0);
			this.Instruction.DidFinishInstruction (false);
		}

		public void OnProgress (string p0) {
//			System.Diagnostics.Debug.WriteLine ("{0}OnProgress: {1}", Tag, p0);
		}

		public void OnSuccess (string p0) {
//			System.Diagnostics.Debug.WriteLine ("{0}OnSuccess: {1}", Tag, p0);
			long end = DateTime.Now.Ticks;
			long diff = (end - this.Start);
			System.Diagnostics.Debug.WriteLine (string.Format ("Conversion took {0}", TimeSpan.FromTicks (diff).Duration ()));
			ApplicationModel.SharedPlatform.GetFileSystemManager ().MoveFileAtPath (this.Instruction.OutputPath, this.Instruction.InputPath);
			this.Instruction.DidFinishInstruction (true);
		}

		public void OnFinish () {
//			System.Diagnostics.Debug.WriteLine (string.Format ("{0}OnFinish", Tag));
		}

		public void OnStart () {
//			System.Diagnostics.Debug.WriteLine (string.Format ("{0}OnStart", Tag));
			this.Start = DateTime.Now.Ticks;
		}
	}

	public class FFmpegLoadBinaryResponseHandler : Java.Lang.Object, IFFmpegLoadBinaryResponseHandler {
		private VideoConverter This { get; set; }
		private const string Tag = "FFmpegLoadBinaryResponseHandler: ";

		public static FFmpegLoadBinaryResponseHandler From (VideoConverter self) {
			FFmpegLoadBinaryResponseHandler h = new FFmpegLoadBinaryResponseHandler ();
			h.This = self;
			return h;
		}

		public void OnFailure () {
//			System.Diagnostics.Debug.WriteLine (string.Format ("{0}OnFailure", Tag));
			this.This.CanConvertVideo = false;
		}

		public void OnSuccess () {
//			System.Diagnostics.Debug.WriteLine (string.Format ("{0}OnSuccess", Tag));
			this.This.CanConvertVideo = true;
		}

		public void OnFinish () {
			this.This.Initialized = true;

//			System.Diagnostics.Debug.WriteLine (string.Format ("{0}OnFinish", Tag));
			if (this.This.ConvertWhenReady) {
				IList<ConvertVideoInstruction> instructions = this.This.InMemoryInstructions;
				foreach (ConvertVideoInstruction instruction in instructions) {
					this.This.ConvertVideo (instruction);
				}

				instructions.Clear ();

				this.This.ConvertWhenReady = false;
			}
		}

		public void OnStart () {
//			System.Diagnostics.Debug.WriteLine (string.Format ("{0}OnStart", Tag));
		}
	}

}