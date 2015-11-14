using System;
using Android;
using Android.Support.V7.Widget;
using Android.Graphics.Drawables;
using Android.Content;
using Android.Views;

namespace Emdroid {
	public class SimpleDividerItemDecoration : RecyclerView.ItemDecoration {
		private const int Padding = 30;
		private Drawable BottomDivider { get; set; }
		private Drawable TopDivider { get; set; }
		private bool UseFullWidthLine { get; set; }
		private bool ShouldDrawBottomLine { get; set; }

		public SimpleDividerItemDecoration (Context ctx) {
			this.ShouldDrawBottomLine = true;
			this.UseFullWidthLine = false;
			Init (ctx);
		}

		public SimpleDividerItemDecoration (Context ctx, bool useFullWidthLine, bool shouldDrawBottomLine) {
			this.UseFullWidthLine = useFullWidthLine;
			this.ShouldDrawBottomLine = shouldDrawBottomLine;
			Init (ctx);
		}

		private void Init (Context ctx) {
			this.BottomDivider = ctx.Resources.GetDrawable (Resource.Drawable.line_divider_thin);
			this.TopDivider = ctx.Resources.GetDrawable (Resource.Drawable.line_divider_thin);
		}

		public override void OnDrawOver (Android.Graphics.Canvas c, RecyclerView parent, RecyclerView.State state) {
			base.OnDrawOver (c, parent, state);

			int templateLeft = parent.PaddingLeft;
			int templateRight = parent.Width - parent.PaddingRight;

			if (!this.UseFullWidthLine) {
				templateLeft += Padding;
				templateRight -= Padding;
			}

			int childCount = parent.ChildCount;
			for (int i = 0; i<childCount; i++) {
				View child = parent.GetChildAt (i);

				// When the row is being swiped, we get its translation and offset that to our left and right coordinates.
				// This'll create the effect of the line moving along with the row.
				int translationX = (int)child.TranslationX;
				int translationY = (int)child.TranslationY;

				int left = templateLeft + translationX;
				int right = templateRight + translationX;

				ViewGroup.MarginLayoutParams childParams = (ViewGroup.MarginLayoutParams) child.LayoutParameters;

				int bottomDividerTop = child.Bottom + childParams.BottomMargin;
				int bottomDividerBottom = bottomDividerTop + this.BottomDivider.IntrinsicHeight;

				bottomDividerTop += translationY;
				bottomDividerBottom += translationY;

				if (i != childCount-1) {
					this.BottomDivider.Bounds = new Android.Graphics.Rect (left, bottomDividerTop, right, bottomDividerBottom);
					this.BottomDivider.Draw (c);
				} else {
					if (this.ShouldDrawBottomLine) {
						this.BottomDivider.Bounds = new Android.Graphics.Rect (left, bottomDividerTop, right, bottomDividerBottom);
						this.BottomDivider.Draw (c);
					}
				}

				// We only need to draw the top divider if it's the first row. The row after the first row's Top Divider line will be the preceding row's Bottom Divider Line.
				if (i == 0) {
					int topDividerTop = child.Top + childParams.TopMargin;
					int topDividerBottom = topDividerTop + this.TopDivider.IntrinsicHeight;

					topDividerTop += translationY;
					topDividerBottom += translationY;

					this.TopDivider.Bounds = new Android.Graphics.Rect (left, topDividerTop, right, topDividerBottom);
					this.TopDivider.Draw (c);
				}
			}
		}
	}
}

