using System;
using UIKit;
using CoreGraphics;
using Foundation;
using em;

namespace iOS {
	public class BurgerButton : UIButton {

		private int _unreadCount = 0;
		public int UnreadCount {
			get { return this._unreadCount; }
			set { 
				EMTask.DispatchMain (() => {
					this._unreadCount = value;
					if (this._unreadCount == 0) {
						this.NotificationNumber.Hidden = true;
					} else {
						this.NotificationNumber.Hidden = false;
						string unreadCountStr = NSNumberFormatter.LocalizedStringFromNumbernumberStyle (new NSNumber (this._unreadCount), NSNumberFormatterStyle.Decimal);
						NSString badgeString = new NSString (unreadCountStr);
						this.NotificationNumber.BadgeText = badgeString;
						this.NotificationNumber.SetNeedsDisplay ();
					}
				});
			}
		}

		private CustomBadge NotificationNumber { get; set; }
		private const int BurgerIconWidth = 30;
		private const int BurgerIconHeight = 25;

		public BurgerButton () : base (UIButtonType.Custom) {
			this.SetBackgroundImage (UIImage.FromFile ("iconMenu.png"), UIControlState.Normal);
			this.SetBackgroundImage (UIImage.FromFile ("iconMenu.png"), UIControlState.Highlighted);
			this.Frame = new CGRect (0, 0, BurgerIconWidth, BurgerIconHeight);

			this.NotificationNumber = CustomBadge.CustomBadgeWithStyle ("0", BadgeStyle.DefaultStyle);

			// We hide it in the beginning, once we've pulled an unread count number out, we can show it.
			this.NotificationNumber.Hidden = true;
			this.NotificationNumber.Frame = new CGRect (
				BurgerIconWidth - (this.NotificationNumber.Frame.Width / 2) - UI_CONSTANTS.TINY_MARGIN,
				0 - (this.NotificationNumber.Frame.Height / 2) + UI_CONSTANTS.TINY_MARGIN,
				this.NotificationNumber.Frame.Width,
				this.NotificationNumber.Frame.Height
			);

			this.Add (this.NotificationNumber);
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}
	}
}

