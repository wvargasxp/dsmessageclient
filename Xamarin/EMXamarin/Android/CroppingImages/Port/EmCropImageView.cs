using System;
using Com.Android.Camera;
using System.Collections.Generic;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Graphics;

namespace Emdroid {
	public class EmCropImageView : ImageViewTouchBase {

        private IList<EmHighlightView> mHVs = new List<EmHighlightView>();
		public IList<EmHighlightView> MotionHighlightViews { get { return this.mHVs; } set { this.mHVs = value; } } 

        private EmHighlightView mHV = null;
        public EmHighlightView MotionHighlightView { get { return this.mHV; } set { this.mHV = value; } }

        private float LastX { get; set; }
        private float LastY { get; set; }
        private int MotionEdge { get; set; }

		public EmCropImageView (Context context) : base (context) {}

		public EmCropImageView (Context context, IAttributeSet attrs) : base (context, attrs) {}

		protected override void OnLayout (bool changed, int left, int top, int right, int bottom) {
			base.OnLayout (changed, left, top, right, bottom);
			if (this.MBitmapDisplayed.Bitmap != null) {
				foreach (EmHighlightView hv in this.MotionHighlightViews) {
					hv.Matrix.Set (this.ImageMatrix);
					hv.Invalidate ();
					if (hv.Focused) {
						CenterBasedOnHighlightView(hv);
					}
				}
			}
		}

		protected override void ZoomTo (float scale, float centerX, float centerY) {
			base.ZoomTo (scale, centerX, centerY);
			foreach (EmHighlightView hv in this.MotionHighlightViews) {
				hv.Matrix.Set (this.ImageMatrix);
				hv.Invalidate ();
			}
		}

		protected override void ZoomIn () {
			base.ZoomIn ();
			foreach (EmHighlightView hv in this.MotionHighlightViews) {
				hv.Matrix.Set (this.ImageMatrix);
				hv.Invalidate ();
			}
		}

		protected override void ZoomOut () {
			base.ZoomOut ();
			foreach (EmHighlightView hv in this.MotionHighlightViews) {
				hv.Matrix.Set (this.ImageMatrix);
				hv.Invalidate ();
			}
		}

		protected override void PostTranslate (float deltaX, float deltaY) {
			base.PostTranslate (deltaX, deltaY);
			int count = this.MotionHighlightViews.Count;
			for (int i = 0; i < count; i++) {
				EmHighlightView hv = this.MotionHighlightViews [i];
				hv.Matrix.PostTranslate (deltaX, deltaY);
				hv.Invalidate ();
			}
		}
		
		// According to the event's position, change the focus to the first
		// hitting cropping rectangle.
		private void RecomputeFocus (MotionEvent e) {
			int count = this.MotionHighlightViews.Count;

			for (int i = 0; i < count; i++) {
				EmHighlightView hv = this.MotionHighlightViews [i];
				hv.Focused = false;
				hv.Invalidate ();
			}

			for (int i = 0; i < count; i++) {
				EmHighlightView hv = this.MotionHighlightViews [i];
				int edge = hv.GetHit (e.GetX (), e.GetY ());
				if (edge != HighlightView.GrowNone) {
					if (!hv.Focused) {
						hv.Focused = true;
						hv.Invalidate ();
					}
					break;
				}
			}

			this.Invalidate ();
		}

