using System;
using UIKit;
using System.Collections.Generic;
using em;
using CoreGraphics;

namespace iOS {
	public class FromAliasPickerViewModel : UIPickerViewModel {

		public event EventHandler<AliasInfoPickerChangedEventArgs> PickerChanged;
		private IList<AliasInfo> aliases;

		public FromAliasPickerViewModel (IList<AliasInfo> _aliases) {
			aliases = _aliases;
		}

		public override nint GetComponentCount (UIPickerView picker) {
			return 1;
		}

		public override nint GetRowsInComponent (UIPickerView picker, nint component) {
			return aliases.Count + 1;
		}

		public override string GetTitle (UIPickerView picker, nint row, nint component) {
			if (row == aliases.Count)
				return AppDelegate.Instance.applicationModel.account.accountInfo.displayName;
			else
				return aliases [(int)row].displayName;
		}

		public override nfloat GetRowHeight (UIPickerView picker, nint component) {
			return 40f;
		}

		public override UIView GetView (UIPickerView picker, nint row, nint component, UIView view) {
			// TODO: This was written very quickly, go back and spruce it up.
			var v = new UIView (new CGRect (0, 0, picker.Frame.Width, 40));

			string text = "";
			if (row == aliases.Count)
				text = AppDelegate.Instance.applicationModel.account.accountInfo.displayName;
			else
				text = aliases [(int)row].displayName;

			UILabel l = new UILabel (new CGRect (10, 10, 320, 35));
			l.Text = text;
			v.Add (l);

			return v;
		}

		public override void Selected (UIPickerView picker, nint row, nint component) {
			if (PickerChanged != null) {
				if (row == AppDelegate.Instance.applicationModel.account.accountInfo.ActiveAliases.Count)
					PickerChanged (this, new AliasInfoPickerChangedEventArgs { SelectedValue = null });
				else
					PickerChanged (this, new AliasInfoPickerChangedEventArgs { SelectedValue = AppDelegate.Instance.applicationModel.account.accountInfo.ActiveAliases [(int)row] });
			}
		}
	}

	public class AliasInfoPickerChangedEventArgs : EventArgs {
		public AliasInfo SelectedValue { get; set; }
	}
}
