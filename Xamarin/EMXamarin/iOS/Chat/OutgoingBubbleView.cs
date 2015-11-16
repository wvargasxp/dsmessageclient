using System;
using System.Diagnostics;
using CoreGraphics;
using em;
using EMXamarin;
using UIDevice_Extension;
using UIKit;

namespace iOS {
	using String_UIKit_Extension;

	public class OutgoingBubbleView : BasicBubbleView {
		public Message Message { get; set; }
		public UIImageView mediaButtonOverlay {get; set;}
		public UIButton ImageButton { get; set; }
		public UIProgressView ProgressView { get; set; }
		public MessageStatusView MessageStatusView { get; set; }

		public OutgoingBubbleView () {
			this.MessageStatusView = new MessageStatusView ();
			this.MessageStatusView.Tag = 0x1C;
			this.Add (this.MessageStatusView);

			this.ImageButton = new UIButton (UIButtonType.Custom);
			this.ImageButton.Tag = 0x1E;
			this.Add (this.ImageButton);

			this.ProgressView = new UIProgressView (UIProgressViewStyle.Bar);
			this.ProgressView.Tag = 0x1F;
			this.Add (this.ProgressView);

			this.mediaButtonOverlay = new UIImageView ();
			this.mediaButtonOverlay.Tag = 0x2C;
			this.Add (mediaButtonOverlay);
		}

		public void CreateStretchableImage (BackgroundColor c) {
			c.GetRoundedRectangleResource ((UIImage stretchableBubble) => {
				CGSize size = stretchableBubble.Size;
				stretchableBubble = stretchableBubble.StretchableImage ((int) (size.Width / 2), (int) (size.Height / 2 ));
				Debug.Assert (BubbleBackground != null, "**OutgoingBubbleView: CreateStretchableImage: bubblebackground is null");
				this.BubbleBackground.Image = stretchableBubble;
			});
		}

		public static void SizeOfWithMessage(Message message, ref CGSize containingSize) {
			CGRect d1 = new CGRect (), d2 = new CGRect (), d3 = new CGRect (), d4 = new CGRect (), d5 = new CGRect (), d6 = new CGRect ();
			SizeOfWithMessage (message, ref d1, ref d2, ref d3, ref d4, ref d5, ref d6, ref containingSize);
		}

		public static void SizeOfWithMessage(Message message, ref CGRect nameRect, ref CGRect messageRect, ref CGRect mediaRect, ref CGRect recordingRect, ref CGRect progressRect, ref CGRect statusRect) {
			var d1 = new CGSize ();
			SizeOfWithMessage (message, ref nameRect, ref messageRect, ref mediaRect, ref recordingRect, ref progressRect, ref statusRect, ref d1);
		}

		public static void SizeOfWithMessage(Message message, ref CGRect nameRect, ref CGRect messageRect, ref CGRect mediaRect, ref CGRect recordingRect, ref CGRect progressRect, ref CGRect statusRect, ref CGSize containingSize) {
			var constrainedTo = new CGSize (250, 9999);

			/* Support RTL */
			string fromString = message.FromString ();
			CGSize name = fromString.SizeOfTextWithFontAndLineBreakMode (NameFont, constrainedTo, UILineBreakMode.Clip);
			nameRect.Location = UIDevice.CurrentDevice.IsRightLeftLanguage () ? new CGPoint (constrainedTo.Width - name.Width - 10, 5) : new CGPoint (10, 5);
			nameRect.Size = new CGSize ((float)((int)(name.Width + 0.5)), (float)((int)(name.Height + 0.5)));

			/* Support RTL */
			statusRect.Location = new CGPoint (UIDevice.CurrentDevice.IsRightLeftLanguage () ? nameRect.Location.X - nameRect.Size.Width - 2 : nameRect.Location.X + nameRect.Size.Width + 2, ((nameRect.Location.Y + nameRect.Size.Height) / 2) - 4);
			statusRect.Size = new CGSize (26, 12);

			if (message.HasMedia ()) {
				messageRect.Location = new CGPoint (0, 0);
				messageRect.Size = new CGSize (0, 0);

				float heightToWidth = Math.Abs (message.heightToWidth);
				CGSize mediaSize = MediaSizeHelper.SharedInstance.ThumbnailSizeForHeightToWidth (heightToWidth);

				mediaRect.Location = new CGPoint ((int)(250 - (mediaSize.Width + 10)), nameRect.Location.Y + nameRect.Size.Height + 3);
				mediaRect.Size = mediaSize;

				progressRect.Location = new CGPoint (mediaRect.Location.X, mediaRect.Location.Y + (mediaRect.Size.Height - 8));
				progressRect.Size = new CGSize (mediaSize.Width, -1); // height not included, we just use the bars height

				containingSize.Width = constrainedTo.Width + 30;
				containingSize.Height = mediaRect.Location.Y + mediaRect.Height + 10;

				recordingRect.Location = new CGPoint (mediaRect.Location.X, mediaRect.Location.Y);
				recordingRect.Size = new CGSize (iOS_Constants.RECORDING_BUTTON_BUBBLE_SIZE, iOS_Constants.RECORDING_BUTTON_BUBBLE_SIZE);
			}
			else {
				mediaRect.Location = new CGPoint (0, 0);
				mediaRect.Size = new CGSize (0, 0);

				constrainedTo = new CGSize (240, 9999);

				UIFont messageFont = MessageFont;
				if (message.ShouldEnlargeEmoji ()) {
					messageFont = EnlargedMessageFont;
				}

				/* Support RTL */
				CGSize messageSize = message.message.SizeOfTextWithFontAndLineBreakMode (messageFont, constrainedTo, UILineBreakMode.WordWrap);
				messageRect.Location = UIDevice.CurrentDevice.IsRightLeftLanguage () ? new CGPoint (constrainedTo.Width - messageSize.Width - 5, nameRect.Location.Y + nameRect.Size.Height + 3) : new CGPoint (10, nameRect.Location.Y + nameRect.Size.Height + 3);
				messageRect.Size = new CGSize ((float)((int)(messageSize.Width + 0.5)), (float)((int)(messageSize.Height + 0.5)));

				containingSize.Width = constrainedTo.Width + 40;
				containingSize.Height = messageRect.Location.Y + messageRect.Height + 5;
			}
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			this.BubbleBackground.Frame = Bounds;

			CGRect nameRect = new CGRect (), messageRect = new CGRect (), statusRect = new CGRect (), mediaRect = new CGRect (), recordingRect = new CGRect (), progressRect = new CGRect ();
			SizeOfWithMessage (Message, ref nameRect, ref messageRect, ref mediaRect, ref recordingRect, ref progressRect, ref statusRect);

			this.FromLabel.Frame = nameRect;
			this.MessageTextView.Frame = messageRect;
			this.ImageButton.Frame = mediaRect;
			this.MessageStatusView.Frame = statusRect;

			this.mediaButtonOverlay.Frame = recordingRect;
			this.mediaButtonOverlay.ContentMode = UIViewContentMode.ScaleAspectFit;

			// we don't overwrite the progress bars height
			this.ProgressView.Frame = new CGRect (progressRect.Location, new CGSize (progressRect.Size.Width, ProgressView.Frame.Size.Height));
		}
	}
}