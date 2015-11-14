using System;
using Android.Views;
using Android.Widget;
using Android.Support.V7.Widget;

namespace Emdroid {
	public class UndoViewHolder : RecyclerView.ViewHolder {
		public Button DeleteButton { get; set; }
		public Button UndoButton { get; set; }

		protected Action<int> DeleteTapped { get; set; }
		protected Action<int> UndoTapped { get; set; }

		public static UndoViewHolder NewInstance (ViewGroup parent, Action<int> deleteTapped, Action<int> undoTapped) {
			View view = LayoutInflater.From (parent.Context).Inflate (Resource.Layout.undo_row, parent, false);
			UndoViewHolder h = new UndoViewHolder (view);
			h.UndoTapped = undoTapped;
			h.DeleteTapped = deleteTapped;
			return h;
		}

		public UndoViewHolder (View view) : base (view) {
			this.DeleteButton = view.FindViewById<Button> (Resource.Id.undo_row_btn);
			this.UndoButton = view.FindViewById<Button> (Resource.Id.undo_row_undobutton);

			this.DeleteButton.Click += DeleteButton_Click;
			this.UndoButton.Click += UndoButton_Click;
		}

		private void UndoButton_Click (object sender, EventArgs e) {
			this.UndoTapped (base.AdapterPosition);
		}

		private void DeleteButton_Click (object sender, EventArgs e) {
			this.DeleteTapped (base.AdapterPosition);
		}
	}
}

