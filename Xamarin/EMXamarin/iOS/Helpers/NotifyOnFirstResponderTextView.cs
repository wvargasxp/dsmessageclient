using System;
using UIKit;
using CoreGraphics;
using Foundation;
using em;
using UIDevice_Extension;

namespace iOS {
	public class NotifyOnFirstResponderTextView : UITextView {

		public bool BlockNextNotify { get; set; }

		public NotifyOnFirstResponderTextView (CGRect frame) : base (frame) {}

		public override bool BecomeFirstResponder () {
			bool first = base.BecomeFirstResponder ();

			if (UIDevice.CurrentDevice.IsIos9Later ()) {
				if (!this.BlockNextNotify) {
					NSNotificationCenter.DefaultCenter.PostNotificationName (iOS_Constants.NOTIFICATION_TEXTVIEW_BECAME_FIRST_RESPONDER, null);
				}

				this.BlockNextNotify = false;
			}

			return first;
		}
	}
}

