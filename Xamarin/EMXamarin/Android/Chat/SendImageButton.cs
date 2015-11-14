using System;
using Android.Content;
using Android.Util;
using Android.Widget;

namespace Emdroid {
	public class SendImageButton : ImageButton {

		public SendImageButtonMode Mode {
			get;
			set;
		}

		public SendImageButton(Context context) : base (context) {
		}

		public SendImageButton(Context context, IAttributeSet attrs) : base (context, attrs) {
		}

		public SendImageButton(Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle) {
		}

		public enum SendImageButtonMode {
			Send,
			Record,
			Disabled,
		}
	}
}

