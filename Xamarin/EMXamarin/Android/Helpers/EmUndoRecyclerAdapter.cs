using System;
using System.Collections.Generic;

namespace Emdroid {
	public abstract class EmUndoRecyclerAdapter : EmRecyclerViewAdapter, ISwipeListener {

		protected IList<int> RowsWithUndoState { get; set; }
		protected int RowHeight { get; set; }

		public EmUndoRecyclerAdapter () {
			this.RowsWithUndoState = new List<int> ();
		}

		protected virtual void UndoTapped (int row) {
			this.RowsWithUndoState.Remove (row);
			this.NotifyItemChanged (row);
		}

		protected virtual void DeleteTapped (int row) {
			this.RowsWithUndoState.Remove (row);
			this.NotifyItemRemoved (row);
		}

		public void SetUndoStateAtRow (int row) {
			this.RowsWithUndoState.Add (row);
			this.NotifyItemChanged (row);
		}

		#region ISwipeListener implementation

		public virtual bool CanSwipeAtRow (int row) {
			if (this.RowsWithUndoState.Contains (row)) {
				return false;
			}

			return true;
		}

		public virtual void RowSwiped (int row) {
			this.SetUndoStateAtRow (row);
		}

		#endregion
	}
}

