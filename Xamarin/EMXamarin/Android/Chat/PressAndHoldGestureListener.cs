using System;
using System.Diagnostics;
using Android.Views;

namespace Emdroid {
	public class PressAndHoldGestureListener : Java.Lang.Object, View.IOnTouchListener {

		private const double MAX_DISTANCE_AWAY_FROM_TOUCH_DOWN_FOR_CANCEL = 500d;

		public Action OnKeyDown { get; set; }

		public Action OnKeyUp { get; set; }

		public Action OnCancel { get; set; }

		private float downX;
		private float downY;

		public bool OnTouch (Android.Views.View view, Android.Views.MotionEvent motionEvent) {

			if (motionEvent.Action == MotionEventActions.Down) {
				RecordLocation (motionEvent);
				this.OnKeyDown ();
			} else if (motionEvent.Action == MotionEventActions.Up) {
				if (WithinDelta (motionEvent)) {
					this.OnKeyUp ();
				} else {
					this.OnCancel ();
				}
			} else if (motionEvent.Action == MotionEventActions.Cancel) {
				this.OnCancel ();
			}

			return true;
		}

		private void RecordLocation (Android.Views.MotionEvent motionEvent) {
			this.downX = motionEvent.GetX ();
			this.downY = motionEvent.GetY ();
		}

		private bool WithinDelta (Android.Views.MotionEvent motionEvent) {
			float releaseX = motionEvent.GetX ();
			float releaseY = motionEvent.GetY ();
			double delta = Math.Sqrt (Math.Pow (releaseX - this.downX, 2) + Math.Pow (releaseY - this.downY, 2));

			return delta < MAX_DISTANCE_AWAY_FROM_TOUCH_DOWN_FOR_CANCEL;
		}
	}
}

