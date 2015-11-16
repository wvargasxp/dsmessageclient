using em;
using Foundation;
using UIKit;
using CoreGraphics;
using System;

namespace iOS {
	public class ColorThemePickerViewControllerSource : UITableViewSource {

		readonly WeakReference pickerControllerRef;

		public ColorThemePickerViewControllerSource (ColorThemePickerViewControllerController controller) {
			pickerControllerRef = new WeakReference(controller);
		}

		public override nint NumberOfSections (UITableView tableView) {
			return 1;
		}

		public override nint RowsInSection (UITableView tableview, nint section) {
			return BackgroundColor.AllColors.Length;
		}

		public override string TitleForHeader (UITableView tableView, nint section) {
			return "CR";
		}

		public override UIView GetViewForHeader (UITableView tableView, nint section) {
			var heading = new UILabel (new CGRect (0, 0, tableView.Frame.Width, 20f));
			heading.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			heading.TextAlignment = UITextAlignment.Center;
			heading.Font = FontHelper.DefaultFontForLabels();
			heading.TextColor = iOS_Constants.WHITE_COLOR;
			heading.BackgroundColor = UIColor.Clear;
			heading.Text = "CHOOSE_COLOR_TITLE".t ();

			return heading;
		}

		public override UITableViewCell GetCell (UITableView tableView, NSIndexPath indexPath) {
			var cell = tableView.DequeueReusableCell (ColorThemePickerViewControllerCell.Key) as ColorThemePickerViewControllerCell ?? new ColorThemePickerViewControllerCell ();

			BackgroundColor theme = BackgroundColor.AllColors [indexPath.Row];
			UIImage image = UIImage.FromFile (theme.GetColorSelectionSquareResource ());
			image = image.StretchableImage ((int)(image.Size.Width / 2), (int)(image.Size.Height / 2));

			cell.ColorImageView.Image = image;
			
			return cell;
		}

		public override void RowSelected (UITableView tableView, NSIndexPath indexPath) {
			ColorThemePickerViewControllerController pickerController = pickerControllerRef.Target as ColorThemePickerViewControllerController;
			if (pickerController != null) {
				tableView.DeselectRow (indexPath, true);
				pickerController.DidPickColorDelegate (BackgroundColor.AllColors [indexPath.Row]);
			}
		}
	}
}