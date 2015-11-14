using System;
using Android.Support.V7.Widget.Helper;
using Android.Support.V7.Widget;
using Android.Support.V4.View;
using Android.OS;
using Android.Graphics;

namespace Emdroid {
	public class EmItemTouchHelper : ItemTouchHelper.SimpleCallback {

		private WeakReference _r = null;
		private ISwipeListener Listener {
			get { return this._r != null ? this._r.Target as ISwipeListener : null; }
			set { this._r = new WeakReference (value); }
		}

		public EmItemTouchHelper (ISwipeListener listener, int g, int x) : base (g, x) {
			this.Listener = listener;
		}

		#region implemented abstract members of Callback
		public override void OnSwiped (RecyclerView.ViewHolder p0, int p1) {
			ISwipeListener listener = this.Listener;
			if (listener == null) return;
			int row = p0.AdapterPosition;
			listener.RowSwiped (row);
		}

		public override bool OnMove (RecyclerView p0, RecyclerView.ViewHolder p1, RecyclerView.ViewHolder p2) {
			return true;
		}

		#endregion

		private bool IsElevated { get; set; }

		public override void OnChildDraw (Canvas p0, RecyclerView p1, RecyclerView.ViewHolder p2, float p3, float p4, int p5, bool p6) {
			SwipeActionState state = (SwipeActionState)p5;
			if (state != SwipeActionState.Swipe) {
				base.OnChildDraw (p0, p1, p2, p3, p4, p5, p6);
			} else {
				float width = p2.ItemView.Width;
				float alpha = 1.0f - Math.Abs (p3) / width;
				p2.ItemView.Alpha = alpha;
				p2.ItemView.TranslationX = p3;
			}
		}

		public override void ClearView (RecyclerView p0, RecyclerView.ViewHolder p1) {
			base.ClearView (p0, p1);
			this.IsElevated = false;
		}

		public override int GetSwipeDirs (RecyclerView p0, RecyclerView.ViewHolder p1) {
			int row = p1.AdapterPosition;
			int swipeDir = 0;
			ISwipeListener listener = this.Listener;

			if (listener != null && listener.CanSwipeAtRow (row)) {
				swipeDir = base.GetSwipeDirs (p0, p1);
			}

			return swipeDir;
		}
	}

	// https://developer.android.com/reference/android/support/v7/widget/helper/ItemTouchHelper.html#ACTION_STATE_IDLE
	public enum SwipeActionState {
		Idle = 0,
		Swipe = 1,
		Drag = 2
	}

	public interface ISwipeListener {
		/* Called when we need to check if the row can be swiped. */
		bool CanSwipeAtRow (int row);

		/* Called when a row is fully swiped out of screen. */
		void RowSwiped (int row);
	}
}