using System;
using Android.Views;
using Android.Widget;
using em;
using System.Collections.Generic;
using System.Diagnostics;
using Android.Graphics;
using System.Linq;

namespace Emdroid {
	public class PopupWindowController : Java.Lang.Object, ISurfaceHolderCallback, ISurfaceHolderCallback2 {

		// todo weakref
		private SurfaceView Surface { get; set; }
		private ISurfaceHolder SurfaceHolder { get; set; }
		private ChatFragment Fragment { get; set; }

        private bool _shouldFlowGraphBackwards = false;
		private bool ShouldFlowGraphBackwards { get { return this._shouldFlowGraphBackwards; } set { this._shouldFlowGraphBackwards = value; } }

		private const float XCoordinateOffset = .25f;
		private const int PaddingBeforeHittingMaximumWidth = 20;
		private const int PointsPerLine = 4; // x0,y0,x1,y1

        private IList<float> _points = new List<float> ();
        private IList<float> Points { get { return this._points; } set { this._points = value; } } 

        private object _modifyPointsListLock = new object ();
        private object ModifyPointListLock { get { return this._modifyPointsListLock; } set { this._modifyPointsListLock = value; } }

		private Color Color { get; set; }

		public PopupWindowController (ChatFragment f, Color color) {
			this.Fragment = f;
			this.Surface = f.AudioSurface;
			this.SurfaceHolder = this.Surface.Holder;
			this.SurfaceHolder.AddCallback (this);

			// Transparent background.
			this.Surface.SetZOrderOnTop (true);
			this.SurfaceHolder.SetFormat (Format.Transparent);

			// Color of graph.
			this.Color = color;
		}

		public void Show () {
			NotificationCenter.DefaultCenter.AddWeakObserver (null, Android_Constants.AndroidSoundRecordingRecorder_HasUpdatedAmplitude, HandleNotificationHasUpdatedAmplitude);
		}

		public void Dismiss () {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
			EMTask.DispatchBackground (() => {
				lock (this.ModifyPointListLock) {
					this.Points.Clear ();
					this.ShouldFlowGraphBackwards = false;
				}
			});
		}
			
