using System;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using Java.Lang;
using Android.Support.V4.View;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Animation;

namespace Emdroid {
	public class AnimatingListView : ListView {
		public static readonly IListItemAnimation NONE = null;
		public static readonly IListItemAnimation FADE = new FadeListItemAnimation();
		public static readonly IListItemAnimation LEFT = new LeftListItemAnimation();
		public static readonly IListItemAnimation RIGHT = new RightListItemAnimation();

		public AnimatingListView (Context context) :
			base (context) {
			Initialize ();
		}

		public AnimatingListView (Context context, IAttributeSet attrs) :
			base (context, attrs) {
			Initialize ();
		}

		public AnimatingListView (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle) {
			Initialize ();
		}

		ScrollState ScrollState = ScrollState.Idle;
		void Initialize () {
			ScrollStateChanged += (object sender, ScrollStateChangedEventArgs e) => {
				ScrollState = e.ScrollState;
			};
		}

		int activePreAnimations = 0;
		int activePostAnimations = 0;
		List<AnimationRecord> animationRecords = null;
		List<View> viewsToIgnore = new List<View>();
		public void BeginUpdates() {
			animationRecords = new List<AnimationRecord>();
			viewsToIgnore.Clear ();
			activePreAnimations = 0;
			activePostAnimations = 0;
		}

		public void EndUpdates(Action doneWithAnimation) {
			Enabled = false;

			// get initial scroll
			int firstVisible = FirstVisiblePosition;

			// if they are scrolling skip animation as it
			// seems problematic.
			if (ScrollState != ScrollState.Idle)
				animationRecords.Clear ();

			foreach (AnimationRecord record in animationRecords) {
				if (record.HasPreDrawSteps) {
					activePreAnimations++;

					// now find row to delete
					int deletePos = record.Position - FirstVisiblePosition;
					View v = GetChildAt (deletePos);

					// fade out row we are deleting
					if (v == null) {
						activePreAnimations--;
					} else {
						viewsToIgnore.Add (v);
						record.Animation.AnimateForRemove (this, v, () => {
							activePreAnimations--;
							ContinueEndUpdatesPreCompleted(doneWithAnimation);
						});
					}
				}
			}

			// case where there's no pre notify steps
			if ( activePreAnimations == 0 )
				ContinueEndUpdatesPreCompleted(doneWithAnimation);
		}

		protected void ContinueEndUpdatesPreCompleted(Action doneWithAnimation) {
			if (activePreAnimations > 0)
				return;

			((BaseAdapter)Adapter).NotifyDataSetChanged();

			AnimateOtherViews (this, viewsToIgnore, () => {
				foreach (AnimationRecord record in animationRecords) {
					if (record.HasPostDrawSteps) {
						activePostAnimations++;
						int insertPos = record.Position - FirstVisiblePosition;
						View v = GetChildAt (insertPos);
						if (v != null)
							record.Animation.PrepareAnimateForInsert (this, v);
					}
				}
			},
				() => {
					foreach (AnimationRecord record in animationRecords) {
						if (record.HasPostDrawSteps) {
							int insertPos = record.Position - FirstVisiblePosition;
							View v = GetChildAt (insertPos);
							if (v == null) {
								activePostAnimations--;
							} else {
								record.Animation.AnimateForInsert (this, v, () => {
									activePostAnimations--;
									ContinueEndUpdatesPostOtherViewsAnimated (doneWithAnimation);
								});
							}
						}
					}

					// case where there's post animation steps
					if ( activePostAnimations == 0 )
						ContinueEndUpdatesPostOtherViewsAnimated(doneWithAnimation);
				});
		}

		protected void ContinueEndUpdatesPostOtherViewsAnimated (Action doneWithAnimation) {
			if (activePostAnimations > 0)
				return;
			
			viewsToIgnore.Clear ();
			animationRecords = null;

			if (doneWithAnimation != null)
				doneWithAnimation ();

			Enabled = true;
		}

		protected bool InAnimationTransaction { get { return animationRecords != null; } }

		// called to perform animations, this should be before the
		// adapter signals it has updates
		public void DeleteRowAt(int position, IListItemAnimation animation) {
			bool inTransaction = InAnimationTransaction;
			if (!inTransaction)
				BeginUpdates ();

			animationRecords.Add(new AnimationRecord(position, animation, true, false));

			if (!inTransaction)
				EndUpdates (null);
		}

