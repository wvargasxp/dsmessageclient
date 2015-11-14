using System;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidHUD;
using em;

namespace Emdroid {
	public class ProfileFragment : BasicAccountFragment {

		private HiddenReference<ApplicationModel> _appModel;
		private ApplicationModel appModel {
			get { return this._appModel != null ? this._appModel.Value : null; }
			set { this._appModel = new HiddenReference<ApplicationModel> (value); }
		}

		CounterParty profile;

		private HiddenReference<SharedProfileController> _shared;
		private SharedProfileController sharedProfileController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedProfileController> (value); }
		}

		#region UI
		public Button AddContactButton, BlockContactButton, SendMessageButton;
		TextView nameText;
		#endregion

		public static ProfileFragment NewInstance (CounterParty cp) {
			var f = new ProfileFragment ();

			f.sharedProfileController = new SharedProfileController(f.appModel, f, cp);
			f.profile = f.sharedProfileController.Profile;

			return f;
		}

		public ProfileFragment() {
			appModel = EMApplication.GetInstance ().appModel;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.profile, container, false);
			profile.colorTheme.GetBackgroundResource ((string file) => {
				if (v != null && this.Resources != null) {
					BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
				}
			});
			this.LeftBarButton = v.FindViewById<Button> (Resource.Id.leftBarButton);
			ViewClickStretchUtil.StretchRangeOfButton (this.LeftBarButton);
			this.ThumbnailBackgroundView = v.FindViewById<ImageView> (Resource.Id.ThumbnailBackgroundView);
			this.ThumbnailButton = v.FindViewById<ImageView> (Resource.Id.ThumbnailButton);
			this.ProgressBar = v.FindViewById<ProgressBar> (Resource.Id.ProgressBar);
			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			View.FindViewById<TextView> (Resource.Id.titleTextView).Text = "MY_ACCOUNT_TITLE".t ();
			View.FindViewById<TextView> (Resource.Id.titleTextView).Typeface = FontHelper.DefaultFont;
		
			nameText = View.FindViewById<TextView> (Resource.Id.NameText);
			nameText.Typeface = FontHelper.DefaultFont;
			nameText.Text = profile.displayName;

			AddContactButton = View.FindViewById<Button> (Resource.Id.AddContactButton);
			AddContactButton.Typeface = FontHelper.DefaultFont;
			AddContactButton.Click += DidTapAddContact;
			sharedProfileController.DidChangeTempProperty (); // This will set the button text

			BlockContactButton = View.FindViewById<Button> (Resource.Id.BlockContactButton);
			BlockContactButton.Typeface = FontHelper.DefaultFont;
			BlockContactButton.Click += DidTapBlockContact;

			SendMessageButton = View.FindViewById<Button> (Resource.Id.SendMessageButton);
			SendMessageButton.Typeface = FontHelper.DefaultFont;
			SendMessageButton.Click += DidTapSendMessage;

			profile.colorTheme.GetButtonResource ((Drawable drawable) => {
				AddContactButton.SetBackgroundDrawable (drawable);
				BlockContactButton.SetBackgroundDrawable (drawable);
				SendMessageButton.SetBackgroundDrawable (drawable);
			});

			UpdateBlockAndSendMessageButton (profile as Contact);

			ThemeController ();
			UpdateThumbnailPicture ();

			AnalyticsHelper.SendView ("Profile View");
		}

		public void UpdateBlockAndSendMessageButton (Contact contact) {
			SendMessageButton.Enabled = true;
			SendMessageButton.Alpha = 1.0f;

			string blockButtonLabel = "BLOCK_CONTACT_BUTTON".t ();
			if (contact != null && contact.IsBlocked) {
				blockButtonLabel = "UNBLOCK_CONTACT_BUTTON".t ();

				SendMessageButton.Enabled = false;
				SendMessageButton.Alpha = 0.6f;
			}

			BlockContactButton.Text = blockButtonLabel;
		}

		protected override void Dispose(bool disposing) {
			sharedProfileController.Dispose ();
			base.Dispose (disposing);
		}

		public override void OnResume () {
			base.OnResume ();
		}

		public override void OnPause () {
			base.OnPause ();
		}

		void DidTapAddContact(object sender, EventArgs e) {
			AndHUD.Shared.Show (this.Activity, "WAITING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null);
			sharedProfileController.AddContactAsync (obj => AndHUD.Shared.Dismiss (this.Activity));
		}

		void DidTapBlockContact(object sender, EventArgs e) {
			AndHUD.Shared.Show (this.Activity, "WAITING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null);
			sharedProfileController.DidTapBlockButton (obj => AndHUD.Shared.Dismiss (this.Activity));
		}

		protected void DidTapSendMessage(object sender, EventArgs eventArgs) {
			sharedProfileController.SendMessage ();
		}

		public void TransitionToChatController (ChatEntry chatEntry) {
			var chatFragment = ChatFragment.NewInstance (chatEntry);
			var args = new Bundle ();
			var index = EMApplication.Instance.appModel.chatList.entries.IndexOf (chatEntry);
			args.PutInt ("Position", index >= 0 ? index : ChatFragment.NEW_MESSAGE_INITIATED_FROM_NOTIFICATION_POSITION);
			chatFragment.Arguments = args;
			Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, chatFragment, "chatEntry" + chatEntry.chatEntryID)
				.AddToBackStack ("chatEntry" + chatEntry.chatEntryID)
				.Commit ();
		}

		#region basic account 
		public override BackgroundColor ColorTheme { 
			get { return profile.colorTheme; }
		}

		public override CounterParty CounterParty { 
			get { return profile; }
		}

		// ProfileFragment doesn't have an edit text, so this field isn't needed.
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
	}

	public class SharedProfileController : AbstractProfileController {
		private WeakReference _r;
		private ProfileFragment Self {
			get { return this._r != null ? this._r.Target as ProfileFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedProfileController(ApplicationModel appModel, ProfileFragment pf, CounterParty cp) : base (appModel, cp) {
			this.Self = pf;
		}

		public override void DidChangeTempProperty () {
			ProfileFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			var contact = Profile as Contact;
			self.AddContactButton.Text = contact.tempContact.Value ? "ADD_CONTACT_BUTTON".t () : "REMOVE_CONTACT_BUTTON".t ();
		}

		public override void DidChangeBlockStatus (Contact c) {
			ProfileFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.UpdateBlockAndSendMessageButton (c);
		}

		public override void TransitionToChatController (ChatEntry chatEntry) {
			EMTask.DispatchMain (() => {
				ProfileFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.TransitionToChatController (chatEntry);
			});
		}
	}
}
