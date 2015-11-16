using System;
using CoreGraphics;
using em;
using EMXamarin;
using Foundation;
using UIKit;

namespace iOS {
	using String_UIKit_Extension;

	public class InboxTableViewCell : UITableViewCell {
		public static readonly NSString Key = new NSString ("InboxTableViewCell");

		InboxThumbnailView thumbnailView;
		public InboxThumbnailView Thumbnail {
			get { return thumbnailView; }
		}

		UIImageView hasUnreadImageView;
		UIImageView aliasIconImageView;
		UILabel fromLabel;
		UILabel dateLabel;
		UILabel previewLabel;
		UIView colorBand;
		ThumbnailAnimationStrategy thumbnailAnimationStrategy;
		private bool isNotificationBanner;
		public bool IsNotificationBanner {
			get { return isNotificationBanner; }
			set {
				isNotificationBanner = value;
				if (value) {
					this.hasUnreadImageView.Alpha = 0;
					this.dateLabel.Alpha = 0;
				} else {
					this.hasUnreadImageView.Alpha = 1;
					this.dateLabel.Alpha = 1;
				}
			}
		}

		private UIImageView AkaMaskIconImageView { get; set; }

		readonly float PREVIEW_LABEL_HEIGHT = 60;
		readonly float INITIAL_OFFSET = 80; // the offset from ContentView's origin (0), to after the thumbnailView
		readonly float ALIAS_ICON_SIZE = 15;

		public InboxTableViewCell () : base (UITableViewCellStyle.Default, Key) {
			ContentView.AutosizesSubviews = true;

			thumbnailView = new InboxThumbnailView ();
			thumbnailView.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			ContentView.AddSubview (thumbnailView);

			thumbnailAnimationStrategy = new QueuedThumbnailAnimationStrategy (thumbnailView, thumbnailView.ThumbnailImageView);

			UIImage chatDotGreen = ImageSetter.GetResourceImage ("chat/dotGreen.png");
			hasUnreadImageView = new UIImageView (chatDotGreen);
			ContentView.AddSubview (hasUnreadImageView);

			fromLabel = new UILabel ();
			fromLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			fromLabel.Font = FontHelper.DefaultBoldFontWithSize (UIFont.SystemFontSize - 1);
			ContentView.AddSubview (fromLabel);

			dateLabel = new UILabel ();
			dateLabel.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			dateLabel.Font = FontHelper.DefaultFontForLabels ( UIFont.SmallSystemFontSize - 2 );
			dateLabel.TextColor = UIColor.LightGray;
			ContentView.AddSubview (dateLabel);

			previewLabel = new UILabel ();
			previewLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			previewLabel.Font = FontHelper.DefaultFontForLabels ( UIFont.SystemFontSize - 2 );
			previewLabel.Lines = 2;
			ContentView.AddSubview (previewLabel);

			colorBand = new UIView (new CGRect (Bounds.Size.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, Bounds.Size.Height));
			colorBand.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleHeight;
			ContentView.AddSubview (colorBand);

			aliasIconImageView = new UIImageView ();
			aliasIconImageView.Layer.CornerRadius = iOS_Constants.DEFAULT_CORNER_RADIUS;
			aliasIconImageView.Layer.MasksToBounds = true;
			ContentView.AddSubview (aliasIconImageView);

			this.AkaMaskIconImageView = new UIImageView ();
			this.ContentView.AddSubview (this.AkaMaskIconImageView);

			CGRect contentViewBounds = ContentView.Bounds;
			var bottomSeparatorLine = new UIView(new CGRect(0, contentViewBounds.Size.Height - 1, contentViewBounds.Size.Width - 5, 1));
			bottomSeparatorLine.BackgroundColor = iOS_Constants.INBOX_ROW_SEPERATOR_COLOR;
			bottomSeparatorLine.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleWidth;
			ContentView.AddSubview (bottomSeparatorLine);
		}

		public override void WillTransitionToState (UITableViewCellState mask) {
			base.WillTransitionToState (mask);
		}

