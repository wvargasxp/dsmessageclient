using System;
using Android.Views;
using Android.Graphics;
using System.Collections.Generic;

namespace Emdroid {
	public class TouchDelegateComposite : TouchDelegate {

        private List<TouchDelegate> _touchDelegates = new List<TouchDelegate> ();
		private List<TouchDelegate> TouchDelegates { get { return this._touchDelegates; } set { this._touchDelegates = value; } }

        private static Rect _emptyRect = new Rect ();
        private static Rect EmptyRect { get { return _emptyRect; } set { _emptyRect = value; } }

		public TouchDelegateComposite (View view) : base (EmptyRect, view) {}

		public void AddDelegate (TouchDelegate touchDelegate) {
			if (touchDelegate != null) {
				this.TouchDelegates.Add (touchDelegate);
			}
		}

		public override bool OnTouchEvent (MotionEvent e) {
			bool res = false;
			float x = e.GetX ();
			float y = e.GetY ();
			foreach (TouchDelegate touchDelegate in this.TouchDelegates) {
				e.SetLocation (x, y);
				res = touchDelegate.OnTouchEvent (e) || res;
			}

			return res;
		}

		public static void ExpandClickArea (View view, View root, int paddingToAddInDP, bool setEnabled = true) {
			// https://gist.github.com/nikosk/3854f2effe65438cfa40
			// https://stackoverflow.com/questions/6799066/how-to-use-multiple-touchdelegate
			// https://developer.android.com/training/gestures/viewgroup.html#delegate
			root.Post (() => {
				Rect delegateArea = new Rect();
				if (setEnabled) {
					view.Enabled = true;
				}
				view.GetHitRect (delegateArea);

				int paddingInPixels = paddingToAddInDP.DpToPixelUnit ();
				delegateArea.Right += paddingInPixels;
				delegateArea.Bottom += paddingInPixels;
				delegateArea.Top -= paddingInPixels;
				delegateArea.Left -= paddingInPixels;

				View parentView = view.Parent as View;
				if (parentView != null) {
					TouchDelegate parentTouchDelegate = parentView.TouchDelegate;
					TouchDelegate newDelegate = new TouchDelegate (delegateArea, view);

					if (parentTouchDelegate != null) {
						TouchDelegateComposite compositeTouchDelegate = parentTouchDelegate as TouchDelegateComposite;
						if (compositeTouchDelegate != null) {
							compositeTouchDelegate.AddDelegate (newDelegate);
							newDelegate = compositeTouchDelegate;
						} else {
							TouchDelegateComposite composite = new TouchDelegateComposite (view);
							composite.AddDelegate (parentTouchDelegate);
							composite.AddDelegate (newDelegate);
							newDelegate = composite;
						}
					}

					parentView.TouchDelegate = newDelegate;
				}
			});
		} 
	}
}