		// called for swipe style deletes
		public void DeleteRowRowAlreadyGone(int position, Action<int> doneWithAnimation) {
			// now find iew to delete
			int deletePos = position - FirstVisiblePosition;
			View v = GetChildAt (deletePos);
			v.Alpha = 0;

			((BaseAdapter)Adapter).NotifyDataSetChanged();

			List<View> viewAsList = new List<View> ();
			viewAsList.Add (v);
			AnimateOtherViews(this, viewAsList, null, () => {
				// resetting Alpha
				v.Alpha = 1;
				doneWithAnimation(position);
			});
		}

		public void MoveRowFromTo(int position, int to) {
			bool inTransaction = InAnimationTransaction;
			if (!inTransaction)
				BeginUpdates ();

			animationRecords.Add(new AnimationRecord(position, null, false, false));

			if (!inTransaction)
				EndUpdates (null);
		}

		public void AddRowAt(int position, IListItemAnimation animation) {
			bool inTransaction = InAnimationTransaction;
			if (!inTransaction)
				BeginUpdates ();

			animationRecords.Add(new AnimationRecord(position, animation, false, true));

			if (!inTransaction)
				EndUpdates (null);
		}

		Dictionary<View,PreAnimationDetails> listViewPreAnimationDetails = new Dictionary<View,PreAnimationDetails>();
		PreAnimationDetails[] listViewPreAnimationDetailsArray = null;
		List<BitmapDrawable> listViewBitmapsToDraw = new List<BitmapDrawable>();
		void AnimateOtherViews(ListView listView, IList<View> ignoreViews, Action inPreDrawCallback, Action animationCompleteCallback) {
			listViewPreAnimationDetailsArray = new PreAnimationDetails[ listView.ChildCount ];
			for ( int i=0; i<listView.ChildCount; i++ ) {
				View child = listView.GetChildAt(i);
				if ( !ignoreViews.Contains(child) ) {
					int position = FirstVisiblePosition + i;

					PreAnimationDetails details = new PreAnimationDetails ();
					listViewPreAnimationDetailsArray [i] = details;
					listViewPreAnimationDetails [child] = details;

					details.rect = new Rect (child.Left, child.Top, child.Right, child.Bottom);
					details.bitmapDrawable = GetBitmapDrawableFromView (child);
					details.OriginalChildPosition = position;
					details.AfterChildPosition = -1;
					details.Offset = -1;
					details.View = child;

					if ( VersionHelper.SupportsListItemViewsWithTransientState() )
						// Use transient state to avoid recycling during upcoming layout
						child.HasTransientState = true;
				}
			}

			ViewTreeObserver observer = listView.ViewTreeObserver;
			observer.AddOnPreDrawListener ( new PreDrawListener(this, inPreDrawCallback, animationCompleteCallback));
		}

		protected override void DispatchDraw(Canvas canvas) {
			base.DispatchDraw (canvas);
			if (listViewBitmapsToDraw.Count > 0) {
				foreach (BitmapDrawable bitmapDrawable in listViewBitmapsToDraw)
					bitmapDrawable.Draw (canvas);
			}
		}

		private BitmapDrawable GetBitmapDrawableFromView (View v) {
			Bitmap bitmap = Bitmap.CreateBitmap (v.Width, v.Height, Bitmap.Config.Argb8888);
			Canvas canvas = new Canvas (bitmap);
			v.Draw (canvas);
			return new BitmapDrawable (Resources, bitmap);
		}

		class PreDrawListener : Java.Lang.Object, Android.Views.ViewTreeObserver.IOnPreDrawListener {
			AnimatingListView listView;
			Action inPreDrawCallback;
			Action completionCallback;

			public PreDrawListener(AnimatingListView theListView, Action preDrawAction, Action animationCompleteCallback) {
				listView = theListView;
				inPreDrawCallback = preDrawAction;
				completionCallback = animationCompleteCallback;
			}

