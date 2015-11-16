using System;
using System.Collections.Generic;
using CoreGraphics;
using em;
using EMXamarin;
using GoogleAnalytics.iOS;
using UIKit;
using Foundation;
using System.Diagnostics;
using System.Linq;

namespace iOS {
	public class AddressBookViewController : UIViewController, IContactSearchController {

		#region data source + delegates
		private AddressBookTableViewDataSource ResultsSource { get; set; }
		private AddressBookTableViewDelegate ResultsDelegate { get; set; }
		public AddressBookSearchResultsTableViewDataSource SearchSource { get; set; }
		public AddressBookSearchResultsTableViewDelegate SearchDelegate { get; set; }
		#endregion

		#region picker that shows up when user selects an aggregate contact
		private UITextField HiddenPickerTextField { get; set; }
		protected UIPickerView AggregateContactPicker { get; set; }
		protected AggregateContactPickerViewModel AggregateContactPickerViewModel { get; set; }

		private UIToolbar _pickerToolbar = null;
		protected UIToolbar PickerToolBar {
			get { 
				if (_pickerToolbar == null) {
					var toolbar = new UIToolbar ();
					toolbar.BarStyle = UIBarStyle.Default;
					toolbar.Translucent = true;
					toolbar.SizeToFit ();

					var doneButton = new UIBarButtonItem("DONE_BUTTON".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>(HandlePickerToolbarButtonPressed).HandleEvent<object,EventArgs>);

					doneButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
					toolbar.SetItems (new []{ doneButton }, true); 
					_pickerToolbar = toolbar;
				}
				return _pickerToolbar; 
			}
			set { _pickerToolbar = value; }
		}

		private UIView _transparentView = null;
		protected UIView TransparentView {
			get {
				if (this._transparentView == null) {
					this._transparentView = new UIView (this.View.Frame);
					this._transparentView.BackgroundColor = UIColor.Black;
					this._transparentView.Alpha = .6f;
					this._transparentView.Layer.MasksToBounds = false;
					this._transparentView.Hidden = true;
				}

				return this._transparentView;
			}
		}

		#endregion

		#region UI
		UITableView tableView;
		public UITableView MainTableView {
			get { return tableView; }
		}

		UIView lineView;
		#endregion

		public delegate void DelegateDidSelectContact (AddressBookSelectionResult result);
		public DelegateDidSelectContact DelegateContactSelected = delegate {};

		public IList<AggregateContact> ContactList {
			get { return Shared.ContactList; }
		}

		public SharedAddressBookController Shared { get; set; }

		private AddressBookArgs Args { get; set; }

		#region contact search related
		private UIView SearchWrapperBar { get; set; }
		public IList<Contact> SearchResults { get; set; }
		private string SearchQuery { get; set; }

		private UITableView contactSearchTableView;
		public UITableView ContactSearchTableView {
			get {
				if (contactSearchTableView == null) {
					contactSearchTableView = new UITableView (new CGRect (0, 0, View.Frame.Width, View.Frame.Height), UITableViewStyle.Plain);
					contactSearchTableView.DataSource = this.SearchSource;
					contactSearchTableView.Delegate = this.SearchDelegate;
					contactSearchTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
					contactSearchTableView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin;
					contactSearchTableView.Hidden = true; // hidden at first
					contactSearchTableView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;
				}
				return contactSearchTableView;
			}
		}

		private ContactSearchTextView contactSearchTextView;
		public ContactSearchTextView ContactSearchTextView {
			get {
				if (contactSearchTextView == null) {
					contactSearchTextView = new ContactSearchTextView (new CGRect (0, 0, this.View.Frame.Width, 44));
					contactSearchTextView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
					contactSearchTextView.Delegate = new ContactSearchTextViewDelegate (contactSearchTextView, this);
					contactSearchTextView.Font = FontHelper.DefaultFontForTextFields ();
					contactSearchTextView.ClipsToBounds = true;
					contactSearchTextView.AutocorrectionType = UITextAutocorrectionType.No;
					contactSearchTextView.BackgroundColor = UIColor.Clear;
				}
				return contactSearchTextView;
			}
		}
		#endregion

		public AddressBookViewController (AddressBookArgs args) {
			this.Args = args;
			LoadContactsAsync ();
			this.Shared = new SharedAddressBookController (this, (UIApplication.SharedApplication.Delegate as AppDelegate).applicationModel);
			this.Shared.SetInitialSelectedContacts (this.Args.Contacts);
		}

