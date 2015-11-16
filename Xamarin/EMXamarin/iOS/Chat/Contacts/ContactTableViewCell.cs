using CoreGraphics;
using EMXamarin;
using Foundation;
using UIKit;
using em;

namespace iOS {
	using String_UIKit_Extension;

	public class ContactTableViewCell : UITableViewCell {

		public static readonly NSString Key = new NSString ("ContactTableViewCell");

		readonly ContactThumbnailView thumbnailView;
		readonly UIImageView aliasIconImageView;
		readonly UILabel contactNameLabel;
		readonly UILabel contactDescriptionLabel;
		readonly ThumbnailAnimationStrategy thumbnailAnimationStrategy;

		private CheckboxView CheckBox { get; set; }

		bool disabled;
		public bool Disabled {
			get { return disabled; }
			set {
				disabled = value;
				this.UserInteractionEnabled = !disabled;
				this.ContentView.Alpha = disabled ? .4f : 1f;
			}
		}

		public ContactTableViewCell () : base (UITableViewCellStyle.Default, Key) {
			ContentView.AutosizesSubviews = true;

			thumbnailView = new ContactThumbnailView ();
			thumbnailView.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			ContentView.AddSubview (thumbnailView);

			thumbnailAnimationStrategy = new QueuedThumbnailAnimationStrategy (thumbnailView, thumbnailView.ThumbnailImageView);

			aliasIconImageView = new UIImageView ();
			aliasIconImageView.Layer.CornerRadius = iOS_Constants.DEFAULT_CORNER_RADIUS;
			aliasIconImageView.Layer.MasksToBounds = true;
			ContentView.AddSubview (aliasIconImageView);

			contactNameLabel = new UILabel ();
			contactNameLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			contactNameLabel.Font = FontHelper.DefaultFontWithSize (UIFont.SystemFontSize);
			contactNameLabel.Lines = 2;
			ContentView.AddSubview (contactNameLabel);

			contactDescriptionLabel = new UILabel ();
			contactDescriptionLabel.AutoresizingMask = UIViewAutoresizing.FlexibleRightMargin;
			contactDescriptionLabel.Font = FontHelper.DefaultItalicFont(UIFont.SmallSystemFontSize);
			contactDescriptionLabel.Lines = 2;
			ContentView.AddSubview (contactDescriptionLabel);

			CGRect contentViewBounds = ContentView.Bounds;
			var bottomSeparatorLine = new UIView(new CGRect(0, contentViewBounds.Size.Height - 1, contentViewBounds.Size.Width, 1));
			bottomSeparatorLine.BackgroundColor = iOS_Constants.INBOX_ROW_SEPERATOR_COLOR;
			bottomSeparatorLine.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleWidth;
			ContentView.AddSubview (bottomSeparatorLine);

			this.CheckBox = new CheckboxView (new CGRect (0, 0, CheckboxView.DefaultCheckBoxSize, CheckboxView.DefaultCheckBoxSize));
			this.ContentView.Add (this.CheckBox);
			this.ContentView.BringSubviewToFront (CheckBox);
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();

			thumbnailView.Frame = new CGRect (4, 8, 70, 60);

			aliasIconImageView.Frame = new CGRect (new CGPoint (thumbnailView.Frame.X + thumbnailView.Frame.Width + 7, thumbnailView.Frame.Y + 2), new CGSize (15, 15));

			if(string.IsNullOrEmpty(contactDescriptionLabel.Text) || (Contact != null && Contact.preferred)) {
				CGSize size = contactNameLabel.Text.SizeOfTextWithFontAndLineBreakMode (contactNameLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
				size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
				contactNameLabel.Frame = new CGRect (new CGPoint (80, (ContentView.Frame.Height - size.Height)/2), size);
			} else {
				CGSize size = contactNameLabel.Text.SizeOfTextWithFontAndLineBreakMode (contactNameLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
				size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
				contactNameLabel.Frame = new CGRect (new CGPoint (80, ((ContentView.Frame.Height - size.Height)/2) - 10), size);

				size = contactDescriptionLabel.Text.SizeOfTextWithFontAndLineBreakMode (contactDescriptionLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
				size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
				contactDescriptionLabel.Frame = new CGRect (new CGPoint (80, ((ContentView.Frame.Height - size.Height)/2)  + 10), size);
			}

			this.CheckBox.Frame = new CGRect (ContentView.Frame.Width - 10 - this.CheckBox.Frame.Width, ContentView.Frame.Height / 2 - this.CheckBox.Frame.Height / 2, this.CheckBox.Frame.Width, this.CheckBox.Frame.Height);
		}

		public void UpdateCheckBox (bool showCheckMark) {
			this.CheckBox.ShouldDrawBorder = true;
			if (showCheckMark) {
				this.CheckBox.IsOn = true;
			} else {
				this.CheckBox.IsOn = false;
			}
		}

		Contact contact;
		public Contact Contact {
			get { return contact; }
			set {
				contact = value;
				if (contact != null) {
					// setting color
					thumbnailView.ColorTheme = contact.colorTheme;

					SetContactName (contact.displayName);
					SetContactLabelColor (contact.colorTheme);
					SetContactDescription (contact);
				
					ImageSetter.SetThumbnailImage (contact, iOS_Constants.DEFAULT_NO_IMAGE, (UIImage loadedImage) => {
						SetThumbnail (loadedImage);
					});

					ImageSetter.UpdateThumbnailFromMediaState (contact, thumbnailView);

					if(contact.fromAlias != null) {
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
				}
				else {
					SetContactName ("");
					SetContactDescription (null);
					SetThumbnail (null);
					SetIcon (null);
				}
			}
		}

		public static ContactTableViewCell Create () {
			return new ContactTableViewCell ();
		}

		public void SetEvenRow(bool isEven) {
			ContentView.BackgroundColor = isEven ? iOS_Constants.EVEN_COLOR : iOS_Constants.ODD_COLOR;
		}

		public void SetContactName (string name) {
			SetContactName (name, false);
		}

		public void SetContactName (string name, bool animated) {
			if ( !animated )
				contactNameLabel.Text = name;
			else {
				bool animatePreview = !name.Equals(contactNameLabel.Text);
				if (animatePreview) {
					UIView.Animate (0.2,
						() => {
							if (animatePreview)
								contactNameLabel.Alpha = 0;
						},
						() => UIView.Animate (0.2, () => {
							if (animatePreview) {
								contactNameLabel.Text = name;
								contactNameLabel.Alpha = 1;
							}
							SetNeedsLayout ();
						}));
				}
			}
		}

		public void SetContactLabelColor (BackgroundColor color) {
			contactNameLabel.TextColor = color.GetColor ();
		}

		public void SetContactDescription(Contact c) {
			SetContactDescription (c, false);
		}

		public void SetContactDescription (Contact c, bool animated) {
			if(c != null) {
				//don't show descriptions for preferred contacts
				if (c.preferred) {
					contactDescriptionLabel.Alpha = 0;
					return;
				}

				string text = string.IsNullOrEmpty (c.label) ? c.description : string.Format ("{0} ({1})", c.description, c.label);
				
				if ( !animated ) {
					contactDescriptionLabel.Text = text;
					contactDescriptionLabel.TextColor = c.colorTheme.GetColor ();
					contactDescriptionLabel.Alpha = 1;
				} else {
					bool animatePreview = !text.Equals(contactDescriptionLabel.Text);
					if (animatePreview) {
						UIView.Animate (0.2,
							() => {
								if (animatePreview)
									contactDescriptionLabel.Alpha = 0;
							},
							() => UIView.Animate (0.2, () => {
								if (animatePreview) {
									contactDescriptionLabel.Text = text;
									contactDescriptionLabel.TextColor = c.colorTheme.GetColor ();
									contactDescriptionLabel.Alpha = 1;
								}
								SetNeedsLayout ();
							}));
					}
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