			public bool OnPreDraw () {
				listView.ViewTreeObserver.RemoveOnPreDrawListener (this);

				if (inPreDrawCallback != null)
					inPreDrawCallback ();

				List<Animator> animations = new List<Animator> ();
				List<int> viewsWithNoStartState = new List<int> ();
				// first pass handle all 
				for (int i = 0; i < listView.ChildCount; i++) {
					View child = listView.GetChildAt (i);
					PreAnimationDetails preAnimationDetails = listView.listViewPreAnimationDetails.ContainsKey (child) ? listView.listViewPreAnimationDetails [child] : null;
					if (preAnimationDetails == null)
						viewsWithNoStartState.Add (i);
					else {
						int top = child.Top;
						int startTop = preAnimationDetails.rect.Top;
						if (startTop == top)
							preAnimationDetails.Offset = 0; // overrites -1
						else {
							int delta = (int)startTop - top;
							preAnimationDetails.Offset = delta;
							preAnimationDetails.AfterChildPosition = listView.FirstVisiblePosition + i;
							child.TranslationY = delta;
							ObjectAnimator animator = ObjectAnimator.OfFloat (child, "translationY", 0);
							animations.Add (animator);
						}

						// don't need this bitmap as the view never left the screen
						if (preAnimationDetails.bitmapDrawable != null) {
							preAnimationDetails.bitmapDrawable.Bitmap.Recycle ();
							preAnimationDetails.bitmapDrawable = null;
						}

						listView.listViewPreAnimationDetails.Remove (child);
					}
				}

				// now go through views that had no previous state (they are appearing
				// on screen from offscreen)
				foreach (int i in viewsWithNoStartState) {
					View child = listView.GetChildAt (i);
					// Animate new views along with the others.  The catch is that they don't
					// exist in the start state, so we must calculate their starting position
					// based on on neighboring views.
					int top = child.Top;
					int startTop = 0;
					int heightOfOthers = 0;
					View prevChild;
					if (i > 0) {
						// entering from bottom
						for (int j = 1; i - j >= 0; j++) {
							prevChild = listView.GetChildAt (i - j);
							PreAnimationDetails prevPreAnimationDetails = listView.FindDetailsForView (prevChild);
							if ( prevPreAnimationDetails == null ) {
								// it means adjacent view wasn't onscreen either.
								heightOfOthers += prevChild.Height + listView.DividerHeight;
								continue;
							}
							startTop = top + prevPreAnimationDetails.Offset + heightOfOthers;
							break;
						}
					}
					else {
						for (int j = 1; i + j < listView.ChildCount; j++) {
							// entering from top
							prevChild = listView.GetChildAt (i + j);
							PreAnimationDetails prevPreAnimationDetails = listView.FindDetailsForView (prevChild);
							if ( prevPreAnimationDetails == null ) {
								// it means adjacent view wasn't onscreen either.
								heightOfOthers += prevChild.Height + listView.DividerHeight;
								continue;
							}
							startTop = top + prevPreAnimationDetails.Offset - heightOfOthers;
							break;
						}
					}
					int delta = startTop - top;
					if (delta != 0) {
						child.TranslationY = delta;
						ObjectAnimator animator = ObjectAnimator.OfFloat (child, "translationY", 0);
						animations.Add (animator);
					}
				}

				// remaining items are views that existed before the animation
				// but have since moved off screen.  First we start from the top
				// (position 0) and start looking for a view that remains on screen
				// so we can copy its offset.
				for (int i = 0; i < listView.listViewPreAnimationDetailsArray.Length; i++) {
					PreAnimationDetails details = listView.listViewPreAnimationDetailsArray [i];
					// if its got a bitmap drawable, it's one of the views we need to animate
					// and it has no final position so we skip it looking for rows that have
					// an known offset.
					if (details == null || details.bitmapDrawable != null)
						continue;

					// after finding an offset, we loop backwards towards zero applying
					// that same offset to all those bitmaps heading off screen.
					int height = details.Offset;
					for (int j = i - 1; j >= 0; j--) {
						PreAnimationDetails offScreenDetails = listView.listViewPreAnimationDetailsArray [j];
						if ( offScreenDetails != null )
							offScreenDetails.Offset = -height;
					}
				}

				// next we move up from the bottom, if views are heading downwards offscreen we
				// try to pick up the offset from the last remaining view towards the bottom and
				// copy its offset.
				for (int i = listView.listViewPreAnimationDetailsArray.Length - 1; i >=0 ; i--) {
					// if its got a bitmap drawable, it's one of the views we need to animate
					// and it has no final position so we skip it looking for rows that have
					// an known offset.
					PreAnimationDetails details = listView.listViewPreAnimationDetailsArray [i];
					if (details == null || details.bitmapDrawable != null)
						continue;

					// after finding an offset, we loop backwards towards the end applying
					// that same offset to all those bitmaps heading off screen.
					int height = details.Offset;
					for (int j = i+1; j < listView.listViewPreAnimationDetailsArray.Length; j++) {
						PreAnimationDetails offScreenDetails = listView.listViewPreAnimationDetailsArray [j];
						if ( offScreenDetails != null )
							offScreenDetails.Offset = height;
					}
				}

				foreach (View child in listView.listViewPreAnimationDetails.Keys) {
					PreAnimationDetails preAnimationDetails = listView.listViewPreAnimationDetails [child];
					Rect startBounds = preAnimationDetails.rect;
					preAnimationDetails.bitmapDrawable.Bounds = startBounds;
					listView.listViewBitmapsToDraw.Add (preAnimationDetails.bitmapDrawable);

					// view isn't going anywhere, so don't bother creating an animation for it.
					if (preAnimationDetails.Offset == 0)
						continue;

					Rect endBounds = new Rect (startBounds);
					endBounds.Offset (0, preAnimationDetails.Offset);

					ObjectAnimator animation = ObjectAnimator.OfObject(preAnimationDetails.bitmapDrawable, "bounds", sBoundsEvaluator, startBounds, endBounds);

					Rect currentBound = new Rect ();
					Rect lastBound = null;
					animation.Update += (object sender, ValueAnimator.AnimatorUpdateEventArgs e) => {
						ValueAnimator valueAnimator = e.Animation;
						Rect bounds = (Rect)valueAnimator.AnimatedValue;
						currentBound.Set (bounds);
						if (lastBound != null)
							currentBound.Union (lastBound);
						lastBound = bounds;
						listView.Invalidate (currentBound);
					};
					animations.Add (animation);
				}

				if (animations.Count == 0) {
					listView.CleanAnimationDictionaries ();
					listView.Invalidate ();
					completionCallback ();
				}
				else {
					AnimatorSet set = new AnimatorSet ();
					set.SetDuration (Android_Constants.MOVE_ANIMATION_DURATION_MILLIS);
					set.PlayTogether (animations.ToArray());
					set.AnimationEnd += (object sender, EventArgs e) => {
						listView.CleanAnimationDictionaries();
						listView.Invalidate();
						completionCallback();
					};
					set.Start ();

					if (VersionHelper.SupportsListItemViewsWithTransientState ())
						foreach (PreAnimationDetails details in listView.listViewPreAnimationDetailsArray) {
							if ( details != null )
								details.View.HasTransientState = false;
						}
				}

				return true;
			}
		}

