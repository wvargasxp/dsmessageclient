using UIKit;
using Foundation;
using CoreGraphics;

namespace iOS {
	public class BlockContextMenuTextView : NotifyOnFirstResponderTextView {
		public BlockContextMenuTextView (CGRect frame) : base (frame) {
			this.ContentSize = new CGSize (frame.Width, frame.Height);
			this.Selectable = false;
		}

		public override bool CanPerform(ObjCRuntime.Selector action, NSObject withSender) {
			return false;
		}

		public override void DeleteBackward () {
			base.DeleteBackward ();
		}
	}
}