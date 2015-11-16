using CoreGraphics;
using Foundation;
using UIKit;

namespace iOS
{
	public class ColorThemePickerViewControllerCell : UITableViewCell
	{
		public static readonly NSString Key = new NSString ("ColorThemePickerViewControllerCell");

		public UIImageView ColorImageView;
		public ColorThemePickerViewControllerCell () : base (UITableViewCellStyle.Value1, Key) {
			ColorImageView = new UIImageView();
			ColorImageView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			CGRect frame = ContentView.Bounds;
			frame = new CGRect (new CGPoint (15, 5), new CGSize (frame.Size.Width - 30, frame.Size.Height - 10));
			ColorImageView.Frame = frame;

			ContentView.AutosizesSubviews = true;
			ContentView.AddSubview (ColorImageView);
		}
	}
}