		PreAnimationDetails FindDetailsForView(View v) {
			foreach (PreAnimationDetails details in listViewPreAnimationDetailsArray) {
				if (details != null && details.View == v)
					return details;
			}

			return null;
		}

		static ITypeEvaluator sBoundsEvaluator = new OurTypeEvaluator ();

		protected void CleanAnimationDictionaries() {
			foreach (BitmapDrawable bitmapDrawable in listViewBitmapsToDraw)
				bitmapDrawable.Bitmap.Recycle ();

			listViewBitmapsToDraw.Clear ();
			listViewPreAnimationDetails.Clear ();
			listViewPreAnimationDetailsArray = null;
		}

		public Action<int> RemoveFromModel { get; set; }

		SwipeToDeleteHandler mTouchListener;
		public SwipeToDeleteHandler GetSwipingOnTouchListener(Context ctx) {
			if ( mTouchListener == null ) {
				mTouchListener = new SwipeToDeleteHandler(ctx, this);
			}

			return mTouchListener;
		}
	}

	class PreAnimationDetails {
		public Rect rect { get; set; }
		public BitmapDrawable bitmapDrawable { get; set; }
		public int Offset { get; set; }
		public int OriginalChildPosition { get; set; }
		public int AfterChildPosition { get; set; }
		public View View { get; set; }
	}

	public class SwipeToDeleteHandler : GestureDetector.SimpleOnGestureListener {
		public bool mSwiping = false;
		AnimatingListView mListView;
		Context mContext;
		float mDownX;
		private int mSwipeSlop = -1;

		GestureDetectorCompat compat;

		public SwipeToDeleteHandler(Context theContext, AnimatingListView theListView) {
			mContext = theContext;
			mListView = theListView;

			compat = new GestureDetectorCompat (theContext, this);
		}

