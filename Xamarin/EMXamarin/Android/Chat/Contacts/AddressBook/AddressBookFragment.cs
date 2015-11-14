using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Com.Nhaarman.Listviewanimations.Itemmanipulation;
using em;

namespace Emdroid {
	public class AddressBookFragment : Fragment, IMultiSpinnerListener, IContactSearchController {

		public Action<AddressBookSelectionResult> CompletionCallback { get; set; }

		#region UI
		private DynamicListView listView = null;
		private ContactSearchEditText searchBar = null;
		private Button leftBarButton = null;
		private Button RightBarButton { get; set; }
		private MultiSpinner Spinner { get; set; }
		#endregion

		private AddressBookListAdapter contactListAdapter = null;
		protected AddressBookListAdapter ContactListAdapter {
			get { return contactListAdapter; }
			set { contactListAdapter = value; }
		}

		protected AddressBookArgs Args { get; set; }

		public IList<AggregateContact> ContactList { 
			get { return Shared.ContactList; }
			set { Shared.ContactList = value; } 
		}

		public Context Context {
			get { return this.Activity; }
		}

		private HiddenReference<SharedAddressBookController> _shared;
		public SharedAddressBookController Shared { 
			get { return this._shared.Value; }
			set { this._shared = new HiddenReference<SharedAddressBookController> (value); }
		}

		public static AddressBookFragment NewInstance (AddressBookArgs args) {
			var fragment = new AddressBookFragment ();
			fragment.Args = args;
			return fragment;
		}

		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
			// Create your fragment here
			this.Shared = new SharedAddressBookController (this, EMApplication.GetInstance ().appModel);
			this.Shared.SetInitialSelectedContacts (this.Args.Contacts);
		}

		public override void OnResume () {
			base.OnResume ();

			UpdateToField ();
		}

		public override void OnDestroy() {
			if ( Shared != null )
				Shared.Dispose (true);
			base.OnDestroy ();
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate (Resource.Layout.contacts, container, false);
			this.RightBarButton = v.FindViewById<Button> (Resource.Id.rightBarButton);
			this.RightBarButton.Text = "DONE_BUTTON".t ();
			this.RightBarButton.Click += WeakDelegateProxy.CreateProxy<object, EventArgs> (DidTapDoneButton).HandleEvent<object, EventArgs>;
			this.RightBarButton.Typeface = FontHelper.DefaultFont;
			this.Spinner = v.FindViewById<MultiSpinner> (Resource.Id.multi_spinner);
			ThemeController (v);
			return v;
		}

		public void ThemeController () {
			ThemeController (this.View);
		}

