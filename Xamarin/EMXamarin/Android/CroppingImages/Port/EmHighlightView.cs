using System;
using Android.Views;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Content.Res;

namespace Emdroid {
	public class EmHighlightView {
		private static String TAG = "HighlightView";

		View mContext;  // The View displaying the image.

		public static int GROW_NONE = (1 << 0);
		public static int GROW_LEFT_EDGE = (1 << 1);
		public static int GROW_RIGHT_EDGE = (1 << 2);
		public static int GROW_TOP_EDGE = (1 << 3);
		public static int GROW_BOTTOM_EDGE = (1 << 4);
		public static int MOVE = (1 << 5);
	
		private Color DEFAULT_OUTLINE_COLOR = Color.Pink;
		private Color DEFAULT_OUTLINE_CIRCLE_COLOR = Color.DeepPink;
		private Color OutlineColor { get; set; }
		private Color OutlineCircleColor { get; set; }

		public enum ModifyMode { None, Move, Grow }
		public ModifyMode MMode = ModifyMode.None;

		public Rect MDrawRect;  // in screen space
		private RectF MImageRect;  // in image space
		public RectF MCropRect { get; set; }  // in image space
		public Matrix Matrix { get; set; }

        private bool _maintainAspectRatio = false;
		private bool MaintainAspectRatio { get { return this._maintainAspectRatio; } set { this._maintainAspectRatio = value; } }

		private float InitialAspectRatio { get; set; }

        private bool _circling = false;
        private bool Circling { get { return this._circling; } set { this._circling = value; } }

		private Drawable ResizeDrawableWidth { get; set; }
		private Drawable ResizeDrawableHeight { get; set; }
		private Drawable ResizeDrawableDiagonal { get; set; }

		private Paint FocusPaint = new Paint ();
		private Paint NoFocusPaint = new Paint ();
		private Paint OutlinePaint = new Paint ();

		public bool Focused { get; set; }
		public bool Hidden { get; set; }

		public EmHighlightView (View ctx) {
			Construct (ctx, DEFAULT_OUTLINE_COLOR, DEFAULT_OUTLINE_CIRCLE_COLOR);
		}

		public EmHighlightView (View ctx, Color outlineColor, Color outlineCircleColor) {
			// Note, blocking out custom colors, using defaults.
			Construct (ctx, DEFAULT_OUTLINE_COLOR, DEFAULT_OUTLINE_CIRCLE_COLOR);
		}

		private void Construct (View v, Color outlineColor, Color outlineCircleColor) {
			this.mContext = v;
			this.OutlineColor = outlineColor;
			this.OutlineCircleColor = outlineCircleColor;
		}

		private void Init () {
			Resources resources = this.mContext.Resources;
			this.ResizeDrawableWidth = resources.GetDrawable (Resource.Drawable.camera_crop_width);
			this.ResizeDrawableHeight = resources.GetDrawable (Resource.Drawable.camera_crop_height);
			this.ResizeDrawableDiagonal = resources.GetDrawable (Resource.Drawable.indicator_autocrop);
		}

