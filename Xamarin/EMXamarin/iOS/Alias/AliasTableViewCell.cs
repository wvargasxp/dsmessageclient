using System;
using CoreGraphics;
using em;
using Foundation;
using UIKit;

namespace iOS {
	using String_UIKit_Extension;

	public class AliasTableViewCell : UITableViewCell {

		public static readonly NSString Key = new NSString ("AliasTableViewCell");

		public Action<int> ShareClickCallback { get; set; }

		readonly AliasThumbnailView thumbnailView;
		readonly UIImageView aliasIconImageView;
		readonly UILabel aliasNameLabel, shareButtonLabel;
		readonly UIButton shareButton;
		readonly UIView colorBand;
		ThumbnailAnimationStrategy thumbnailAnimationStrategy;

		public AliasTableViewCell() : base (UITableViewCellStyle.Default, Key) {
			ContentView.AutosizesSubviews = true;

			thumbnailView = new AliasThumbnailView ();
			thumbnailView.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			ContentView.AddSubview (thumbnailView);

			thumbnailAnimationStrategy = new QueuedThumbnailAnimationStrategy (thumbnailView, thumbnailView.ThumbnailImageView);

			aliasNameLabel = new UILabel ();
			aliasNameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			aliasNameLabel.Font = FontHelper.DefaultFontWithSize (UIFont.SystemFontSize);
			aliasNameLabel.Lines = 2;
			ContentView.AddSubview (aliasNameLabel);

			shareButton = new UIButton (new CGRect (0, 0, 35, 35));
			shareButton.TouchUpInside += DidTapShare;
			ContentView.Add (shareButton);

			shareButtonLabel = new UILabel (new CGRect(0, 0, Bounds.Size.Width, 20));
			shareButtonLabel.Font = FontHelper.DefaultItalicFont (9f);
			shareButtonLabel.Text = "SHARE_PROFILE".t ();
			shareButtonLabel.TextColor = iOS_Constants.BLACK_COLOR;
			ContentView.Add (shareButtonLabel);

			aliasIconImageView = new UIImageView ();
			aliasIconImageView.Layer.CornerRadius = iOS_Constants.DEFAULT_CORNER_RADIUS;
			aliasIconImageView.Layer.MasksToBounds = true;

			ContentView.AddSubview (aliasIconImageView);

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

			// preview
			CGSize size = aliasNameLabel.Text.SizeOfTextWithFontAndLineBreakMode (aliasNameLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
			size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
			aliasNameLabel.Frame = new CGRect (new CGPoint (80, (ContentView.Frame.Height - size.Height)/2), size);

			aliasIconImageView.Frame = new CGRect (new CGPoint (thumbnailView.Frame.X + thumbnailView.Frame.Width + 7, thumbnailView.Frame.Y + 2), new CGSize (15, 15));

			shareButton.Frame = new CGRect (ContentView.Frame.Width - iOS_Constants.COLOR_BAND_WIDTH - shareButton.Frame.Width - 20, 15, shareButton.Frame.Width, shareButton.Frame.Height);

			CGSize sizeSBL = shareButtonLabel.Text.SizeOfTextWithFontAndLineBreakMode (shareButtonLabel.Font, new CGSize (UIScreen.MainScreen.Bounds.Width, 20), UILineBreakMode.Clip);
			sizeSBL = new CGSize ((float)((int)(sizeSBL.Width + 1.5)), (float)((int)(sizeSBL.Height + 1.5)));
			var xCoordSBL = (ContentView.Frame.Width - (shareButton.Frame.Width + iOS_Constants.COLOR_BAND_WIDTH + 20) + (shareButton.Frame.Width / 2)) - (sizeSBL.Width / 2);
			shareButtonLabel.Frame = new CGRect (new CGPoint(xCoordSBL, shareButton.Frame.Y + shareButton.Frame.Height + 5), sizeSBL);

			colorBand.Frame = new CGRect (ContentView.Frame.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, ContentView.Frame.Height);
		}

		public void DidTapShare(object sender, EventArgs e) {
			if(ShareClickCallback != null)
				ShareClickCallback ((int) Tag);
		}

		AliasInfo ai;
		public AliasInfo Alias {
			get {
				return ai;
			}

			set {
				ai = value;
				if (ai != null) {
					if (ai.lifecycle != ContactLifecycle.Active) {
						ContentView.Alpha = 0.3f;
						shareButton.Enabled = false;
						shareButton.Alpha = 0;
						shareButtonLabel.Alpha = 0;
					} else {
						ContentView.Alpha = 1;
						shareButton.Enabled = true;
						shareButton.Alpha = 1;
						shareButtonLabel.Alpha = 1;
					}

					// setting color
					colorBand.BackgroundColor = ai.colorTheme.GetColor ();
					thumbnailView.ColorTheme = ai.colorTheme;

					SetAliasName (ai.displayName);

					ImageSetter.SetThumbnailImage (ai, iOS_Constants.DEFAULT_NO_IMAGE, (UIImage loadedImage) => {
						SetThumbnail (loadedImage);
					});

					ImageSetter.UpdateThumbnailFromMediaState (ai, thumbnailView);

					ImageSetter.SetImage (ai.MediaForIcon, iOS_Constants.DEFAULT_ALIAS_ICON_IMAGE, (UIImage loadedImage) => {
						SetIcon (loadedImage);
					});

					SetShareButton (ai.colorTheme);
				}
				else {
					SetAliasName ("");
					SetThumbnail (null);
					SetIcon (null);
					SetShareButton (em.BackgroundColor.Default);
				}
			}
		}

		public static AliasTableViewCell Create () {
			return new AliasTableViewCell ();
		}

		public void SetEvenRow(bool isEven) {
			ContentView.BackgroundColor = isEven ? iOS_Constants.EVEN_COLOR : iOS_Constants.ODD_COLOR;
		}

		public void SetAliasName(string name) {
			SetAliasName(name, false);
		}

		public void SetAliasName(string name, bool animated) {
			if ( !animated )
				aliasNameLabel.Text = name;
			else {
				bool animatePreview = !name.Equals(aliasNameLabel.Text);
				if (animatePreview) {
					UIView.Animate (0.2,
						() => {
							if (animatePreview)
								aliasNameLabel.Alpha = 0;
						},
						() => UIView.Animate (0.2, () => {
							if (animatePreview) {
								aliasNameLabel.Text = name;
								aliasNameLabel.Alpha = 1;
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
			if ( !animated )
				aliasIconImageView.Image = image;
			else {
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

		public void SetShareButton(BackgroundColor colorTheme) {
			SetShareButton (colorTheme, false);
		}

		public void SetShareButton(BackgroundColor colorTheme, bool animated) {
			if (!animated) {
				colorTheme.GetShareImageResource ((UIImage image) => {
					if (shareButton != null) {
						shareButton.SetBackgroundImage (image, UIControlState.Normal);
					}
				});
			} else {
				UIView.Animate(0.2,
					() => {
						shareButton.Alpha = 0;
					},
					() => UIView.Animate (0.2, () => {
						shareButton.Alpha = 1;
						colorTheme.GetShareImageResource ( (UIImage image) => {
							if (shareButton != null) {
								shareButton.SetBackgroundImage (image, UIControlState.Normal);
							}
						});
				}));
			}
		}
	}
}