		private void HandleNotificationHasUpdatedAmplitude (Notification n) {
			lock (this.ModifyPointListLock) {
				int length = this.Points.Count;

				// Obtain values of the rect we're drawing on.
				Rect rect = this.SurfaceHolder.SurfaceFrame;

				float rectWidth = rect.Width ();
				float rectHeight = rect.Height ();

				if (rectWidth == 0) {
					return;
				}

				Dictionary<string, int> extra = n.Extra as Dictionary<string, int>;
				Debug.Assert (extra != null, "HandleNotificationHasUpdatedAmplitude, Extra is null when it shouldn't be.");

				// Normalize the amplitude and scale it according to the height of what we're drawing in.
				float amplitude = (float)extra [Android_Constants.AndroidSoundRecordingRecorder_LastAmplitudeKey] / 32767f /*max signed short */;
				amplitude *= ((rectHeight) / 2) + 1;

				// Since we're plotting a line graph, just use the previous xcoordinate as the basis for the new xcoordinate.
				// Obtain the previous coordinate and add an offset to it (XCoordinateOffset).
				float newXCoordinate = length > 1 ? this.Points [length - 2] + XCoordinateOffset : 0;

				// We're drawing the amplitude in the center of the rect.
				// We want it to be symmetrical so calculate the amplitude and its negative value.
				float centerOfRect = rectHeight / 2;
				float amplitudeToUse = amplitude; // (amplitude > 1 ? amplitude : 0);
				float heightPoint = centerOfRect + amplitudeToUse;
				float negativeHeightPoint = centerOfRect - amplitudeToUse;

				if (length == 0) {
					// Length is zero. It's the start of the wave form.
					this.Points.Add (newXCoordinate);
					this.Points.Add (heightPoint);

					this.Points.Add (newXCoordinate);
					this.Points.Add (heightPoint);
				} else {
					// We need to get the previous coordinates to avoid a break in the line.
					float previousYCoordinate = this.Points [length - 1];
					float previousXCoordinate = this.Points [length - 2];

					// Adding previous points to form a new line without break.
					this.Points.Add (previousXCoordinate);
					this.Points.Add (previousYCoordinate);
			
					// Add the new x,y coordinates to form a line with the previous point.
					this.Points.Add (newXCoordinate);
					this.Points.Add (heightPoint);

					// Add the point again so we can form a line with the negative version.
					this.Points.Add (newXCoordinate);
					this.Points.Add (heightPoint);

					// Draw a negative version of the amplitude so that wave form is mirrored.
					this.Points.Add (newXCoordinate);
					this.Points.Add (negativeHeightPoint);
				}
					
				// We need to keep track of when we should start flowing the graph backwards.
				if (!this.ShouldFlowGraphBackwards && (newXCoordinate + PaddingBeforeHittingMaximumWidth) > rectWidth) {
					this.ShouldFlowGraphBackwards = true;
				}

				// If we're flowing the graph backwards, we need to continually offset the xcoordinate values.
				if (this.ShouldFlowGraphBackwards) {
					int newLength = this.Points.Count;
					IList<float> newPoints = new List<float> ();
					Debug.Assert (ValidListOfCoordinatePoints (this.Points), "PopupWindowController, List of points should be valid at this time but is not.");

					for (int i = 0; i < newLength; i = i + PointsPerLine) {
						float x0 = this.Points [i + 0];
						float y0 = this.Points [i + 1];
						float x1 = this.Points [i + 2];
						float y1 = this.Points [i + 3];

						// x0 should be less than or equal to x1.
						x0 = x0 - XCoordinateOffset;
						x1 = x1 - XCoordinateOffset;

						// We check if the 'line' is still going to be drawn by the 2nd xcoordinate.
						// This is because even if the first xcoordinate is offscreen,
						// we'd still need it in the points list to recreate the line.
						if (x1 >= 0) { 
							// If it's still inside the rect, we can add all the points.
							newPoints.Add (x0);
							newPoints.Add (y0);
							newPoints.Add (x1);
							newPoints.Add (y1);
						}
					}

					this.Points = newPoints;
				}
			
				DrawOntoSurface ();
			}
		}

		private void DrawOntoSurface () {
			ISurfaceHolder holder = this.SurfaceHolder;
			if (holder != null && holder.Surface != null && ValidListOfCoordinatePoints (this.Points)) {
				// Paint to use for drawing.
				Paint linePaint = new Paint();
				linePaint.StrokeWidth = 2f;
				linePaint.AntiAlias = true;
				linePaint.Color = this.Color;

				// Canvas of the surface we're drawing on.
				Canvas canvas = holder.LockCanvas ();

				// Clear the canvas before drawing the new lines.
				canvas.DrawColor (Color.Transparent, PorterDuff.Mode.Clear);

				// Get the list of points to draw and draw the lines.
				float[] points = this.Points.ToArray ();
				canvas.DrawLines (points, linePaint);
				holder.UnlockCanvasAndPost (canvas);
			}
		}

		private bool ValidListOfCoordinatePoints (IList<float> points) {
			return points.Count % PointsPerLine == 0; // We need at least 4 points to draw a line. x1,y1,x2,y2.
		}

		#region ISurfaceHolderCallback
		public void SurfaceChanged (ISurfaceHolder holder, Android.Graphics.Format format, int width, int height) {
			//Debug.WriteLine ("SurfaceChanged");
			lock (this.ModifyPointListLock) {
				DrawOntoSurface ();
			}
		}

		public void SurfaceCreated (ISurfaceHolder holder) {
			//Debug.WriteLine ("SurfaceCreated");
		}

		public void SurfaceDestroyed (ISurfaceHolder holder) {
			//Debug.WriteLine ("SurfaceDestroyed");
		}
		#endregion

		#region ISurfaceHolderCallback2
		public void SurfaceRedrawNeeded (ISurfaceHolder holder) {
			//Debug.WriteLine ("SurfaceRedrawNeeded");
		}
		#endregion
	}
}

