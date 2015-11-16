using UIKit;
using CoreGraphics;

namespace iOS {
	public static class ViewAdjuster {
		const int DEFAULT_OFFSET = 22;
		public static void OffSetViewMinusY (UIView view, int offset) {
			CGRect frame;
			frame = view.Frame;
			frame.Y -= offset;
			view.Frame = frame;
		}

		public static void OffSetViewMinusY (UIView view) {
			OffSetViewMinusY (view, DEFAULT_OFFSET);
		}
	}
}