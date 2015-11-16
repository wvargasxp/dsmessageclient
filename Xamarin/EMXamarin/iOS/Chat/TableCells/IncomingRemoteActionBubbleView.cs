using CoreGraphics;
using em;
using String_UIKit_Extension;
using UIDevice_Extension;
using UIKit;

namespace iOS {
	public class IncomingRemoteActionBubbleView : BasicBubbleView {

		public UIImageView MediaButtonOverlay {get; set;}
		public EMButton RemoteActionButton { get; set; }
		public UIProgressView ProgressView { get; set; }
		public Message Message { get; set; }

		private Contact Contact { get; set; }

		static UIFont NameFont = FontHelper.DefaultBoldFontWithSize (UIFont.SystemFontSize - 1);

		public void SetFromContact(Contact fromContact) {
			fromContact.colorTheme.GetRoundedRectangleResource ((UIImage stretchableBubble) => {
				CGSize size = stretchableBubble.Size;
				stretchableBubble = stretchableBubble.StretchableImage ((int) (size.Width / 2), (int) (size.Height / 2 ));
				this.BubbleBackground.Image = stretchableBubble;
			});

			this.RemoteActionButton.ThemeButton (fromContact.colorTheme.GetColor ());
		}

		public IncomingRemoteActionBubbleView () : base () {
			this.RemoteActionButton = new EMButton (UIButtonType.Custom);
			this.RemoteActionButton.Tag = 0x1C;

			AddSubview (this.RemoteActionButton);

			this.ProgressView = new UIProgressView (UIProgressViewStyle.Bar);
			this.ProgressView.Tag = 0x1D;
			AddSubview (this.ProgressView);

			this.MediaButtonOverlay = new UIImageView ();
			this.MediaButtonOverlay.Tag = 0x1A;
			AddSubview (this.MediaButtonOverlay);
		}
			
		public static void SizeOfWithMessage (string fromString, string message, ref CGSize containingSize) {
			CGRect d1 = new CGRect (), d2 = new CGRect ();
			var messageRect = new CGRect ();
			var progressRect = new CGRect ();
			var recordRect = new CGRect ();
			SizeOfWithMessage (fromString, message, ref d1, ref d2, ref recordRect, ref progressRect, ref containingSize, ref messageRect);
		}

		public static void SizeOfWithMessage (
			string fromString, 
			string message,
			float heightToWidth, 
			ref CGRect nameRect, 
			ref CGRect mediaRect, 
			ref CGRect recordRect, 
			ref CGRect progressRect,
			ref CGRect messageRect) {

			var d1 = new CGSize ();
			SizeOfWithMessage (fromString, message, ref nameRect, ref mediaRect, ref recordRect, ref progressRect, ref d1, ref messageRect);
		}

		public static void SizeOfWithMessage (
			string fromString, 
			string message,
			ref CGRect nameRect, 
			ref CGRect mediaRect, 
			ref CGRect recordRect, 
			ref CGRect progressRect, 
			ref CGSize containingSize,
			ref CGRect messageRect) {

			var constrainedTo = new CGSize (200, 9999);

			/* Support RTL */
			CGSize name = fromString.SizeOfTextWithFontAndLineBreakMode (NameFont, constrainedTo, UILineBreakMode.Clip);
			nameRect.Location = new CGPoint (UIDevice.CurrentDevice.IsRightLeftLanguage () ? constrainedTo.Width - name.Width + 25 : 45, 5);
			nameRect.Size = new CGSize ((float)((int)(name.Width + 0.5)), (float)((int)(name.Height + 0.5)));

			/* Support RTL */
			CGSize messageSize = message.SizeOfTextWithFontAndLineBreakMode(MessageFont, constrainedTo, UILineBreakMode.WordWrap);
			messageRect.Location = UIDevice.CurrentDevice.IsRightLeftLanguage () ? new CGPoint (constrainedTo.Width - messageSize.Width + 25, nameRect.Location.Y + nameRect.Size.Height + 3) : new CGPoint (45, nameRect.Location.Y + nameRect.Size.Height + 3);
			messageRect.Size = new CGSize ((float)((int)(messageSize.Width + 1.5)), (float)((int)(messageSize.Height + 1.5)));

			var mediaSize = new CGSize (190, 35);

			mediaRect.Location = new CGPoint (UIDevice.CurrentDevice.IsRightLeftLanguage () ? constrainedTo.Width - mediaSize.Width + 25 : 45, messageRect.Location.Y + messageRect.Size.Height + 10);
			mediaRect.Size = mediaSize;

			recordRect.Location = new CGPoint (mediaRect.Location.X, mediaRect.Location.Y);
			recordRect.Size = new CGSize (iOS_Constants.RECORDING_BUTTON_BUBBLE_SIZE, iOS_Constants.RECORDING_BUTTON_BUBBLE_SIZE);

			progressRect.Location = new CGPoint (mediaRect.Location.X, mediaRect.Location.Y + (mediaRect.Size.Height - 8));
			progressRect.Size = new CGSize (mediaSize.Width, -1); // height not included, we just use the bars height

			containingSize.Width = constrainedTo.Width + 50;
			containingSize.Height = messageRect.Location.Y + messageRect.Height + mediaRect.Height + 25 ;
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			this.BubbleBackground.Frame = Bounds;

			CGRect nameRect = new CGRect (), mediaRect = new CGRect (), recordRect = new CGRect (), progressRect = new CGRect ();
			CGRect messageRect = new CGRect ();
			SizeOfWithMessage (this.Message.FromString(), this.Message.message, this.Message.heightToWidth, ref nameRect, ref mediaRect, ref recordRect, ref progressRect, ref messageRect);

			this.FromLabel.Frame = nameRect;
			this.RemoteActionButton.Frame = mediaRect;

			this.MediaButtonOverlay.Frame = recordRect;
			this.MediaButtonOverlay.ContentMode = UIViewContentMode.ScaleAspectFit;

			this.MessageTextView.Frame = messageRect;

			// we don't overwrite the progress bars height
			this.ProgressView.Frame = new CGRect (progressRect.Location, new CGSize (progressRect.Size.Width, ProgressView.Frame.Size.Height));
		}
	}
}

