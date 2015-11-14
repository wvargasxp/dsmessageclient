using System;
using Android.Views;

namespace Emdroid {

	public class LongPressAndSlideGestureListener : Java.Lang.Object, View.IOnLongClickListener, View.IOnTouchListener {
		
		public double ThresholdDelta { get; set; }

		private bool longPressed = false;
		private bool triggered = false;

		private double startX;
		private double startY;

		public Action OnTriggered { get; set; }

		public Action OnKeyUp { get; set; }

		public bool OnLongClick (Android.Views.View view) {
			System.Diagnostics.Debug.WriteLine ("long pressed! " + view.GetX () + ", " + view.GetY ());
			this.longPressed = true;
			return false;
		}

		public bool OnTouch (Android.Views.View view, Android.Views.MotionEvent motionEvent) {
			if (motionEvent.Action == MotionEventActions.Down) {
				this.startX = motionEvent.GetX ();
				this.startY = motionEvent.GetY ();
			}
			if (!this.longPressed) {
				return false;
			}

			if (!this.triggered && motionEvent.Action == MotionEventActions.Move) {
				double x1 = this.startX;
				double y1 = this.startY;
				double x2 = motionEvent.GetX ();
				double y2 = motionEvent.GetY ();
				double dist = Math.Sqrt (Math.Pow (x1 - x2, 2) + Math.Pow (y1- y2, 2));

				if (dist > this.ThresholdDelta) {
					this.triggered = true;
					System.Diagnostics.Debug.WriteLine ("trigger!");

					this.OnTriggered ();
				}
			}

			if (motionEvent.Action == MotionEventActions.Up) {
				System.Diagnostics.Debug.WriteLine ("up!");
				bool shouldOnKeyUp = triggered;
				this.triggered = false;
				this.longPressed = false;

				if (shouldOnKeyUp) {
					this.OnKeyUp ();
				}
			}
				
			return false;

		}
	}
}