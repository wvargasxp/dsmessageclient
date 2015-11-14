using System;
using System.IO;
using Android.App;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidHUD;
using em;

namespace Emdroid {

	public class EditAliasFragment : BasicAccountFragment {

		public string AliasServerID;

		private HiddenReference<ApplicationModel> _appModel;
		private ApplicationModel appModel {
			get { return this._appModel != null ? this._appModel.Value : null; }
			set { this._appModel = new HiddenReference<ApplicationModel> (value); }
		}

		private HiddenReference<SharedEditAliasController> _shared;
		private SharedEditAliasController sharedEditAliasController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedEditAliasController> (value); }
		}

		public AbstractEditAliasController SharedController { get { return sharedEditAliasController; } }

		bool aquiringThumbnail = false;
		bool aquiringIcon = false;

		#region UI
		public Button DeleteButton;
		ImageView aliasIcon;
		TextView chooseIconText, nameText, nameDescriptionText;
		#endregion

		public static EditAliasFragment NewInstance (string aid) {
			var f = new EditAliasFragment ();
			f.AliasServerID = aid;
			f.sharedEditAliasController = new SharedEditAliasController (f.appModel, f);
			f.sharedEditAliasController.SetInitialAlias (aid);

			return f;
		}

		public EditAliasFragment() {
			appModel = EMApplication.GetInstance ().appModel;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.alias_edit, container, false);
			sharedEditAliasController.ColorTheme.GetBackgroundResource ((string file) => {
				if (v != null && this.Resources != null) {
					BitmapSetter.SetBackgroundFromFile (v, this.Resources, file);
				}
			});
			this.ProgressBar = v.FindViewById <ProgressBar> (Resource.Id.ProgressBar);
			this.LeftBarButton = v.FindViewById<Button> (Resource.Id.LeftBarButton);

			this.RightBarButton = v.FindViewById<Button> (Resource.Id.RightBarButton);
			this.RightBarButton.Typeface = FontHelper.DefaultFont;
			this.RightBarButton.SetBackgroundColor (Color.Transparent);

			this.NameEditText = v.FindViewById<EditText> (Resource.Id.NameEditText);

			this.ColorThemeSpinner = v.FindViewById<Spinner> (Resource.Id.ColorThemeSpinner);
			this.ColorThemeButton = v.FindViewById<ImageView> (Resource.Id.ColorThemeButton);
			this.ColorThemeText = v.FindViewById<TextView> (Resource.Id.ColorThemeLabel);
			this.ThumbnailBackgroundView = v.FindViewById<ImageView> (Resource.Id.ThumbnailBackgroundView);
			this.ThumbnailButton = v.FindViewById<ImageView> (Resource.Id.ThumbnailButton);

			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);
			TextView title = View.FindViewById<TextView> (Resource.Id.starterLabel);
			title.Typeface = FontHelper.DefaultFont;
			title.Text = AliasServerID == null ? "ADD_ALIAS_BUTTON".t () : "EDIT_ALIAS_TITLE".t ();

			this.LeftBarButton.Visibility = AliasServerID == null ? ViewStates.Visible : ViewStates.Gone;

			this.NameEditText.EditorAction += HandleDoneAction;
			this.NameEditText.FocusChange += (sender, e) => {
				if (!e.HasFocus) {
					KeyboardUtil.HideKeyboard (this.View);
				}
			};
			this.NameEditText.AfterTextChanged += (object sender, Android.Text.AfterTextChangedEventArgs e) => {
				this.TextInDisplayField = this.NameEditText.Text;
			};

			nameDescriptionText = View.FindViewById<TextView> (Resource.Id.NameDescriptionLabel);

			nameText = View.FindViewById<TextView> (Resource.Id.NameText);
			nameText.Typeface = FontHelper.DefaultFont;
			nameText.Text = sharedEditAliasController.Alias.displayName;

			DeleteButton = View.FindViewById<Button> (Resource.Id.DeleteAliasButton);
			sharedEditAliasController.Alias.colorTheme.GetButtonResource ((Drawable drawable) => {
				DeleteButton.SetBackgroundDrawable (drawable);
			});
			DeleteButton.Typeface = FontHelper.DefaultFont;
			DeleteButton.Click += DidTapDelete;

			if (AliasServerID == null) {
				this.NameEditText.Visibility = ViewStates.Visible;
				nameDescriptionText.Visibility = ViewStates.Visible;
				nameText.Visibility = ViewStates.Gone;
				DeleteButton.Visibility = ViewStates.Gone;
				KeyboardUtil.ShowKeyboard (this.NameEditText);
			} else {
				this.NameEditText.Visibility = ViewStates.Gone;
				nameDescriptionText.Visibility = ViewStates.Gone;
				nameText.Visibility = ViewStates.Visible;
				DeleteButton.Visibility = ViewStates.Visible;
			}
	
			chooseIconText = View.FindViewById<TextView> (Resource.Id.ChooseIconLabel);
		
			this.ThumbnailButton.Click += (sender, e) => { 
				KeyboardUtil.HideKeyboard (this.View);
				DidTapThumbnail();
			};

			aliasIcon = View.FindViewById<ImageView> (Resource.Id.AliasIconButton);
			aliasIcon.Click += (sender, e) => { 
				KeyboardUtil.HideKeyboard (this.View);
				DidTapIcon();
			};

			ThemeController ();
			UpdateThumbnailPicture ();
			UpdateIcon ();

			CenterText ();

			AnalyticsHelper.SendView ("Edit Alias View");
		}

		protected override void Dispose(bool disposing) {
			sharedEditAliasController.Dispose ();
			base.Dispose (disposing);
		}

		public override void OnResume () {
			base.OnResume ();
		}

		public override void OnPause () {
			base.OnPause ();
		}

		void HandleDoneAction(object sender, TextView.EditorActionEventArgs e) {
			e.Handled = false; 
			if (e.ActionId == ImeAction.Done) {
				KeyboardUtil.HideKeyboard (this.View);
				e.Handled = true;   
			}
		}

		void DidTapThumbnail() {
			aquiringThumbnail = true;
			this.UsingSquareCropper = false;
			StartAcquiringImage ();
		}

		void DidTapIcon() {
			aquiringIcon = true;
			this.UsingSquareCropper = true;
			StartAcquiringImage ();
		}

		void DidTapSave(object sender, EventArgs e) {
			AndHUD.Shared.Show (this.Activity, "WAITING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null);

			sharedEditAliasController.SaveOrUpdateAliasAsync ();
		}

		void DidTapDelete(object sender, EventArgs e) {
			sharedEditAliasController.DeleteAliasAsync (AliasServerID, true);
		}

		protected void DidSelectColorTheme (object sender, AdapterView.ItemSelectedEventArgs e) {
			sharedEditAliasController.Alias.colorTheme = BackgroundColor.AllColors [e.Position];
			ThemeController ();
		}

		public void UpdateIcon (bool expectingMediaToDownload = false, bool useRefs = true) {
			if ( IsAdded )
				BitmapSetter.SetImage (sharedEditAliasController.Alias.iconMedia, this.Resources, aliasIcon, Resource.Drawable.Icon, 100);
		}

		public void UpdateNameField () {
			EMTask.DispatchMain (() => {
				if (this.IsAdded && this.NameEditText != null)
					this.NameEditText.Text = sharedEditAliasController.Alias.displayName;
			});
		}

		public void CenterText() {
			var center = 57.5 * Resources.DisplayMetrics.Density; //35dp from thumbnail button, image is 45 dp wide, so center of image is at 57.5dp

			//first, center color theme label
			var ctWidth = this.ColorThemeText.Paint.MeasureText (Resources.GetString (Resource.String.COLOR_THEME));
			var layoutParamsColorTheme = (RelativeLayout.LayoutParams) this.ColorThemeText.LayoutParameters;
			layoutParamsColorTheme.RightMargin = (int)(center - (ctWidth / 2));
			this.ColorThemeText.LayoutParameters = layoutParamsColorTheme;

			//next, center choose icon label
			var sWidth = chooseIconText.Paint.MeasureText (Resources.GetString (Resource.String.SHARE_PROFILE));
			var layoutParamsShare = (RelativeLayout.LayoutParams) chooseIconText.LayoutParameters;
			layoutParamsShare.LeftMargin = (int)(center - (sWidth / 2));
			chooseIconText.LayoutParameters = layoutParamsShare;
		}

		protected override int PopupMenuInflateResource () {
			return Resource.Menu.account_thumbnail_options;
		}

		protected override View PopupMenuAnchorView () {
			return aquiringThumbnail ? this.ThumbnailButton : aliasIcon;
		}
			
		protected override void DidAcquireMedia (string mediaType, string path) {
			if (path != null) {
				byte[] fileAtPath = File.ReadAllBytes (path);
				if (fileAtPath != null) {
					if (aquiringThumbnail) {
						string aliasThumbnailPath = sharedEditAliasController.GetStagingFilePathForAliasThumbnail ();
						appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (aliasThumbnailPath);
						appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (aliasThumbnailPath, fileAtPath, null);
						sharedEditAliasController.Alias.UpdateThumbnailUrlAfterMovingFromCache (aliasThumbnailPath);
						sharedEditAliasController.UpdatedThumbnail = fileAtPath;
						UpdateThumbnailPicture ();
					} else if (aquiringIcon) {
						string aliasIconPath = sharedEditAliasController.GetStagingFilePathForAliasIconThumbnail ();
						appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (aliasIconPath);
						appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (aliasIconPath, fileAtPath, null);
						sharedEditAliasController.Alias.UpdateIconUrlAfterMovingFromCache (aliasIconPath);
						sharedEditAliasController.UpdatedIcon = fileAtPath;
						UpdateIcon (false, false);
					}
				}
			}

			aquiringThumbnail = false;
			aquiringIcon = false;
		}

		protected override bool AllowsImageCropping () {
			return true;
		}

		public override BackgroundColor ColorTheme {
			get { return sharedEditAliasController.ColorTheme; }
		}

		public override CounterParty CounterParty {
			get { return sharedEditAliasController.Alias; }
		}

		public override string TextInDisplayField {
			get { return sharedEditAliasController.TextInDisplayNameField; }
			set { sharedEditAliasController.TextInDisplayNameField = value; }
		}

		private void Exit () {
			this.SharedController.UserChoseToLeaveUponBeingAsked = true;
			this.FragmentManager.PopBackStack ();
		}

		public override void LeftBarButtonClicked (object sender, EventArgs e) {
			if (this.sharedEditAliasController.ShouldStopUserFromExiting) {
				string title = "ALERT_ARE_YOU_SURE".t ();
				string message = "UNSAVED_CHANGES".t ();
				string action = "EXIT".t ();

				UserPrompter.PromptUserWithAction (title, message, action, Exit, this.Activity);
			} else {
				this.FragmentManager.PopBackStack ();
			}
		}

		public override void RightBarButtonClicked (object sender, EventArgs e) {
			DidTapSave (sender, e);
		}

		public override void ColorThemeSpinnerItemClicked (object sender, AdapterView.ItemSelectedEventArgs e) {
			DidSelectColorTheme (sender, e);
		}

		public override void AdditionalUIChangesOnResume () {
			UpdateIcon ();
		}

		public override void AdditionalThemeController () {
			sharedEditAliasController.Alias.colorTheme.GetButtonResource ((Drawable drawable) => {
				DeleteButton.SetBackgroundDrawable (drawable);
				UpdateIcon ();
			});
		}

		public override string ImageSearchSeedString { 
			get {
				string seedString = this.NameEditText != null ? this.NameEditText.Text : string.Empty;
				if (seedString.Equals (string.Empty)) {
					seedString = this.nameText != null ? this.nameText.Text : string.Empty;
				}
				return seedString;
			}
		}
	}

	public class SharedEditAliasController : AbstractEditAliasController {
		private WeakReference _r;
		private EditAliasFragment Self {
			get { return this._r != null ? this._r.Target as EditAliasFragment : null; }
			set {  this._r = new WeakReference (value); }
		}

		readonly ApplicationModel _appModel;

		public SharedEditAliasController(ApplicationModel appModel, EditAliasFragment fragment) : base (appModel) {
			_appModel = appModel;
			this.Self = fragment;
		}

		public override void ThumbnailUpdated () {
			EMTask.DispatchMain (() => {
				EditAliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.ThemeController ();
			});
		}

		public override void DidAliasActionFail (string message) {
			EMTask.DispatchMain (() => {
				EditAliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;

				AndHUD.Shared.Dismiss (self.Activity);

				var dialog = new AndroidModalDialogs ();
				dialog.ShowBasicOKMessage ("APP_TITLE".t (), message, (sender, args) => { });
			});
		}

		public override void DidSaveAlias(bool saved) {
			EMTask.DispatchMain (() => {
				EditAliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;

				if (saved) {
					EMTask.DispatchBackground (() => {
						#region saving intagram photo screenshot to share
						byte[] thumbnailBytes = null;
						if(UpdatedThumbnail != null) {
							thumbnailBytes = new byte[UpdatedThumbnail.Length];
							UpdatedThumbnail.CopyTo(thumbnailBytes, 0);
						}

						ShareHelper.GenerateInstagramSharableFile (_appModel, Alias, thumbnailBytes, () => { });

						// clear images in memory on success only
						UpdatedThumbnail = null;
						UpdatedIcon = null;
						#endregion
					});
				}

				AndHUD.Shared.Dismiss (self.Activity);
				self.FragmentManager.PopBackStack ();
			});
		}

		public override void DidDeleteAlias() {
			EMTask.DispatchMain (() => {
				EditAliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;

				AndHUD.Shared.Dismiss (self.Activity);

				self.FragmentManager.PopBackStack ();
			});
		}

		public override void DidChangeColorTheme() {
			EMTask.DispatchMain (() => {
				EditAliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.ThemeController ();
			});
		}
			
		public override void ConfirmWithUserDelete (String serverID, Action<bool> onCompletion) {
			EditAliasFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;

			var builder = new AlertDialog.Builder(self.Activity);
			builder.SetTitle ("ALERT_ARE_YOU_SURE".t ());
			builder.SetMessage ("ALIAS_DELETE_CONFIRMATION_MESSAGE".t ());
			builder.SetPositiveButton("DELETE_BUTTON".t (), (sender, dialogClickEventArgs) => {
				AndHUD.Shared.Show (self.Activity, "WAITING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null);
				onCompletion(true);
			});
			builder.SetNegativeButton("CANCEL_BUTTON".t (), (sender, dialogClickEventArgs) => onCompletion (false));
			builder.Create();
			builder.Show();
		}
	}
}