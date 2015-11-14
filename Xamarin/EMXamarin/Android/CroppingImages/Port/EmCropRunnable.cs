using System;
using Java.Lang;
using Java.Util.Concurrent;
using Android.Graphics;
using Com.Android.Camera.Gallery;

namespace Emdroid {
	public class EmCropRunnable : Java.Lang.Object, IRunnable {

		private WeakReference eciRef;
		private EmCropImage ECI { 
			get { return eciRef.Target as EmCropImage; }
			set { eciRef = new WeakReference (value); }
		}

		public EmCropRunnable (EmCropImage parent) {
			this.ECI = parent;
		}

		public void Run () {
			EmCropImage eci = this.ECI;

			if (eci == null) {
				return;
			}

			CountDownLatch latch = new CountDownLatch (1);

			Bitmap b = (eci.Image != null) ? eci.Image.FullSizeBitmap (IImageConstants.Unconstrained, 1024*1024) : eci.Bitmap;
			eci.MHandler.Post (() => {
				if (b != eci.Bitmap && b != null) {
					eci.ImageView.SetImageBitmapResetBase (b, true);
					eci.Bitmap.Recycle ();
					eci.Bitmap = b;
				}

				if (eci.ImageView.GetScale () == 1F) {
					eci.ImageView.Center (true, true);
				}

				latch.CountDown ();
			});

			try {
				latch.Await ();
			} catch (InterruptedException e) {
				throw new RuntimeException(e);
			}

			eci.FaceDetectionRunnable.Run ();
		}
	}
}

