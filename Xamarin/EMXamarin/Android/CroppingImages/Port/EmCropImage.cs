using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Android.Camera;
using Android.Graphics;
using Com.Android.Camera.Gallery;
using Android.Provider;
using Java.Lang;
using Java.IO;
using System.IO;
using Java.Util.Concurrent;

namespace Emdroid {
	[Activity (Label = "EmCropImage")]			
	public class EmCropImage : MonitoredActivity {
		private const string TAG = "CropImage";

		// These are various options can be specified in the intent.
        private Bitmap.CompressFormat _outputFormat = Bitmap.CompressFormat.Jpeg; // only used with mSaveUri
		private Bitmap.CompressFormat OutputFormat { get { return this._outputFormat; } set { this._outputFormat = value; } } 

        private int _outputQuality = 100; // only used with mSaveUri and JPEG format
		private int OutputQuality { get { return this._outputQuality; } set { this._outputQuality = value; } }

        private Android.Net.Uri _saveUri = null;
		private Android.Net.Uri SaveUri { get { return this._saveUri; } set { this._saveUri = value; } }

		public int AspectX { get; set; }
		public int AspectY { get; set; }

        private bool _circleCropping = false;
		public bool CircleCropping { get { return this._circleCropping;} set { this._circleCropping = value; } }

        private Handler _mHandler = new Handler ();
		public Handler MHandler { get { return this._mHandler; } set { this._mHandler = value; } }

        private bool _waitingToPick = false;
		public bool WaitingToPick { get { return this._waitingToPick; } set { this._waitingToPick = value; } }

		// These options specifiy the output image size and whether we should
		// scale the output to fit it (or just crop it).
		private int OutputX { get; set; }  
		private int OutputY { get; set; }
		private bool Scale { get; set; }

        private bool _shouldScaleUp = true;
        private bool ShouldScaleUp { get { return this._shouldScaleUp; } set { this._shouldScaleUp = value; } }

		public bool Saving { get; set; }  // Whether the "save" button is already clicked.

		public EmCropImageView ImageView { get; set; }
		public Bitmap Bitmap { get; set; }
		public EmHighlightView MCrop { get; set; }

		private IImageList AllImages { get; set; }
		public IImage Image { get; set; }

		public Color MOutlineColor { get; set; } // not supported
		public Color MOutlineCircleColor { get; set; } // not supported

        private bool _doingFaceDetection = false;
		public bool DoingFaceDetection { get { return this._doingFaceDetection; } set { this._doingFaceDetection = value; } }

		private Button DiscardButton { get; set; }
		private Button SaveButton { get; set; }

		public IRunnable FaceDetectionRunnable { get; set; }
		public IRunnable MainCropRunnable { get; set; }

		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			// Create your application here

			this.RequestWindowFeature (WindowFeatures.NoTitle);
			this.SetContentView (Resource.Layout.croppyimage);

			this.ImageView = this.FindViewById<EmCropImageView> (Resource.Id.image);


			// Work-around for devices incapable of using hardware-accelerated clipPath.
			// (android.view.GLES20Canvas.clipPath)
			//
			// See also:
			// - https://code.google.com/p/android/issues/detail?id=20474
			// - https://github.com/lvillani/android-croppyimage/issues/20
			if (Build.VERSION.SdkInt > BuildVersionCodes.Gingerbread && Build.VERSION.SdkInt < BuildVersionCodes.JellyBean) {
				this.ImageView.SetLayerType (LayerType.Software, null);
			}

			Intent intent = this.Intent;
			Bundle extras = intent.Extras;