		public override void DidTransitionToState (UITableViewCellState mask) {
			base.DidTransitionToState (mask);
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			hasUnreadImageView.Frame = new CGRect (new CGPoint (7, 7), hasUnreadImageView.Bounds.Size);
			thumbnailView.Frame = new CGRect (4, 8, 70, 60);

			colorBand.Frame = new CGRect (ContentView.Frame.Size.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, ContentView.Frame.Size.Height);

			// Date
			CGSize size = dateLabel.Text.SizeOfTextWithFontAndLineBreakMode (dateLabel.Font, new CGSize (320, 50), UILineBreakMode.Clip);
			size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
			dateLabel.Frame = new CGRect (new CGPoint(ContentView.Frame.Size.Width - (size.Width + 15), 10), size);

			nfloat fromLabelPossibleWidth = dateLabel.Frame.X - (thumbnailView.Frame.X + thumbnailView.Frame.Width) - ALIAS_ICON_SIZE - UI_CONSTANTS.TINY_MARGIN*3; // times 3 to account for some padding between alias icon and date
			// name
			size = fromLabel.Text.SizeOfTextWithFontAndLineBreakMode (fromLabel.Font, new CGSize (fromLabelPossibleWidth, 50), UILineBreakMode.Clip);
			size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
			CGRect fromFrame = new CGRect (new CGPoint (INITIAL_OFFSET, 6), new CGSize(size.Width, 20));
			fromLabel.Frame = fromFrame;

			this.AkaMaskIconImageView.Frame = new CGRect (
				new CGPoint (fromFrame.Location.X + fromFrame.Size.Width + UI_CONSTANTS.STINY_MARGIN, fromFrame.Y + 2), 
				new CGSize (iOS_Constants.AKA_MASK_ICON_WIDTH, iOS_Constants.AKA_MASK_ICON_HEIGHT));

			if (this.AkaMaskIconImageView.Hidden) {
				aliasIconImageView.Frame = new CGRect (new CGPoint (fromFrame.Location.X + fromFrame.Size.Width + 5, fromFrame.Y + 2), new CGSize (ALIAS_ICON_SIZE, ALIAS_ICON_SIZE));
			} else {
				aliasIconImageView.Frame = new CGRect (new CGPoint (this.AkaMaskIconImageView.Frame.Location.X + this.AkaMaskIconImageView.Frame.Size.Width + 5, fromFrame.Y + 2), new CGSize (ALIAS_ICON_SIZE, ALIAS_ICON_SIZE));
			}

			// Getting the width of the preview label means the width of the tableViewCell (ContentView.Frame) 
			// minus the initial offset (INITIAL_OFFSET) 
			// minus some other amount of pixels to determine  how far the label reaches to the right edge of the table cell.
			previewLabel.Frame = new CGRect (INITIAL_OFFSET, ContentView.Frame.Height / 2 - PREVIEW_LABEL_HEIGHT / 2, ContentView.Frame.Width - INITIAL_OFFSET - (ContentView.Frame.Width - dateLabel.Frame.X - (dateLabel.Frame.Width / 2)), PREVIEW_LABEL_HEIGHT);
		}

