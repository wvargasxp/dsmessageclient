using System;
using System.Collections.Generic;
using em;
using Foundation;
using UIKit;
using CoreGraphics;

namespace iOS {
	public class ContactSearchTextViewDelegate : UITextViewDelegate {

		private SharedContactSearchDelegate Shared { get; set; }

		private WeakReference controllerRef;
		public IContactSearchController Controller {
			get { return controllerRef.Target as IContactSearchController; }
			private set {
				controllerRef = new WeakReference (value);
			}
		}

		private WeakReference mainTextViewRef; 
		public UITextView MainTextView {
			get { return mainTextViewRef.Target as UITextView; }
			private set {
				mainTextViewRef = new WeakReference (value);
			}
		}

		private bool deleteContactAfterTextChanged = false;
		protected bool DeleteContactAfterTextChanged {
			get { return deleteContactAfterTextChanged; }
			set { deleteContactAfterTextChanged = value; }
		}
			
		private string _replacementString = "";
		protected string ReplacementString {
			get { return _replacementString; }
			set { _replacementString = value; }
		}

		private const int PlaceHolderLabelTag = 123123123;

		public ContactSearchTextViewDelegate (UITextView textView, IContactSearchController _controller) {
			this.Controller = _controller;
			this.MainTextView = textView;
			this.Shared = new IOSSharedContactSearchDelegate (this);

			UILabel placeHolderLabel = new UILabel (new CGRect (10, textView.Frame.Height / 2 - (34 / 2), textView.Frame.Width - 10, 34));
			placeHolderLabel.Text = "SEARCH_TITLE".t ();
			placeHolderLabel.TextColor = UIColor.LightGray;
			placeHolderLabel.Tag = PlaceHolderLabelTag;
			textView.Add (placeHolderLabel);
		}

		#region editing callbacks
		public override void EditingEnded (UITextView textView) {
			UpdatePlaceHolderVisibility (textView);
		}

		public override void EditingStarted (UITextView textView) {
			if (this.Shared.DebuggingMode)
				this.Shared.ShowContactList ();
		}

		public override bool ShouldBeginEditing (UITextView textView) {
			return true;
		}

		public override bool ShouldEndEditing (UITextView textView) {
			return true;
		}
		#endregion

		private void UpdatePlaceHolderVisibility (UITextView textView) {
			UILabel placeHolderLabel = textView.ViewWithTag (PlaceHolderLabelTag) as UILabel;
			if (placeHolderLabel == null) return;
			MatchPlaceHolderLabelWithExclusionPath (textView);

			if (!textView.HasText) {
				placeHolderLabel.Hidden = false;
			} else {
				placeHolderLabel.Hidden = true;
			}
		}

		public void MatchPlaceHolderLabelWithExclusionPath (UITextView textView) {
			UILabel placeHolderLabel = textView.ViewWithTag (PlaceHolderLabelTag) as UILabel;
			if (placeHolderLabel == null) return;

			UITextRange range = textView.SelectedTextRange;
			if (range != null) {
				UIBezierPath[] paths = textView.TextContainer.ExclusionPaths;

				int length = paths != null ? paths.Length : 0;
				int leftOffset = 0;
				for (int i = 0; i < length; i++) {
					UIBezierPath p = paths [i];

					nfloat offset = (nfloat)p.Bounds.X + p.Bounds.Width;
					// Find the left offset from exclusion path.
					if (offset < leftOffset || leftOffset == 0) {
						leftOffset = (int)offset;
					}
				}

				leftOffset += 7; // The caret is right over the placeholder text, lets offset it a little more.

				placeHolderLabel.Frame = new CGRect (leftOffset, placeHolderLabel.Frame.Y, placeHolderLabel.Frame.Width, placeHolderLabel.Frame.Height);
			}
		}

		public override void SelectionChanged (UITextView textView) {

			if (this.Shared.IgnoreSelectionChange) {
				this.Shared.IgnoreSelectionChange = false;
				return;
			}

			nint cursorPosition = textView.SelectedRange.Location;
			if (this.Shared.LastContactPositions != null) {
				for (int i = 0; i < this.Shared.LastContactPositions.Count; i++) {
					int lastPosition = this.Shared.LastContactPositions [i];
					if (cursorPosition < lastPosition+2) {
						this.Shared.ContactPositionNearCursor = lastPosition;
						NSRange range = new NSRange ();
						range.Location = this.Shared.LastFirst [lastPosition];
						range.Length = lastPosition + 2 - this.Shared.LastFirst [lastPosition];
						this.Shared.IgnoreSelectionChange = true;
						textView.SelectedRange = range;
						this.Shared.PossibleDeleteInProgress = true;
						return;
					}
				}
			}
		}

