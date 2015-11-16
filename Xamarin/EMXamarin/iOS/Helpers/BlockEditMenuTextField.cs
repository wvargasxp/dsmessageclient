using System;
using UIKit;
using CoreGraphics;

namespace iOS {
	public class BlockEditMenuTextField : UITextField {
		public BlockEditMenuTextField ()
		{
		}

		public BlockEditMenuTextField (CGRect frame) : base (frame) {}

		public override bool CanPerform (ObjCRuntime.Selector action, Foundation.NSObject withSender) {
			return false;
		}
	}
}