		public void Draw (Canvas canvas) {
			if (this.Hidden) {
				return;
			}

			canvas.Save ();
			Path path = new Path();
			if (!this.Focused) {
				this.OutlinePaint.Color = this.OutlineColor;
				canvas.DrawRect (this.MDrawRect, this.OutlinePaint);
			} else {
				Rect viewDrawingRect = new Rect();
				mContext.GetDrawingRect (viewDrawingRect);
				if (this.Circling) {
					float width = this.MDrawRect.Width ();
					float height = this.MDrawRect.Height ();
					path.AddCircle (this.MDrawRect.Left + (width / 2),
						this.MDrawRect.Top + (height / 2),
						width / 2,
						Path.Direction.Cw);
					this.OutlinePaint.Color = this.OutlineColor;
				} else {
					path.AddRect (new RectF (this.MDrawRect), Path.Direction.Cw);
					this.OutlinePaint.Color = this.OutlineColor;
				}
				canvas.ClipPath (path, Region.Op.Difference);
				canvas.DrawRect (viewDrawingRect,
					this.Focused ? this.FocusPaint : this.NoFocusPaint);

				canvas.Restore ();
				canvas.DrawPath (path, this.OutlinePaint);

				if (this.MMode == ModifyMode.Grow) {
					if (this.Circling) {
						int width = this.ResizeDrawableDiagonal.IntrinsicWidth;
						int height = this.ResizeDrawableDiagonal.IntrinsicHeight;

						int d = (int) Math.Round (Math.Cos (/*45deg*/Math.PI / 4D)
							* (this.MDrawRect.Width () / 2D));
						int x = this.MDrawRect.Left
							+ (this.MDrawRect.Width () / 2) + d - width / 2;
						int y = this.MDrawRect.Top
							+ (this.MDrawRect.Height () / 2) - d - height / 2;
						this.ResizeDrawableDiagonal.SetBounds (x, y,
							x + this.ResizeDrawableDiagonal.IntrinsicWidth,
							y + this.ResizeDrawableDiagonal.IntrinsicHeight);
						this.ResizeDrawableDiagonal.Draw (canvas);
					} else {
						int left = this.MDrawRect.Left + 1;
						int right = this.MDrawRect.Right + 1;
						int top = this.MDrawRect.Top + 4;
						int bottom = this.MDrawRect.Bottom + 3;

						int widthWidth = this.ResizeDrawableWidth.IntrinsicWidth / 2;
						int widthHeight = this.ResizeDrawableWidth.IntrinsicHeight / 2;
						int heightHeight = this.ResizeDrawableHeight.IntrinsicHeight / 2;
						int heightWidth = this.ResizeDrawableHeight.IntrinsicWidth / 2;

						int xMiddle = this.MDrawRect.Left
							+ ((this.MDrawRect.Right - this.MDrawRect.Left) / 2);
						int yMiddle = this.MDrawRect.Top
							+ ((this.MDrawRect.Bottom - this.MDrawRect.Top) / 2);

						this.ResizeDrawableWidth.SetBounds (left - widthWidth,
							yMiddle - widthHeight,
							left + widthWidth,
							yMiddle + widthHeight);
						this.ResizeDrawableWidth.Draw (canvas);

						this.ResizeDrawableWidth.SetBounds (right - widthWidth,
							yMiddle - widthHeight,
							right + widthWidth,
							yMiddle + widthHeight);
						this.ResizeDrawableWidth.Draw (canvas);

						this.ResizeDrawableHeight.SetBounds (xMiddle - heightWidth,
							top - heightHeight,
							xMiddle + heightWidth,
							top + heightHeight);
						this.ResizeDrawableHeight.Draw (canvas);

						this.ResizeDrawableHeight.SetBounds (xMiddle - heightWidth,
							bottom - heightHeight,
							xMiddle + heightWidth,
							bottom + heightHeight);
						this.ResizeDrawableHeight.Draw (canvas);
					}
				}
			}
		}

		public void SetMode (ModifyMode mode) {
			if (mode != this.MMode) {
				this.MMode = mode;
				this.mContext.Invalidate ();
			}
		}