			if (extras != null) {
				if (extras.GetBoolean ("circleCrop", false)) {
					this.CircleCropping = true;
					this.AspectX = 1;
					this.AspectY = 1;
					this.OutputFormat = Bitmap.CompressFormat.Png;
				}

				this.SaveUri = (Android.Net.Uri) extras.GetParcelable (MediaStore.ExtraOutput);
				if (this.SaveUri != null) {
					string outputFormatString = extras.GetString ("outputFormat");
					if (outputFormatString != null) {
						this.OutputFormat = Bitmap.CompressFormat.ValueOf (outputFormatString);
					}
					this.OutputQuality = extras.GetInt ("outputQuality", 100);
				} 

				this.Bitmap = (Bitmap)extras.GetParcelable ("data");
				this.AspectX = extras.GetInt ("aspectX");
				this.AspectY = extras.GetInt ("aspectY");
				this.OutputX = extras.GetInt ("outputX");
				this.OutputY = extras.GetInt ("outputY");

				this.Scale = extras.GetBoolean ("scale", true);
				this.ShouldScaleUp = extras.GetBoolean ("scaleUpIfNeeded", true);
			}

			if (this.Bitmap == null) {
				Android.Net.Uri target = intent.Data;
				this.AllImages = ImageManager.MakeImageList (ContentResolver, target, ImageManager.SortAscending);
				this.Image = this.AllImages.GetImageForUri (target);
				if (this.Image != null) {
					// Don't read in really large bitmaps. Use the (big) thumbnail
					// instead.
					// TODO when saving the resulting bitmap use the
					// decode/crop/encode api so we don't lose any resolution.
					this.Bitmap = this.Image.ThumbBitmap (IImageConstants.RotateAsNeeded);
				}
			}

			if (this.Bitmap == null) {
				this.Finish ();
				return;
			}

			this.Window.AddFlags (WindowManagerFlags.Fullscreen);

			this.DiscardButton = this.FindViewById<Button> (Resource.Id.discard);
			this.SaveButton = this.FindViewById<Button> (Resource.Id.save);

			this.DiscardButton.Click += (object sender, EventArgs e) => {
				this.SetResult (Result.Canceled);
				this.Finish ();
			};

			this.SaveButton.Click += (object sender, EventArgs e) => {
				OnSaveClicked ();
			};

			this.FaceDetectionRunnable = new EmFaceDetectionRunnable (this);
			this.MainCropRunnable = new EmCropRunnable (this);

			StartFaceDetection ();
		}

		private void StartFaceDetection () {
			if (this.IsFinishing) {
				return; 
			}

			this.ImageView.SetImageBitmapResetBase (this.Bitmap, true);

			Util.StartBackgroundJob (this, null, "LOADING".t (), this.MainCropRunnable, this.MHandler);
		}

		private void OnSaveClicked () {
			// TODO this code needs to change to use the decode/crop/encode single
			// step api so that we don't require that the whole (possibly large)
			// bitmap doesn't have to be read into memory
			if (this.MCrop == null) {
				return;
			}

			if (this.Saving) {
				return;
			}

			this.Saving = true;
			Bitmap croppedImage;

			// If the output is required to a specific size, create an new image
			// with the cropped image in the center and the extra space filled.
			if (this.OutputX != 0 && this.OutputY != 0 && !this.Scale) {
				// Don't scale the image but instead fill it so it's the
				// required dimension

				croppedImage = Bitmap.CreateBitmap (this.OutputX, this.OutputY, Bitmap.Config.Rgb565);

				Canvas canvas = new Canvas (croppedImage);
				Rect srcRect = this.MCrop.GetCropRect ();
				Rect dstRect = new Rect (0, 0, this.OutputX, this.OutputY);

				int dx = (srcRect.Width () - dstRect.Width ()) / 2;
				int dy = (srcRect.Height () - dstRect.Height ()) / 2;

				// If the srcRect is too big, use the center part of it.
				srcRect.Inset (Java.Lang.Math.Max (0, dx), Java.Lang.Math.Max (0, dy));

				// If the dstRect is too big, use the center part of it.
				dstRect.Inset (Java.Lang.Math.Max (0, -dx), Java.Lang.Math.Max (0, -dy));

				// Draw the cropped bitmap in the center
				canvas.DrawBitmap (this.Bitmap, srcRect, dstRect, null);

				// Release bitmap memory as soon as possible
				this.ImageView.Clear ();
				this.Bitmap.Recycle ();

			} else {
				Rect r = this.MCrop.GetCropRect ();
				
				int width = r.Width ();
				int height = r.Height ();
			
				croppedImage = Bitmap.CreateBitmap (width, height, Bitmap.Config.Rgb565);
				
								Canvas canvas = new Canvas(croppedImage);
								Rect dstRect = new Rect(0, 0, width, height);
				canvas.DrawBitmap (this.Bitmap, r, dstRect, null);
				
								// Release bitmap memory as soon as possible
				this.ImageView.Clear ();
				this.Bitmap.Recycle ();

				// If the required dimension is specified, scale the image.
				if (this.OutputX != 0 && this.OutputY != 0 && this.Scale) {
					croppedImage = Util.Transform (new Matrix(), croppedImage, this.OutputX, this.OutputY, this.ShouldScaleUp, Util.RecycleInput);
				}
			}

			this.ImageView.SetImageBitmapResetBase (croppedImage, true);
			this.ImageView.Center (true, true);
			this.ImageView.MotionHighlightViews.Clear ();

			Bundle myExtras = this.Intent.Extras;
			if (myExtras != null && myExtras.GetParcelable ("data") != null || myExtras.GetBoolean ("return-data")) {
								Bundle extras = new Bundle ();
				extras.PutParcelable ("data", croppedImage);
					this.SetResult(Result.Ok, (new Intent()).SetAction ("inline-data").PutExtras (extras));
				this.Finish ();
			} else {
				Bitmap b = croppedImage;
				int waitingStringId = Resource.String.WAITING;
				Util.StartBackgroundJob (this, null, this.Resources.GetString (waitingStringId), new Runnable (() => {
					SaveOutput (b);
				}), this.MHandler);
			}
		}

