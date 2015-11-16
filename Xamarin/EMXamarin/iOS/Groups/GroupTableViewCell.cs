using System;
using CoreGraphics;
using em;
using Foundation;
using UIKit;

namespace iOS {
	using String_UIKit_Extension;

	public class GroupTableViewCell : UITableViewCell {

		public static readonly NSString Key = new NSString ("GroupTableViewCell");

		public Action SendButtonClickCallback { get; set; }

		readonly GroupThumbnailView thumbnailView;
		readonly UILabel groupNameLabel;
		readonly UIImageView aliasIconImageView;
		readonly UIButton sendButton;
		readonly UIView colorBand;
		readonly ThumbnailAnimationStrategy thumbnailAnimationStrategy;

		readonly int SEND_BUTTON_SIZE = 30;

		public GroupTableViewCell() : base (UITableViewCellStyle.Default, Key) {
			ContentView.AutosizesSubviews = true;

			thumbnailView = new GroupThumbnailView ();
			thumbnailView.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			ContentView.AddSubview (thumbnailView);

			thumbnailAnimationStrategy = new QueuedThumbnailAnimationStrategy (thumbnailView, thumbnailView.ThumbnailImageView);

			aliasIconImageView = new UIImageView ();
			aliasIconImageView.Layer.CornerRadius = iOS_Constants.DEFAULT_CORNER_RADIUS;
			aliasIconImageView.Layer.MasksToBounds = true;
			ContentView.AddSubview (aliasIconImageView);

			groupNameLabel = new UILabel ();
			groupNameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			groupNameLabel.Font = FontHelper.DefaultFontWithSize (UIFont.SystemFontSize);
			groupNameLabel.Lines = 2;
			ContentView.AddSubview (groupNameLabel);

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
				if (sendButton != null) {
					sendButton.SetBackgroundImage (image, UIControlState.Normal);
				}
			});
			sendButton.ImageView.ContentMode = UIViewContentMode.ScaleAspectFit;
			sendButton.Frame = new CGRect (0, 0, SEND_BUTTON_SIZE, SEND_BUTTON_SIZE);
			sendButton.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
			sendButton.TouchUpInside += DidTapSendButton;
			ContentView.AddSubview (sendButton);
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			thumbnailView.Frame = new CGRect (4, 8, 70, 60);

			colorBand.Frame = new CGRect (ContentView.Frame.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, ContentView.Frame.Height);

			// preview
			CGSize size = groupNameLabel.Text.SizeOfTextWithFontAndLineBreakMode (groupNameLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
			size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
			groupNameLabel.Frame = new CGRect (new CGPoint (80, (ContentView.Frame.Height - size.Height)/2), size);

			aliasIconImageView.Frame = new CGRect (new CGPoint (thumbnailView.Frame.X + thumbnailView.Frame.Width + 7, thumbnailView.Frame.Y + 2), new CGSize (15, 15));

			sendButton.Frame = new CGRect (new CGPoint (this.ContentView.Frame.Width - sendButton.Frame.Width - UI_CONSTANTS.EXTRA_MARGIN, this.ContentView.Frame.Height / 2 - sendButton.Frame.Height / 2), new CGSize (sendButton.Frame.Width, sendButton.Frame.Height));
		}

		public void DidTapSendButton (object sender, EventArgs e) {
			if (SendButtonClickCallback != null)
				SendButtonClickCallback ();
		}

		Contact grp;
		public Contact Group {
			get { return grp; }
			set {
				grp = value;
				if (grp != null) {
					// setting color
					colorBand.BackgroundColor = grp.colorTheme.GetColor ();
					thumbnailView.ColorTheme = grp.colorTheme;
					grp.colorTheme.GetChatSendButtonResource ((UIImage image) => {
						if (sendButton != null) {
							sendButton.SetBackgroundImage (image, UIControlState.Normal);
						}
					});

					SetGroupName (grp.displayName);

					ImageSetter.SetThumbnailImage (grp, iOS_Constants.DEFAULT_NO_IMAGE, SetThumbnail);

					ImageSetter.UpdateThumbnailFromMediaState (grp, thumbnailView);

					if(grp.fromAlias != null) {
						var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
						var alias = appDelegate.applicationModel.account.accountInfo.AliasFromServerID (grp.fromAlias);
						if(alias != null)
							ImageSetter.SetImage (alias.MediaForIcon, iOS_Constants.DEFAULT_ALIAS_ICON_IMAGE, SetIcon);
						else
							SetIcon (null);
					} else
						SetIcon (null);

					if (grp.me)
						sendButton.Hidden = true;
				}
				else {
					SetGroupName ("");
					SetThumbnail (null);
					SetIcon (null);
				}
			}
		}

		public static GroupTableViewCell Create () {
			return new GroupTableViewCell ();
		}

		public void SetEvenRow(bool isEven) {
			ContentView.BackgroundColor = isEven ? iOS_Constants.EVEN_COLOR : iOS_Constants.ODD_COLOR;
		}

		public void SetGroupName(string name) {
			SetGroupName(name, false);
		}

		public void SetGroupName(string name, bool animated) {
			if ( !animated )
				groupNameLabel.Text = name;
			else {
				bool animatePreview = !name.Equals(groupNameLabel.Text);
				if (animatePreview) {
					UIView.Animate (0.2,
						() => {
							if (animatePreview)
								groupNameLabel.Alpha = 0;
						},
						() => UIView.Animate (0.2, () => {
							if (animatePreview) {
								groupNameLabel.Text = name;
								groupNameLabel.Alpha = 1;
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

		public void SetIcon(UIImage image) {
			SetIcon(image, false);
		}

		public void SetIcon(UIImage image, bool animated) {
			if(image == null) {
				aliasIconImageView.Alpha = 0;
			} else {
				if (!animated) {
					aliasIconImageView.Image = image;
					aliasIconImageView.Alpha = 1;
				} else {
					UIView.Animate(0.2,
						() => {
							aliasIconImageView.Alpha = 0;
						},
						() => UIView.Animate (0.2, () => {
							aliasIconImageView.Alpha = 1;
							aliasIconImageView.Image = image;
						}));
				}
			}
		}

		public void SetAbandoned () {
			groupNameLabel.Text += " " + "LEFT_GROUP_EXTENSION".t ();
			groupNameLabel.Alpha = 0.5f;
		}
	}
}