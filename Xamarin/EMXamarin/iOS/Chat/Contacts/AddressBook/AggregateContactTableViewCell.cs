using CoreGraphics;
using em;
using Foundation;
using String_UIKit_Extension;
using UIKit;

namespace iOS
{
	public class AggregateContactTableViewCell : UITableViewCell {

		public static readonly NSString Key = new NSString ("AggregateContactTableViewCell");

		readonly ContactThumbnailView thumbnailView;
		readonly UILabel descriptionLabel;
		readonly UIView colorBand;
		readonly UIButton sendButton;
		readonly ThumbnailAnimationStrategy thumbnailAnimationStrategy;

		readonly int SEND_BUTTON_SIZE = 30;

		public AggregateContactTableViewCell() : base (UITableViewCellStyle.Default, Key) {
			ContentView.AutosizesSubviews = true;

			thumbnailView = new ContactThumbnailView ();
			thumbnailView.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			ContentView.AddSubview (thumbnailView);

			thumbnailAnimationStrategy = new QueuedThumbnailAnimationStrategy (thumbnailView, thumbnailView.ThumbnailImageView);

			descriptionLabel = new UILabel ();
			descriptionLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			descriptionLabel.Font = FontHelper.DefaultFontWithSize (UIFont.SystemFontSize);
			descriptionLabel.Lines = 1;
			ContentView.AddSubview (descriptionLabel);

			colorBand = new UIView (new CGRect (Bounds.Size.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, Bounds.Size.Height));
			colorBand.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleHeight;
			ContentView.AddSubview (colorBand);

			CGRect contentViewBounds = ContentView.Bounds;
			var bottomSeparatorLine = new UIView(new CGRect(0, contentViewBounds.Size.Height - 1, contentViewBounds.Size.Width - 5, 1));
			bottomSeparatorLine.BackgroundColor = iOS_Constants.INBOX_ROW_SEPERATOR_COLOR;
			bottomSeparatorLine.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleWidth;
			ContentView.AddSubview (bottomSeparatorLine);

			sendButton = new UIButton (UIButtonType.RoundedRect);
			em.BackgroundColor.Gray.GetChatSendButtonResource ((UIImage image) => {
				sendButton.SetBackgroundImage (image, UIControlState.Normal);
			});
			sendButton.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
			sendButton.Frame = new CGRect (0, 0, SEND_BUTTON_SIZE, SEND_BUTTON_SIZE);
			sendButton.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			ContentView.AddSubview (sendButton);
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			thumbnailView.Frame = new CGRect (4, 8, 70, 60);

			colorBand.Frame = new CGRect (ContentView.Frame.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, ContentView.Frame.Height);

			// preview
			CGSize size = descriptionLabel.Text.SizeOfTextWithFontAndLineBreakMode (descriptionLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
			size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
			descriptionLabel.Frame = new CGRect (new CGPoint (80, (ContentView.Frame.Height - size.Height)/2), size);

			sendButton.Frame = new CGRect (new CGPoint (this.ContentView.Frame.Width - sendButton.Frame.Width - UI_CONSTANTS.EXTRA_MARGIN, this.ContentView.Frame.Height / 2 - sendButton.Frame.Height / 2), new CGSize (sendButton.Frame.Width, sendButton.Frame.Height));
		}

		Contact c;
		public Contact AggContact {
			get {
				return c;
			}

			set {
				c = value;
				if (c != null) {
					// setting color
					colorBand.BackgroundColor = c.colorTheme.GetColor ();
					thumbnailView.ColorTheme = c.colorTheme;
					c.colorTheme.GetChatSendButtonResource ((UIImage image) => {
						if (sendButton != null) {
							sendButton.SetBackgroundImage (image, UIControlState.Normal);
						}
					});
					SetDescription (c.label, c.description);

					ImageSetter.SetThumbnailImage (c, iOS_Constants.DEFAULT_NO_IMAGE, SetThumbnail);

					ImageSetter.UpdateThumbnailFromMediaState (c, thumbnailView);
				}
				else {
					SetDescription ("", "");
					SetThumbnail (null);
				}
			}
		}

		public static AggregateContactTableViewCell Create () {
			return new AggregateContactTableViewCell ();
		}

		public void SetEvenRow(bool isEven) {
			ContentView.BackgroundColor = isEven ? iOS_Constants.EVEN_COLOR : iOS_Constants.ODD_COLOR;
		}

		public void SetDescription(string label, string value) {
			SetDescription(label, value, false);
		}

		public void SetDescription(string label, string value, bool animated) {
			string text = string.IsNullOrEmpty (label) ? value : string.Format ("{0} ({1})", value, label);

			if (!animated)
				descriptionLabel.Text = text;
			else {
				bool animatePreview = !text.Equals(descriptionLabel.Text);
				if (animatePreview) {
					UIView.Animate (0.2,
						() => {
							if (animatePreview)
								descriptionLabel.Alpha = 0;
						},
						() => UIView.Animate (0.2, () => {
							if (animatePreview) {
								descriptionLabel.Text = text;
								descriptionLabel.Alpha = 1;
							}
							SetNeedsLayout ();
						}));
				}
			}
		}

		public void SetThumbnail(UIImage image) {
			SetThumbnail(image, false);
		}

		public void SetThumbnail(UIImage image, bool animated) {
			thumbnailAnimationStrategy.AnimateThumbnail (image, animated);
		}
	}
}