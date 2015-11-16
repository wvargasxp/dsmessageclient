using System;
using UIKit;

namespace iOS {
	public class SendUIButton : UIButton {

		public SendUIButtonState Mode {
			get;
			set;
		}

		public SendUIButton (UIButtonType buttonType) : base (buttonType) {

		}

		public SendUIButton () {

		}

		public enum SendUIButtonState {
			Record,
			Send,
			Disabled
		}
	}
}

