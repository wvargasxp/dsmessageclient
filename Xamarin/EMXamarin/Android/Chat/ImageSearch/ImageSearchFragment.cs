using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidHUD;
using EMXamarin;
using em;

namespace Emdroid {
	public class ImageSearchFragment : Fragment {
		RelativeLayout titlebarLayout;

		RelativeLayout searchbarLayout;
		ImageButton searchButton;
		EditText searchTextField;

		GridView gridview;
		ImageSearchAdapter gridViewAdapter;

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		Action<byte[]> imageSelectedCallback;
		public Action<byte[]> ImageSelectedCallback {
			get { return imageSelectedCallback; }
			set { imageSelectedCallback = value; }
		}

		private HiddenReference<SharedImageSearchController> _sharedController;
		public SharedImageSearchController SharedController {
			get { 
				return this._sharedController != null ? this._sharedController.Value : null;
			}

			set { 
				this._sharedController = new HiddenReference<SharedImageSearchController> (value);
			}
		}

        private object _cancelActionsAfterProgressLock = new object ();
        private object CancelActionsAfterProgressLock { get { return this._cancelActionsAfterProgressLock; } set { this._cancelActionsAfterProgressLock = value; } }

		private bool shouldCancelActionsAfterProgress = false;
		private bool ShouldCancelBackgroundTasks { 
			get {
				lock (this.CancelActionsAfterProgressLock) {
					return shouldCancelActionsAfterProgress;
				}
			}

			set {
				lock (this.CancelActionsAfterProgressLock) {
					shouldCancelActionsAfterProgress = value;
				}
			}
		}

		private ImageSearchParty ThirdParty { get; set; }
		private string InitialSeedString { get; set; }

		public static ImageSearchFragment NewInstance (ImageSearchParty thirdParty, Action<byte[]> gCb, string seedString) {
			var fragment = new ImageSearchFragment ();
			fragment.ThirdParty = thirdParty;
			fragment.InitialSeedString = seedString;
			fragment.ImageSelectedCallback = gCb;
			return fragment;
		}

		public ImageSearchFragment () {}

		#region lifecycle

		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			this.SharedController = new SharedImageSearchController (this, this.ThirdParty, this.InitialSeedString);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.imagesearchlayout, container, false);
			gridview = v.FindViewById<GridView> (Resource.Id.gridview);

			#region setting the title bar 
			titlebarLayout = v.FindViewById<RelativeLayout> (Resource.Id.titlebarlayout);
			TextView titleTextView = titlebarLayout.FindViewById<TextView> (Resource.Id.titleTextView);
			Button leftBarButton = titlebarLayout.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += WeakDelegateProxy.CreateProxy <object, EventArgs> (BackButtonClicked).HandleEvent<object, EventArgs>;
			titleTextView.Text = "SEARCH_TITLE".t ();
			#endregion

			#region search bar
			searchbarLayout = v.FindViewById<RelativeLayout> (Resource.Id.searchbarlayout);
			TextView leftSideText = searchbarLayout.FindViewById<TextView> (Resource.Id.leftSideText);
			leftSideText.Text = "SEARCH_TITLE".t ();

			BackgroundColor mainColor = EMApplication.Instance.appModel.account.accountInfo.colorTheme;

			searchButton = searchbarLayout.FindViewById<ImageButton> (Resource.Id.rightSideButton);
			mainColor.GetChatSendButtonResource ( (string filepath) => {
				if (searchButton != null) {
					searchButton.SetImageDrawable (Drawable.CreateFromPath (filepath));
				}
			});

