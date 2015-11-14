using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using em;
using EMXamarin;

namespace Emdroid {
	public class AccountFragment : BasicAccountFragment {

		AccountUtils.UserProfile Profile;

		private HiddenReference<ApplicationModel> _appModel;
		private ApplicationModel appModel {
			get { return this._appModel != null ? this._appModel.Value : null; }
			set { this._appModel = new HiddenReference<ApplicationModel> (value); }
		}

		AccountInfo accountInfo;
		protected AccountInfo AccountInfo {
			get { return accountInfo; }
		}
			
		private HiddenReference<SharedAccountController> _shared;
		private SharedAccountController sharedAccountController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedAccountController> (value); }
		}

		public static AccountFragment NewInstance (bool onboarding) {
			var f = new AccountFragment ();
			f.sharedAccountController.IsOnboarding = onboarding;

			//reset static variable on MainActivity so that when Account is dismissed, Inbox will show
			MainActivity.IsOnboarding &= !onboarding;

			return f;
		}

		public AccountFragment () {
			appModel = EMApplication.GetInstance ().appModel;

			this.sharedAccountController = new SharedAccountController(appModel, this);

			this.accountInfo = sharedAccountController.AccountInfo;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.account, container, false);
			sharedAccountController.ColorTheme.GetBackgroundResource ((string file) => {
				if (v != null && this.Resources != null) {
					BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
				}
			});

			#region setting the title bar 
			this.TitleBarLayout = v.FindViewById<RelativeLayout> (Resource.Id.titlebarlayout);
			TextView titleTextView = this.TitleBarLayout.FindViewById<TextView> (Resource.Id.titleTextView);

			this.LeftBarButton = this.TitleBarLayout.FindViewById<Button> (Resource.Id.leftBarButton);
			this.LeftBarButton.SetBackgroundColor (Color.Transparent);
			this.LeftBarButton.Typeface = FontHelper.DefaultFont;
			this.LeftBarButton.Text = "DONE_BUTTON".t ();

			titleTextView.Text = "MY_ACCOUNT_TITLE".t ();
			titleTextView.Typeface = FontHelper.DefaultFont;
			#endregion

			this.ProgressBar = v.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
			this.NameEditText = v.FindViewById<EditText> (Resource.Id.NameText);
			this.ThumbnailBackgroundView = v.FindViewById<ImageView> (Resource.Id.ThumbnailBackgroundView);
			this.ThumbnailButton = v.FindViewById<ImageView> (Resource.Id.ThumbnailButton);
			this.ColorThemeText = v.FindViewById<TextView> (Resource.Id.ColorThemeLabel); 
			this.ColorThemeButton = v.FindViewById<ImageView> (Resource.Id.ColorThemeButton);
			this.ColorThemeSpinner = v.FindViewById<Spinner> (Resource.Id.ColorThemeSpinner);
			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			bool isOnboarding = sharedAccountController.IsOnboarding;
			if (isOnboarding) {
				MainActivity activity = this.Activity as MainActivity;
				if (activity != null) {
					//force keyboard to show
					KeyboardUtil.ShowKeyboard (this.View);
				}
			}

			this.Profile = AccountUtils.GetUserProfile (this.Activity.BaseContext);

			if (isOnboarding && Profile != null && Profile.PossibleNames ().Count > 0)
				this.NameEditText.Text = Profile.PossibleNames () [0];
			else if (isOnboarding)
				this.NameEditText.Text = "";

			sharedAccountController.TextInDisplayNameField = NameEditText.Text;

			this.NameEditText.EditorAction += HandleDoneAction;
			this.NameEditText.AfterTextChanged += (sender, e) => {
				this.TextInDisplayField = this.NameEditText.Text;
			};
			this.NameEditText.FocusChange += (sender, e) => {
				if (!e.HasFocus) {
					KeyboardUtil.HideKeyboard (this.View);
				}
			};

			this.ThumbnailButton.Click += (sender, e) => { 
				KeyboardUtil.HideKeyboard (this.View);
				DidTapThumbnail();
			};

			ThemeController ();
			UpdateThumbnailPicture ();
			CenterText ();

			KeyboardUtil.ShowKeyboard (this.NameEditText);

			AnalyticsHelper.SendView (isOnboarding ? "Account (Onboarding) View" : "Account View");
		}

		protected override void Dispose(bool disposing) {
			base.Dispose (disposing);
		}

		void HandleDoneAction(object sender, TextView.EditorActionEventArgs e) {
			e.Handled = false; 
			if (e.ActionId == ImeAction.Done) {
				KeyboardUtil.HideKeyboard (this.View);
				this.LeftBarButton.PerformClick ();
				e.Handled = true;   
			}
		}

		void DidTapThumbnail() {
			StartAcquiringImage ();
		}

		void DidTapSave (object sender, EventArgs e) {
			sharedAccountController.TrySaveAccount ();
		}

		public void DismissAccountController () {
			// Hiding the keyboard
			KeyboardUtil.HideKeyboard (this.View);

			if (sharedAccountController.IsOnboarding) {
				(Activity as MainActivity).CompletedExternalActivity ();
				(Activity as MainActivity).ReplaceWithInbox (true); //show onboarding accessing address book spinner
			} else {
				FragmentManager.PopBackStack ();
			}
		}

		public void DisplayBlankTextInDisplayAlert () {
			var title = "MY_ACCOUNT_TITLE".t ();
			var message = string.Format ("BLANK_DISPLAY_NAME".t (), accountInfo.username.Replace(" ", ""));
			var action = "CONTINUE_BUTTON".t ();

			var builder = new AlertDialog.Builder (Activity);

			builder.SetTitle (title);
			builder.SetMessage (message);
			builder.SetPositiveButton(action, (s1, dialogClickEventArgs) => sharedAccountController.SaveAccountAsync ());
			builder.SetNegativeButton("EDIT_BUTTON".t (), (s2, dialogClickEventArgs) => { });
			builder.Create ();
			builder.Show ();
		}

		protected void DidSelectColorTheme (object sender, AdapterView.ItemSelectedEventArgs e) {
			sharedAccountController.ColorTheme = BackgroundColor.AllColors [e.Position];
		}

		public void UpdateNameField () {
			EMTask.DispatchMain (() => {
				if ( this.IsAdded && this.NameEditText != null)
					this.NameEditText.Text = sharedAccountController.DisplayName;
			});
		}

		public void CenterText() {
			var center = 57.5 * Resources.DisplayMetrics.Density; //35dp from thumbnail button, image is 45 dp wide, so center of image is at 57.5dp

			//first, center color theme label
			var ctWidth = this.ColorThemeText.Paint.MeasureText (Resources.GetString (Resource.String.COLOR_THEME));
			var layoutParamsColorTheme = (RelativeLayout.LayoutParams) this.ColorThemeText.LayoutParameters;
			layoutParamsColorTheme.RightMargin = (int)(center - (ctWidth / 2));
			this.ColorThemeText.LayoutParameters = layoutParamsColorTheme;
		}

		protected override int PopupMenuInflateResource () {
			return Resource.Menu.account_thumbnail_options;
		}

		protected override View PopupMenuAnchorView () {
			return this.ThumbnailButton;
		}
			
		protected override void DidAcquireMedia (string mediaType, string path) {
			if (path != null) {
				byte[] fileAtPath = File.ReadAllBytes (path);
				if (fileAtPath != null) {
					string accountInfoThumbnailPath = appModel.uriGenerator.GetStagingPathForAccountInfoThumbnailLocal ();
					appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (accountInfoThumbnailPath);
					appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (accountInfoThumbnailPath, fileAtPath, null);
					sharedAccountController.AccountInfo.UpdateThumbnailUrlAfterMovingFromCache (accountInfoThumbnailPath);
					this.UpdateThumbnailPicture ();
					sharedAccountController.UpdatedThumbnail = fileAtPath;
				}
			}
		}

		protected override bool AllowsImageCropping () {
			return true;
		}

		public override BackgroundColor ColorTheme { 
			get { return sharedAccountController.ColorTheme; }
		}

		public override CounterParty CounterParty { 
			get { return this.AccountInfo; }
		}

		public override string TextInDisplayField {
			get { return sharedAccountController.TextInDisplayNameField; }
			set { sharedAccountController.TextInDisplayNameField = value; }
		}

		public override void LeftBarButtonClicked (object sender, EventArgs e) {
			DidTapSave (sender, e);
		}

		public override void RightBarButtonClicked (object sender, EventArgs e) {
			throw new NotImplementedException ();
		}

		public override void ColorThemeSpinnerItemClicked (object sender, AdapterView.ItemSelectedEventArgs e) {
			DidSelectColorTheme (sender, e);
		}

		public override void AdditionalUIChangesOnResume () {}
		public override void AdditionalThemeController () {}

		public override string ImageSearchSeedString { 
			get {
				string seedString = this.NameEditText != null ? this.NameEditText.Text : string.Empty;
				return seedString;
			}
		}
	}

	public class SharedAccountController : AbstractAccountController {
		private WeakReference _r = null;
		private AccountFragment Self {
			get { return this._r != null ? this._r.Target as AccountFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedAccountController(ApplicationModel appModel, AccountFragment avc) : base (appModel) {
			this.Self = avc;
		}

		public override void DidChangeThumbnailMedia () {
			ApplicationModel appModel = EMApplication.GetInstance ().appModel;
			AccountFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.UpdateThumbnailPicture ();
		}

		public override void DidDownloadThumbnail () {
			AccountFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.UpdateThumbnailPicture ();
		}

		public override void DidChangeColorTheme() {
			AccountFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.ThemeController ();
		}

		public override void DidChangeDisplayName () {
			AccountFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.UpdateNameField ();
		}

		public override void DismissAccountController () {
			AccountFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.DismissAccountController ();
		}

		public override void DisplayBlankTextInDisplayAlert () {
			AccountFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.DisplayBlankTextInDisplayAlert ();
		}
	}

	public class BackgroundColorThemeArrayAdapter : BaseAdapter<BackgroundColor>, ISpinnerAdapter {
		Context context;
		readonly BackgroundColor[] colors;

		public BackgroundColorThemeArrayAdapter(Context theContext, BackgroundColor[] theColors) {
			context = theContext;
			colors = theColors;
		}

		public override View GetView (int position, View convertView, ViewGroup parent) {
			View view = View.Inflate (context, Resource.Layout.account_color_theme, null);
			ImageView colorThemeImageView = view.FindViewById<ImageView> (Resource.Id.ColorThemeImageView);

			BackgroundColor color = BackgroundColor.AllColors [position];
			colorThemeImageView.SetBackgroundDrawable ( context.Resources.GetDrawable( color.GetStretchableColorSelectionSquareResource ()));

			return view;
		}

		public override View GetDropDownView (int position, View convertView, ViewGroup parent) {
			return GetView (position, convertView, parent);
		}

		public override int Count {
			get { return colors.Length; }
		}
			
		public override long GetItemId (int position) {
			return (long) position;
		}

		public override BackgroundColor this [int index] { 
			get { return colors [index]; }
		}
	}
}