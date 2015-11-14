using System;
using Android.Graphics;
using Com.Android.Camera;
using Android.Content;
using Android.Provider;

namespace Emdroid {
	public class EmCropImageIntentBuilder {
		private static string EXTRA_ASPECT_X = "aspectX";
		private static string EXTRA_ASPECT_Y = "aspectY";
		private static string EXTRA_OUTPUT_X = "outputX";
		private static string EXTRA_OUTPUT_Y = "outputY";
		private static string EXTRA_BITMAP_DATA = "data";
		private static string EXTRA_SCALE = "scale";
		private static string EXTRA_SCALE_UP_IF_NEEDED = "scaleUpIfNeeded";
		private static string EXTRA_NO_FACE_DETECTION = "noFaceDetection";
		private static string EXTRA_CIRCLE_CROP = "circleCrop";
		private static string EXTRA_OUTPUT_FORMAT = "outputFormat";
		private static string EXTRA_OUTPUT_QUALITY = "outputQuality";
		private static string EXTRA_OUTLINE_COLOR = "outlineColor";
		private static string EXTRA_OUTLINE_CIRCLE_COLOR = "outlineCircleColor";

		private static int DEFAULT_SCALE = 1;

        private bool _scale = true;
		public bool Scale { get { return this._scale; } set { this._scale = value; } }

        private bool _scaleUpIfNeeded = true;
		public bool ScaleUpIfNeeded { get { return this._scaleUpIfNeeded; } set { this._scaleUpIfNeeded = value; } }

        private bool _circleCrop = false;
		public bool CircleCrop { get { return this._circleCrop; } set { this._circleCrop = value; } }

        private string _outputFormat = null;
		public string OutputFormat { get { return this._outputFormat; } set { this._outputFormat = value; } }

        private int _outputQuality = 100;
        public int OutputQuality { get { return this._outputQuality; } set { this._outputQuality = value; } }

		public Android.Net.Uri SourceImage { get; set; }
		public Bitmap Bitmap { get; set; }

        private int _outlineColor = HighlightView.DefaultOutlineColor;
		public int OutlineColor { get { return this._outlineColor; } set { this._outlineColor = value; } }

        private int _outlineCircleColor = HighlightView.DefaultOutlineCircleColor;
		public int OutlineCircleColor { get { return this._outlineCircleColor; } set { this._outlineCircleColor = value; } } 

		public int AspectX { get; set; }
		public int AspectY { get; set; }
		public int OutputX { get; set; }
		public int OutputY { get; set; }
		public Android.Net.Uri SaveUri { get; set; }

		public EmCropImageIntentBuilder (int outputX, int outputY, Android.Net.Uri saveUri) {
			Initialize (DEFAULT_SCALE, DEFAULT_SCALE, outputX, outputY, saveUri);
		}

		public EmCropImageIntentBuilder (int aspectX, int aspectY, int outputX,
			int outputY, Android.Net.Uri saveUri) {
			Initialize (aspectX, aspectY, outputX, outputY, saveUri);
		}

		private void Initialize (int aspectX, int aspectY, int outputX,
			int outputY, Android.Net.Uri saveUri) {
			this.AspectX = aspectX;
			this.AspectY = aspectY;
			this.OutputX = outputX;
			this.OutputY = outputY;
			this.SaveUri = saveUri;
		}

		public Intent GetIntent (Context context) {
			Intent intent = new Intent (context, typeof (EmCropImage));

			//
			// Required Intent extras.
			//
			intent.PutExtra (EXTRA_ASPECT_X, this.AspectX);
			intent.PutExtra (EXTRA_ASPECT_Y, this.AspectY);
			intent.PutExtra (EXTRA_OUTPUT_X, this.OutputX);
			intent.PutExtra (EXTRA_OUTPUT_Y, this.OutputY);
			intent.PutExtra (MediaStore.ExtraOutput, this.SaveUri);

			//
			// Optional Intent Extras
			//
			intent.PutExtra (EXTRA_SCALE, this.Scale);
			intent.PutExtra (EXTRA_SCALE_UP_IF_NEEDED, this.ScaleUpIfNeeded);
			intent.PutExtra (EXTRA_CIRCLE_CROP, this.CircleCrop);
			intent.PutExtra (EXTRA_OUTPUT_FORMAT, this.OutputFormat);
			intent.PutExtra (EXTRA_OUTPUT_QUALITY, this.OutputQuality);
			intent.PutExtra (EXTRA_OUTLINE_COLOR, this.OutlineColor);
			intent.PutExtra (EXTRA_OUTLINE_CIRCLE_COLOR, this.OutlineCircleColor);

			if (this.Bitmap != null) {
				intent.PutExtra (EXTRA_BITMAP_DATA, this.Bitmap);
			}

			if (this.SourceImage != null) {
				intent.SetData (this.SourceImage);
			}

			return intent;
		}
	}
}

