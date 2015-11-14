using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using em;
using EMXamarin;

namespace Emdroid {
	class ContactListViewHolder: EMBitmapViewHolder {

		private ImageView photoFrame;
		private ImageView thumbnailView;
		private ImageView aliasIcon;
		private TextView displayNameTextView;
		private TextView descriptionTextView;
		private ProgressBar progressbar;

		public ImageView PhotoFrame {
			get { return photoFrame; }
			set { photoFrame = value; }
		}

		public ImageView ThumbnailView {
			get { return thumbnailView; }
			set { thumbnailView = value; }
		}

		public ImageView AliasIcon {
			get { return aliasIcon; }
			set { aliasIcon = value; }
		}

		public TextView DisplayNameTextView {
			get { return displayNameTextView; }
			set { displayNameTextView = value; }
		}

		public TextView DescriptionTextView {
			get { return descriptionTextView; }
			set { descriptionTextView = value; }
		}

		public ProgressBar ProgressBar {
			get { return progressbar; }
			set { progressbar = value; }
		}

		public CheckBox CheckBox { get; set; }

		CounterParty counterParty;

		public CounterParty CounterParty {
			get { return counterParty; }
			set {
				counterParty = value;
				if (counterParty != null) {
					displayNameTextView.Text = counterParty.displayName;
					displayNameTextView.Typeface = FontHelper.DefaultFont;
					displayNameTextView.SetTextColor (counterParty.colorTheme.GetColor ());

					if (counterParty.GetType () == typeof(Contact)) {
						var c = counterParty as Contact;
						if (c.preferred)
							descriptionTextView.Text = "";
						else
							descriptionTextView.Text = string.IsNullOrEmpty (c.label) ? c.description : string.Format ("{0} ({1})", c.description, c.label);
					} else
						descriptionTextView.Text = "";
					
					descriptionTextView.Typeface = FontHelper.DefaultFontItalic;
					descriptionTextView.SetTextColor (counterParty.colorTheme.GetColor ());
					counterParty.colorTheme.GetPhotoFrameLeftResource ((string file) => {
						if (photoFrame != null && EMApplication.Instance != null) {
							BitmapSetter.SetBackgroundFromFile (photoFrame, EMApplication.Instance.Resources, file);
						}
					});
				}
			}
		}

		public ContactListViewHolder() {}

		public void PossibleShowProgressIndicator (CounterParty c) {
			if (BitmapSetter.ShouldShowProgressIndicator (c)) {
				this.ProgressBar.Visibility = ViewStates.Visible;
				this.PhotoFrame.Visibility = ViewStates.Invisible;
				this.ThumbnailView.Visibility = ViewStates.Invisible;
			} else {
				this.ProgressBar.Visibility = ViewStates.Gone;
				this.PhotoFrame.Visibility = ViewStates.Visible;
				this.ThumbnailView.Visibility = ViewStates.Visible;
			}
		}

		public void PossibleShowAliasIcon(IList<Contact> contacts) {
			if(contacts != null && contacts.Count > 0) {
				foreach(Contact contact in contacts) {
					PossibleShowAliasIcon (contact);
				}
			}
		}

		public void PossibleShowAliasIcon(CounterParty counterparty) {
			if(counterparty != null) {
				if(counterparty is AccountInfo) {
					AliasIcon.Visibility = ViewStates.Invisible;
				} else if(counterparty is Contact) {
					var contact = counterparty as Contact;
					if (contact.fromAlias != null) {
						var alias = EMApplication.GetInstance ().appModel.account.accountInfo.AliasFromServerID (contact.fromAlias);
						if (alias != null) {
							AliasIcon.Visibility = ViewStates.Visible;
							BitmapSetter.SetImage (this, alias.iconMedia, EMApplication.Instance.Resources, AliasIcon, Resource.Drawable.Icon, 100);
						} else {
							AliasIcon.Visibility = ViewStates.Invisible;
						}
					} else {
						AliasIcon.Visibility = ViewStates.Invisible;
					}
				} else if(counterparty is AliasInfo) {
					var alias = counterparty as AliasInfo;
					AliasIcon.Visibility = ViewStates.Visible;
					BitmapSetter.SetImage (this, alias.iconMedia, EMApplication.Instance.Resources, AliasIcon, Resource.Drawable.Icon, 100);
				}
			}
		}

		public void PossibleHideDescription(AggregateContact contact) {
			//don't show description for preferred contacts, or an aggregate contact with multiple contacts
			if(contact != null) {
				if(!contact.SingleContact || contact.HasPreferredContact)
					DescriptionTextView.Visibility = ViewStates.Gone;
				else
					DescriptionTextView.Visibility = ViewStates.Visible;
			}
		}
	}
}