		public void LoadContactsAsync () {
			AppDelegate appDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
			Contact.FindAllContactsWithServerIDsRolledUpAsync (appDelegate.applicationModel, loadedContacts => {
				this.Shared.ContactList = loadedContacts;
				if (IsViewLoaded) {
					this.MainTableView.ReloadData ();
				}
			}, this.Args);
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			BackgroundColor mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && lineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					lineView.BackgroundColor = mainColor.GetColor ();
				}
			});

			if (this.NavigationController != null) {
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (this.NavigationController.NavigationBar);
			}
		}

		public override void ViewWillAppear (bool animated) {
			base.ViewWillAppear (animated);
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Contacts View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}
			
		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			AutomaticallyAdjustsScrollViewInsets = false;
			Title = "CONTACTS_TITLE".t ();

			ChatEntry chatEntry = this.Args.ChatEntry;

			#region UI
			UINavigationBarUtil.SetBackButtonToHaveNoText (this.NavigationItem);

			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (lineView);

			tableView = new UITableView (new CGRect (0, 0, View.Frame.Width, View.Frame.Height), UITableViewStyle.Plain);
			tableView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;

			this.ResultsSource = new AddressBookTableViewDataSource (this, chatEntry);
			this.ResultsDelegate = new AddressBookTableViewDelegate (this); 
			this.SearchSource = new AddressBookSearchResultsTableViewDataSource (this, chatEntry);
			this.SearchDelegate = new AddressBookSearchResultsTableViewDelegate (this);

			tableView.DataSource = this.ResultsSource;
			tableView.Delegate = this.ResultsDelegate;

			tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
			tableView.BackgroundColor = UIColor.Clear;
			tableView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleBottomMargin;
			View.Add (tableView);
			View.SendSubviewToBack (tableView);
			#endregion

			UIBarButtonItem rightBarButton = this.EditButtonItem;
			rightBarButton.Title = "DONE_BUTTON".t ();
			rightBarButton.Clicked += WeakDelegateProxy.CreateProxy<object,EventArgs> (HandleDoneButtonPressed).HandleEvent<object,EventArgs>;
			rightBarButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes (), UIControlState.Normal);
			rightBarButton.Enabled = true;
			NavigationItem.RightBarButtonItem = rightBarButton;

			this.SearchWrapperBar = new UIView (new CGRect (0, 0, this.View.Frame.Width, 44));
			this.SearchWrapperBar.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			this.SearchWrapperBar.BackgroundColor = UIColor.FromRGBA (Constants.RGB_TOOLBAR_COLOR [0], Constants.RGB_TOOLBAR_COLOR[1], Constants.RGB_TOOLBAR_COLOR[2], 255);
			this.SearchWrapperBar.Layer.BorderWidth = 0.5f;
			this.View.Add (this.SearchWrapperBar);

			this.SearchWrapperBar.Add (this.ContactSearchTextView);
			this.View.Add (this.ContactSearchTableView);

			this.HiddenPickerTextField = new UITextField (new CGRect (0, 0, 0, 0));
			this.View.Add (this.HiddenPickerTextField);
			this.View.Add (this.TransparentView);
			this.View.BringSubviewToFront (this.TransparentView);

			UpdateToField ();
		}

		public override void TouchesBegan (NSSet touches, UIEvent evt) {
			base.TouchesBegan (touches, evt);
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();
			ThemeController (InterfaceOrientation);
			nfloat displacement_y = this.TopLayoutGuide.Length;

			lineView.Frame = new CGRect (0, displacement_y, lineView.Frame.Width, lineView.Frame.Height);

			this.SearchWrapperBar.Frame = new CGRect (0, lineView.Frame.Y + lineView.Frame.Height, this.View.Frame.Width, this.SearchWrapperBar.Frame.Height);
			this.ContactSearchTextView.Frame = new CGRect (0, 0, this.ContactSearchTextView.Frame.Width, this.ContactSearchTextView.Frame.Height);

			this.MainTableView.Frame = new CGRect (0 , this.SearchWrapperBar.Frame.Y + this.SearchWrapperBar.Frame.Height, tableView.Frame.Width, View.Frame.Height - this.SearchWrapperBar.Frame.Height - displacement_y - lineView.Frame.Height);
			this.ContactSearchTableView.Frame = this.MainTableView.Frame;
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			if ( DisposeOnDisappear ) {
				this.Shared.Dispose (true);
			}
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		bool DisposeOnDisappear = false;
		public override void ViewWillDisappear(bool animated) {
			base.ViewWillDisappear(animated);
			if (UINavigationControllerHelper.IsViewControllerBeingPopped (this))
				DisposeOnDisappear = true;
		}

		public void ReloadSearchContacts () {
			LoadContactsAsync ();
		}
			
		public void HandleRowSelected (UITableView tableView, NSIndexPath indexPath, AggregateContact contact) {
			HandleAggregateContactSelected (contact);

			// Update checkbox appearance.
			bool shouldShowCheckbox = this.Shared.ShouldShowCheckboxForContact (contact);
			AddressBookContactTableViewCell cell = tableView.CellAt (indexPath) as AddressBookContactTableViewCell;
			if (cell != null) {
				cell.UpdateCheckBox (shouldShowCheckbox);
			}
		}

		private void HandleAggregateContactSelected (AggregateContact contact) {
			this.Shared.HandleAggregateContactSelected (contact);
		}

		private void HandleDoneButtonPressed (object o, EventArgs a) {
			this.Shared.FinishContactSelection ();
		}

		private void HandlerPickerSelectionUpdated (object d, AggregateContactPickerChanged args) {
			Contact contact = args.SelectedValue;
		}

		private void HandlePickerToolbarButtonPressed (object d , EventArgs g) {
			this.HiddenPickerTextField.ResignFirstResponder ();
			this.TransparentView.Hidden = true;

			bool[] selectedItems = this.AggregateContactPickerViewModel.Chosen;
			this.Shared.HandleSelectionFromPopupSpinner (selectedItems);
			this.MainTableView.ReloadData (); // todo, do it more cleanly
		}

		public void Clear () {
			this.Shared.SelectedContacts.Clear ();
			if (this.MainTableView.Hidden) {
				this.ContactSearchTableView.ReloadData ();
			} else {
				this.MainTableView.ReloadData ();
			}

			UpdateToField ();
		}

		#region IContactSearchController
		public void UpdateContactsAfterSearch (IList<Contact> listOfContacts, string currentSearchFilter) {
			UpdateSearchContacts (listOfContacts, currentSearchFilter);
		}

		public void ShowList (bool shouldShowMainList) {
			if (shouldShowMainList) {
				this.MainTableView.Hidden = false;
				this.ContactSearchTableView.Hidden = true;
			} else {
				this.MainTableView.Hidden = true;
				this.ContactSearchTableView.Hidden = false;
			}
		}

		public void RemoveContactAtIndex (int index) {
			this.Shared.SelectedContacts.RemoveAt (index);
			this.Shared.UpdateContactSelectionState ();

			// TODO Possible double ReloadData (), see UpdateContactSelectionState ()
			if (!this.MainTableView.Hidden) {
				this.MainTableView.ReloadData ();
			} else {
				this.ContactSearchTableView.ReloadData ();
			}

			ThemeController (InterfaceOrientation);
		}

		public void InvokeFilter (string currentSearchFilter) {
			ShouldReloadForSearchString (currentSearchFilter);
		}

		public string GetDisplayLabelString () {
			return this.Shared.SelectedContactDisplay;
		}

		public bool HasResults () {
			return this.SearchHasResults;
		}

		#endregion

		#region actual search filtering 

		private string _lastQuery = string.Empty; // keep track of the last query so we can search with it again if the contact list changes
		private string LastQuery { get { return this._lastQuery; } set { this._lastQuery = value; } }

		bool hasResults = false;
		public bool SearchHasResults {
			get { return hasResults; }
			set { hasResults = value; }
		}

		public void ShouldReloadForSearchString (string searchQuery) {
			this.LastQuery = searchQuery;
			FilterContacts (searchQuery);
		}

		public void ReloadForMatches (IList<AggregateContact> queryMatches) {
			this.SearchSource = new AddressBookSearchResultsTableViewDataSource (this, queryMatches, this.Args.ChatEntry);
			this.SearchDelegate = new AddressBookSearchResultsTableViewDelegate (this);

			this.ContactSearchTableView.Delegate = this.SearchDelegate;
			this.ContactSearchTableView.DataSource = this.SearchSource;
			this.ContactSearchTableView.ReloadData ();
		}

		public void UpdateSearchContacts (IList<Contact> listOfContacts, string currentSearchFilter) {
			this.SearchResults = listOfContacts;
			this.SearchQuery = currentSearchFilter;
			FilterContacts (currentSearchFilter);
		}

		private void FilterContacts (string query) {
			// If query is 0, it's empty, no need to search.
			if (query.Length <= 0) {
				return;
			}

			// Sort them alphabetically by display name.
			IList<AggregateContact> sortedResults = this.ContactList.OrderBy (c => c.DisplayName).ToList ();

			// Filter out results from local (db/inmemory).
			IList<AggregateContact> localSearchResults = ContactSearchUtil.FilterAggregateContactsBySearchQuery (sortedResults, query);

			// We check the original search query that got us a contact from the server with the current query. 
			// If the current query matches, we add the server result contact to the results list.
			// Ex. Type 'Bob' -> Server sends back Bob contact. Current SearchQuery is 'Bob'.
			// Now if we backspace, the query is 'Bo'. Since the current SearchQuery is 'Bob', it matches and we add the result in.
			// We do this because we want to automatically add any server contacts returned from the server (as in don't apply a filter to it).
			// At the same time, we need to preserve the last server contact returned.
			if (!string.IsNullOrWhiteSpace (this.SearchQuery) && this.SearchQuery.Contains (query)) {
				IList<Contact> fromServerResults = this.SearchResults;

				// Add the ones that came from server to results.
				foreach (Contact g in fromServerResults) {
					AggregateContact agg = new AggregateContact (g);
					localSearchResults.Insert (0, agg);
				}
			}

			if (localSearchResults.Count == 0) {
				this.SearchHasResults = false;
			} else {
				this.SearchHasResults = true;
			}

			// Reload and track of we have results or not.
			EMTask.DispatchMain (() => {
				ReloadForMatches (localSearchResults);
			});
		}
		#endregion

		#region SharedAddressBookController

		public void UpdateToField () {
			ContactSearchTextViewDelegate c = this.ContactSearchTextView.Delegate as ContactSearchTextViewDelegate;
			if (c == null) return;
			c.UpdateContactSearchTextView ();
		}

		public void GoToAggregateSelection (AggregateContact contact, bool[] chosen) {
			this.AggregateContactPicker = new UIPickerView ();

			this.AggregateContactPickerViewModel = new AggregateContactPickerViewModel (this, contact, chosen);
			this.AggregateContactPickerViewModel.PickerChanged += WeakDelegateProxy.CreateProxy<object, AggregateContactPickerChanged> (HandlerPickerSelectionUpdated).HandleEvent<object, AggregateContactPickerChanged>;

			this.AggregateContactPicker.Model = this.AggregateContactPickerViewModel;
			this.AggregateContactPicker.ShowSelectionIndicator = true;

			this.HiddenPickerTextField.InputView = this.AggregateContactPicker;
			this.HiddenPickerTextField.InputAccessoryView = this.PickerToolBar;
			this.HiddenPickerTextField.BecomeFirstResponder ();
			this.TransparentView.Hidden = false;
		}

		public virtual void FinishSelectingContact (AddressBookSelectionResult result) {
			DelegateContactSelected (result);
			this.NavigationController.PopViewController (true);
		}

		public void UpdateContactSelectionStateRows (ContactSelectionState state) {
			ContactSelectionState old = this.ResultsSource.State;
			if (old != state) {
				this.ResultsSource.State = state;
				this.SearchSource.State = state;

				if (!this.MainTableView.Hidden) {
					this.MainTableView.ReloadData ();
				} else {
					this.ContactSearchTableView.ReloadData ();
				}
			}
		}
		#endregion

		#region Rotation
		public override void WillRotate (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillRotate (toInterfaceOrientation, duration);
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}
		#endregion

		public class SharedAddressBookController : AbstractAddressBookController {
			private WeakReference _r;
			private AddressBookViewController Self {
				get { return this._r != null ? this._r.Target as AddressBookViewController : null; }
				set {  this._r = new WeakReference (value); }
			}

			public SharedAddressBookController (AddressBookViewController pc, ApplicationModel appModel) : base (appModel) {
				this.Self = pc;
			}

			public override void UpdateToField () {
				AddressBookViewController self = this.Self;
				if (self == null) return;
				if (self.IsViewLoaded) {
					self.UpdateToField ();
				}
			}

			public override void DidChangeColorTheme () {
				AddressBookViewController self = this.Self;
				if (self == null) return;
				if (self.IsViewLoaded) {
					self.ThemeController (self.InterfaceOrientation);
				}
			}

			public override void DidDownloadThumbnail (Contact contact) {
				ReloadTableViewRows (contact);
			}

			public override void DidChangeThumbnail (Contact contact) {
				ReloadTableViewRows (contact);
			}

			private bool InRange (NSIndexPath curr, NSIndexPath min, NSIndexPath max) {
				if (curr.Section >= min.Section) {
					if (curr.Section == min.Section && curr.Row < min.Row) {
						return false;
					}
					if (curr.Section <= max.Section) {
						if (curr.Section == max.Section && curr.Row > max.Row) {
							return false;
						}
						return true;
					}
				} 
				return false;
			}

			private void ReloadMainViewRows (Contact contact) {
				AddressBookViewController self = this.Self;
				AddressBookTableViewDataSource dataSource = self.ResultsSource;
				UITableView tableView = self.MainTableView;
				IList<String> headers = dataSource.headers;
				List<int[]> headerBoundaries = dataSource.headerBoundaries;
				if (contact.displayName == null || contact.displayName.Equals (" ")) {
					return;
				}
				string firstLetter = contact.displayName.Substring (0, 1).ToUpper ();
				int intHolder = 0;
				bool isNum = int.TryParse (firstLetter, out intHolder);
				if (isNum) {
					firstLetter = "#";
				}
				int currSection = (int)dataSource.SectionFor (tableView, firstLetter, 0);
				if (currSection == -1 || currSection >= headerBoundaries.Count) {
					return;
				}
				int offset = headerBoundaries [currSection] [0];
				int row = 0;
				IList<AggregateContact> aggregateContacts = self.ContactList;
				int contactsLength = aggregateContacts.Count;
				int maxRow = (int)dataSource.RowsInSection (tableView, currSection);
				for (; row < maxRow; row++) {
					if (offset + row >= aggregateContacts.Count) {
						return;
					}
					if (aggregateContacts [offset + row].ServerContactID == contact.serverContactID) {
						break;
					}
				}
				NSIndexPath[] visibleIndices = tableView.IndexPathsForVisibleRows;
				if (visibleIndices == null || visibleIndices.Count () == 0) {
					return;
				}
				NSIndexPath indexPath = NSIndexPath.FromRowSection (row, currSection);
				if (InRange(indexPath, visibleIndices [0], visibleIndices [visibleIndices.Length - 1])) {
					AggregateContact aggContact = headers.Count == 0 ? aggregateContacts [row] : aggregateContacts [offset + row];
					((AddressBookContactTableViewCell)tableView.CellAt (indexPath)).Contact = aggContact;
				}
			}

			private void ReloadSearchViewRows (Contact contact) {
				AddressBookViewController self = this.Self;
				AddressBookSearchResultsTableViewDataSource dataSource = self.SearchSource;
				if (dataSource == null) return;
				IList<AggregateContact> aggContacts = dataSource.FilteredContacts;
				AggregateContact aggContact = null;
				if (aggContacts == null) return;
				int row = 0;
				for (; row < aggContacts.Count; row++) {
					if (aggContacts [row].ServerContactID == contact.serverContactID) {
						aggContact = aggContacts [row];
						break;
					}
				}
				UITableView tableView = self.ContactSearchTableView;
				NSIndexPath[] visiblePaths = tableView.IndexPathsForVisibleRows;
				if (visiblePaths == null || visiblePaths.Count () == 0) {
					return;
				}
				int minRow = visiblePaths [0].Row;
				int maxRow = visiblePaths [visiblePaths.Length - 1].Row;
				if (row >= minRow && row <= maxRow) {
					NSIndexPath updateIndexPath = NSIndexPath.FromRowSection (row, 0);
					((AddressBookContactTableViewCell)tableView.CellAt (updateIndexPath)).Contact = aggContact;
				}
			}

			void ReloadTableViewRows (Contact contact) {
				AddressBookViewController self = this.Self;
				if (self == null) return;
				if (self.IsViewLoaded) {
					if (!self.MainTableView.Hidden) {
						ReloadMainViewRows (contact);
					} else {
						ReloadSearchViewRows (contact);
					}
				}
			}

			public override void ReloadContactSearchContacts () {
				AddressBookViewController self = this.Self;
				if (self == null) return;
				self.ReloadSearchContacts ();
			}

			public override void GoToAggregateSelection (AggregateContact contact, bool[] chosen) {
				AddressBookViewController self = this.Self;
				if (self == null) return;
				self.GoToAggregateSelection (contact, chosen);
			}

			public override void FinishSelectingContact (AddressBookSelectionResult result) {
				AddressBookViewController self = this.Self;
				if (self == null) return;
				self.FinishSelectingContact (result);
			}

			public override void UpdateContactSelectionStateRows (ContactSelectionState state) {
				AddressBookViewController self = this.Self;
				if (self == null) return;
				self.UpdateContactSelectionStateRows (state);
			}
		}
	}
}