// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;
using System.CodeDom.Compiler;

namespace iOS
{
	[Register ("IncomingTextTableViewCell")]
	partial class IncomingTextTableViewCell
	{
		[Outlet]
		MonoTouch.UIKit.UIImageView ChatBubble { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel FromLabel { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel MessageLabel { get; set; }

		[Outlet]
		MonoTouch.UIKit.UILabel TimestampLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (ChatBubble != null) {
				ChatBubble.Dispose ();
				ChatBubble = null;
			}

			if (FromLabel != null) {
				FromLabel.Dispose ();
				FromLabel = null;
			}

			if (MessageLabel != null) {
				MessageLabel.Dispose ();
				MessageLabel = null;
			}

			if (TimestampLabel != null) {
				TimestampLabel.Dispose ();
				TimestampLabel = null;
			}
		}
	}
}