		private void SaveOutput (Bitmap croppedImage) {
			if (this.SaveUri != null) {
				using (Stream outputStream = ContentResolver.OpenOutputStream (this.SaveUri)) {
					try {
						if (outputStream != null) {
							croppedImage.Compress (this.OutputFormat, this.OutputQuality, outputStream);

						}
					} catch (System.IO.IOException ex) {
						System.Diagnostics.Debug.WriteLine ("Cannot open file {0} {1}", this.SaveUri, ex);
					}
				}

				Bundle extras = new Bundle ();
				this.SetResult (Result.Ok, new Intent(this.SaveUri.ToString ()).PutExtras (extras));
			} else {
				Bundle extras = new Bundle();
				extras.PutString ("rect", this.MCrop.GetCropRect ().ToString ());
				
				Java.IO.File oldPath = new Java.IO.File (this.Image.DataPath);
				Java.IO.File directory = new Java.IO.File (oldPath.Parent);

				int x = 0;
				string fileName = oldPath.Name;
				fileName = fileName.Substring (0, fileName.LastIndexOf ("."));

				// Try file-1.jpg, file-2.jpg, ... until we find a filename which
				// does not exist yet.
				while (true) {
					x += 1;
					string candidate = directory.ToString () + "/" + fileName + "-" + x + ".jpg";
					bool exists = (new Java.IO.File (candidate)).Exists ();
					if (!exists) {
						break;
					}
				}

				try {
					int[] degree = new int [1];
					Android.Net.Uri newUri = ImageManager.AddImage (ContentResolver, 
						this.Image.Title, 
						this.Image.DateTaken, 
						null, 
						directory.ToString (), 
						fileName + "-" + x + ".jpg", croppedImage, null, 
						degree);
					this.SetResult (Result.Ok, new Intent().SetAction (newUri.ToString()).PutExtras (extras));
				} catch (Java.Lang.Exception ex) {
					// basically ignore this or put up
					// some ui saying we failed
					System.Diagnostics.Debug.WriteLine ("storge image fail, continue anyway {0}", ex);
				}
			}

			Bitmap b = croppedImage;
			this.MHandler.Post (() => {
				this.ImageView.Clear ();
				b.Recycle ();
			});

			this.Finish ();
		}

		protected override void OnPause () {
			base.OnPause ();
		}

		protected override void OnDestroy () {
			if (this.AllImages != null) {
				this.AllImages.Close ();
			}

			base.OnDestroy ();
		}
	}
}

