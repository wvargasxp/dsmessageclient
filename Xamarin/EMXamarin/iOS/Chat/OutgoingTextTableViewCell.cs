using System;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using UIKit;
using System.Diagnostics;

namespace iOS {
	using Media_iOS_Extension;

	public class OutgoingTextTableViewCell : AbstractOutgoingChatViewTableCell {
		
		const float OPAQUE = 1;
		const float INVISIBLE = 0;
		const float SEMI_OPAQUE = 0.5f;

		public static readonly NSString TextKey = new NSString ("OutgoingTextTableViewCellText");
		public static readonly NSString DeletedKey = new NSString ("OutgoingTextTableViewCellDeleted");

		public static NSString ReuseKeyForMessage (Message message, nfloat height) { 
			Debug.Assert (message.HasMedia (), "This should only be called for messages with media.");
			// Use a different reuse key for deleted messages as those 
			bool deletedMessage = message.messageLifecycle == MessageLifecycle.deleted;
			if (deletedMessage) {
				return OutgoingTextTableViewCell.DeletedKey;
			}
				
			string key = string.Format ("OutgoingMediaCell{0}", height);
			return new NSString (key);
		}

		private bool shouldCenterThumbnail;
		public bool ShouldCenterThumbnail {
			get { return shouldCenterThumbnail; }
			set { shouldCenterThumbnail = value; }
		}

		public static NSString ReuseKeyForHeight (nfloat height) {
			string key = string.Format ("OutgoingTextTableViewCellText{0}", height);
			return new NSString (key);
		}

		public UIButton button { 
			get {
				return bubbleView.ImageButton;
			}
		}

		readonly OutgoingBubbleView bubbleView;
		readonly OutgoingThumbnailView thumbnail;
		public override BasicThumbnailView Thumbnail {
			get { return thumbnail; }
		}

		public OutgoingTextTableViewCell (UITableViewCellStyle style, string key) : base (style, key) {
			ContentView.AutosizesSubviews = true;

			bubbleView = new OutgoingBubbleView ();
			bubbleView.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			bubbleView.Tag = 0xB;
			ContentView.AddSubview (bubbleView);

			thumbnail = new OutgoingThumbnailView ();
			thumbnail.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			thumbnail.Tag = 0xC;
			ContentView.AddSubview (thumbnail);
		}

		public static OutgoingTextTableViewCell Create (NSString cellReuseKey) {
			var retVal = new OutgoingTextTableViewCell (UITableViewCellStyle.Default, cellReuseKey);
			return retVal;
		}

		public static void SizeWithMessage(Message m, nfloat boundsWidth, ref CGSize totalSize) {
			CGRect d1 = new CGRect(), d2 = new CGRect(), d3 = new CGRect();
			SizeWithMessage (m, boundsWidth, ref d1, ref d2, ref d3, ref totalSize);
		}

		public static void SizeWithMessage(Message m, nfloat boundsWidth, ref CGRect sendDateRect, ref CGRect textBubbleRect, ref CGRect thumbnailRect) {
			var d1 = new CGSize ();
			SizeWithMessage (m, boundsWidth, ref sendDateRect, ref textBubbleRect, ref thumbnailRect, ref d1);
		}

