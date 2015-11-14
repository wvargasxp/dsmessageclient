using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;
using Com.Nhaarman.Listviewanimations.Itemmanipulation;
using em;
using EMXamarin;

namespace Emdroid {
	public class AggregateContactSelectionFragment : BasicAccountFragment {

		readonly ApplicationModel appModel;

		public Action<AddressBookSelectionResult> CompletionCallback { get; set; }

		AggregateContactListAdapter aggregateContactListAdapter = null;
		public AggregateContactListAdapter ListAdapter {
			get { return aggregateContactListAdapter; }
			set { aggregateContactListAdapter = value; }
		}

		AggregateContact c;
		protected AggregateContact AggregateContact {
			get { return c; }
			set { c = value; }
		}

		#region UI
		Button leftBarButton;
		TextView nameText;
		DynamicListView listView = null;
		#endregion

		public static AggregateContactSelectionFragment NewInstance (AggregateContact contact) {
			return new AggregateContactSelectionFragment (contact);
		}

		public AggregateContactSelectionFragment(AggregateContact ac) {
			appModel = EMApplication.GetInstance ().appModel;
			AggregateContact = ac;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate (Resource.Layout.aggregatecontactselection, container, false);
			EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme.GetBackgroundResource ((string file) => {
				if (v != null && this.Resources != null) {
					BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
				}
			});

			#region setting the title bar 
			RelativeLayout titlebarLayout = v.FindViewById<RelativeLayout> (Resource.Id.titlebarlayout);
			TextView titleTextView = titlebarLayout.FindViewById<TextView> (Resource.Id.titleTextView);
			titleTextView.Typeface = FontHelper.DefaultFont;
			titleTextView.Text = AggregateContact.DisplayName;

			leftBarButton = titlebarLayout.FindViewById<Button> (Resource.Id.leftBarButton);
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);
			leftBarButton.Click += (object sender, EventArgs e) =>  {
				CompletionCallback = null;
				this.FragmentManager.PopBackStack ();
			};

			ThumbnailBackgroundView = v.FindViewById<ImageView> (Resource.Id.ThumbnailBackgroundView);
			ThumbnailButton = v.FindViewById<ImageView> (Resource.Id.ThumbnailButton);
			ProgressBar = v.FindViewById<ProgressBar> (Resource.Id.ProgressBar);

			#endregion
			return v;
		}

		public override void OnResume () {
			base.OnResume ();
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			nameText = View.FindViewById<TextView> (Resource.Id.NameText);
			nameText.Typeface = FontHelper.DefaultFont;
			nameText.Text = AggregateContact.DisplayName;

			listView = View.FindViewById<DynamicListView> (Resource.Id.ContactsList);
			listView.FastScrollEnabled = true;
			listView.ItemClick += DidTapItem;
			this.ListAdapter = new AggregateContactListAdapter (this, Android.Resource.Layout.SimpleListItem1);
			listView.Adapter = this.ListAdapter;

			ThemeController ();
			UpdateThumbnailPicture ();

			AnalyticsHelper.SendView ("Contact Selection View");
		}

		public override void OnPause () {
			base.OnPause ();
		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnStop () {
			base.OnStop ();
		}

		public override void OnDetach () {
			base.OnDetach ();
		}

		public override void OnDestroy () {
			base.OnDestroy ();
		}

		#region basic account 
		public override BackgroundColor ColorTheme { 
			get { return AggregateContact.ContactForDisplay.colorTheme; }
		}

		public override CounterParty CounterParty { 
			get { return AggregateContact.ContactForDisplay; }
		}

		// AggregateContactSelectionFragment doesn't have an edit text, so this field isn't needed.
		public override string TextInDisplayField {
			get { return string.Empty; }
			set { value = null; }
		}

		public override void LeftBarButtonClicked (object sender, EventArgs e) {
			this.FragmentManager.PopBackStackImmediate ();
		}

		public override void RightBarButtonClicked (object sender, EventArgs e) {}
		public override void ColorThemeSpinnerItemClicked (object sender, AdapterView.ItemSelectedEventArgs e) {}
		public override void AdditionalUIChangesOnResume () {}
		public override void AdditionalThemeController () {}
		public override string ImageSearchSeedString {
			get { return string.Empty; } // no image serach
		}
		#endregion

		protected void DidTapItem(object sender, AdapterView.ItemClickEventArgs e) {
			Contact contact = this.AggregateContact.Contacts [e.Position];

			IList<Contact> contacts = new List<Contact> ();
			contacts.Add (contact);
			AddressBookSelectionResult result = new AddressBookSelectionResult (contacts);
			CompletionCallback (result);

			// x2 to get back to chat
			this.FragmentManager.PopBackStack ();
			this.FragmentManager.PopBackStack (); 
		}

		public class AggregateContactListAdapter : ArrayAdapter<Contact>  {

			WeakReference fragmentRef;

			public AggregateContactListAdapter (AggregateContactSelectionFragment g, int resource) : base (g.Activity, resource, new List<Contact>()) {
				// Adapter requires a list in its constructor, so pass in an empty contact list and then load the contacts.
				fragmentRef = new WeakReference (g);
			}

			public IList<Contact> ContactList { 
				get { 
					var fragment = (AggregateContactSelectionFragment)fragmentRef.Target;
					return fragment != null ? fragment.AggregateContact.Contacts : null;
				}
			}

			public Contact ResultFromPosition (int position) {
				Contact contact = this.ContactList [position];
				return contact;
			}

			public override int Count {
				get { return this.ContactList == null ? 0 : this.ContactList.Count; }
			}

			public override long GetItemId(int position) {
				return position;
			}

			public override View GetView (int position, View convertView, ViewGroup parent) {
				View retVal = convertView;
				AggregateContactViewHolder holder;

				var fragment = (AggregateContactSelectionFragment)fragmentRef.Target;
				if (fragment == null)
					return retVal;

				if (convertView == null) {
					retVal = LayoutInflater.From (fragment.Activity).Inflate (Resource.Layout.aggregate_contact_entry, parent, false);
					holder = new AggregateContactViewHolder ();
					holder.DescriptionTextView = retVal.FindViewById<TextView> (Resource.Id.descriptionTextView);
					holder.PhotoFrame = retVal.FindViewById<ImageView> (Resource.Id.photoFrame);
					holder.ThumbnailView = retVal.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
					holder.ProgressBar = retVal.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
					holder.SendButton = retVal.FindViewById<ImageView> (Resource.Id.sendButton);
					holder.ColorTrimView = retVal.FindViewById<View> (Resource.Id.trimView);
					retVal.Tag = holder;
				} else {
					holder = (AggregateContactViewHolder)convertView.Tag;
				}

				holder.Position = position;

				Contact contact = this.ContactList [position];
				holder.C = contact;

				BitmapSetter.SetThumbnailImage (holder, contact, fragment.Activity.Resources, holder.ThumbnailView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
				holder.PossibleShowProgressIndicator (contact);

				BasicRowColorSetter.SetEven (position % 2 == 0, retVal);

				return retVal;
			}
		}
	}
}