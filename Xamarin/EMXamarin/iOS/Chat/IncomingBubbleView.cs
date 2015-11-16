using CoreGraphics;
using em;
using UIDevice_Extension;
using UIKit;

namespace iOS {
	using String_UIKit_Extension;
	public class IncomingBubbleView : BasicBubbleView {

		private UIImageView AkaMaskIconImageView { get; set; }

		public IncomingBubbleView () {
			this.AkaMaskIconImageView = new UIImageView ();
			this.AddSubview (this.AkaMaskIconImageView);
		}

		public void SetFromContact(Contact fromContact) {
			if (fromContact.IsAKA) {
				fromContact.colorTheme.GetAKAMaskResource ((UIImage akaMask) => {
					this.AkaMaskIconImageView.Image = akaMask;	
				});
			}
			fromContact.colorTheme.GetRoundedRectangleResource ((UIImage stretchableBubble) => {
				if (this.BubbleBackground != null) {
					CGSize size = stretchableBubble.Size;
					stretchableBubble = stretchableBubble.StretchableImage ((int) (size.Width / 2), (int) (size.Height / 2 ));
					this.BubbleBackground.Image = stretchableBubble;
				}
			});
		}

		public static void SizeOfWithMessage(string fromString, string message, ref CGSize containingSize) {
			CGRect d1 = new CGRect (), d2 = new CGRect ();
			SizeOfWithMessage (fromString, message, ref d1, ref d2, ref containingSize);
		}

		public static void SizeOfWithMessage(string fromString, string message, ref CGRect nameRect, ref CGRect messageRect) {
			var d1 = new CGSize ();
			SizeOfWithMessage (fromString, message, ref nameRect, ref messageRect, ref d1);
		}

		public static void SizeOfWithMessage(string fromString, string message, ref CGRect nameRect, ref CGRect messageRect, ref CGSize containingSize) {
			var constrainedTo = new CGSize (200, 9999);

			/* Support RTL */
			CGSize name = fromString.SizeOfTextWithFontAndLineBreakMode (NameFont, constrainedTo, UILineBreakMode.Clip);
			nameRect.Location = UIDevice.CurrentDevice.IsRightLeftLanguage () ? new CGPoint (constrainedTo.Width - name.Width + 25, 5) : new CGPoint (45, 5);
			nameRect.Size = new CGSize ((float)((int)(name.Width + 0.5)), (float)((int)(name.Height + 0.5)));

			UIFont messageFont = MessageFont;
			if (Message.MatchOnlyEmoji (message)) {
				messageFont = EnlargedMessageFont;
			}

			/* Support RTL */
			CGSize messageSize = message.SizeOfTextWithFontAndLineBreakMode(messageFont, constrainedTo, UILineBreakMode.WordWrap);
			messageRect.Location = UIDevice.CurrentDevice.IsRightLeftLanguage () ? new CGPoint (constrainedTo.Width - messageSize.Width + 25, nameRect.Location.Y + nameRect.Size.Height + 3) : new CGPoint (45, nameRect.Location.Y + nameRect.Size.Height + 3);
			messageRect.Size = new CGSize ((float)((int)(messageSize.Width + 1.5)), (float)((int)(messageSize.Height + 1.5)));

			containingSize.Width = constrainedTo.Width + 50;
			containingSize.Height = messageRect.Location.Y + messageRect.Height + 5;
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();
			this.BubbleBackground.Frame = Bounds;

			CGRect nameRect = new CGRect (), messageRect = new CGRect ();
			SizeOfWithMessage (FromLabel.Text, this.MessageTextView.Text, ref nameRect, ref messageRect);

			this.FromLabel.Frame = nameRect;
			this.MessageTextView.Frame = messageRect;

			this.AkaMaskIconImageView.Frame = new CGRect (
				this.FromLabel.Frame.X + this.FromLabel.Frame.Width + UI_CONSTANTS.STINY_MARGIN, 
				this.FromLabel.Frame.Y + UI_CONSTANTS.STINY_MARGIN, 
				iOS_Constants.AKA_MASK_ICON_WIDTH, 
				iOS_Constants.AKA_MASK_ICON_HEIGHT);
		}

		public void ShowHideAliasMaskIcon (Contact contact) {
			if (contact.IsAKA) {
				this.AkaMaskIconImageView.Hidden = false;
			} else {
				this.AkaMaskIconImageView.Hidden = true;
			}
		}
	}
}