		public bool OnTouchHandleMethod(View v, MotionEvent evt) {
			// if we have no way of removing from model, skip
			if (mListView.RemoveFromModel == null)
				return true;

			MotionEvent evtInListViewCoords = MotionEvent.Obtain (evt);
			evtInListViewCoords.SetLocation (evt.GetX() + v.Left, evt.GetY() + v.Top);
			
			if (mSwipeSlop < 0)
				mSwipeSlop = ViewConfiguration.Get (mContext).ScaledTouchSlop;

			switch (evt.Action) {
			case MotionEventActions.Down:
				mDownX = evt.GetX ();
				break;

			case MotionEventActions.Cancel:
				mSwiping = false;
				v.Alpha = 1;
				v.TranslationX = 0;
				break;

			case MotionEventActions.Move:
				{
					float x = evt.GetX () + v.TranslationX;
					float deltaX = x - mDownX;
					float deltaXAbs = System.Math.Abs (deltaX);
					if (!mSwiping) {
						if (deltaXAbs > mSwipeSlop) {
							mSwiping = true;
							mListView.RequestDisallowInterceptTouchEvent (true);
						}
					}
					if (mSwiping) {
						v.TranslationX = x - mDownX;
						//v.Alpha = 1 - deltaXAbs / v.Width;
					}
				}
				break;

			case MotionEventActions.Up:
				{
					// User let go - figure out whether to animate the view out, or back
					if (mSwiping ) {
						float x = evt.GetX () - v.TranslationX;
						float deltaX = x - mDownX;
						float deltaXAbs = System.Math.Abs (deltaX);
						float fractionCovered;
						float endX;
						//float endAlpha;
						bool remove;
						if (deltaXAbs > v.Width / 3.5) {
							// Greater than a bit less than third of the width - animate it out
							fractionCovered = deltaXAbs / v.Width;
							endX = deltaX < 0 ? v.Width : -v.Width;
							//endAlpha = 0;
							remove = true;
						} else {
							// Not far enough - animate it back
							fractionCovered = 1 - (deltaXAbs / v.Width);
							endX = 0;
							//endAlpha = 1;
							remove = false;
						}
						// Animate position and alpha
						long duration = (int)((1 - fractionCovered) * 200); // TODO make constant
						v.Animate ().SetDuration (duration)
							.X (endX).
							WithEndAction (new Runnable (() => {
							//v.Alpha = 1;
							v.TranslationX = 0;
							if (remove) {
								int position = mListView.GetPositionForView (v);
								// handle remove
								// Animate everything else into place
								mListView.RemoveFromModel(position);
							} else {
								mSwiping = false;
							}
						}));
					}
				}
				break;

			default:
				return compat.OnTouchEvent (evtInListViewCoords);
			}
			return compat.OnTouchEvent (evtInListViewCoords) || true;
		}

		public override void OnLongPress(MotionEvent evt) {
			int x = (int) (evt.GetX() + 0.5f);
			int y = (int) (evt.GetY() + 0.5f);
			View v = null;
			int i;
			for (i = 0; i < mListView.ChildCount; i++) {
				v = mListView.GetChildAt (i);

				if (x > v.Left && x < v.Right && y > v.Top && y < v.Bottom)
					break;
			}

			int position = mListView.GetPositionForView (v);
			mListView.OnItemLongClickListener.OnItemLongClick(mListView, v, position, ((IListAdapter)mListView.Adapter).GetItemId(position));
		}

		public override bool OnSingleTapUp(MotionEvent evt) {
			int x = (int) (evt.GetX() + 0.5f);
			int y = (int) (evt.GetY() + 0.5f);
			View v = null;
			int i;
			for (i = 0; i < mListView.ChildCount; i++) {
				v = mListView.GetChildAt (i);

				if (x > v.Left && x < v.Right && y > v.Top && y < v.Bottom)
					break;
			}

			int position = mListView.GetPositionForView (v);
			mListView.OnItemClickListener.OnItemClick(mListView, v, position, ((IListAdapter)mListView.Adapter).GetItemId(position));
			return true;
		}
	}

	class AnimationRecord {
		public int Position { get; set; }
		public IListItemAnimation Animation { get; set; }
		public bool HasPreDrawSteps { get; set; }
		public bool HasPostDrawSteps { get; set; }

