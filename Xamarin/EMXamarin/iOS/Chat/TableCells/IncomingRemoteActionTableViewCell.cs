using System;
using Foundation;
using System.Diagnostics;
using em;
using UIKit;
using CoreGraphics;

namespace iOS {
	public class IncomingRemoteActionTableViewCell : AbstractChatViewTableCell {

		public static readonly NSString Key = new NSString ("IncomingRemoteActionTableViewCell");

		public static NSString ReuseKeyForMessage (Message message) { 
			Debug.Assert (message.HasRemoteAction, "Using Incoming remote action cell but message has no remote action.");
			return Key;
		}

		private IncomingThumbnailView _thumbnail = null;
		private IncomingRemoteActionBubbleView _bubbleView = null;
		private bool _needsToCalcLayout = false;

		public EMButton RemoteActionButton {
			get {
				return this._bubbleView.RemoteActionButton;
			}
		}

		public static IncomingRemoteActionTableViewCell Create (string cellReuseKey) {
			IncomingRemoteActionTableViewCell cell = new IncomingRemoteActionTableViewCell (UITableViewCellStyle.Default, cellReuseKey);
			return cell;
		}

		public IncomingRemoteActionTableViewCell (UITableViewCellStyle style, string key) : base (style, key) {
			this._bubbleView = new IncomingRemoteActionBubbleView ();

			this.ContentView.Add (this._bubbleView);

			this._thumbnail = new IncomingThumbnailView ();
			this._thumbnail.BackgroundImageButton.TouchUpInside += DidTapThumbnail;

			this.ContentView.Add (this._thumbnail);
		}

		public static void SizeWithMessage (Message m, ref CGSize totalSize) {
			CGRect d1 = new CGRect (), d2 = new CGRect (), d3 = new CGRect ();
			SizeWithMessage (m, ref d1, ref d2, ref d3, ref totalSize);
		}

		public static void SizeWithMessage (Message m, ref CGRect sendDateRect, ref CGRect textBubbleRect, ref CGRect thumbnailRect) {
			var d1 = new CGSize ();
			SizeWithMessage (m, ref sendDateRect, ref textBubbleRect, ref thumbnailRect, ref d1);
		}

		public static void SizeWithMessage (Message m, ref CGRect sendDateRect, ref CGRect textBubbleRect, ref CGRect thumbnailRect, ref CGSize totalSize) {
			TimestampSizeWithMessage (m, ref sendDateRect);

			// Bubble
			var bubbleSize = new CGSize ();
			IncomingRemoteActionBubbleView.SizeOfWithMessage (m.fromContact.displayName, m.message, ref bubbleSize);
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

			if (this._needsToCalcLayout) {
				CGRect sendDateRect = new CGRect (), textBubbleRect = new CGRect (), thumbnailRect = new CGRect ();
				SizeWithMessage (message, ref sendDateRect, ref textBubbleRect, ref thumbnailRect);

				timestampLabel.Frame = new CGRect (sendDateRect.Location, new CGSize (this.Bounds.Size.Width, sendDateRect.Size.Height));
				this._bubbleView.Frame = textBubbleRect;
				this._thumbnail.Frame = thumbnailRect;

				this._needsToCalcLayout = false;
			}
		}

		#region implemented abstract members of AbstractChatViewTableCell

		public override void DidSetMessage (em.Message m) {
			if (m == null) return;

			this._bubbleView.Alpha = m.messageLifecycle == MessageLifecycle.deleted ? 0.5f : 1;

			this._thumbnail.SetFromContact (m.fromContact);

			this._bubbleView.SetFromContact (m.fromContact);
			this._bubbleView.Message = m;
			this._bubbleView.MessageTextView.Text = m.message;
			this._bubbleView.FromLabel.Text = m.fromContact.displayName;
			this._bubbleView.FromLabel.TextColor = m.fromContact.colorTheme.GetColor ();

			this._bubbleView.UpdateAttributedString ();

			this._bubbleView.RemoteActionButton.SetTitle (m.RemoteAction.label, UIControlState.Normal);

			if (!m.showSentDate)
				timestampLabel.Alpha = 0;
			else {
				timestampLabel.Alpha = 1;
				timestampLabel.Text = m.FormattedSentDate;
			}

			UpdateThumbnailImage (m.fromContact);

			SetNeedsLayout ();
			this._bubbleView.SetNeedsLayout ();
			this._needsToCalcLayout = true;
		}

		public override void UpdateThumbnailImage (em.CounterParty c) {
			ImageSetter.SetThumbnailImage (c, iOS_Constants.DEFAULT_NO_IMAGE, (UIImage loadedImage) => {
				SetThumbnailImage (loadedImage);
			});

			ImageSetter.UpdateThumbnailFromMediaState (c, this.Thumbnail);
		}

		public override void SetThumbnailImage (UIKit.UIImage image) {
			this._thumbnail.Image = image;
		}

		public override void UpdateColorTheme (em.BackgroundColor c) {}

		public override void UpdateCellFromMediaState (em.Message m, bool duringSetMessage = false) {}

		public override void MediaDidUpdateImageResource (em.Notification notif) {}

		public override BasicThumbnailView Thumbnail {
			get {
				return this._thumbnail;
			}
		}

		#endregion
	}
}

