using System;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using UIKit;
using System.Diagnostics;

namespace iOS {
	using Media_iOS_Extension;

	public class IncomingMediaTableViewCell : AbstractChatViewTableCell {
		
		public static readonly NSString DeletedKey = new NSString ("IncomingMediaTableViewCellDeleted");

		public static NSString ReuseKeyForMessage (Message message, nfloat height) { 
			Debug.Assert (message.HasMedia (), "This should only be called for messages with media.");
			// Use a different reuse key for deleted messages as those 
			bool deletedMessage = message.messageLifecycle == MessageLifecycle.deleted;
			if (deletedMessage) {
				return IncomingMediaTableViewCell.DeletedKey;
			}
				
			string key = string.Format ("IncomingMediaTableViewCell{0}", height);
			return new NSString (key);
		}

		public static readonly int BUBBLE_INSET = 40;
		public static readonly int BUBBLE_INSET_RIGHT = 10;
		public static readonly int BUBBLE_WIDTH = 280;

		public Action thumbnailClickCallback;

		IncomingMediaBubbleView bubbleView;
		public readonly IncomingThumbnailView thumbnail;
		public override BasicThumbnailView Thumbnail {
			get {
				return thumbnail;
			}
		}

		public IncomingMediaTableViewCell (UITableViewCellStyle style, string key) : base (style, key) {
			bubbleView = new IncomingMediaBubbleView ();
			bubbleView.Tag = 0xB;

			BringSubviewToFront (bubbleView.progressView);

			ContentView.AddSubview (bubbleView);

			thumbnail = new IncomingThumbnailView ();
			thumbnail.Tag = 0xC;
			thumbnail.BackgroundImageButton.TouchUpInside += WeakDelegateProxy.CreateProxy<object, EventArgs> (DidTapThumbnail).HandleEvent<object, EventArgs>;
			ContentView.AddSubview (thumbnail);

			message = null;
		}

		public static IncomingTextTableViewCell Create (NSString cellReuseKey) {
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
			IncomingMediaBubbleView.SizeOfWithMessage (m.FromString(), m.heightToWidth, ref bubbleSize);
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

			totalSize.Width = UIScreen.MainScreen.Bounds.Width;
			totalSize.Height = (nfloat)Math.Max (textBubbleRect.Location.Y + textBubbleRect.Size.Height, thumbnailRect.Location.Y + thumbnailRect.Size.Height) + iOS_Constants.CHAT_MESSAGE_BUBBLE_PADDING;
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();

			if (needsToCalcLayout) {
				CGRect sendDateRect = new CGRect (), textBubbleRect = new CGRect (), thumbnailRect = new CGRect ();
				SizeWithMessage (message, ref sendDateRect, ref textBubbleRect, ref thumbnailRect);

				timestampLabel.Frame = new CGRect (sendDateRect.Location, new CGSize (Bounds.Size.Width, sendDateRect.Size.Height));
				bubbleView.Frame = textBubbleRect;
				thumbnail.Frame = thumbnailRect;

				needsToCalcLayout = false;
			}
		}

		bool needsToCalcLayout = false;
		public override void DidSetMessage (Message m) {
			bubbleView.message = m;
			if (m != null) {
				bubbleView.Alpha = m.messageLifecycle == MessageLifecycle.deleted ? 0.5f : 1;

				Contact contact = m.fromContact;
				thumbnail.SetFromContact (contact);
				bubbleView.SetFromContact (contact);
				bubbleView.fromLabel.Text = contact.displayName;
				bubbleView.fromLabel.TextColor = contact.colorTheme.GetColor ();
				bubbleView.ShowHideAliasMaskIcon (contact);

				if (!m.showSentDate)
					timestampLabel.Alpha = 0;
				else {
					timestampLabel.Alpha = 1;
					timestampLabel.Text = m.FormattedSentDate;
				}
					
				UpdateThumbnailImage (m.fromContact);
				UpdateMediaResourceIfEligible ();
				UpdateCellFromMediaState (m, true);

				SetNeedsLayout ();
				bubbleView.SetNeedsLayout ();
				needsToCalcLayout = true;
			}
		}

