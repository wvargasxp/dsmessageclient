using UIKit;
using Foundation;
using CoreGraphics;
using System;
using System.Diagnostics;
using em;

namespace iOS {
	public class BasicBubbleTextView : UITextView {

		// This TapGestureRecognizer is for when the EditMenu is displaying on screen.
		// We detect if 'this' has been tapped and propagate the event by posting a notification.
		// The controller listening on that notification can then decide if it needs to dismiss the on screen UIMenuController.
		UITapGestureRecognizer TapGestureRecognizer { get; set; }

		private const string Selector = "textViewTouchedSelector";

		public BasicBubbleTextView () {
			this.TapGestureRecognizer = new UITapGestureRecognizer (this, new ObjCRuntime.Selector (Selector));
			this.TapGestureRecognizer.NumberOfTapsRequired = 1;
			this.AddGestureRecognizer (this.TapGestureRecognizer);
		}
			
		[Export (Selector)]
		public void Tapped () {
			UITableViewCell cell = this.SuperCell;
			if (cell != null) {
				if (cell.IsFirstResponder) {
					cell.ResignFirstResponder ();
					UIMenuController.SharedMenuController.SetMenuVisible (false, true);
				}
			}
		}

		// Don't allow this TextView to become a Responder, otherwise functionality will break.
		// TextView as first responder handles edit menu actions differently.
		public override bool CanBecomeFirstResponder {
			get {
				return false;
			}
		}

		// Keep track of Pressed state so we can see if we should cancel the long press action.
        private bool _pressed = false;
        private bool Pressed { get { return this._pressed; } set { this._pressed = value; } }
		const string LONG_PRESS_SELECTOR = "longPressSelector";

		[Export (LONG_PRESS_SELECTOR)]
		private void LongPressedEvent () {
			NSNotificationCenter.DefaultCenter.PostNotificationName (iOS_Constants.NOTIFICATION_CHAT_TEXTVIEW_TAPPED, this.SuperCell);
		}

		#region touch callbacks
		public override void TouchesBegan (NSSet touches, UIEvent evt) {
			base.TouchesBegan (touches, evt);
			this.Pressed = false;
			this.PerformSelector (new ObjCRuntime.Selector(LONG_PRESS_SELECTOR), null, .35);
		}

		public override void TouchesMoved (NSSet touches, UIEvent evt) {
			base.TouchesMoved (touches, evt);
		}

		public override void TouchesEnded (NSSet touches, UIEvent evt) {
			if (this.Pressed) {
				base.TouchesEnded (touches, evt);
			} else {
				base.TouchesEnded (touches, evt);
				NSObject.CancelPreviousPerformRequest (this);
			}
		}

		public override void TouchesCancelled (NSSet touches, UIEvent evt) {
			base.TouchesCancelled (touches, evt);
		}
		#endregion

		// We need access to the TableViewCell containing this TextView because we'll need to post it as the related object in our notification.
		// Keep a weak reference to the cell so as to not leak memory.
		// The below code is simply just checking the current object's superviews until it finds the UITableViewCell.
		WeakReference superCellRef;
		protected UITableViewCell SuperCell {
			get {
				UITableViewCell superCell = superCellRef != null ? superCellRef.Target as UITableViewCell : null;

				if (superCell == null) {
					UIView obj = this;

					while (obj != null) {
						obj = obj.Superview;

						Type objType = obj.GetType ();
						Type tableViewCellType = typeof(UITableViewCell);
						if (objType.IsSubclassOf (tableViewCellType) || objType == tableViewCellType) {
							break;
						}
					}

					if (obj != null) {
						superCell = (UITableViewCell)obj;
						superCellRef = new WeakReference (superCell);
					}
				}

				return superCell;
			}
		}
	}
}
