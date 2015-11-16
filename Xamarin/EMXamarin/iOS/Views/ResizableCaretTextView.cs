using UIKit;
using CoreGraphics;

namespace iOS {
	public class ResizableCaretTextView : NotifyOnFirstResponderTextView {

		public UIResponder OverrideNextResponder { get; set; }

		bool usingLargeCaret;
		public bool UseLargeCaret {
			get { return usingLargeCaret; }
			set { usingLargeCaret = value; }
		}

		public ResizableCaretTextView (CGRect rect) : base (rect) {
			usingLargeCaret = false;
		}

		public override CGRect GetCaretRectForPosition (UITextPosition position) {
			CGRect caretRect = base.GetCaretRectForPosition (position);
			if (this.UseLargeCaret)
				caretRect.Height += 75;
			
			return caretRect;
		}
			
		#region copy + paste related 
		// https://stackoverflow.com/questions/13601643/uimenucontroller-hides-the-keyboard

		// We keep track of NextResponder so that if another object becomes the FirstResponder.
		// We can make it so that the Keyboard doesn't dismiss itself.
		public override UIResponder NextResponder {
			get {
				if (this.OverrideNextResponder != null) {
					return this.OverrideNextResponder;
				} else {
					return base.NextResponder;
				}
			}
		}

		public override bool CanPerform (ObjCRuntime.Selector action, Foundation.NSObject withSender) {
			// If the OverrideNextResponder is not null. We don't want any of this class (UITextView)'s context menu items to show up.
			if (this.OverrideNextResponder != null) {
				return false;
			}
				
			return base.CanPerform (action, withSender);
		}
		#endregion
	}
}