		public static void SizeWithMessage(Message m, nfloat boundsWidth, ref CGRect sendDateRect, ref CGRect textBubbleRect, ref CGRect thumbnailRect, ref CGSize totalSize) {
			TimestampSizeWithMessage (m, ref sendDateRect);

			// Bubble
			var bubbleSize = new CGSize ();
			OutgoingBubbleView.SizeOfWithMessage (m, ref bubbleSize);
			textBubbleRect.Size = bubbleSize;

			// thumbnail
			thumbnailRect.Size = new CGSize (56, 45);

			// offsets relative to text bubble and thumbnail
			if (textBubbleRect.Size.Height > thumbnailRect.Size.Height) {
				textBubbleRect.Location = new CGPoint (boundsWidth - 310, sendDateRect.Location.Y + sendDateRect.Size.Height);
				thumbnailRect.Location = new CGPoint (boundsWidth - 60, sendDateRect.Location.Y + sendDateRect.Size.Height + 5);
			}
			else {
				textBubbleRect.Location = new CGPoint(boundsWidth - 310, sendDateRect.Location.Y + sendDateRect.Size.Height + 10);
				thumbnailRect.Location = new CGPoint (boundsWidth - 60, sendDateRect.Location.Y + sendDateRect.Size.Height);
			}

			totalSize.Width = UIScreen.MainScreen.Bounds.Width;
			totalSize.Height = (nfloat)Math.Max (textBubbleRect.Location.Y + textBubbleRect.Size.Height, thumbnailRect.Location.Y + thumbnailRect.Size.Height) + iOS_Constants.CHAT_MESSAGE_BUBBLE_PADDING;
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();

			if (needsToCalcLayout) {
				CGRect sendDateRect = new CGRect (), textBubbleRect = new CGRect (), thumbnailRect = new CGRect ();

				SizeWithMessage (message, Bounds.Width, ref sendDateRect, ref textBubbleRect, ref thumbnailRect);

				timestampLabel.Frame = new CGRect (sendDateRect.Location, new CGSize (Bounds.Size.Width, sendDateRect.Size.Height));
				bubbleView.Frame = textBubbleRect;
				bool shouldEnlargeEmoji = bubbleView.MessageTextView.Font.Equals (BasicBubbleView.EnlargedMessageFont);

				if (!shouldEnlargeEmoji) {
					// Ideally, be able to detect if message is one line long, but this is a very dirty workaround
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
				bubbleView.Alpha = m.messageLifecycle == MessageLifecycle.deleted ? SEMI_OPAQUE : OPAQUE;

				bubbleView.Message = m;
				if (m.ShouldEnlargeEmoji ()) {
					this.ShouldCenterThumbnail = false;
					bubbleView.MessageTextView.Font = BasicBubbleView.EnlargedMessageFont;
				}
				bubbleView.MessageTextView.Text = m.message;
				bubbleView.FromLabel.Text = m.chatEntry.SenderName;
				bubbleView.FromLabel.TextColor = m.chatEntry.SenderColorTheme.GetColor ();
				bubbleView.CreateStretchableImage (m.chatEntry.SenderColorTheme);

				bubbleView.UpdateAttributedString ();

				thumbnail.CreatePhotoFrame (m.chatEntry.SenderColorTheme);

				if (!m.showSentDate)
					timestampLabel.Alpha = INVISIBLE;
				else {
					timestampLabel.Alpha = OPAQUE;
					timestampLabel.Text = m.FormattedSentDate;
				}
					
				UpdateMediaResourceIfEligible ();

				UpdateCellFromMediaState (message, true);

				UpdateThumbnailImage (m.chatEntry.SenderCounterParty);

				setMessageStatus (m, false);

				SetNeedsLayout ();
				bubbleView.SetNeedsLayout ();
				needsToCalcLayout = true;
			}
		}
			
		public override void MediaDidUpdateImageResource (Notification notif) {
			UpdateMediaResourceIfEligible ();
		}

		void UpdateMediaResourceIfEligible () {
			if (message.HasMedia ()) {
				BackgroundColor color = message.chatEntry.SenderColorTheme;

				ContentType type = ContentTypeHelper.FromMessage (message);
				bool isSoundMedia = ContentTypeHelper.IsAudio (type);

				bubbleView.ImageButton.SetImage (null, UIControlState.Normal);
				int position = this.Position;
				WeakReference thisRef = new WeakReference (this);
				ImageSetter.SetImage (message.media, (UIImage loadedImage) => {
					EMTask.DispatchMain (() => {
						OutgoingTextTableViewCell weakSelf = thisRef.Target as OutgoingTextTableViewCell;
						if (weakSelf != null) {
							if (position == weakSelf.Position) {
								if (isSoundMedia) {
									loadedImage = ImageSetter.UseSoundWaveFormMask (color, loadedImage);
								}

								UIButton imageButton = weakSelf.bubbleView.ImageButton;
								loadedImage = loadedImage.GetResizedThumbnailIfSmallerThanView (imageButton);

								imageButton.SetImage (loadedImage, UIControlState.Normal);
								if (imageButton.ImageView != null) {
									imageButton.ImageView.Layer.CornerRadius = iOS_Constants.DEFAULT_CORNER_RADIUS;
									imageButton.ImageView.Layer.MasksToBounds = true;
								}
							}
						}
					});
				});
			}
		}

		public override void UpdateCellFromMediaState (Message m, bool duringSetMessage = false) {
			Media media = m.media;
			if (media == null) {
				bubbleView.ProgressView.Alpha = 0;
				return;
			}

			bubbleView.mediaButtonOverlay.Alpha = INVISIBLE;

			switch (media.MediaState) {
			case MediaState.Absent:
				{
					bubbleView.ProgressView.Alpha = 1;
					bubbleView.ImageButton.Alpha = 0;
					bubbleView.ProgressView.Progress = Constants.BASE_PROGRESS_ON_PROGRESS_VIEW;
					break;
				}
			case MediaState.Encoding:
			case MediaState.Uploading:
				{
					bubbleView.ImageButton.Alpha = SEMI_OPAQUE;
					bubbleView.ProgressView.Alpha = SEMI_OPAQUE;
					bubbleView.ProgressView.ProgressTintColor = UIColor.Blue;
					bubbleView.ProgressView.TrackTintColor = UIColor.Clear;
					BringSubviewToFront (bubbleView.ProgressView);
					bubbleView.ProgressView.Progress = (float)m.media.Percentage;
					DisplaySoundRecordingControlsIfEligible (m);
					break;
				}
			case MediaState.Present:
				{
					bubbleView.ImageButton.Alpha = OPAQUE;
					bubbleView.ProgressView.Alpha = INVISIBLE;
					DisplaySoundRecordingControlsIfEligible (m);
					if (!duringSetMessage)
						DidSetMessage (m);
					break;
				}
			case MediaState.FailedUpload:
				{
					bubbleView.ImageButton.Alpha = SEMI_OPAQUE;
					bubbleView.ProgressView.Alpha = OPAQUE;
					BringSubviewToFront (bubbleView.ProgressView);
					bubbleView.ProgressView.ProgressTintColor = UIColor.Red;
					bubbleView.ProgressView.TrackTintColor = UIColor.Black;
					DisplaySoundRecordingControlsIfEligible (m);
					break;
				}
			case MediaState.Downloading:
				{
					bubbleView.ProgressView.Alpha = OPAQUE;
					bubbleView.ImageButton.Alpha = INVISIBLE;
					float percentage = (float)media.Percentage;
					if (percentage < Constants.BASE_PROGRESS_ON_PROGRESS_VIEW)
						percentage = Constants.BASE_PROGRESS_ON_PROGRESS_VIEW;
					bubbleView.ProgressView.Progress = percentage;
					break;
				}
			case MediaState.FailedDownload:
				{
					bubbleView.ImageButton.Alpha = OPAQUE;
					if (AppEnv.DEBUG_MODE_ENABLED) {
						bubbleView.ImageButton.BackgroundColor = UIColor.Red;
					}
					break;
				}
			}

			// If this is being called from DidSetMessage, we don't need to do any do any additional view updates.
			if (!duringSetMessage)
				this.SetNeedsDisplay ();

		}

		public void DisplaySoundRecordingControlsIfEligible (Message m) {
			ContentType type = ContentTypeHelper.FromMessage (m);

			if(ContentTypeHelper.IsAudio (type)) {
				Media media = m.media;
				UIImage controlIcon;
				BackgroundColor themeColor = m.chatEntry.SenderColorTheme;
				Action<UIImage> onFinishHandler = ((UIImage image) => {
					if (bubbleView != null && bubbleView.mediaButtonOverlay != null) {
						bubbleView.mediaButtonOverlay.Image = image;
						bubbleView.mediaButtonOverlay.Alpha = OPAQUE;
					}
				});
				switch (media.SoundState) {
				default:
				case MediaSoundState.Stopped:
					themeColor.GetSoundRecordingControlPlayInlineResource (onFinishHandler);
					break;
				case MediaSoundState.Playing:
					themeColor.GetSoundRecordingControlStopInlineResource (onFinishHandler);
					break;
				}
			}
		}

		public override void UpdateThumbnailImage (CounterParty c) {
			ImageSetter.SetThumbnailImage (c, iOS_Constants.DEFAULT_NO_IMAGE, SetThumbnailImage);

			ImageSetter.UpdateThumbnailFromMediaState (c, this.Thumbnail);
		}

		public override void SetThumbnailImage (UIImage image) {
			thumbnail.Image = image;
		}

		public override void setMessageStatus(Message m, bool animated) {
			bubbleView.MessageStatusView.messageStatus = m.messageStatus;
		}

		public override void UpdateColorTheme (BackgroundColor c) {
			if (bubbleView != null)
				bubbleView.CreateStretchableImage (c);
			if (thumbnail != null)
				thumbnail.CreatePhotoFrame (c);
		}
	}
}