		public override bool ShouldChangeText (UITextView textView, NSRange range, string replacementString) {
			if (textView.SelectedRange.Length > 1) {
				// trying to save the query string to append after the textview has been programatically changed
				if (textView.HasText) {
					int queryIndex = textView.Text.LastIndexOf (',') + 2;
					if (textView.Text.Length > queryIndex)
						this.Shared.CurrentSearchFilter = textView.Text.Substring (queryIndex);
					else
						this.Shared.CurrentSearchFilter = "";
				} else {
					this.Shared.CurrentSearchFilter = "";
				}

				// case where user has initiated a possible delete, and then replaced the text with something else
				if (this.Shared.PossibleDeleteInProgress) {
					this.DeleteContactAfterTextChanged = true;
					this.Shared.IndexToDelete = this.Shared.LastContactPositions.IndexOf (this.Shared.ContactPositionNearCursor);
					// keep a copy of the replacement string to append to the current query
					this.ReplacementString = replacementString;
					return true;
				}
			}

			int lastPositionInText = this.Shared.LastPositionInText ();
			if (lastPositionInText != 0) {
				if (range.Location == lastPositionInText + 1) {
					// position indicates user is initiating a possible delete of the contact
					NSRange rr = TextRangeOfLastContactInList (textView.Text);
					this.Shared.IgnoreSelectionChange = true;
					textView.SelectedRange = rr;
					this.Shared.PossibleDeleteInProgress = true;

					for (int i = 0; i < this.Shared.LastContactPositions.Count; i++) {
						int lastPosition = this.Shared.LastContactPositions [i];
						if (range.Location < lastPosition+2) {
							this.Shared.ContactPositionNearCursor = lastPosition;
							return false;
						}
					}

					return false;
				}
			}

			this.Shared.PossibleDeleteInProgress = false;
			return true;
		}

		private NSRange TextRangeOfLastContactInList (string inputString) {
			// ex. "John Smith, John D, "
			// We want the string range of "John D, ".
			int length = inputString.Length;
			int indexOfSecondCommaBackwards = -1;
			char[] inputArray = inputString.ToCharArray ();

			int foundCommaCounter = 0;
			for (int i = length - 1; i >= 0; i--) {
				if (inputArray [i].Equals (',')) {
					indexOfSecondCommaBackwards = i;
					foundCommaCounter++;
				}

				if (foundCommaCounter == 2)
					break;
			}

			NSRange range = new NSRange ();
			if (foundCommaCounter == 1) {
				// beginning of string since we only found one comma
				// ex. "John Smith, "
				indexOfSecondCommaBackwards = 0;
				range.Length = inputString.Length - indexOfSecondCommaBackwards;
				range.Location = indexOfSecondCommaBackwards;

			} else {
				// add 2 to skip the first ", "
				// ex. "John Smith, Abigail Wilson, "
				range.Length = inputString.Length - indexOfSecondCommaBackwards + 2;
				range.Location = indexOfSecondCommaBackwards + 2;
			}
				
			return range;
		}

		public override void Changed (UITextView textView) {
			IContactSearchController controller = this.Controller;

			UpdatePlaceHolderVisibility (textView);

			if (controller != null && this.DeleteContactAfterTextChanged) {
				controller.RemoveContactAtIndex (this.Shared.IndexToDelete);

				if (textView.SelectedRange.Location != textView.Text.Length || textView.SelectedRange.Length != 0)
					this.Shared.IgnoreSelectionChange = true;
				NSRange range = new NSRange ();
				range.Location = textView.Text.Length;
				range.Length = 0;
				textView.SelectedRange = range;

				UpdateContactSearchTextView ();

				if (this.ReplacementString.Length != 0) {
					if (this.Shared.CurrentSearchFilter.Length != 0)
						this.ReplacementString = this.ReplacementString.ToLower ();
					this.Shared.IgnoreSelectionChange = true;
					textView.Text = textView.Text + this.Shared.CurrentSearchFilter + this.ReplacementString;
				}

				this.Shared.UpdateEndPoints ();
				UpdateTextAppearance ();

				this.Shared.PossibleDeleteInProgress = false;
				this.DeleteContactAfterTextChanged = false;
			}

			ApplyContactSearchFilter ();

			UpdateTextAppearance ();
		}
			
		private void ApplyContactSearchFilter () {
			IContactSearchController controller = this.Controller;
			UITextView mainTextView = this.MainTextView;
			if (controller == null || mainTextView == null) return;

			int lastPositionInText = this.Shared.LastPositionInText ();

			if (lastPositionInText != 0) {
				this.Shared.CurrentSearchFilter = mainTextView.Text.Substring (lastPositionInText + 2);
			} else {
				this.Shared.CurrentSearchFilter = mainTextView.Text.Substring (0);
			}

			controller.InvokeFilter (this.Shared.CurrentSearchFilter);
			this.Shared.TimedSearchForContactsOnServer ();

			if (!this.Shared.DebuggingMode) {
				this.Shared.PossibleShowHide ();
			}
		}

