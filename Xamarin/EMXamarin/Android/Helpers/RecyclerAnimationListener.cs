using System;
using Android.Support.V7.Widget;
using em;

namespace Emdroid {
	/* Wrapper class for classes that want to listen in on the recycler view animations, but don't want to inherit from Java.Lang.Object. */
	public class RecyclerAnimationListener : Java.Lang.Object, RecyclerView.ItemAnimator.IItemAnimatorFinishedListener {
		private WeakReference _r = null;
		private EmRecyclerAnimationListener Listener {
			get { return this._r != null ? this._r.Target as EmRecyclerAnimationListener : null; }
			set { this._r = new WeakReference (value); }
		}


		private Action Callback { get; set; }

		public static RecyclerAnimationListener From (EmRecyclerAnimationListener lst) {
			RecyclerAnimationListener s = new RecyclerAnimationListener ();
			s.Listener = lst;
			return s;
		}

		public static RecyclerAnimationListener FromCallback (Action callback) {
			RecyclerAnimationListener s = new RecyclerAnimationListener ();
			s.Callback = callback;
			return s;
		}

		public RecyclerAnimationListener ()
		{
		}

		#region IItemAnimatorFinishedListener implementation

		public void OnAnimationsFinished () {
			EmRecyclerAnimationListener listener = this.Listener;
			if (listener != null) {
				listener.OnAnimationsFinished ();
			}

			Action callback = this.Callback;
			if (callback != null) {
				callback ();
				callback = null;
			}
		}

		#endregion
	}

	public interface EmRecyclerAnimationListener {
		void OnAnimationsFinished ();
	}
}

