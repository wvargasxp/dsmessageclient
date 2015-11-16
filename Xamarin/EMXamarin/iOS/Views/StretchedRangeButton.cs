using System;
using System.Collections.Generic;
using System.Diagnostics;
using CoreGraphics;
using Foundation;
using UIKit;
using MediaPlayer;
using em;
using System.Threading;

namespace iOS {
	public class StretchedRangeButton : UIButton {

		public StretchedRangeButton (UIButtonType buttonType) : base (buttonType) {

		}

		public StretchedRangeButton () {

		}

		public override bool PointInside(CGPoint point, UIEvent uievent) {
			float margin = 5.0f;
			CGRect area = this.Bounds.Inset (-margin, -margin);
			return area.Contains (point);
		}
	}
}