		public override bool OnTouchEvent (MotionEvent e) {

			EmCropImage cropImage = (EmCropImage)this.Context;
			if (cropImage.Saving) {
				return false;
			}

			switch (e.Action) {
			case MotionEventActions.Down:
				{
					if (cropImage.WaitingToPick) {
						RecomputeFocus (e);
					} else {
						for (int i = 0; i < this.MotionHighlightViews.Count; i++) {
							EmHighlightView hv = this.MotionHighlightViews [i];
							int edge = hv.GetHit (e.GetX (), e.GetY ());
							if (edge != HighlightView.GrowNone) {
								this.MotionEdge = edge;
								this.MotionHighlightView = hv;
								this.LastX = e.GetX ();
								this.LastY = e.GetY ();
								this.MotionHighlightView.SetMode (
									(edge == EmHighlightView.MOVE)
									? EmHighlightView.ModifyMode.Move
									: EmHighlightView.ModifyMode.Grow);
								break;
							}
						}
					}

					break;
				}
			case MotionEventActions.Up:
				{
					if (cropImage.WaitingToPick) {
						for (int i = 0; i < this.MotionHighlightViews.Count; i++) {
							EmHighlightView hv = this.MotionHighlightViews [i];
							if (hv.Focused) {
								cropImage.MCrop = hv;
								for (int j = 0; j < this.MotionHighlightViews.Count; j++) {
									if (j == i) {
										continue;
									}
									this.MotionHighlightViews [j].Hidden = true;
								}

								CenterBasedOnHighlightView (hv);
								((EmCropImage) this.Context).WaitingToPick = false;
								return true;
							}
						}
					} else {
						if (this.MotionHighlightView != null) {
							CenterBasedOnHighlightView (this.MotionHighlightView);
							this.MotionHighlightView.SetMode (EmHighlightView.ModifyMode.None);
						}
					}

					this.MotionHighlightView = null;
					break;
				}
			case MotionEventActions.Move:
				{
					if (cropImage.WaitingToPick) {
						RecomputeFocus (e);
					} else {
						if (this.MotionHighlightView != null) {
							this.MotionHighlightView.HandleMotion (this.MotionEdge,
								e.GetX () - this.LastX,
								e.GetY () - this.LastY);
							this.LastX = e.GetX ();
							this.LastY = e.GetY ();

							if (true) {
								// This section of code is optional. It has some user
								// benefit in that moving the crop rectangle against
								// the edge of the screen causes scrolling but it means
								// that the crop rectangle is no longer fixed under
								// the user's finger.
								EnsureVisible (this.MotionHighlightView);
							}
						}
					}
					break;
				}
			}

			switch (e.Action) {
			case MotionEventActions.Up:
				{
					this.Center (true, true);
					break;
				}
			case MotionEventActions.Move:
				{
					if (this.GetScale () == 1f) {
						Center (true, true);
					}
					break;
				}
			}

			return true;
		}

		// Pan the displayed image to make sure the cropping rectangle is visible.
		private void EnsureVisible (EmHighlightView hv) {
			Rect r = hv.MDrawRect;
			
			int panDeltaX1 = Math.Max(0, this.Left - r.Left);
			int panDeltaX2 = Math.Min(0, this.Right - r.Right);

			int panDeltaY1 = Math.Max(0, this.Top - r.Top);
			int panDeltaY2 = Math.Min(0, this.Bottom - r.Bottom);
			
			int panDeltaX = panDeltaX1 != 0 ? panDeltaX1 : panDeltaX2;
			int panDeltaY = panDeltaY1 != 0 ? panDeltaY1 : panDeltaY2;

			if (panDeltaX != 0 || panDeltaY != 0) {
				this.PanBy (panDeltaX, panDeltaY);
			}
		}

		// If the cropping rectangle's size changed significantly, change the
		// view's center and scale according to the cropping rectangle.
		private void CenterBasedOnHighlightView (EmHighlightView hv) {
			Rect drawRect = hv.MDrawRect;

			float width = drawRect.Width ();
			float height = drawRect.Height ();

			float thisWidth = this.Width;
			float thisHeight = this.Height;

			float z1 = thisWidth / width * .6F;
			float z2 = thisHeight / height * .6F;

			float zoom = Math.Min (z1, z2);
			zoom = zoom * this.GetScale ();
			zoom = Math.Max (1F, zoom);

			if ((Math.Abs (zoom - this.GetScale ()) / zoom) > .1) {
				float [] coordinates = new float[] { hv.MCropRect.CenterX (),
					hv.MCropRect.CenterY() };
				this.ImageMatrix.MapPoints (coordinates);
				ZoomTo (zoom, coordinates[0], coordinates[1], 300F);
			}

			EnsureVisible (hv);
		}

		protected override void OnDraw (Canvas canvas) {
			base.OnDraw (canvas);
			for (int i = 0; i < this.MotionHighlightViews.Count; i++) {
				this.MotionHighlightViews [i].Draw (canvas);
			}
		}

		public void Add (EmHighlightView hv) {
			this.MotionHighlightViews.Add (hv);
			this.Invalidate ();
		}

		public float GetScale () {
			return base.GetScale (this.MSuppMatrix);
		}

	}
}