		public void UpdateContactSearchTextView () {
			IContactSearchController controller = this.Controller;
			UITextView mainTextView = this.MainTextView;
			if (controller != null && mainTextView != null) {
				if (mainTextView.SelectedRange.Location != mainTextView.Text.Length || mainTextView.SelectedRange.Length != 0) {
					this.Shared.IgnoreSelectionChange = true;
				}

				string displayString = controller.GetDisplayLabelString ();
				mainTextView.Text = displayString;

				this.Shared.UpdateEndPoints ();

				UpdateTextAppearance ();
				ApplyContactSearchFilter ();

				mainTextView.ScrollRangeToVisible (new NSRange (mainTextView.Text.Length, 0));
				UpdatePlaceHolderVisibility (mainTextView);
			}
		}

		protected override void Dispose (bool disposing) {
			this.Shared = null;
			base.Dispose (disposing);
		}

		#region attributing text

		private void UpdateTextAppearance () {
			IContactSearchController controller = this.Controller;
			UITextView mainTextView = this.MainTextView;
			if (controller == null || mainTextView == null) return;

			var prettyString = new NSMutableAttributedString (mainTextView.Text);

			var defaultAttributes = new UIStringAttributes {
				ForegroundColor = UIColor.Black,
			};

			var isContactAttributes = new UIStringAttributes {
				ForegroundColor = UIColor.Blue,
			};

			var badQueryAttributes = new UIStringAttributes {
				ForegroundColor = UIColor.Red,
			};

			prettyString.SetAttributes (defaultAttributes, new NSRange (0, mainTextView.Text.Length));
			if (this.Shared.LastContactPositions != null) {
				int length = this.Shared.LastContactPositions.Count;
				for (int i = 0; i < length; i++) {
					int end = this.Shared.LastContactPositions [i];
					int begin = this.Shared.LastFirst [end];
					prettyString.SetAttributes (isContactAttributes, new NSRange (begin, end - begin));
				}
			}

			if (!controller.HasResults ()) {
				int lastPositionInText = this.Shared.LastPositionInText ();
				int searchQueryIndex = 0;
				if (lastPositionInText != 0)
					searchQueryIndex = lastPositionInText + 2;
				prettyString.SetAttributes (badQueryAttributes, new NSRange (searchQueryIndex, this.MainTextView.Text.Length - searchQueryIndex));
			}

			mainTextView.AttributedText = prettyString;
			mainTextView.Font = FontHelper.DefaultFontForTextFields ();

		}
		#endregion

		class IOSSharedContactSearchDelegate : SharedContactSearchDelegate {
			private WeakReference contactSearchTextViewDelegateRef;
			private ContactSearchTextViewDelegate ContactSearchTextViewDelegate {
				get { return contactSearchTextViewDelegateRef.Target as ContactSearchTextViewDelegate; }
				set { 
					contactSearchTextViewDelegateRef = new WeakReference (value);
				}
			}

			public IOSSharedContactSearchDelegate (ContactSearchTextViewDelegate parent) {
				this.ContactSearchTextViewDelegate = parent;
			}

			public override ApplicationModel AppModel {
				get {
					return (UIApplication.SharedApplication.Delegate as AppDelegate).applicationModel;
				}
			}

			public override string CurrentText {
				get {
					ContactSearchTextViewDelegate editText = this.ContactSearchTextViewDelegate;
					UITextView mainTextView = editText.MainTextView;
					if (editText != null && mainTextView != null)
						return mainTextView.Text;

					return ""; // Should not be happening.
				}
			}

			public override void RemoveContactAtIndex (int indexToDelete) {
				ContactSearchTextViewDelegate editText = this.ContactSearchTextViewDelegate;
				if (editText == null) return;
				editText.Controller.RemoveContactAtIndex (indexToDelete);
			}

			public override void ShowMainList () {
				ContactSearchTextViewDelegate editText = this.ContactSearchTextViewDelegate;
				if (editText == null) return;
				editText.Controller.ShowList (shouldShowMainList: true);
			}

			public override void ShowContactList () {
				ContactSearchTextViewDelegate editText = this.ContactSearchTextViewDelegate;
				if (editText == null) return;
				editText.Controller.ShowList (shouldShowMainList: false);
			}

			public override void ReloadContactsAfterSearch (IList<Contact> listOfContacts) {
				ContactSearchTextViewDelegate editText = this.ContactSearchTextViewDelegate;
				if (editText == null) return;
				editText.Controller.UpdateContactsAfterSearch (listOfContacts, CurrentSearchFilter);
			}
		}
	}
}