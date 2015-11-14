using System;
using Android.Support.V7.Widget;

namespace Emdroid {
	class RecyclerScrollListener : RecyclerView.OnScrollListener {
		public event EventHandler<OnScrollEventArgs> OnScrolledEvent;
		public event EventHandler<OnScrollStateChangedArgs> OnScrollStateChangedEvent;

		public override void OnScrolled (RecyclerView recyclerView, int dx, int dy) {
			base.OnScrolled (recyclerView, dx, dy);
			if (OnScrolledEvent != null) {
				OnScrollEventArgs args = OnScrollEventArgs.From (recyclerView, dx, dy);
				OnScrolledEvent (this, args);
			}
		}

		public override void OnScrollStateChanged (RecyclerView recyclerView, int newState) {
			base.OnScrollStateChanged (recyclerView, newState);
			if (OnScrollStateChangedEvent != null) {
				OnScrollStateChangedArgs args = OnScrollStateChangedArgs.From (recyclerView, newState);
				OnScrollStateChangedEvent (this, args);
			}
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
			if (OnScrolledEvent != null) {
				OnScrolledEvent = null;
			}

			if (OnScrollStateChangedEvent != null) {
				OnScrollStateChangedEvent = null;
			}
		}
	}

	public class OnScrollEventArgs {
		public RecyclerView RecyclerView { get; set; }
		public int X { get; set; }
		public int Y { get; set; }

		public static OnScrollEventArgs From (RecyclerView recyclerView, int dx, int dy) {
			OnScrollEventArgs ar = new OnScrollEventArgs ();
			ar.RecyclerView = recyclerView;
			ar.X = dx;
			ar.Y = dy;
			return ar;
		}
	}

	public class OnScrollStateChangedArgs {
		public RecyclerView RecyclerView { get; set; }
		private int NewState { get; set; }
		public RecyclerScrollState State {
			get {
				return (RecyclerScrollState)this.NewState;
			}
		}

		public static OnScrollStateChangedArgs From (RecyclerView recyclerView, int newState) {
			OnScrollStateChangedArgs ar = new OnScrollStateChangedArgs ();
			ar.RecyclerView = recyclerView;
			ar.NewState = newState;
			return ar;
		}
	}

	// https://developer.android.com/reference/android/support/v7/widget/RecyclerView.html#SCROLL_STATE_IDLE
	// These are not made up values.
	public enum RecyclerScrollState {
		Idle = 0,
		Dragging = 1,
		Settling = 2,
	}

}