using CoreGraphics;
using Foundation;
using UIKit;
using em;
using EMXamarin;

namespace iOS {
	using String_UIKit_Extension;

	public class ProfileTableViewCell : UITableViewCell {

		public static readonly NSString Key = new NSString ("ProfileTableViewCell");

		readonly ProfileThumbnailView thumbnailView;
		readonly UILabel nameLabel;
		readonly UIImageView aliasIconImageView;
		readonly UIView colorBand;
		readonly ThumbnailAnimationStrategy thumbnailAnimationStrategy;

		public ProfileTableViewCell() : base (UITableViewCellStyle.Default, Key) {
			ContentView.AutosizesSubviews = true;

			thumbnailView = new ProfileThumbnailView ();
			thumbnailView.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			ContentView.AddSubview (thumbnailView);

			thumbnailAnimationStrategy = new QueuedThumbnailAnimationStrategy (thumbnailView, thumbnailView.ThumbnailImageView);

			aliasIconImageView = new UIImageView ();
			aliasIconImageView.Layer.CornerRadius = iOS_Constants.DEFAULT_CORNER_RADIUS;
			aliasIconImageView.Layer.MasksToBounds = true;
			ContentView.AddSubview (aliasIconImageView);

			nameLabel = new UILabel ();
			nameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			nameLabel.Font = FontHelper.DefaultFontWithSize (UIFont.SystemFontSize);
			nameLabel.Lines = 2;
			ContentView.AddSubview (nameLabel);

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

			colorBand.Frame = new CGRect (ContentView.Frame.Width - iOS_Constants.COLOR_BAND_WIDTH, 0, iOS_Constants.COLOR_BAND_WIDTH, ContentView.Frame.Height);

			aliasIconImageView.Frame = new CGRect (new CGPoint (thumbnailView.Frame.X + thumbnailView.Frame.Width + 7, thumbnailView.Frame.Y + 2), new CGSize (15, 15));

			// preview
			CGSize size = nameLabel.Text.SizeOfTextWithFontAndLineBreakMode (nameLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
			size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
			nameLabel.Frame = new CGRect (new CGPoint (80, (ContentView.Frame.Height - size.Height)/2), size);
		}

		CounterParty cp;
		public CounterParty counterparty {
			get {
				return cp;
			}

			set {
				cp = value;
				if (cp != null) {
					// setting color
					colorBand.BackgroundColor = cp.colorTheme.GetColor ();
					thumbnailView.ColorTheme = cp.colorTheme;

					SetName (cp.displayName);
					ImageSetter.SetThumbnailImage (cp, iOS_Constants.DEFAULT_NO_IMAGE, (UIImage loadedImage) => {
						SetThumbnail (loadedImage);
					});

					if(cp is Contact) {
						var contact = cp as Contact;
						if(contact != null && contact.fromAlias != null) {
							var appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
							var alias = appDelegate.applicationModel.account.accountInfo.AliasFromServerID (contact.fromAlias);
							if(alias != null) {
								ImageSetter.SetImage (alias.MediaForIcon, iOS_Constants.DEFAULT_ALIAS_ICON_IMAGE, (UIImage loadedImage) => {
									SetIcon (loadedImage);
								});
							} else {
								SetIcon (null);
							}
						} else {
							SetIcon (null);
						}
					} else if(cp is AliasInfo) {
						var alias = cp as AliasInfo;
						if(alias != null) {
							ImageSetter.SetImage (alias.MediaForIcon, iOS_Constants.DEFAULT_ALIAS_ICON_IMAGE, (UIImage loadedImage) => {
								SetIcon (loadedImage);
							});
						} else {
							SetIcon (null);
						}
					}
				}
				else {
					SetName ("");
					SetThumbnail (null);
					SetIcon (null);
				}
			}
		}

		public static ProfileTableViewCell Create () {
			return new ProfileTableViewCell ();
		}

		public void SetEvenRow(bool isEven) {
			ContentView.BackgroundColor = isEven ? iOS_Constants.EVEN_COLOR : iOS_Constants.ODD_COLOR;
		}

		public void SetName(string name) {
			SetName(name, false);
		}

		public void SetName(string name, bool animated) {
			if ( !animated )
				nameLabel.Text = name;
			else {
				bool animatePreview = !name.Equals(nameLabel.Text);
				if (animatePreview) {
					UIView.Animate (0.2,
						() => {
							if (animatePreview)
								nameLabel.Alpha = 0;
						},
						() => UIView.Animate (0.2, () => {
							if (animatePreview) {
								nameLabel.Text = name;
								nameLabel.Alpha = 1;
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
		}
	}
}