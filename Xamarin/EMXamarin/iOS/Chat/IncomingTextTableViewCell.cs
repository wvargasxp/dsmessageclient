using System;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using UIKit;
using UIDevice_Extension;

namespace iOS {

	public class IncomingTextTableViewCell : AbstractChatViewTableCell {
		
		public static readonly NSString Key = new NSString ("IncomingTextTableViewCell");

		public static NSString ReuseKeyForHeight (nfloat height) {
			string key = string.Format ("IncomingTextTableViewCell{0}", height);
			return new NSString (key);
		}

		private bool shouldCenterThumbnail;
		public bool ShouldCenterThumbnail {
			get { return shouldCenterThumbnail; }
			set { shouldCenterThumbnail = value; }
		}

		public static readonly int BUBBLE_INSET = 40;
		public static readonly int BUBBLE_INSET_RIGHT = 10;
		public static readonly int BUBBLE_WIDTH = 280;

		IncomingBubbleView bubbleView;
		public readonly IncomingThumbnailView thumbnail;

		public override BasicThumbnailView Thumbnail {
			get {
				return thumbnail;
			}
		}

		public IncomingTextTableViewCell (UITableViewCellStyle style, string key) : base (style, key) {
			bubbleView = new IncomingBubbleView ();
			bubbleView.Tag = 0xB;
			ContentView.AddSubview (bubbleView);

			thumbnail = new IncomingThumbnailView ();
			thumbnail.Tag = 0xC;
			thumbnail.BackgroundImageButton.TouchUpInside += DidTapThumbnail;
			ContentView.AddSubview (thumbnail);

			message = null;
		}

		public static IncomingTextTableViewCell Create (string cellReuseKey) {
			var retVal = new IncomingTextTableViewCell (UITableViewCellStyle.Default, cellReuseKey);
			return retVal;
		}

		public static void SizeWithMessage(Message m, ref CGSize totalSize) {
			CGRect d1 = new CGRect (), d2 = new CGRect (), d3 = new CGRect ();
			SizeWithMessage (m, ref d1, ref d2, ref d3, ref totalSize);
		}

		public static void SizeWithMessage(Message m, ref CGRect sendDateRect, ref CGRect textBubbleRect, ref CGRect thumbnailRect) {
			var d1 = new CGSize ();
			SizeWithMessage (m, ref sendDateRect, ref textBubbleRect, ref thumbnailRect, ref d1);
		}

		public static void SizeWithMessage(Message m, ref CGRect sendDateRect, ref CGRect textBubbleRect, ref CGRect thumbnailRect, ref CGSize totalSize) {
			TimestampSizeWithMessage (m, ref sendDateRect);

			// Bubble
			var bubbleSize = new CGSize ();
			IncomingBubbleView.SizeOfWithMessage (m.fromContact.displayName, m.message, ref bubbleSize);
			textBubbleRect.Size = bubbleSize;

			// thumbnail
			thumbnailRect.Size = new CGSize (55, 47);

			// offsets relative to text bubble and thumbnail
			if (textBubbleRect.Size.Height > thumbnailRect.Size.Height) {
				textBubbleRect.Location = new CGPoint (25, sendDateRect.Location.Y + sendDateRect.Size.Height);
				thumbnailRect.Location = new CGPoint (2, sendDateRect.Location.Y + sendDateRect.Size.Height + 5);
			}
			else {
				textBubbleRect.Location = new CGPoint( 25, sendDateRect.Location.Y + sendDateRect.Size.Height + 10);
				thumbnailRect.Location = new CGPoint (2, sendDateRect.Location.Y + sendDateRect.Size.Height);
			}

			totalSize.Width = 320; // FIXME?
			totalSize.Height = (nfloat)Math.Max (textBubbleRect.Location.Y + textBubbleRect.Size.Height, thumbnailRect.Location.Y + thumbnailRect.Size.Height) + iOS_Constants.CHAT_MESSAGE_BUBBLE_PADDING;
		}
			
		public override void LayoutSubviews () {
			base.LayoutSubviews ();
			if (needsToCalcLayout) {
				CGRect sendDateRect = new CGRect (), textBubbleRect = new CGRect (), thumbnailRect = new CGRect ();
				SizeWithMessage (message, ref sendDateRect, ref textBubbleRect, ref thumbnailRect);

				timestampLabel.Frame = new CGRect (sendDateRect.Location, new CGSize (Bounds.Size.Width, sendDateRect.Size.Height));
				bubbleView.Frame = textBubbleRect;
				bool shouldEnlargeEmoji = bubbleView.MessageTextView.Font.Equals (BasicBubbleView.EnlargedMessageFont);
				if (!shouldEnlargeEmoji) {
					ShouldCenterThumbnail = (textBubbleRect.Height < 60);
				}
				if (ShouldCenterThumbnail) {
					thumbnailRect.Y = textBubbleRect.Y + textBubbleRect.Height / 2 - thumbnailRect.Height / 2;
				}
				thumbnail.Frame = thumbnailRect;

				needsToCalcLayout = false;
			}
		}

		bool needsToCalcLayout = false;
		public override void DidSetMessage (Message m) {
			if (m != null) {
				bubbleView.Alpha = m.messageLifecycle == MessageLifecycle.deleted ? 0.5f : 1;

				thumbnail.SetFromContact(m.fromContact);
				bool shouldEnlargeEmoji = m.ShouldEnlargeEmoji ();
				if (shouldEnlargeEmoji) {
					ShouldCenterThumbnail = false;
					bubbleView.MessageTextView.Font = BasicBubbleView.EnlargedMessageFont;
				}
				string setMessage = m.message;
				if (!UIDevice.CurrentDevice.IsIos8v3Later ()) {
					setMessage = Message.RemoveEmojiSkinModifier (setMessage);
				}
				bubbleView.MessageTextView.Text = setMessage;
				bubbleView.SetFromContact (m.fromContact);
				bubbleView.FromLabel.Text = m.fromContact.displayName;
				bubbleView.FromLabel.TextColor = m.fromContact.colorTheme.GetColor ();
				bubbleView.ShowHideAliasMaskIcon (m.fromContact);

				bubbleView.UpdateAttributedString ();

				if (!m.showSentDate)
					timestampLabel.Alpha = 0;
				else {
					timestampLabel.Alpha = 1;
					timestampLabel.Text = m.FormattedSentDate;
				}

				UpdateThumbnailImage (m.fromContact);

				SetNeedsLayout ();
				bubbleView.SetNeedsLayout ();
				needsToCalcLayout = true;
			}
		}
			
		public override void UpdateThumbnailImage (CounterParty c) {
			ImageSetter.SetThumbnailImage (c, iOS_Constants.DEFAULT_NO_IMAGE, (UIImage loadedImage) => {
				SetThumbnailImage (loadedImage);
			});

			ImageSetter.UpdateThumbnailFromMediaState (c, this.Thumbnail);
		}

		public override void SetThumbnailImage (UIImage image) {
			thumbnail.Image = image;
		}

		public override void UpdateColorTheme (BackgroundColor c) {}
		public override void UpdateCellFromMediaState (Message m, bool duringSetMessage = false) {}

		public override void MediaDidUpdateImageResource (Notification notif) {}
	}
}