		public AnimationRecord(int p, IListItemAnimation a, bool pre, bool post) {
			Position = p;
			Animation = a;
			HasPreDrawSteps = pre;
			HasPostDrawSteps = post;
		}
	}

	public interface IListItemAnimation {
		void PrepareAnimateForRemove (AnimatingListView listView, View v);
		void AnimateForRemove (AnimatingListView listView, View v, Action completedAnimation);

		void PrepareAnimateForInsert (AnimatingListView listView, View v);
		void AnimateForInsert (AnimatingListView listView, View v, Action completedAnimation);
	}

	public class FadeListItemAnimation : IListItemAnimation {
		public void PrepareAnimateForRemove (AnimatingListView listView, View v) {
		}

		public void AnimateForRemove (AnimatingListView listView, View v, Action completedAnimation) {
			v.Animate ()
				.SetDuration (Android_Constants.DELETE_ANIMATION_DURATION_MILLIS)
				.Alpha (0)
				.WithEndAction (new Java.Lang.Runnable (() => {
					completedAnimation();

					// resetting Alpha
					v.Alpha = 1;
				}));
		}

		public void PrepareAnimateForInsert (AnimatingListView listView, View v) {
			v.Alpha = 0;
		}

		public void AnimateForInsert (AnimatingListView listView, View v, Action completedAnimation) {
			v.Alpha = 0;
			v.Animate ().Alpha (1).WithEndAction (new Runnable (completedAnimation));
		}
	}

	public class LeftListItemAnimation : IListItemAnimation {
		public void PrepareAnimateForRemove (AnimatingListView listView, View v) {
		}

		public void AnimateForRemove (AnimatingListView listView, View v, Action completedAnimation) {
			v.Animate ()
				.SetDuration (Android_Constants.DELETE_ANIMATION_DURATION_MILLIS)
				.Alpha (0)
				.TranslationX( -v.Width )
				.WithEndAction (new Java.Lang.Runnable (() => {
					completedAnimation();

					// resetting Alpha
					v.Alpha = 1;
				}));
		}

		public void PrepareAnimateForInsert (AnimatingListView listView, View v) {
			v.TranslationX = -v.Width;
			v.Alpha = 0;
		}

		public void AnimateForInsert (AnimatingListView listView, View v, Action completedAnimation) {
			v.Animate ()
				.SetDuration (Android_Constants.INSERT_ANIMATION_DURATION_MILLIS)
				.Alpha (1)
				.TranslationX( 0 )
				.WithEndAction (new Runnable (completedAnimation));
		}
	}

	public class RightListItemAnimation : IListItemAnimation {
		public void PrepareAnimateForRemove (AnimatingListView listView, View v) {
		}

		public void AnimateForRemove (AnimatingListView listView, View v, Action completedAnimation) {
			v.Animate ()
				.SetDuration (Android_Constants.DELETE_ANIMATION_DURATION_MILLIS)
				.Alpha (0)
				.TranslationX( v.Width )
				.WithEndAction (new Java.Lang.Runnable (() => {
					completedAnimation();

					// resetting Alpha
					v.Alpha = 1;
				}));
		}

		public void PrepareAnimateForInsert (AnimatingListView listView, View v) {
			v.TranslationX = v.Width;
			v.Alpha = 0;
		}

		public void AnimateForInsert (AnimatingListView listView, View v, Action completedAnimation) {
			v.Animate ()
				.SetDuration (Android_Constants.INSERT_ANIMATION_DURATION_MILLIS)
				.Alpha (1)
				.TranslationX( 0 )
				.WithEndAction (new Runnable (completedAnimation));
		}
	}

	class OurTypeEvaluator : Java.Lang.Object, ITypeEvaluator
	{
		public Java.Lang.Object Evaluate (float fraction, Java.Lang.Object sv, Java.Lang.Object ev)
		{
			Rect startValue = sv as Rect;
			Rect endValue = ev as Rect;
			return new Rect (
				interpolate (startValue.Left, endValue.Left, fraction),
				interpolate (startValue.Top, endValue.Top, fraction),
				interpolate (startValue.Right, endValue.Right, fraction),
				interpolate (startValue.Bottom, endValue.Bottom, fraction));
		}

		public int interpolate(int start, int end, float fraction) {
			return (int)(start + fraction * (end - start));
		}
	}
}

