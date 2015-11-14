using System;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Views;

namespace Emdroid {
	public static class BasicRowColorSetter {
		public static void SetEven (bool isEven, View view) {
			if (view == null) return;
			Color color = isEven ? Android_Constants.EvenColor : Android_Constants.OddColor;
			Color selectedColor = isEven ? Android_Constants.SelectedEvenColor : Android_Constants.SelectedOddColor;
			var states = new StateListDrawable ();
			// Do not reverse the order of this. It's checked from top to bottom (first to last added).
			states.AddState (new int[] { Android.Resource.Attribute.StatePressed }, new ColorDrawable (selectedColor));
			states.AddState (new int[] { Android.Resource.Attribute.StateEnabled }, new ColorDrawable (color));
			BitmapSetter.SetBackground (view, states);
		}
	}
}

