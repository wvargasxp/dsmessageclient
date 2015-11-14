using System;
using Java.Lang;
using Android.Graphics;
using Android.Media;
using Com.Android.Camera;
using Android.Widget;

namespace Emdroid {
	public class EmFaceDetectionRunnable : Java.Lang.Object, IRunnable {

        private float sc = 1F;
		private float Scale { get { return this.sc; } set { this.sc = value; } }

		private Matrix MImageMatrix { get; set; }

        private FaceDetector.Face[] fcs = new FaceDetector.Face [3];
        private FaceDetector.Face[] Faces { get { return this.fcs; } set { this.fcs = value; } }

		private int NumberOfFaces { get; set; }

		private WeakReference eciRef;
		private EmCropImage ECI { 
			get { return eciRef.Target as EmCropImage; }
			set { eciRef = new WeakReference (value); }
		}

		public EmFaceDetectionRunnable (EmCropImage parent) {
			this.ECI = parent;
		}

		// For each face, we create a HightlightView for it.
		private void HandleFace (FaceDetector.Face f) {
			EmCropImage eci = this.ECI;
			if (eci == null) {
				return;
			}

			PointF midPoint = new PointF();

			int r = ((int) (f.EyesDistance () * this.Scale)) * 2;
			f.GetMidPoint (midPoint);
			midPoint.X *= this.Scale;
			midPoint.Y *= this.Scale;

			int midX = (int) midPoint.X;
			int midY = (int) midPoint.Y;

			EmHighlightView hv = new EmHighlightView (eci.ImageView, eci.MOutlineColor, eci.MOutlineCircleColor);

			int width = eci.Bitmap.Width;
			int height = eci.Bitmap.Height;

			Rect imageRect = new Rect (0, 0, width, height);

			RectF faceRect = new RectF (midX, midY, midX, midY);
			faceRect.Inset (-r, -r);
			if (faceRect.Left < 0) {
				faceRect.Inset (-faceRect.Left, -faceRect.Left);
			}

			if (faceRect.Top < 0) {
				faceRect.Inset (-faceRect.Top, -faceRect.Top);
			}

			if (faceRect.Right > imageRect.Right) {
				faceRect.Inset (faceRect.Right - imageRect.Right, faceRect.Right - imageRect.Right);
			}

			if (faceRect.Bottom > imageRect.Bottom) {
				faceRect.Inset (faceRect.Bottom - imageRect.Bottom, faceRect.Bottom - imageRect.Bottom);
			}

			hv.Setup (this.MImageMatrix, imageRect, faceRect, eci.CircleCropping, eci.AspectX != 0 && eci.AspectY != 0);

			eci.ImageView.Add (hv);
		}

		private void MakeDefault () {

			EmCropImage eci = this.ECI;
			if (eci == null) {
				return;
			}

			EmHighlightView hv = new EmHighlightView (eci.ImageView, eci.MOutlineColor, eci.MOutlineCircleColor);

			int width = eci.Bitmap.Width;
			int height = eci.Bitmap.Height;

			Rect imageRect = new Rect (0, 0, width, height);

			// make the default size about 4/5 of the width or height
			int cropWidth = Java.Lang.Math.Min (width, height) * 4 / 5;
			int cropHeight = cropWidth;

			if (eci.AspectX != 0 && eci.AspectY != 0) {
				if (eci.AspectX > eci.AspectY) {
					cropHeight = cropWidth * eci.AspectY / eci.AspectX;
				} else {
					cropWidth = cropHeight * eci.AspectX / eci.AspectY;
				}
			}

			int x = (width - cropWidth) / 2;
			int y = (height - cropHeight) / 2;

			RectF cropRect = new RectF (x, y, x + cropWidth, y + cropHeight);
			hv.Setup (this.MImageMatrix, imageRect, cropRect, eci.CircleCropping,
				eci.AspectX != 0 && eci.AspectY != 0);
			eci.ImageView.Add (hv);
		}

		// Scale the image down for faster face detection.
		private Bitmap PrepareBitmap () {
			EmCropImage eci = this.ECI;
			if (eci == null) {
				return null;
			}

			if (eci.Bitmap == null ||  eci.Bitmap.IsRecycled) {
				return null;
			}

			// 256 pixels wide is enough.
			if (eci.Bitmap.Width > 256) {
				this.Scale = 256.0F / eci.Bitmap.Width;
			}

			Matrix matrix = new Matrix();
			matrix.SetScale (this.Scale, this.Scale);
			Bitmap faceBitmap = Bitmap.CreateBitmap (eci.Bitmap, 0, 0, eci.Bitmap
				.Width, eci.Bitmap.Height, matrix, true);
			return faceBitmap;
		}

		public void Run () { 
			EmCropImage eci = this.ECI;
			if (eci == null) {
				return;
			}

			this.MImageMatrix = eci.ImageView.ImageMatrix;
			Bitmap faceBitmap = PrepareBitmap ();
			if (faceBitmap != null) {
				this.Scale = 1.0F / this.Scale;
				if (faceBitmap != null && eci.DoingFaceDetection) {
					FaceDetector detector = new FaceDetector (faceBitmap.Width,
						faceBitmap.Height, this.Faces.Length);
					this.NumberOfFaces = detector.FindFaces (faceBitmap, this.Faces);
				}

				if (faceBitmap != null && faceBitmap != eci.Bitmap) {
					faceBitmap.Recycle ();
				}
			}

			eci.MHandler.Post (() => {
				eci.WaitingToPick = this.NumberOfFaces > 1;

				if (this.NumberOfFaces > 0) {
					for (int i = 0; i < this.NumberOfFaces; i++) {
						HandleFace (this.Faces[i]);
					}
				} else {
					MakeDefault ();
				}

				eci.ImageView.Invalidate ();

				if (eci.ImageView.MotionHighlightViews.Count == 1) {
					eci.MCrop = eci.ImageView.MotionHighlightViews [0];
					eci.MCrop.Focused = true;
				}

				// Translation Note: I have no idea how to get to this area of code.
				// It has something to do with face recognition.
				if (this.NumberOfFaces > 1) {
					Toast t = Toast.MakeText (eci,
						"Touch a face to begin.",
						ToastLength.Short);
					t.Show ();
				}
			});
		}
	}
}