		ChatEntry ce;
		public ChatEntry chatEntry {
			get {
				return ce;
			}

			set {
				ce = value;
				if (ce != null) {
					// setting color
					SetColorBand (ce.IncomingColorTheme);
					SetContactsLabel (ce.ContactsLabel);

					SetPreviewLabel (ce.preview, ce.FormattedPreviewDate);
					SetHasUnread (ce.hasUnread);

					//if alias, show icon
					if (chatEntry.fromAlias == null) {
						aliasIconImageView.Image = null;
						aliasIconImageView.Alpha = 0;
					}
					else {
						var appDelegate = (AppDelegate) UIApplication.SharedApplication.Delegate;
						AccountInfo account = appDelegate.applicationModel.account.accountInfo;
						if (account != null && account.aliases != null ) {
							aliasIconImageView.Alpha = 1;
							AliasInfo alias = account.AliasFromServerID (ce.fromAlias);
							if (alias != null) {
								ImageSetter.SetImage (alias.MediaForIcon, iOS_Constants.DEFAULT_ALIAS_ICON_IMAGE, (UIImage loadedImage) => {
									SetIcon (loadedImage);
								});
							}
						}
					}

					if (chatEntry.HasAKAContact) {
						UpdateAKAMask (chatEntry.IncomingColorTheme);
						this.AkaMaskIconImageView.Hidden = false;
					} else {
						this.AkaMaskIconImageView.Hidden = true;
					}

					if (ce.IsAdHocGroupChat())
						SetThumbnail (ImageSetter.GetResourceImage ("EMUserImage.png"));
					else if(ce.contacts.Count > 0)
						UpdateThumbnailImage (chatEntry.FirstContactCounterParty);
				}
				else {
					SetContactsLabel ("");
					SetPreviewLabel ("", "");
					SetHasUnread (false);
					SetThumbnail (null);

					aliasIconImageView.Image = null;
					aliasIconImageView.Alpha = 0;
				}
			}
		}

		public static InboxTableViewCell Create () {
			var cell = new InboxTableViewCell ();
			return cell;
		}

		/*
		 * even colored row if true, odd colored row if false
		 */
		public void SetEvenRow(bool isEven) {
			ContentView.BackgroundColor = isEven ? iOS_Constants.EVEN_COLOR : iOS_Constants.ODD_COLOR;
		}

		public void SetContactsLabel(string label) {
			SetContactsLabel (label, false);
		}

		public void SetContactsLabel(string label, bool animated) {
			if (!animated)
				fromLabel.Text = label;
			else {
				bool animateLabel = !label.Equals(fromLabel.Text);
				if( animateLabel ) 
					UIView.Animate(iOS_Constants.FADE_ANIMATION_DURATION,
						() => {
							fromLabel.Alpha = 0;
						},
						() => UIView.Animate (iOS_Constants.FADE_ANIMATION_DURATION, () => {
							fromLabel.Text = label;
							fromLabel.Alpha = 1;
						}));
			}
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

		public string GetPreviewLabelText() {
			if (this.previewLabel == null)
				return null;
			string previewText = this.previewLabel.Text;
			return previewText;
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

			ImageSetter.UpdateThumbnailFromMediaState (chatEntry.FirstContactCounterParty, this.Thumbnail);
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
				fromLabel.TextColor = color.GetColor ();
				thumbnailView.colorTheme = color;
			} else {
				UIView.Animate(iOS_Constants.FADE_ANIMATION_DURATION,
					() => {
						colorBand.Alpha = 0;
						fromLabel.Alpha = 0;
						thumbnailView.Alpha = 0;
					},
					() => UIView.Animate (iOS_Constants.FADE_ANIMATION_DURATION, () => {
						colorBand.BackgroundColor = color.GetColor ();
						colorBand.Alpha = 1;
						fromLabel.TextColor = color.GetColor ();
						fromLabel.Alpha = 1;
						thumbnailView.colorTheme = color;
						thumbnailView.Alpha = 1;
					}));
			}
		}

		public void UpdateAKAMask (BackgroundColor colorTheme) {
			if (chatEntry.HasAKAContact) {
				colorTheme.GetAKAMaskResource ((UIImage image) => {
					if (this.AkaMaskIconImageView != null) {
						this.AkaMaskIconImageView.Image = image;
					}
				});
			}
		}

		public void SetIcon(UIImage image) {
			SetIcon(image, false);
		}

		public void SetIcon(UIImage image, bool animated) {
			if ( !animated )
				aliasIconImageView.Image = image;
			else {
				UIView.Animate(iOS_Constants.FADE_ANIMATION_DURATION,
					() => {
						aliasIconImageView.Alpha = 0;
					},
					() => UIView.Animate (iOS_Constants.FADE_ANIMATION_DURATION, () => {
						aliasIconImageView.Alpha = 1;
						aliasIconImageView.Image = image;
					}));
			}
		}
	}
}