			searchTextField = searchbarLayout.FindViewById<EditText> (Resource.Id.searchTextEntryField);
			searchTextField.InputType = InputTypes.TextFlagNoSuggestions | InputTypes.TextFlagAutoCorrect | InputTypes.TextFlagAutoComplete;
			searchTextField.ImeOptions = ImeAction.Search;
			searchTextField.EditorAction += WeakDelegateProxy.CreateProxy <object, TextView.EditorActionEventArgs> (HandleSearchAction).HandleEvent<object, TextView.EditorActionEventArgs>;
			searchTextField.Text = this.SharedController.BeginningQueryString;
			searchTextField.SetSelectAllOnFocus (true);
			#endregion
			mainColor.GetBackgroundResource ((string file) => {
				BitmapSetter.SetBackgroundFromFile (v, this.Resources, file);
			});
			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState); 
			gridViewAdapter = new ImageSearchAdapter (this, this.Activity.BaseContext);
			gridview.Adapter = gridViewAdapter;
		}

		public override void OnResume () {
			base.OnResume ();
			this.Visible = true;
			gridview.ItemClick += GridViewItemSelected;
			searchButton.Click += SearchButtonClicked;
			this.SharedController.InitializeAndSearch ();

			KeyboardUtil.ShowKeyboard (this.searchTextField);
		}

		public override void OnPause () {
			base.OnPause ();
			this.Visible = false;
			gridview.ItemClick -= GridViewItemSelected;
			searchButton.Click -= SearchButtonClicked;

			KeyboardUtil.HideKeyboard (this.View);
		}

		public override void OnStop () {
			base.OnStop ();
		}

		public override void OnDestroy () {
			base.OnDestroy ();
		}

		public override void OnDetach () {
			base.OnDetach ();
		}
		#endregion

		#region UI updates
		public void PauseUI () {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				ImageSearchFragment self = thisRef.Target as ImageSearchFragment;
				if (GCCheck.ViewGone (self)) return;
				AndHUD.Shared.Show (self.Activity, "LOADING".t (), -1, MaskType.None, default(TimeSpan?), null, true, WeakDelegateProxy.CreateProxy (self.AndHudCancelCallback).HandleEvent);
			});
		}

		public void ResumeUI () {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				ImageSearchFragment self = thisRef.Target as ImageSearchFragment;
				if (GCCheck.ViewGone (self)) return;
				AndHUD.Shared.Dismiss (self.Activity);
			});
		}

		public void ReloadUI () {
			EMTask.DispatchMain (gridViewAdapter.NotifyDataSetChanged);
		}

		private void AndHudCancelCallback () {
			// This gets called if user presses the back button while AndHud progress indicator is the foremost activity (if keyboard is up, keyboard would be considered foremost activity).
			// User stays on screen. We set a flag to cancel any background tasks surfacing after this is called.
			this.ShouldCancelBackgroundTasks = true;
		}

		private void DisplayErrorMessage (string errorMessage, bool showKeyboardOnError) {
			WeakReference thisRef = new WeakReference (this);
			EMTask.DispatchMain (() => {
				ImageSearchFragment self = thisRef.Target as ImageSearchFragment;
				if (GCCheck.ViewGone (self)) return;

				if (!this.ShouldCancelBackgroundTasks) {
					var dialog = new AndroidModalDialogs ();
					dialog.ShowBasicOKMessage ("APP_TITLE".t (), errorMessage, (sender, args) => { 
						ImageSearchFragment self2 = thisRef.Target as ImageSearchFragment;
						if (GCCheck.ViewGone (self2)) return;
						if (showKeyboardOnError) {
							KeyboardUtil.ShowKeyboard (this.searchTextField);
						}
					});
				} else {
					this.ShouldCancelBackgroundTasks = false;
				}
			});
		}

		private void DisplayErrorMessage (string errorMessage) {
			DisplayErrorMessage (errorMessage, false);
		}
		#endregion

		public void GridViewItemSelected (object sender, AdapterView.ItemClickEventArgs args) {
			AbstractSearchImage searchImage = this.SharedController.SearchImages [args.Position];
			PauseUI ();
			searchImage.GetFullImageAsBytesAsync ((byte[] imageInBytes) => {
				if (!this.ShouldCancelBackgroundTasks) {
					if (imageInBytes != null) {
						if (EMApplication.Instance.appModel.platformFactory.OnMainThread) {
							EMTask.DispatchBackground (() => ImageSelectedCallback (imageInBytes));
						} else {
							ImageSelectedCallback (imageInBytes);
						}
					} else {
						DisplayErrorMessage ("IMAGE_SELECTION_ERROR".t ());
					}

					ResumeUI ();
					this.FragmentManager.PopBackStack ();
				} else {
					this.ShouldCancelBackgroundTasks = false;
				}
			});
		}

		public void SearchButtonClicked (object sender, EventArgs e) {
			this.SharedController.SearchForImagesWithTerm (searchTextField.Text);
			KeyboardUtil.HideKeyboard (this.View);
		}

		public void BackButtonClicked (object sender, EventArgs e) {
			// If the back button is pressed, we'd also want to cancel any possible error messages if they occur.
			this.ShouldCancelBackgroundTasks = true;
			ResumeUI ();
			this.FragmentManager.PopBackStack ();
		}

		void HandleSearchAction (object sender, TextView.EditorActionEventArgs e) {
			e.Handled = false; 
			if (e.ActionId == ImeAction.Search) {
				searchButton.PerformClick ();
				e.Handled = true;   
			}
		}

		protected override void Dispose(bool disposing) {
			//sharedController.Dispose ();
			base.Dispose (disposing);
		}

		public class ImageSearchAdapter : BaseAdapter {
			readonly Context context;
			readonly WeakReference fragmentRef;
			public ImageSearchAdapter (ImageSearchFragment f, Context c) {
				context = c;
				fragmentRef = new WeakReference (f);
			}

			public override int Count {
				get { 
					var fragment = fragmentRef.Target as ImageSearchFragment;
					return fragment != null ? fragment.SharedController.SearchImages.Count : 0;
				}
			}

			public override Java.Lang.Object GetItem (int position) {
				return null;
			}

			public override long GetItemId (int position) {
				return 0;
			}

			private int MaxDimensionInPixels { get; set; } 

			// create a new ImageView for each item referenced by the Adapter
			public override View GetView (int position, View convertView, ViewGroup parent) {
				View retVal = convertView;
				ImageSearchItemViewHolder holder;
				if (convertView == null) {
					retVal = LayoutInflater.From (context).Inflate (Resource.Layout.imagesearchlayoutitem, parent, false);
					holder = new ImageSearchItemViewHolder ();
					holder.ImageWrapper = retVal.FindViewById<RelativeLayout> (Resource.Id.ImageWrapper);
					holder.MediaView = retVal.FindViewById<ImageView> (Resource.Id.MediaView);
					retVal.Tag = holder;
				} else {
					holder = (ImageSearchItemViewHolder)convertView.Tag;
				}

				this.MaxDimensionInPixels = holder.ImageWrapper.LayoutParameters.Height; // height and width should be the same

				holder.Position = position;
				holder.MediaView.SetBackgroundDrawable (null);

				ImageSearchFragment fragment = fragmentRef.Target as ImageSearchFragment;
				if (fragment != null) {
					AbstractSearchImage searchImage = fragment.SharedController.SearchImages [position];
					WeakReference fRef = new WeakReference (fragment);
					searchImage.GetThumbnailAsBytesAsync (position, (int originalPosition, byte[] loadedImage) => EMTask.DispatchMain (() => {
						Fragment f = fRef.Target as ImageSearchFragment;
						if (GCCheck.ViewGone (f)) return;
						if (originalPosition == holder.Position) {
							if (loadedImage == null) {
								holder.MediaView.SetBackgroundColor (Color.Red);
							} else {
								BitmapSetter.SetSearchImage (holder, holder.MediaView, loadedImage, searchImage.ThumbnailKeyForCache, f.Resources, this.MaxDimensionInPixels);
							}
						}
					}));
				}
					
				return retVal;
			}
		}

		public class SharedImageSearchController : AbstractImageSearchController {
			readonly WeakReference fragmentRef;
			public SharedImageSearchController (ImageSearchFragment c, ImageSearchParty thirdParty, string seedString) 
				: base (EMApplication.Instance.appModel, EMApplication.Instance.appModel.account, thirdParty, seedString) {
				fragmentRef = new WeakReference (c);
			}

			public override void PauseUI () {
				var fragment = fragmentRef.Target as ImageSearchFragment;
				if (fragment != null) {
					fragment.PauseUI ();
				}
			}

			public override void ResumeUI () {
				var fragment = fragmentRef.Target as ImageSearchFragment;
				if (fragment != null) {
					fragment.ResumeUI ();
				}
			}

			public override void ReloadUI () {
				var fragment = fragmentRef.Target as ImageSearchFragment;
				if (fragment != null) {
					fragment.ReloadUI ();
				}
			}

			public override void DisplayError (string errorMessage) {
				var fragment = fragmentRef.Target as ImageSearchFragment;
				if (fragment != null) {
					fragment.DisplayErrorMessage (errorMessage, true);
				}
			}
		}
	}
}