		// Determines which edges are hit by touching at (x, y).
		public int GetHit (float x, float y) {
			Rect r = ComputeLayout ();
			const float hysteresis = 20F;
			int retval = GROW_NONE;

			if (this.Circling) {
				float distX = x - r.CenterX ();
				float distY = y - r.CenterY ();
				int distanceFromCenter =
					(int) Math.Sqrt (distX * distX + distY * distY);
				int radius  = this.MDrawRect.Width () / 2;
				int delta = distanceFromCenter - radius;
				if (Math.Abs (delta) <= hysteresis) {
					if (Math.Abs (distY) > Math.Abs (distX)) {
						if (distY < 0) {
							retval = GROW_TOP_EDGE;
						} else {
							retval = GROW_BOTTOM_EDGE;
						}
					} else {
						if (distX < 0) {
							retval = GROW_LEFT_EDGE;
						} else {
							retval = GROW_RIGHT_EDGE;
						}
					}
				} else if (distanceFromCenter < radius) {
					retval = MOVE;
				} else {
					retval = GROW_NONE;
				}
			} else {
				// verticalCheck makes sure the position is between the top and
				// the bottom edge (with some tolerance). Similar for horizCheck.
				bool verticalCheck = (y >= r.Top - hysteresis)
					&& (y < r.Bottom + hysteresis);
				bool horizCheck = (x >= r.Left - hysteresis)
					&& (x < r.Right + hysteresis);

				// Check whether the position is near some edge(s).
				if ((Math.Abs (r.Left - x) < hysteresis) && verticalCheck) {
					retval |= GROW_LEFT_EDGE;
				}

				if ((Math.Abs (r.Right - x) < hysteresis) && verticalCheck) {
					retval |= GROW_RIGHT_EDGE;
				}
				if ((Math.Abs (r.Top - y) < hysteresis) && horizCheck) {
					retval |= GROW_TOP_EDGE;
				}

				if ((Math.Abs (r.Bottom - y) < hysteresis) && horizCheck) {
					retval |= GROW_BOTTOM_EDGE;
				}

				// Not near any edge but inside the rectangle: move.
				if (retval == GROW_NONE && r.Contains((int) x, (int) y)) {
					retval = MOVE;
				}
			}
			return retval;
		}

		// Handles motion (dx, dy) in screen space.
		// The "edge" parameter specifies which edges the user is dragging.
		public void HandleMotion (int edge, float dx, float dy) {
			Rect r = ComputeLayout ();
			if (edge == GROW_NONE) {
				return;
			} else if (edge == MOVE) {
				// Convert to image space before sending to moveBy().
				MoveBy (dx * (this.MCropRect.Width () / r.Width ()),
					dy * (this.MCropRect.Height () / r.Height ()));
			} else {
				if (((GROW_LEFT_EDGE | GROW_RIGHT_EDGE) & edge) == 0) {
					dx = 0;
				}

				if (((GROW_TOP_EDGE | GROW_BOTTOM_EDGE) & edge) == 0) {
					dy = 0;
				}

				// Convert to image space before sending to growBy().
				float xDelta = dx * (this.MCropRect.Width () / r.Width ());
				float yDelta = dy * (this.MCropRect.Height () / r.Height());
				GrowBy ((((edge & GROW_LEFT_EDGE) != 0) ? -1 : 1) * xDelta,
					(((edge & GROW_TOP_EDGE) != 0) ? -1 : 1) * yDelta);
			}
		}

		// Grows the cropping rectange by (dx, dy) in image space.
		private void MoveBy (float dx, float dy) {
			Rect invalRect = new Rect (this.MDrawRect);

			this.MCropRect.Offset (dx, dy);

			// Put the cropping rectangle inside image rectangle.
			this.MCropRect.Offset (
				Math.Max (0, this.MImageRect.Left - this.MCropRect.Left),
				Math.Max (0, this.MImageRect.Top  - this.MCropRect.Top));

			this.MCropRect.Offset (
				Math.Min (0, this.MImageRect.Right  - this.MCropRect.Right),
				Math.Min (0, this.MImageRect.Bottom - this.MCropRect.Bottom));

			this.MDrawRect = ComputeLayout ();
			invalRect.Union (this.MDrawRect);
			invalRect.Inset (-10, -10);
			mContext.Invalidate (invalRect);
		}

