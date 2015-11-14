using System;
using EMXamarin;
using Android.Widget;
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using em;

namespace Emdroid {
	public class AliasPickerAdapter : BaseAdapter<AliasInfo>, ISpinnerAdapter {
		IList<AliasInfo> aliasList;
		protected IList<AliasInfo> AliasList {
			get { return aliasList; }
			set { aliasList = value; }
		}

		Context context;
		protected Context Context {
			get { return context; }
			set { context = value; }
		}

		public AliasPickerAdapter (Context context, int resource, IList<AliasInfo> aliasList) {
			this.Context = context;
			this.AliasList = aliasList;

			foreach (CounterParty c in this.AliasList) {
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

		public AliasInfo ResultFromPosition (int position) {
			// After user makes a selection, take the position, find the contact and return a dictionary with server id.
			if (position == this.AliasList.Count)
				return null; // using default name
			AliasInfo aliasInfo = this.AliasList [position];
			return aliasInfo;
		}

		public override int Count {
			get {
				return this.AliasList.Count + 1; // account for the default name
			}
		}

		public override long GetItemId (int position) {
			return position;
		}

		protected override void Dispose (bool disposing) {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
			base.Dispose (disposing);
		}

		public override View GetView (int position, View convertView, ViewGroup parent) {
			View retVal = convertView;
			ContactListViewHolder holder;
			if (convertView == null) {
				retVal = LayoutInflater.From (context).Inflate (Resource.Layout.contact_entry, parent, false);
				holder = new ContactListViewHolder ();
				holder.DisplayNameTextView = retVal.FindViewById<TextView> (Resource.Id.contactTextView);
				holder.DescriptionTextView = retVal.FindViewById<TextView> (Resource.Id.contactDescriptionView);
				holder.PhotoFrame = retVal.FindViewById<ImageView> (Resource.Id.photoFrame);
				holder.ThumbnailView = retVal.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				holder.AliasIcon = retVal.FindViewById<ImageView> (Resource.Id.aliasIcon);
				holder.ProgressBar = retVal.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
				holder.CheckBox = retVal.FindViewById<CheckBox> (Resource.Id.contactCheckBox);

				// We're not displaying a checkbox for from alias picker.
				holder.CheckBox.Visibility = ViewStates.Gone;
				retVal.Tag = holder;
			} else {
				holder = (ContactListViewHolder)convertView.Tag;
			}

			holder.Position = position;
			CounterParty counterParty;
			if (position == this.AliasList.Count)
				counterParty = EMApplication.Instance.appModel.account.accountInfo;
			else
				counterParty = this.AliasList [position];
				
			holder.CounterParty = counterParty;

			BitmapSetter.SetThumbnailImage (holder, counterParty, context.Resources, holder.ThumbnailView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
			holder.PossibleShowProgressIndicator (counterParty);
			BasicRowColorSetter.SetEven (position % 2 == 0, retVal);

			holder.PossibleShowAliasIcon (counterParty);

			return retVal;
		}
			
		public override View GetDropDownView (int position, View convertView, ViewGroup parent) {
			return GetView (position, convertView, parent);
		}

		public override AliasInfo this [int index] { 
			get { 
				if (index == aliasList.Count)
					return null;
				else
					return aliasList [index];
			}
		}
	}

}