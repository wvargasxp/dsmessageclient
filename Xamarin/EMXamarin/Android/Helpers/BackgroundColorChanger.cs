using System;
using System.Linq;
using Android.App;
using Android.Widget;
using em;

namespace Emdroid {
	public static class BackgroundColorChanger {

		public static BackgroundColor[] backgroundColors = {
			BackgroundColor.Blue,
			BackgroundColor.Orange,
			BackgroundColor.Pink,
			BackgroundColor.Green
		};

		public static void SetActionOnButtons (Fragment frag, Button[] buttons, Action<BackgroundColor> callback) {
			int numBackgrounds = backgroundColors.Count ();
			const int TAG_OFFSET = 213123;
			for (int i=0; i<numBackgrounds; i++) {
				Button button = buttons [i];
				button.Tag = TAG_OFFSET + i;
				button.Click += (sender, e) => {
					BackgroundColor color = EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme = backgroundColors [(int)(sender as Button).Tag - TAG_OFFSET];
					color.GetBackgroundResource ( (string file) => {
						if (frag != null && frag.Activity != null && frag.View != null && frag.Activity.Resources != null) {
							BitmapSetter.SetBackgroundFromFile (frag.View, frag.Activity.Resources, file);
							callback(color);
						}
					});
				};
			}
		}
	}
}