		public void ThemeController (View v) {
			if (this.IsAdded && v != null) {
				EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme.GetBackgroundResource ((string file) => {
					if (v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
					}
				});
			}
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			this.View.FindViewById<TextView> (Resource.Id.titleTextView).Text = "CONTACTS_TITLE".t ();
			this.View.FindViewById<TextView> (Resource.Id.titleTextView).Typeface = FontHelper.DefaultFont;

			this.searchBar = this.View.FindViewById<ContactSearchEditText> (Resource.Id.searchBar);
			this.searchBar.SetParent (this);
			this.searchBar.EditorAction += WeakDelegateProxy.CreateProxy<object, TextView.EditorActionEventArgs> (HandleSearchAction).HandleEvent<object, TextView.EditorActionEventArgs>;
			this.searchBar.Hint = "SEARCH_TITLE".t ();
			this.searchBar.SetHintTextColor (Android.Graphics.Color.Gray);

			this.listView = this.View.FindViewById<DynamicListView> (Resource.Id.ContactsList);
			this.listView.FastScrollEnabled = true;
			this.listView.ChoiceMode = ChoiceMode.Multiple;
			this.listView.ItemClick += WeakDelegateProxy.CreateProxy<object, AdapterView.ItemClickEventArgs> (DidTapItem).HandleEvent<object, AdapterView.ItemClickEventArgs>;

			this.leftBarButton = this.View.FindViewById<Button> (Resource.Id.leftBarButton);
			this.leftBarButton.Click += WeakDelegateProxy.CreateProxy<object, EventArgs> (HandleBackButtonPressed).HandleEvent<object, EventArgs>;
			ViewClickStretchUtil.StretchRangeOfButton (this.leftBarButton);

			this.ContactListAdapter = new AddressBookListAdapter (this, Android.Resource.Layout.SimpleListItem1, this.Args);
			this.listView.Adapter = this.ContactListAdapter;

			AnalyticsHelper.SendView ("Contacts View");
		}
			
		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnStop () {
			base.OnStop ();
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		private void HandleSearchAction (object sender, TextView.EditorActionEventArgs e) {
			e.Handled = false; 
			if (e.ActionId == ImeAction.Search) {
				//hide keyboard
				var manager = (InputMethodManager) EMApplication.GetCurrentActivity().GetSystemService(Context.InputMethodService);
				manager.HideSoftInputFromWindow(searchBar.WindowToken, 0);
				e.Handled = true;
			}
		}

		private void DidTapDoneButton (object sender, EventArgs e) {
			this.Shared.FinishContactSelection ();
		}

		protected void DidTapItem (object sender, AdapterView.ItemClickEventArgs e) {
			int position = e.Position;

			AggregateContact contact = this.ContactListAdapter.ResultFromPosition (position);
			this.Shared.HandleAggregateContactSelected (contact);

			// Since we're latching onto a listview event, the checkbox (ui) won't be updated until we scroll, so manually update the view.
			this.ContactListAdapter.UpdateCheckboxAtIndex (position, this.listView);
		}

		private void HandleBackButtonPressed (object sender, EventArgs e) {
			this.FragmentManager.PopBackStack ();
		}

		public void ReloadSearchContacts () {
			this.ContactListAdapter.LoadContactsAsync ();
		}

		public void Clear () {
			this.Shared.SelectedContacts.Clear ();
			this.ContactListAdapter.NotifyDataSetChanged ();
			UpdateToField ();
		}

		#region IContactSearchConteroller
		public void UpdateContactsAfterSearch (IList<Contact> listOfContacts, string currentSearchFilter) {
			this.ContactListAdapter.UpdateSearchContacts (listOfContacts, currentSearchFilter);
		}

		public void ShowList (bool shouldShowMainList) {
			this.listView.Visibility = ViewStates.Visible;
		}

		public void RemoveContactAtIndex (int index) {
			this.Shared.SelectedContacts.RemoveAt (index); // TODO check if this makes sense
			this.Shared.UpdateContactSelectionState ();

			// TODO Possible double NotifyDataSetInvalidated (), see UpdateContactSelectionState ()
			this.ContactListAdapter.NotifyDataSetInvalidated ();
		}

		public void InvokeFilter (string currentSearchFilter) {
			AddressBookListAdapter adapter = this.ContactListAdapter;
			if (adapter != null) {
				adapter.Filter.InvokeFilter (currentSearchFilter);
			}
		}

		public string GetDisplayLabelString () {
			return this.Shared.SelectedContactDisplay;
		}

		public void SetQueryResultCallback (Action callback) {
			this.ContactListAdapter.QueryResultsFinished = callback;
		}

		public bool HasResults () {
			return this.ContactListAdapter.HasResults;
		}

		#endregion

		#region IMultiSpinner Interface
		public void OnItemsSelected (bool[] selectedItems) {
			this.Shared.HandleSelectionFromPopupSpinner (selectedItems);
			this.ContactListAdapter.UpdateCheckboxForContact (this.Shared.PopupContact, this.listView);
		}
		#endregion

		#region SharedAddressBookController
		public void UpdateToField () {
			this.searchBar.UpdateContactSearchTextView ();
		}

		public void GoToAggregateSelection (AggregateContact contact, bool[] chosen) {
			//hide keyboard
			KeyboardUtil.HideKeyboard (searchBar);

			AddressBookPickerAdapter adapter = new AddressBookPickerAdapter (this.Context, contact.Contacts, chosen);
			this.Spinner.SetItems (this, adapter);
			this.Spinner.PerformClick ();
		}

		public virtual void FinishSelectingContact (AddressBookSelectionResult result) {
			CompletionCallback (result);
			FragmentManager.PopBackStackImmediate ();
		}

		public void UpdateContactSelectionStateRows (ContactSelectionState state) {
			this.ContactListAdapter.State = state;
		}
		#endregion

		public class SharedAddressBookController : AbstractAddressBookController {
			private WeakReference _r;
			private AddressBookFragment Self { 
				get { return this._r != null ? this._r.Target as AddressBookFragment : null; }
				set { this._r = new WeakReference (value); }
			}

			public SharedAddressBookController (AddressBookFragment pc, ApplicationModel appModel) : base (appModel) {
				this.Self = pc;
			}

			public override void UpdateToField () {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.UpdateToField ();
			}

			public override void DidChangeColorTheme () {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.ThemeController ();
			}

			public override void DidDownloadThumbnail (Contact contact) {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				ReloadContactRows (contact);
			}

			public override void DidChangeThumbnail (Contact contact) {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				ReloadContactRows (contact);
			}

			private void ReloadContactRows (Contact contact) {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				IList<AggregateContact> contactList = self.ContactList;
				if (contactList == null) {
					return;
				}
				ListView listView = self.listView;
				int firstVisiblePosition = listView.FirstVisiblePosition;
				int lastVisiblePosition = listView.LastVisiblePosition;
				int row = 0;
				bool isVisible = false;
				AggregateContact aggContact = null;
				for (; row + firstVisiblePosition <= lastVisiblePosition; row++) {
					if (row + firstVisiblePosition >= contactList.Count) {
						return;
					}
					if (contactList[row + firstVisiblePosition].ServerContactID == contact.serverContactID) {
						aggContact = contactList [row + firstVisiblePosition];
						isVisible = true;
						break;
					}
				}
				if (!isVisible || row >= listView.Count) {
					return;
				}
				View rowView = listView.GetChildAt (row);
				rowView = self.ContactListAdapter.GetView (row + firstVisiblePosition, rowView, listView);
				ContactListViewHolder viewHolder = rowView.Tag as ContactListViewHolder;
				BitmapSetter.SetThumbnailImage (viewHolder, aggContact.ContactForDisplay , self.Context.Resources, viewHolder.ThumbnailView, Resource.Drawable.userDude, Android_Constants.ROUNDED_THUMBNAIL_SIZE);
			}

			public override void ReloadContactSearchContacts () {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.ReloadSearchContacts ();
			}

			public override void GoToAggregateSelection (AggregateContact contact, bool[] chosen) {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.GoToAggregateSelection (contact, chosen);
			}

			public override void FinishSelectingContact (AddressBookSelectionResult result) {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.FinishSelectingContact (result);
			}

			public override void UpdateContactSelectionStateRows (ContactSelectionState state) {
				AddressBookFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.UpdateContactSelectionStateRows (state);
			}
		}
	}
}