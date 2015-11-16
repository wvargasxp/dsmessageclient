using CoreGraphics;
using EMXamarin;
using Foundation;
using UIKit;
using em;
using System;

namespace iOS {
	using String_UIKit_Extension;

	public class AddressBookContactTableViewCell : UITableViewCell {

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

		public AddressBookContactTableViewCell () : base (UITableViewCellStyle.Default, Key) {
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

			nfloat checkBoxXCoord = ContentView.Frame.Width - 10 - this.CheckBox.Frame.Width;
			const int xOffset = 80;

			if(string.IsNullOrEmpty(contactDescriptionLabel.Text) || (rolledUpContact != null && (!rolledUpContact.SingleContact || rolledUpContact.HasPreferredContact))) {
				// preview
				CGSize size = contactNameLabel.Text.SizeOfTextWithFontAndLineBreakMode (contactNameLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);

				if (xOffset + size.Width > checkBoxXCoord) {
					size.Width -= this.CheckBox.Frame.Width;
				}

				size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
				contactNameLabel.Frame = new CGRect (new CGPoint (xOffset, (ContentView.Frame.Height - size.Height)/2), size);
			} else {
				// preview
				CGSize size = contactNameLabel.Text.SizeOfTextWithFontAndLineBreakMode (contactNameLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
				size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
				contactNameLabel.Frame = new CGRect (new CGPoint (xOffset, ((ContentView.Frame.Height - size.Height)/2) - 10), size);

				size = contactDescriptionLabel.Text.SizeOfTextWithFontAndLineBreakMode (contactDescriptionLabel.Font, new CGSize (Bounds.Width - 110, 75), UILineBreakMode.Clip);
				size = new CGSize ((float)((int)(size.Width + 1.5)), (float)((int)(size.Height + 1.5)));
				contactDescriptionLabel.Frame = new CGRect (new CGPoint (xOffset, ((ContentView.Frame.Height - size.Height)/2)  + 10), size);
			}

			this.CheckBox.Frame = new CGRect (checkBoxXCoord, ContentView.Frame.Height / 2 - this.CheckBox.Frame.Height / 2, this.CheckBox.Frame.Width, this.CheckBox.Frame.Height);
		}

		AggregateContact rolledUpContact;
		public AggregateContact Contact {
			get { return rolledUpContact; }
			set {
				rolledUpContact = value;
				if (rolledUpContact != null) {
					Contact contact = rolledUpContact.ContactForDisplay;

					// setting color
					thumbnailView.ColorTheme = contact.colorTheme;

					SetContactName (contact.displayName);
					SetContactLabelColor (contact.colorTheme);
					SetContactDescription (rolledUpContact);

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

		public void UpdateCheckBox (bool showCheckMark) {
			this.CheckBox.ShouldDrawBorder = true;
			if (showCheckMark) {
				this.CheckBox.IsOn = true;
			} else {
				this.CheckBox.IsOn = false;
			}
		}

		public static AddressBookContactTableViewCell Create () {
			return new AddressBookContactTableViewCell ();
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

		public void SetContactDescription(AggregateContact c) {
			SetContactDescription (c, false);
		}

		public void SetContactDescription (AggregateContact c, bool animated) {
			if(c != null) {
				//don't show descriptions for preferred contacts
				if (!c.SingleContact || c.HasPreferredContact) {
					contactDescriptionLabel.Alpha = 0;
					return;
				}

				if ( !animated ) {
					contactDescriptionLabel.Text = c.ContactForDisplay.description;
					contactDescriptionLabel.TextColor = c.ContactForDisplay.colorTheme.GetColor ();
					contactDescriptionLabel.Alpha = 1;
				} else {
					bool animatePreview = !c.ContactForDisplay.description.Equals(contactDescriptionLabel.Text);
					if (animatePreview) {
						UIView.Animate (0.2,
							() => {
								if (animatePreview)
									contactDescriptionLabel.Alpha = 0;
							},
							() => UIView.Animate (0.2, () => {
								if (animatePreview) {
									contactDescriptionLabel.Text = c.ContactForDisplay.description;
									contactDescriptionLabel.TextColor = c.ContactForDisplay.colorTheme.GetColor ();
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