using System;
using Android.Support.V7.Widget;
using em;

namespace Emdroid {
	public abstract class EmRecyclerViewAdapter : RecyclerView.Adapter {
		protected int SelectedPosition { get; set; }

		protected int SelectedPositionInModel {
			get {
				int uiPos = this.SelectedPosition;
				int modelPos = UIPositionToModelPosition (uiPos);
				return modelPos;
			}
		}

		protected const int UnSelected = -1;

		public EmRecyclerViewAdapter () {
			this.SelectedPosition = UnSelected;
		}

		public virtual int ModelPositionToUIPosition (int modelPosition) {
			return modelPosition;
		}

		public virtual int UIPositionToModelPosition (int uiPosition) {
			return uiPosition;
		}

		/*
		 * The int that this returns is the position of the element in the model.
		 * Not the position of the UI element. If a header is showing, the UI position would be incremented by one.
		 */ 
		public event EventHandler<int> ItemClick;

		/*
		 * The int that this returns is the position of the element in the model.
		 * Not the position of the UI element. If a header is showing, the UI position would be incremented by one.
		 */ 
		public event EventHandler<int> LongItemClick;

		/*
		 * @param uiPosition - A position that would be used in conjunction with our view, including a header if present.
		 * Calls the ItemClick event and returns back a model position.
		 */ 
		protected void OnClick (int uiPosition) {
			ShowTapFeedback (uiPosition);
			if (ItemClick != null) {
				int position = UIPositionToModelPosition (uiPosition);
				ItemClick (this, position);
			}
		}

		/*
		 * @param uiPosition - A position that would be used in conjunction with our view, including a header if present.
		 * Calls the LongItemClick event and returns back a model position.
		 */ 
		protected void OnLongClick (int uiPosition) {
			ManageSelectionState (uiPosition);
			if (LongItemClick != null) {
				int position = UIPositionToModelPosition (uiPosition);
				LongItemClick (this, position);
			}
		}
			
		/*
		 * @param uiPosition - This position would be the position where our row can be selected or unselected. Can also be Unselected value.
		 * Updates the old selected row and the new selected row to show selection feedback.
		 */ 
		protected virtual void ManageSelectionState (int uiPosition) {}

		/*
		 * When a row has been selected, we display to the user it's been selected.
		 * The selection doesn't go away automatically, so we would use this code to remove it.
		 * Would typically be called in the OnScrolled event to remove selection state.
		 */ 
		public void ResetSelectedItems () {
			ManageSelectionState (UnSelected);
		}

		protected virtual void ShowTapFeedback (int uiPosition) {}
	}
}

