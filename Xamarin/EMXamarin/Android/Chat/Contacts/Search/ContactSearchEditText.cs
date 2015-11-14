using System;
using System.Collections.Generic;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class ContactSearchEditText : EditText {
		IContactSearchController fragment;
		public IContactSearchController Fragment {
			get { return fragment; }
		}

		private SharedContactSearchDelegate Shared { get; set; }

		// Used to keep track of a string before text changes incase we need to revert back to the right string.
		string contactSearchTextBeforeChange;
		public string ContactSearchTextBeforeChange {
			get { return contactSearchTextBeforeChange; }
			set { contactSearchTextBeforeChange = value; }
		}

		// Sometimes we're setting a value to the same EditText in its callbacks. 
		// These will trigger additional callbacks which will loop forever.
		bool dontListenOnContactTextChange = false;
		public bool DontListenOnContactTextChange {
			get { return dontListenOnContactTextChange; }
			set { dontListenOnContactTextChange = value; }
		}

		bool didChangeSelectionWhilePossiblyDeleting = false;
		public bool DidChangeSelectionWhilePossiblyDeleting {
			get { return didChangeSelectionWhilePossiblyDeleting; } 
			set { didChangeSelectionWhilePossiblyDeleting = value; }
		}

		protected ContactSearchEditText (IntPtr javaReference, JniHandleOwnership transfer)
			: base (javaReference, transfer) {}

		public ContactSearchEditText (Context context)
			: this (context, null) {}

		public ContactSearchEditText (Context context, IAttributeSet attrs)
			: this (context, attrs, 0) {}

		public ContactSearchEditText (Context context, IAttributeSet attrs, int defStyle)
			: base (context, attrs, defStyle) {
			SetListeners ();
			this.CustomSelectionActionModeCallback = new DisabledCustomSelectionCallback ();
			this.Shared = new AndroidSharedContactSearchDelegate (this);
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}
			
		public void SetParent (IContactSearchController parent) {
			fragment = parent;
		}

		public void SetResultCallback () {
			fragment.SetQueryResultCallback (() => {
				EMTask.DispatchMain (() => {
					if (!EMApplication.Instance.IsSenseUI) {
						UpdateTextAppearance ();
					}
				});
			});
		}

		public void SetListeners () {
			this.BeforeTextChanged += ContactSearchBeforeTextChanged;
			this.AfterTextChanged += ContactSearchAfterTextChanged;
			this.Click += ContactSearchClicked;
			this.FocusChange += ContactSearchFocusChanged;
		}

		public void PossibleReplaceContact () {
			// This would be case where a user was selected to be deleted and we selected another user to replace them.
			this.Shared.PossibleReplaceContact ();
		}

		protected override void OnSelectionChanged (int selStart, int selEnd) {
			if (this.Shared == null)
				return;
			if (this.Shared.IgnoreSelectionChange) {
				this.Shared.IgnoreSelectionChange = false;
				return;
			}
				
			base.OnSelectionChanged (selStart, selEnd);
			int cursorPosition = selStart;
			if (this.Shared.LastContactPositions != null && !this.Shared.PossibleDeleteInProgress) {
				for (int i = 0; i < this.Shared.LastContactPositions.Count; i++) {
					int lastPosition = this.Shared.LastContactPositions [i];
					if (cursorPosition < lastPosition+2) {
						this.Shared.ContactPositionNearCursor = lastPosition;
						this.Shared.CurrentInitialPosition = this.Shared.LastFirst [lastPosition];
						this.Shared.IgnoreSelectionChange = true;
						this.SetSelection (this.Shared.CurrentInitialPosition, lastPosition + 2);
						this.Shared.PossibleDeleteInProgress = true;
						return;
					}
				}
			}
				
			if (this.Shared.PossibleDeleteInProgress) {
				if (cursorPosition < this.Shared.ContactPositionNearCursor + 2 && cursorPosition >= this.Shared.CurrentInitialPosition) {
					this.Shared.IgnoreSelectionChange = true;
					this.SetSelection (Shared.CurrentInitialPosition, this.Shared.ContactPositionNearCursor + 2);
				} else {
					// This case can be when the user selected another contact to delete.
					// Or the user is trying to delete the contact.
					// Since there's no way to differentiate the two, just keep track of the previous selections.
					// If there is a deletion, we'll do a delete with the previous positions, otherwise, the current positions will be updated properly.
					this.Shared.PreviousCurrentInitialPosition = this.Shared.CurrentInitialPosition;
					this.Shared.PreviousContactPositionNearCursor = this.Shared.ContactPositionNearCursor;

					for (int i = 0; i < this.Shared.LastContactPositions.Count; i++) {
						int lastPosition = this.Shared.LastContactPositions [i];
						if (cursorPosition < lastPosition+2) {
							this.Shared.ContactPositionNearCursor = lastPosition;
							this.Shared.CurrentInitialPosition = this.Shared.LastFirst [lastPosition];
							this.Shared.IgnoreSelectionChange = true;
							this.DidChangeSelectionWhilePossiblyDeleting = true;
							this.SetSelection (this.Shared.CurrentInitialPosition, lastPosition + 2);
							this.Shared.PossibleDeleteInProgress = true;
							return;
						}
					}
				}
			}
		}

		public override void SetSelection (int start, int stop) {
			base.SetSelection (start, stop);
		}

		#region attributing text

		private void UpdateTextAppearance () {

			if (this.Text.Length == 0)
				return;

			SpannableString prettyString = new SpannableString (this.Text);

			// Setting a color for each contact.
			if (this.Shared.LastContactPositions != null) {
				int length = this.Shared.LastContactPositions.Count;
				for (int i = 0; i < length; i++) {
					int end = this.Shared.LastContactPositions [i];
					int begin = this.Shared.LastFirst [end];
					prettyString.SetSpan (new ForegroundColorSpan (Color.Blue), begin, end, SpanTypes.ExclusiveExclusive);

				}
			}

			// Setting color if no results on query string.
			if (!fragment.HasResults ()) {
				int lastPositionInText = this.Shared.LastPositionInText ();
				int searchQueryIndex = 0;
				if (lastPositionInText != 0)
					searchQueryIndex = lastPositionInText + 2;
				prettyString.SetSpan (new ForegroundColorSpan (Color.Red), searchQueryIndex, prettyString.Length (), SpanTypes.ExclusiveExclusive);
			}

			this.DontListenOnContactTextChange = true;
			this.Shared.IgnoreSelectionChange = true;
			this.TextFormatted = prettyString;
			this.DontListenOnContactTextChange = false;

			this.Shared.IgnoreSelectionChange = true;
			this.SetSelection (this.Text.Length);
		}
		#endregion

		#region contact search edittext callbacks

		public void UpdateContactSearchTextView () {
			string contactString = fragment.GetDisplayLabelString ();
	
			this.Shared.IgnoreSelectionChange = true;
			this.DontListenOnContactTextChange = true;
			this.Text = contactString;
			this.DontListenOnContactTextChange = false;

			this.Shared.UpdateEndPoints ();
			ApplyContactSearchFilter ();

			if (this.SelectionStart != this.Text.Length)
				this.Shared.IgnoreSelectionChange = true;
			this.SetSelection (this.Text.Length);
		}

		public void ContactSearchBeforeTextChanged (object sender, TextChangedEventArgs e) {
			if (this.DontListenOnContactTextChange)
				return;

			this.ContactSearchTextBeforeChange = ((EditText)sender).Text;
		}

		public void ContactSearchAfterTextChanged (object sender, AfterTextChangedEventArgs e) {
			if (this.DontListenOnContactTextChange) {
				return;
			}

			EditText contactSearchText = ((EditText)sender);
			string curValue = contactSearchText.Text;

			// Case where user is attempting to remove the contact.
			if (this.Shared.PossibleDeleteInProgress) {
				if (this.DidChangeSelectionWhilePossiblyDeleting) {
					this.Shared.IndexToDelete = this.Shared.LastContactPositions.IndexOf (this.Shared.PreviousContactPositionNearCursor);
					this.DidChangeSelectionWhilePossiblyDeleting = false;
				} else {
					this.Shared.IndexToDelete = this.Shared.LastContactPositions.IndexOf (this.Shared.ContactPositionNearCursor);
				}
					
				fragment.RemoveContactAtIndex (this.Shared.IndexToDelete);
				UpdateContactSearchTextView ();

				this.Shared.PossibleDeleteInProgress = false;

				if (this.SelectionStart != this.Text.Length)
					this.Shared.IgnoreSelectionChange = true;
				this.SetSelection (this.Text.Length);

				// We do a final block on the selection change listener so that it doesn't try to reset a selection.
				this.Shared.IgnoreSelectionChange = true;
				return;
			}
				
			// Case where a backspace would backspace part of a contact's name.
			// In that scenario, highlight the entire name and set it so that the next backspace will remove the contact.
			int endOfText = this.Shared.LastPositionInText ();
			if (endOfText != 0 && curValue.Length != 0)
				endOfText = endOfText + 2;

			if (this.ContactSearchTextBeforeChange.Length > curValue.Length && curValue.Length < endOfText) {
				// We need to block the selection change listener everytime we change the text.
				this.DontListenOnContactTextChange = true;
				this.Shared.IgnoreSelectionChange = true;
				contactSearchText.Text = this.ContactSearchTextBeforeChange;
				this.DontListenOnContactTextChange = false;

				int lastPositionOfContactName = endOfText - 2; // "John Doe, ", we want the e, so subtract 2
				this.Shared.CurrentInitialPosition = this.Shared.LastFirst [lastPositionOfContactName];
				this.Shared.ContactPositionNearCursor = lastPositionOfContactName;

				this.Shared.IgnoreSelectionChange = true;
				contactSearchText.SetSelection (this.Shared.CurrentInitialPosition, this.Shared.ContactPositionNearCursor+2);
				this.Shared.PossibleDeleteInProgress = true;

				// We do a final block on the selection change listener so that it doesn't try to reset a selection.
				this.Shared.IgnoreSelectionChange = true;
				return;
			}

			// If it doesn't hit any other case, the user just entered text, so apply a filter to search for contacts.
			this.Shared.UpdateEndPoints ();
			ApplyContactSearchFilter ();
		}
			
		public void ApplyContactSearchFilter () {
			int length = this.Text.Length;
			int lastPositionInText = this.Shared.LastPositionInText ();
			int endSubstring = 0;

			if (lastPositionInText != 0 && length != 0)
				endSubstring = lastPositionInText + 2; // "John Doe, " => ", " == 2
		
			this.Shared.CurrentSearchFilter = this.Text.Substring (endSubstring, length-endSubstring);

			fragment.InvokeFilter (this.Shared.CurrentSearchFilter);

			if (!this.Shared.DebuggingMode) {
				this.Shared.PossibleShowHide ();
			}

			this.Shared.TimedSearchForContactsOnServer ();
		}

		// We need to implement both Click and FocusChanged.
		// Click handles the case where EditText already has focus.
		// FocusChange handles the case where the focus isn't there and the keyboard is coming up, which would skip the click callback.
		public void ContactSearchClicked (object sender, EventArgs e) {
			if (this.Shared.DebuggingMode) {
				fragment.ShowList (shouldShowMainList: false);
			}
		}

		public void ContactSearchFocusChanged (object sender, View.FocusChangeEventArgs e) {
			if (e.HasFocus) {
				if (this.Shared.DebuggingMode)
					this.Shared.ShowContactList ();
			} else {
				this.Shared.ShowMainList ();
			}
		}
		#endregion
	}

	public class DisabledCustomSelectionCallback:Java.Lang.Object, ActionMode.ICallback {
		public bool OnActionItemClicked (ActionMode mode, IMenuItem item) {
			return false;
		}

		public bool OnCreateActionMode (ActionMode mode, IMenu menu) {
			return false;
		}

		public void OnDestroyActionMode (ActionMode mode) {
			return;
		}

		public bool OnPrepareActionMode (ActionMode mode, IMenu menu) {
			return false;
		}
	}

	class AndroidSharedContactSearchDelegate : SharedContactSearchDelegate {
		readonly ContactSearchEditText editText;
		public AndroidSharedContactSearchDelegate (ContactSearchEditText edit_) {
			editText = edit_;
		}

		public override ApplicationModel AppModel {
			get { return EMApplication.GetInstance ().appModel; }
		}

		public override string CurrentText {
			get { return editText.Text; }
		}

		public override void RemoveContactAtIndex (int indexToDelete) {
			editText.Fragment.RemoveContactAtIndex (indexToDelete);
		}

		public override void ShowMainList () {
			editText.Fragment.ShowList (shouldShowMainList: true);
		}

		public override void ShowContactList () {
			editText.Fragment.ShowList (shouldShowMainList: false);
		}

		public override void ReloadContactsAfterSearch (IList<Contact> listOfContacts) {
			editText.Fragment.UpdateContactsAfterSearch (listOfContacts, this.CurrentSearchFilter);
		}
	}
}