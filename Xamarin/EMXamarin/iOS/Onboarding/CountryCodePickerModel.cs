using System;
using UIKit;
using System.Collections.Generic;
using em;
using CoreGraphics;

namespace iOS {
	public class CountryCodePickerModel : UIPickerViewModel {

		const int FLAG_ROW_WIDTH = 58; //aspect ratio 3:2
		const int FLAG_ROW_HEIGHT = 44; //aspect ratio 3:2
		const int FLAG_ROW_PADDING = 15;

		public IList<CountryCode> values;
		public event EventHandler<PickerChangedEventArgs> PickerChanged;

		public CountryCodePickerModel (IList<CountryCode> values) {
			this.values = values;
		}

		public override nint GetComponentCount (UIPickerView pickerView) {
			return 1;
		}

		public override nint GetRowsInComponent (UIPickerView pickerView, nint component) {
			return values.Count;
		}

		public override string GetTitle (UIPickerView pickerView, nint row, nint component) {
			//return translated country name
			return values [(int)row].translationKey.t ();
		}

		public override nfloat GetRowHeight (UIPickerView pickerView, nint component) {
			return 40f;
		}

		public override UIView GetView (UIPickerView pickerView, nint row, nint component, UIView view) {
			var v = new UIView (new CGRect (0, 0, pickerView.Frame.Width, FLAG_ROW_HEIGHT));

			var img = UIImage.FromFile ("flags/" + values [(int)row].photoUrl);
			var imgView = new UIImageView (new CGRect (FLAG_ROW_PADDING, 0, FLAG_ROW_WIDTH, FLAG_ROW_HEIGHT));
			imgView.Image = img;
			v.Add (imgView);

			var txtView = new UITextView (new CGRect (FLAG_ROW_WIDTH + FLAG_ROW_PADDING, 0, pickerView.Frame.Width - FLAG_ROW_WIDTH - FLAG_ROW_PADDING*2, FLAG_ROW_HEIGHT));
			txtView.Text = values [(int)row].translationKey.t ();
			v.Add (txtView);

			return v;
		}

		public override void Selected (UIPickerView pickerView, nint row, nint component) {
			if (PickerChanged != null) {
				PickerChanged (this, new PickerChangedEventArgs{ SelectedValue = values [(int)row] });
			}
		}
	}

	public class PickerChangedEventArgs : EventArgs {
		public CountryCode SelectedValue { get; set; }
	}
}