		// Grows the cropping rectange by (dx, dy) in image space.
		private void GrowBy (float dx, float dy) {
			if (this.MaintainAspectRatio) {
				if (dx != 0) {
					dy = dx / this.InitialAspectRatio;
				} else if (dy != 0) {
					dx = dy * this.InitialAspectRatio;
				}
			}

			// Don't let the cropping rectangle grow too fast.
			// Grow at most half of the difference between the image rectangle and
			// the cropping rectangle.
			RectF r = new RectF (this.MCropRect);
			if (dx > 0F && r.Width () + 2 * dx > this.MImageRect.Width ()) {
				float adjustment = (this.MImageRect.Width () - r.Width ()) / 2F;
				dx = adjustment;
				if (this.MaintainAspectRatio) {
					dy = dx / this.InitialAspectRatio;
				}
			}

			if (dy > 0F && r.Height () + 2 * dy > this.MImageRect.Height ()) {
				float adjustment = (this.MImageRect.Height () - r.Height ()) / 2F;
				dy = adjustment;
				if (this.MaintainAspectRatio) {
					dx = dy * this.InitialAspectRatio;
				}
			}

			r.Inset (-dx, -dy);

			// Don't let the cropping rectangle shrink too fast.
			const float widthCap = 25F;
			if (r.Width () < widthCap) {
				r.Inset (-(widthCap - r.Width ()) / 2F, 0F);
			}
			float heightCap = this.MaintainAspectRatio
				? (widthCap / this.InitialAspectRatio)
				: widthCap;
			if (r.Height () < heightCap) {
				r.Inset (0F, -(heightCap - r.Height ()) / 2F);
			}

			// Put the cropping rectangle inside the image rectangle.
			if (r.Left < this.MImageRect.Left) {
				r.Offset (this.MImageRect.Left - r.Left, 0F);
			} else if (r.Right > this.MImageRect.Right) {
				r.Offset (-(r.Right - this.MImageRect.Right), 0);
			}
			if (r.Top < this.MImageRect.Top) {
				r.Offset (0F, this.MImageRect.Top - r.Top);
			} else if (r.Bottom > this.MImageRect.Bottom) {
				r.Offset (0F, -(r.Bottom - this.MImageRect.Bottom));
			}

			this.MCropRect.Set (r);
			this.MDrawRect = ComputeLayout ();
			mContext.Invalidate ();
		}

		// Returns the cropping rectangle in image space.
		public Rect GetCropRect () {
			return new Rect ((int)this.MCropRect.Left, (int)this.MCropRect.Top, (int)this.MCropRect.Right, (int)this.MCropRect.Bottom);
		}

		private Rect ComputeLayout () { 
			RectF r = new RectF (this.MCropRect.Left, this.MCropRect.Top,
				this.MCropRect.Right, this.MCropRect.Bottom);
			this.Matrix.MapRect (r);
			return new Rect ((int)Math.Round (r.Left), (int)Math.Round (r.Top),
				(int)Math.Round (r.Right), (int)Math.Round (r.Bottom));
		}

		public void Invalidate () {
			this.MDrawRect = ComputeLayout ();
		}

		public void Setup (Matrix m, Rect imageRect, RectF cropRect, bool circle, bool maintainAspectRatio) {
			if (circle) {
				maintainAspectRatio = true;
			}

			this.Matrix = new Matrix (m);

			this.MCropRect = cropRect;
			this.MImageRect = new RectF (imageRect);
			this.MaintainAspectRatio = maintainAspectRatio;
			this.Circling = circle;

			this.InitialAspectRatio = this.MCropRect.Width () / this.MCropRect.Height ();
			this.MDrawRect = ComputeLayout ();

			this.FocusPaint.SetARGB (125, 50, 50, 50);
			this.NoFocusPaint.SetARGB (125, 50, 50, 50);
			this.OutlinePaint.StrokeWidth = 3f;
			this.OutlinePaint.SetStyle (Paint.Style.Stroke);
			this.OutlinePaint.AntiAlias = true;

			this.MMode = ModifyMode.None;
			Init ();
		}
	}
}