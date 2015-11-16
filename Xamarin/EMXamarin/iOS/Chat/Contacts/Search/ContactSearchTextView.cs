using UIKit;
using Foundation;
using CoreGraphics;

namespace iOS {
	public class ContactSearchTextView : NotifyOnFirstResponderTextView {
		public ContactSearchTextView (CGRect frame) : base (frame) {
			this.ContentSize = new CGSize (frame.Width, frame.Height);
		}

		public override bool CanPerform(ObjCRuntime.Selector action, NSObject withSender) {
			if (action == new ObjCRuntime.Selector ("copy:")) {
				if (!this.CursorAtEndOfTextArea) {
					return true;
				}
			}

			if (action == new ObjCRuntime.Selector ("paste:")) {
				if (this.CursorAtEndOfTextArea) {
					return true;
				}
			}

			return false;
		}

		public override void DeleteBackward () {
			base.DeleteBackward ();
		}

		private bool CursorAtEndOfTextArea {
			get {
				NSRange range = this.SelectedRange;
				if (range.Location != NSRange.NotFound) {
					if (range.Location >= this.Text.Length) {
						return true;
					}
				}

				return false;
			}
		}
	}
}