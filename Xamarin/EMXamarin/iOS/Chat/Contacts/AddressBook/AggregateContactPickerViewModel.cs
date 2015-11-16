using System;
using UIKit;
using em;
using CoreGraphics;

namespace iOS {
	public class AggregateContactPickerViewModel : UIPickerViewModel {

		private WeakReference _pickerRef = null;
		private UIPickerView Picker {
			get { return this._pickerRef != null ? this._pickerRef.Target as UIPickerView : null; } 
			set { this._pickerRef = new WeakReference (value); }
		}

		public AggregateContact Contact { get; set; }
		public event EventHandler<AggregateContactPickerChanged> PickerChanged;

		public bool[] Chosen { get; set; }
		private bool AddedGesture { get; set; }

		private nfloat Width { get; set; }

		private WeakReference _controller = null;
		public AddressBookViewController Controller { 
			get { return this._controller != null ? this._controller.Target as AddressBookViewController : null; }
			set { this._controller = new WeakReference (value); }
		}

		public AggregateContactPickerViewModel (AddressBookViewController controller, AggregateContact contact, bool[] chosen) {
			this.Contact = contact;
			this.Controller = controller;
			this.Chosen = chosen;
			this.Width = controller.View.Frame.Width;
			this.AddedGesture = false;
		}

		public override nint GetComponentCount (UIPickerView pickerView) {
			return 1;
		}

		public override nint GetRowsInComponent (UIPickerView pickerView, nint component) {
			return this.Contact.Contacts.Count;
		}
			
		public override nfloat GetRowHeight (UIPickerView pickerView, nint component) {
			return iOS_Constants.APP_CELL_ROW_HEIGHT;
		}

		public override nfloat GetComponentWidth (UIPickerView pickerView, nint component) {
			return this.Width;
		}

		public override UIView GetView (UIPickerView picker, nint row, nint component, UIView view) {
			this.Picker = picker;
			var cell = view as ContactTableViewCell; 

			if (cell == null) {
				cell = ContactTableViewCell.Create ();

				if (!this.AddedGesture) {
					UITapGestureRecognizer tap = new UITapGestureRecognizer (PickerViewTapped); // todo; check for memory leak
					tap.NumberOfTapsRequired = 1;
					tap.ShouldRecognizeSimultaneously += (UIGestureRecognizer tapGesutre, UIGestureRecognizer otherGesture) => { 
						return true; 
					};

					tap.ShouldReceiveTouch += (UIGestureRecognizer recognizer, UITouch touch) => {
						return true;
					};

					picker.AddGestureRecognizer (tap);
					this.AddedGesture = true;
				}

				cell.Bounds = new CGRect (0, 0, this.Width, iOS_Constants.APP_CELL_ROW_HEIGHT);
			}

			AddressBookViewController controller = this.Controller;
			if (controller != null) {
				Contact contact = this.Contact.Contacts [(int)row];
				cell.Contact = contact;
				// Logic to set the odd/even row colors. 
				cell.SetEvenRow (row % 2 == 0);

				UpdateCellCheckmark (cell, row);
				cell.Tag = row;
			}

			return cell;
		}

		private void PickerViewTapped (UITapGestureRecognizer re) {
			UIPickerView picker = this.Picker;
			if (picker == null) return;
			
			if (re.State == UIGestureRecognizerState.Ended) {
				CGRect selectedRowFrame = picker.Bounds.Inset (0, (nfloat)(this.Picker.Frame.Height * .85 / 2.0));
				CGPoint pointInsidePicker = re.LocationInView (picker);
				bool userTappedOnCurrentSelectedRow = selectedRowFrame.Contains (pointInsidePicker);
				if (userTappedOnCurrentSelectedRow) {
					const int component = 0;
					nint row = picker.SelectedRowInComponent (component);

					this.Chosen [(int)row] = !this.Chosen [row]; // toggle
					ContactTableViewCell _cc = picker.ViewFor (row, component) as ContactTableViewCell;
					UpdateCellCheckmark (_cc, row);

					// Invalidate picker view caches.
					picker.ReloadAllComponents ();
				}
			}
		}

		private void UpdateCellCheckmark (ContactTableViewCell cell, nint row) {
			if (cell == null) return;
			cell.UpdateCheckBox (this.Chosen [(int)row]);
		}

		public override void Selected (UIPickerView picker, nint row, nint component) {
			if (PickerChanged != null) {
				PickerChanged (this, new AggregateContactPickerChanged { SelectedValue = this.Contact.Contacts [(int)row] });
			}
		}
	}

	public class AggregateContactPickerChanged : EventArgs {
		public Contact SelectedValue { get; set; }
	}
}