		public override void MediaDidUpdateImageResource (Notification notif) {
			UpdateMediaResourceIfEligible ();
		}

		void UpdateMediaResourceIfEligible () {
			BackgroundColor color = message.fromContact.colorTheme;

			ContentType type = ContentTypeHelper.FromMessage (message);
			bool isSoundMedia = ContentTypeHelper.IsAudio (type);

			int position = this.Position;
			WeakReference thisRef = new WeakReference (this);
			this.bubbleView.mediaButton.SetImage (null, UIControlState.Normal);

			ImageSetter.SetImage (message.media, (UIImage loadedImage) => {
				EMTask.DispatchMain (() => {
					IncomingMediaTableViewCell weakSelf = thisRef.Target as IncomingMediaTableViewCell;
					if (weakSelf != null) {
						if (position == weakSelf.Position) {
							if (isSoundMedia) {
								loadedImage = ImageSetter.UseSoundWaveFormMask (color, loadedImage);
							}

							UIButton imageButton = weakSelf.bubbleView.mediaButton;
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

		public override void UpdateCellFromMediaState (Message m, bool duringSetMessage = false) {
			Media media = m.media;
			if (media == null) {
				bubbleView.progressView.Alpha = 0;
				return;
			}

			bubbleView.mediaButtonOverlay.Alpha = 0;

			switch (media.MediaState) {
			case MediaState.Absent:
				{
					bubbleView.progressView.Alpha = 1;
					bubbleView.mediaButton.Alpha = 0;
					bubbleView.progressView.Progress = Constants.BASE_PROGRESS_ON_PROGRESS_VIEW;
					break;
				}
			case MediaState.Downloading:
				{
					bubbleView.progressView.Alpha = 1;
					bubbleView.mediaButton.Alpha = 0;
					float percentage = (float)media.Percentage;
					if (percentage < Constants.BASE_PROGRESS_ON_PROGRESS_VIEW)
						percentage = Constants.BASE_PROGRESS_ON_PROGRESS_VIEW;
					bubbleView.progressView.Progress = percentage;
					break;
				}
			case MediaState.Present:
				{
					bubbleView.progressView.Alpha = 0;
					bubbleView.mediaButton.Alpha = 1;

					DisplaySoundRecordingControlsIfEligible (m);

					if (!duringSetMessage)
						DidSetMessage (m);
					break;
				}
			case MediaState.FailedDownload:
				{
					bubbleView.mediaButton.Alpha = 1;
					if (AppEnv.DEBUG_MODE_ENABLED) {
						bubbleView.mediaButton.BackgroundColor = UIColor.Red;
					}
					break;
				}
			default:
				{
					break;
				}
			}

			if (!duringSetMessage)
				this.SetNeedsDisplay ();

		}

		void DisplaySoundRecordingControlsIfEligible (Message m) {
			ContentType type = ContentTypeHelper.FromMessage (m);

			if(ContentTypeHelper.IsAudio (type)) {
				Media media = m.media;
				BackgroundColor themeColor = message.fromContact.colorTheme;
				Action<UIImage> onFinishHandler = ((UIImage image) => {
					if (bubbleView != null && bubbleView.mediaButtonOverlay != null) {
						bubbleView.mediaButtonOverlay.Image = image;
						bubbleView.mediaButtonOverlay.Alpha = 1;
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

		public UIButton button { 
			get {
				return bubbleView.mediaButton;
			}
		}

		public void DidTapThumbnail(object sender, EventArgs e) {
			if(thumbnailClickCallback != null)
				thumbnailClickCallback ();
		}

		public override void UpdateThumbnailImage (CounterParty c) {
			ImageSetter.SetThumbnailImage (c, iOS_Constants.DEFAULT_NO_IMAGE, SetThumbnailImage);

			ImageSetter.UpdateThumbnailFromMediaState (c, this.Thumbnail);
		}

		public override void SetThumbnailImage (UIImage image) {
			thumbnail.Image = image;
		}

		public override void UpdateColorTheme (BackgroundColor c) {

		}
	}
}