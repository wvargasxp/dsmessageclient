using CoreGraphics;
using Foundation;
using UIKit;
using em;
using EMXamarin;

namespace iOS {
	using String_UIKit_Extension;

	public class NotificationTableViewCell : UITableViewCell {

		public static readonly NSString Key = new NSString ("NotificationTableViewCell");

		readonly NotificationThumbnailView thumbnailView;
		public NotificationThumbnailView Thumbnail {
			get { return thumbnailView; }
		}

		UIImageView hasUnreadImageView;
		UILabel dateLabel;
		UILabel previewLabel;
		UIView colorBand;
		ThumbnailAnimationStrategy thumbnailAnimationStrategy;

		public NotificationTableViewCell() : base (UITableViewCellStyle.Default, Key) {
			ContentView.AutosizesSubviews = true;

			thumbnailView = new NotificationThumbnailView ();
			thumbnailView.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			ContentView.AddSubview (thumbnailView);

			thumbnailAnimationStrategy = new QueuedThumbnailAnimationStrategy (thumbnailView, thumbnailView.ThumbnailImageView);

			hasUnreadImageView = new UIImageView (UIImage.FromFile ("chat/dotGreen.png"));
			ContentView.AddSubview (hasUnreadImageView);

			dateLabel = new UILabel ();
			dateLabel.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			dateLabel.Font = FontHelper.DefaultFontWithSize (UIFont.SmallSystemFontSize - 2);
			dateLabel.TextColor = UIColor.LightGray;
			ContentView.AddSubview (dateLabel);

			previewLabel = new UILabel ();
			previewLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			previewLabel.Font = FontHelper.DefaultFontWithSize (UIFont.SystemFontSize - 2);
			previewLabel.Lines = 2;
			ContentView.AddSubview (previewLabel);

			colorBand = new UIView (new CGRect (Bounds.Size.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, Bounds.Size.Height));
			colorBand.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleHeight;
			ContentView.AddSubview (colorBand);

			CGRect contentViewBounds = ContentView.Bounds;
			var bottomSeparatorLine = new UIView(new CGRect(0, contentViewBounds.Size.Height - 1, contentViewBounds.Size.Width - 5, 1));
			bottomSeparatorLine.BackgroundColor = iOS_Constants.INBOX_ROW_SEPERATOR_COLOR;
			bottomSeparatorLine.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleWidth;
			ContentView.AddSubview (bottomSeparatorLine);
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			thumbnailView.Frame = new CGRect (4, 8, 70, 60);

			if (ne.Read)
				hasUnreadImageView.Alpha = 0f;

			hasUnreadImageView.Frame = new CGRect (new CGPoint (77, 35), hasUnreadImageView.Bounds.Size);

			colorBand.Frame = new CGRect (ContentView.Frame.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, ContentView.Frame.Height);

			// Date
			CGSize size = dateLabel.Text.SizeOfTextWithFontAndLineBreakMode (dateLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 50), UILineBreakMode.Clip);
			size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
			dateLabel.Frame = new CGRect (new CGPoint(Bounds.Size.Width - (size.Width + 15), 10), size);

			// preview
			size = previewLabel.Text.SizeOfTextWithFontAndLineBreakMode (previewLabel.Font, new CGSize (Bounds.Width - 105, 75), UILineBreakMode.Clip);
			size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
			previewLabel.Frame = new CGRect (new CGPoint (92, (ContentView.Frame.Height - (size.Height + 2))/2), size);
		}

