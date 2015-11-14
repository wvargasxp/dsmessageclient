using System;
using Android.Widget;
using Android.Content;
using Android.Util;
using System.Collections.Generic;
using Android.App;
using em;

namespace Emdroid {
	public class MultiSpinner : Spinner, IDialogInterfaceOnMultiChoiceClickListener, IDialogInterfaceOnCancelListener {

		private bool SpinnerReady {
			get {
				return this.ListAdapter != null; 
			}
		}

		private bool[] Chosen { 
			get {
				return this.ListAdapter.Chosen;
			}
		}

		private WeakReference _dialogRef = null;
		private AlertDialog Dialog {
			get { return this._dialogRef != null ? this._dialogRef.Target as AlertDialog : null; }
			set { this._dialogRef = new WeakReference (value); }
		}

		private IMultiSpinnerListener Listener { get; set; }
		private AddressBookPickerAdapter ListAdapter { get; set; }

		public MultiSpinner (Context context) : base (context) {}

		public MultiSpinner (Context arg0, IAttributeSet attrs) : base (arg0,  attrs) {}

		public MultiSpinner (Context arg0, IAttributeSet attrs, int arg2) : base (arg0, attrs, arg2) {}

		public override void OnClick (IDialogInterface dialog, int which) {
			base.OnClick (dialog, which);
		}

		public void OnClick (IDialogInterface dialog, int which, bool isChecked) {
			if (this.SpinnerReady) {
				this.Chosen [which] = isChecked;
			}
		}

		public void OnCancel (IDialogInterface dialog) {
			this.Listener.OnItemsSelected (this.Chosen);
		}

		public override bool PerformClick () {
			AlertDialog.Builder builder = new AlertDialog.Builder (this.Context);
			builder.SetAdapter (this.ListAdapter, (object sender, DialogClickEventArgs e) => {});

			builder.SetPositiveButton ("OK_BUTTON".t (), HandleOkButtonPressed);
			builder.SetOnCancelListener (this);

			// Set the multi choice on the list view itself, otherwise the dialog disappears after one click.
			AlertDialog dialog = builder.Create ();
			dialog.ListView.ItemsCanFocus = false;
			dialog.ListView.ChoiceMode = ChoiceMode.Multiple;
			dialog.ListView.ItemClick += HandleRowTappedInAlert;
			dialog.Show ();
			this.Dialog = dialog;
			return true;
		}

		public void SetItems (IMultiSpinnerListener listener, AddressBookPickerAdapter adapter) {
			this.Listener = listener;
			this.ListAdapter = adapter;
		}

		#region event handlers
		private void HandleRowTappedInAlert (object sender, ItemClickEventArgs e) {
			if (!this.SpinnerReady) {
				return;
			}

			int position = e.Position;
			this.Chosen [position] = !this.Chosen [position]; // toggle

			// This section here is just trying to see if the dialog is alive (it should be) and then calling a method 
			// on the list adapter to update a single row. If the dialog is not in memory for some reason, just notify data set changed.
			AlertDialog dialog = this.Dialog;
			if (dialog == null) {
				this.ListAdapter.NotifyDataSetChanged ();
			} else {
				this.ListAdapter.UpdateCheckboxAtIndex (position, dialog.ListView);
			}
		}

		private void HandleOkButtonPressed (object sender, DialogClickEventArgs e) {
			IDialogInterface d = sender as IDialogInterface;
			if (d == null) return;
			d.Cancel ();
		}
		#endregion
	}

	public interface IMultiSpinnerListener {
		void OnItemsSelected (bool[] selectedItems);
	}
}