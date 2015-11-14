using Android.Graphics;
using Android.Views;
using Android.Widget;

namespace Emdroid {
  	public static class ViewClickStretchUtil {

    	public static int INCREASED_AREA = 200;

    	public static void StretchRangeOfButton (Button button) {
			var delegateArea = new Rect ();
	      	button.GetHitRect (delegateArea);
	      	delegateArea.Bottom += INCREASED_AREA;
	      	delegateArea.Right += INCREASED_AREA;
	      	delegateArea.Top -= INCREASED_AREA;
	      	delegateArea.Left -= INCREASED_AREA;
	      	var touchDelegate = new TouchDelegate (delegateArea, button);
	      	((View)button.Parent).TouchDelegate = touchDelegate;
		}
  	}
}