		NotificationEntry ne;
		public NotificationEntry notificationEntry {
			get { return ne; }
			set {
				ne = value;
				if (ne != null) {
					// setting color
					SetColorBand (ne.ColorTheme);

					SetPreviewLabel (ne.Title, ne.FormattedNotificationDate);
					SetHasUnread (!ne.Read);

					if (ne.counterparty == null && ne.Title.Equals("NOTIFICATION_WELCOME_TITLE".t ())) {
						SetThumbnail (ImageSetter.GetResourceImage ("EMUserImage.png"));
					} else {
						UpdateThumbnailImage (ne.counterparty);
						ImageSetter.UpdateThumbnailFromMediaState (ne.counterparty, thumbnailView);
					}
				}
				else {
					SetPreviewLabel ("", "");
					SetHasUnread (false);
					SetThumbnail (null);
				}
			}
		}

		public static NotificationTableViewCell Create () {
			var cell = new NotificationTableViewCell ();

			return cell;
		}

		public void SetEvenRow(bool isEven) {
			ContentView.BackgroundColor = isEven ? iOS_Constants.EVEN_COLOR : iOS_Constants.ODD_COLOR;
		}

		public void SetPreviewLabel(string label, string dateTime) {
			SetPreviewLabel(label, dateTime, false);
		}

		public void SetPreviewLabel(string label, string dateTime, bool animated) {
			if ( !animated ) {
				previewLabel.Text = label;
				dateLabel.Text = dateTime;
			}
			else {
				bool animatePreview = !label.Equals(previewLabel.Text);
				bool animateDate = !dateTime.Equals(dateLabel.Text);
				if ( animatePreview || animateDate )
					UIView.Animate(iOS_Constants.FADE_ANIMATION_DURATION,
						() => {
							if ( animatePreview )
								previewLabel.Alpha = 0;
							if ( animateDate )
								dateLabel.Alpha = 0;
						},
						() => UIView.Animate (iOS_Constants.FADE_ANIMATION_DURATION, () => {
							if (animatePreview) {
								previewLabel.Text = label;
								previewLabel.Alpha = 1;
							}
							if (animateDate) {
								dateLabel.Text = dateTime;
								dateLabel.Alpha = 1;
							}
						}));
			}
		}

		public void SetHasUnread(bool flag) {
			SetHasUnread (flag, false);
		}

		public void SetHasUnread(bool flag, bool animated) {
			bool changed = flag != (hasUnreadImageView.Alpha == 1);
			if ( changed ) {
				if ( !animated ) {
					if (!flag)
						hasUnreadImageView.Alpha = 0;
					else
						hasUnreadImageView.Alpha = 1;
				}
				else {
					UIView.Animate(iOS_Constants.FADE_ANIMATION_DURATION,
						() => {
							if (!flag)
								hasUnreadImageView.Alpha = 0;
							else
								hasUnreadImageView.Alpha = 1;
						});
				}
			}
		}

		public void UpdateThumbnailImage (CounterParty c) {
			ImageSetter.SetThumbnailImage (c, iOS_Constants.DEFAULT_NO_IMAGE, (UIImage loadedImage) => {
				SetThumbnail (loadedImage);
			});

			ImageSetter.UpdateThumbnailFromMediaState (c, this.Thumbnail);
		}

		public void SetThumbnail(UIImage image) {
			SetThumbnail(image, false);
		}

		public void SetThumbnail(UIImage image, bool animated) {
			thumbnailAnimationStrategy.AnimateThumbnail (image, animated);
		}

		public void SetColorBand(BackgroundColor color) {
			SetColorBand (color, false);
		}

		public void SetColorBand(BackgroundColor color, bool animated) {
			if (!animated) {
				colorBand.BackgroundColor = color.GetColor ();
				thumbnailView.ColorTheme = color;
			} else {
				UIView.Animate(iOS_Constants.FADE_ANIMATION_DURATION,
					() => {
						colorBand.Alpha = 0;
						thumbnailView.Alpha = 0;
					},
					() => UIView.Animate (iOS_Constants.FADE_ANIMATION_DURATION, () => {
						colorBand.BackgroundColor = color.GetColor ();
						colorBand.Alpha = 1;
						thumbnailView.ColorTheme = color;
						thumbnailView.Alpha = 1;
					}));
			}
		}
	}
}