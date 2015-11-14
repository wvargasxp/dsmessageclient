using System;
using EMXamarin;
using Android.Widget;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using em;

namespace Emdroid {
	public class AddressBookPickerAdapter : ArrayAdapter<Contact> {
		private IList<Contact> List { get; set; }

		public bool[] Chosen { get; set; }

		public AddressBookPickerAdapter (Context context, IList<Contact> list, bool[] chosen) 
			: base (context, Android.Resource.Layout.SimpleListItem1, list) {
			this.List = list;
			this.Chosen = chosen;

			foreach (CounterParty c in this.List) {
				NotificationCenter.DefaultCenter.AddAssociatedObserver (c, Constants.Counterparty_DownloadCompleted, (Notification n) => {
					EMTask.DispatchMain (() => {
						this.NotifyDataSetChanged ();
					});
				}, this);

				NotificationCenter.DefaultCenter.AddAssociatedObserver (c, Constants.Counterparty_ThumbnailChanged, (Notification z) => {
					EMTask.DispatchMain (() => {
						this.NotifyDataSetChanged ();
					});
				}, this);

				NotificationCenter.DefaultCenter.AddAssociatedObserver (c, Constants.Counterparty_DownloadFailed, (Notification gg) => {
					EMTask.DispatchMain (() => {
						this.NotifyDataSetChanged ();
					});
				}, this);
			}
		}

		public Contact ResultFromPosition (int position) {
			return this.List [position];
		}

		public override int Count {
			get {
				return this.List.Count;
			}
		}

		public override long GetItemId (int position) {
			return position;
		}

		protected override void Dispose (bool disposing) {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
			base.Dispose (disposing);
		}

		public override View GetDropDownView (int position, View convertView, ViewGroup parent) {
			return GetView (position, convertView, parent);
		}

		public override View GetView (int position, View convertView, ViewGroup parent) {
			View retVal = convertView;
			ContactListViewHolder holder;
			if (convertView == null) {
				retVal = LayoutInflater.From (this.Context).Inflate (Resource.Layout.contact_entry, parent, false);
				holder = new ContactListViewHolder ();
				holder.DisplayNameTextView = retVal.FindViewById<TextView> (Resource.Id.contactTextView);
				holder.DescriptionTextView = retVal.FindViewById<TextView> (Resource.Id.contactDescriptionView);
				holder.PhotoFrame = retVal.FindViewById<ImageView> (Resource.Id.photoFrame);
				holder.ThumbnailView = retVal.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				holder.AliasIcon = retVal.FindViewById<ImageView> (Resource.Id.aliasIcon);
				holder.ProgressBar = retVal.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
				holder.CheckBox = retVal.FindViewById<CheckBox> (Resource.Id.contactCheckBox);
				retVal.Tag = holder;
			} else {
				holder = (ContactListViewHolder)convertView.Tag;
			}

			holder.Position = position;
			CounterParty counterParty = this.List [position];

			holder.CounterParty = counterParty;

			BitmapSetter.SetThumbnailImage (holder, counterParty, this.Context.Resources, holder.ThumbnailView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
			holder.PossibleShowProgressIndicator (counterParty);
			BasicRowColorSetter.SetEven (position % 2 == 0, retVal);

			holder.PossibleShowAliasIcon (counterParty);
			UpdateCheckboxStateAtCheckbox (position, holder.CheckBox);

			return retVal;
		}

		public void UpdateCheckboxForContact (Contact contact, ListView listView) {
			int index = this.List.IndexOf (contact);
			UpdateCheckboxAtIndex (index, listView);
		}

		public void UpdateCheckboxAtIndex (int index, ListView listView) {
			View view = listView.GetChildAt (index - listView.FirstVisiblePosition);
			ContactListViewHolder holder = (ContactListViewHolder)view.Tag;
			CheckBox checkBox = null;
			if (holder == null) {
				checkBox = view.FindViewById<CheckBox> (Resource.Id.contactCheckBox);
			} else {
				checkBox = holder.CheckBox;
			}

			UpdateCheckboxStateAtCheckbox (index, checkBox);
		}

		private void UpdateCheckboxStateAtCheckbox (int position, CheckBox checkBox) {
			bool newValue = this.Chosen [position];
			checkBox.Checked = newValue;

		}
	}
}