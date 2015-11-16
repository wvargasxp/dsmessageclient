using CoreGraphics;
using UIKit;

// This class is basically a UIView.

namespace iOS {
	public class UINavigationBarLine : UIView {

		public UINavigationBarLine (CGRect frame) : base (frame) {

		}

		public override void DrawRect (CGRect area, UIViewPrintFormatter formatter) {
			base.DrawRect (area, formatter);
		}

		public override void WillMoveToWindow (UIWindow window) {
			base.WillMoveToWindow (window);
		}

	}
}