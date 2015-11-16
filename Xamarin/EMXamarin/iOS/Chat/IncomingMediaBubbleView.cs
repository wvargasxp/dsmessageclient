using System;
using CoreGraphics;
using em;
using UIDevice_Extension;
using UIKit;

namespace iOS {
	using String_UIKit_Extension;

	public class IncomingMediaBubbleView : UIView {
		
		public UILabel fromLabel { get; set; }
		public UIImageView mediaButtonOverlay {get; set;}
		public UIButton mediaButton { get; set; }
		public UIProgressView progressView { get; set; }
		public Message message { get; set; }
		readonly UIImageView bubbleBackground;

		private UIImageView AkaMaskIconImageView { get; set; }

		static UIFont NameFont = FontHelper.DefaultBoldFontWithSize (UIFont.SystemFontSize - 4);

		public IncomingMediaBubbleView () {
			bubbleBackground = new UIImageView (Bounds);
			bubbleBackground.Tag = 0x1A;
			AddSubview (bubbleBackground);

			fromLabel = new UILabel ();
			fromLabel.Tag = 0x1B;
			fromLabel.Font = NameFont;
			fromLabel.LineBreakMode = UILineBreakMode.Clip;
			fromLabel.TextAlignment = UIDevice.CurrentDevice.IsRightLeftLanguage () ? UITextAlignment.Right : UITextAlignment.Left;
			AddSubview (fromLabel);

			mediaButton = new UIButton (UIButtonType.Custom);
			mediaButton.Tag = 0x1C;
			AddSubview (mediaButton);

			progressView = new UIProgressView (UIProgressViewStyle.Bar);
			progressView.Tag = 0x1D;
			AddSubview (progressView);

			mediaButtonOverlay = new UIImageView ();
			mediaButtonOverlay.Tag = 0x1A;
			AddSubview (mediaButtonOverlay);

			this.AkaMaskIconImageView = new UIImageView ();
			this.AkaMaskIconImageView.Image = UIImage.FromFile ("sidemenu/iconAlias2.png");
			this.AddSubview (this.AkaMaskIconImageView);
		}

		public void SetFromContact(Contact fromContact) {
			fromContact.colorTheme.GetRoundedRectangleResource ((UIImage stretchableBubble) => {
				CGSize size = stretchableBubble.Size;
				stretchableBubble = stretchableBubble.StretchableImage ((int) (size.Width / 2), (int) (size.Height / 2 ));
				bubbleBackground.Image = stretchableBubble;
			});
		}

		public static void SizeOfWithMessage(string fromString, float heightToWidth, ref CGSize containingSize) {
			CGRect d1 = new CGRect (), d2 = new CGRect (), d3 = new CGRect (), d4 = new CGRect ();
			SizeOfWithMessage (fromString, heightToWidth, ref d1, ref d2, ref d3, ref d4, ref containingSize);
		}

		public static void SizeOfWithMessage(string fromString, float heightToWidth, ref CGRect nameRect, ref CGRect mediaRect, ref CGRect recordRect, ref CGRect progressRect) {
			var d1 = new CGSize ();
			SizeOfWithMessage (fromString, heightToWidth, ref nameRect, ref mediaRect, ref recordRect, ref progressRect, ref d1);
		}

		public static void SizeOfWithMessage(string fromString, float heightToWidth, ref CGRect nameRect, ref CGRect mediaRect, ref CGRect recordRect, ref CGRect progressRect, ref CGSize containingSize) {
			var constrainedTo = new CGSize (200, 9999);

			/* Support RTL */
			CGSize name = fromString.SizeOfTextWithFontAndLineBreakMode (NameFont, constrainedTo, UILineBreakMode.Clip);
			nameRect.Location = new CGPoint (UIDevice.CurrentDevice.IsRightLeftLanguage () ? constrainedTo.Width - name.Width + 25 : 45, 5);
			nameRect.Size = new CGSize ((float)((int)(name.Width + 0.5)), (float)((int)(name.Height + 0.5)));

			heightToWidth = Math.Abs (heightToWidth);
			CGSize mediaSize = MediaSizeHelper.SharedInstance.ThumbnailSizeForHeightToWidth (heightToWidth);

			mediaRect.Location = new CGPoint (UIDevice.CurrentDevice.IsRightLeftLanguage () ? constrainedTo.Width - mediaSize.Width + 25 : 45, nameRect.Location.Y + nameRect.Size.Height + 3);
			mediaRect.Size = mediaSize;

			recordRect.Location = new CGPoint (mediaRect.Location.X, mediaRect.Location.Y);
			recordRect.Size = new CGSize (iOS_Constants.RECORDING_BUTTON_BUBBLE_SIZE, iOS_Constants.RECORDING_BUTTON_BUBBLE_SIZE);

			progressRect.Location = new CGPoint (mediaRect.Location.X, mediaRect.Location.Y + (mediaRect.Size.Height - 8));
			progressRect.Size = new CGSize (mediaSize.Width, -1); // height not included, we just use the bars height

			containingSize.Width = constrainedTo.Width + 50;
			containingSize.Height = mediaRect.Location.Y + mediaRect.Height + 10;
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			bubbleBackground.Frame = Bounds;

			CGRect nameRect = new CGRect (), mediaRect = new CGRect (), recordRect = new CGRect (), progressRect = new CGRect ();
			SizeOfWithMessage (message.FromString(), message.heightToWidth, ref nameRect, ref mediaRect, ref recordRect, ref progressRect);

			fromLabel.Frame = nameRect;
			mediaButton.Frame = mediaRect;

			mediaButtonOverlay.Frame = recordRect;
			mediaButtonOverlay.ContentMode = UIViewContentMode.ScaleAspectFit;

			// we don't overwrite the progress bars height
			progressView.Frame = new CGRect (progressRect.Location, new CGSize (progressRect.Size.Width, progressView.Frame.Size.Height));

			this.AkaMaskIconImageView.Frame = new CGRect (
				this.fromLabel.Frame.X + this.fromLabel.Frame.Width + UI_CONSTANTS.STINY_MARGIN, 
				this.fromLabel.Frame.Y + UI_CONSTANTS.STINY_MARGIN, 
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