
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Emdroid {
	public class BlockLayoutView : View {

		bool blockLayout = false;

		public BlockLayoutView (Context context) :
			base (context) {
			Initialize ();

		}

		public BlockLayoutView (Context context, IAttributeSet attrs) :
			base (context, attrs) {
			Initialize ();
		}

		public BlockLayoutView (Context context, IAttributeSet attrs, int defStyle) :
			base (context, attrs, defStyle) {
			Initialize ();
		}

		void Initialize () {
			this.SetLayerType (LayerType.Hardware, null);
		}

		public override void RequestLayout () {
			if (!blockLayout)
				base.RequestLayout ();
		}

		public override void SetBackgroundDrawable (Android.Graphics.Drawables.Drawable background) {
			blockLayout = true;
			base.SetBackgroundDrawable (background);
			blockLayout = false;
		}

		protected override void OnDraw (Android.Graphics.Canvas canvas) {
			base.OnDraw (canvas);
		}
	}
}

