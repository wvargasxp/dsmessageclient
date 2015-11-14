using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using em;

namespace Emdroid {
	public class EmailSignInFragment : SignInFragment {

		EmailSignInModel model;

		#region UI
		Button leftBarButton;
		EditText emailTextField;
		#endregion

		public static EmailSignInFragment NewInstance () {
			return new EmailSignInFragment ();
		}

		#region lifecycle - sorted
		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			model = new EmailSignInModel ();
			model.DidFailToRegister += () => {
				var dialog = new AndroidModalDialogs ();
				dialog.ShowBasicOKMessage ("APP_TITLE".t (), "ERROR_REGISTER_EMAIL_EXPLAINATION".t (), (sender, args) => {});
			};

			model.ShouldPauseUI = PauseUI;
			model.ShouldResumeUI = ResumeUI;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.EmailSignInPage, container, false);
			EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme.GetBackgroundResource ((string file) => {
				if (v != null && this.Resources != null) {
					BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
				}
			});

			#region setting the title bar 
			RelativeLayout titlebarLayout = v.FindViewById<RelativeLayout> (Resource.Id.titlebarlayout);
			TextView titleTextView = titlebarLayout.FindViewById<TextView> (Resource.Id.titleTextView);
			titleTextView.Typeface = FontHelper.DefaultFont;

			leftBarButton = titlebarLayout.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += (sender, e) => {
				KeyboardUtil.HideKeyboard (this.emailTextField);
				FragmentManager.PopBackStack ();
			};
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			titleTextView.Text = "EMAIL_TITLE".t ();
			#endregion

			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState); 

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			emailTextField = View.FindViewById<EditText> (Resource.Id.emailTextField);

			if(Profile != null && Profile.PossibleEmails ().Count > 0) {
				emailTextField.Text = Profile.PossibleEmails () [0];
				//emailTextField.SetSelectAllOnFocus (true);
			}

			emailTextField.EditorAction += HandleGoAction;
			emailTextField.TextChanged += (sender, e) => UpdateContinueButtonStatus ();
			emailTextField.FocusChange += (sender, e) => {
				if (!e.HasFocus) {
					KeyboardUtil.HideKeyboard (this.emailTextField);
				}
			};

			ContinueButton.Click += (sender, e) => {
				var title = "VERIFICATION_TITLE".t ();
				var message = string.Format("SEND_VALIDATION_CODE_EXPLAINATION".t (), emailTextField.Text);
				var action = "YES".t ();

				var builder = new AlertDialog.Builder (Activity);

				builder.SetTitle (title);
				builder.SetMessage (message);
				builder.SetPositiveButton(action, (s1, dialogClickEventArgs) => {
					EMApplication application = EMApplication.GetInstance ();
					model.Register (application.appModel.account, emailTextField.Text, SelectedCountry.countryCode, SelectedCountry.phonePrefix, accountID => Activity.FragmentManager.BeginTransaction ().SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut).Replace (Resource.Id.onboarding_frame, MobileVerificationFragment.NewInstance (accountID)).AddToBackStack (null).Commit ());
				});
				builder.SetNegativeButton("EDIT_BUTTON".t (), (s2, dialogClickEventArgs) => { });
				builder.Create ();
				builder.Show ();
			};

			UpdateContinueButtonStatus ();

			AnalyticsHelper.SendView ("Email Sign In View");

			if (AppEnv.SKIP_ONBOARDING) {
				emailTextField.Text = AppEnv.SkipOnboardingEmailToRegisterWith;
				ContinueButton.Enabled = true;
				ContinueButton.PerformClick ();
			}
		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnResume () {
			base.OnResume ();

			KeyboardUtil.ShowKeyboard (this.emailTextField);
		}

		public override void OnPause () {
			base.OnPause ();
		}

		public override void OnStop () {
			base.OnStop ();
		}

		public override void OnDestroyView () {
			base.OnDestroyView ();
		}

		public override void OnDestroy () {
			base.OnDestroy ();
		}

		public override void OnDetach () {
			base.OnDetach ();
		}
		#endregion

		public override void DidSelectCountry(object sender, AdapterView.ItemSelectedEventArgs e) {
			base.DidSelectCountry (sender, e);

			UpdateContinueButtonStatus ();
		}

		void UpdateContinueButtonStatus () {
			ContinueButton.Enabled = model.InputIsValid (emailTextField.Text);
		}

		void HandleGoAction(object sender, TextView.EditorActionEventArgs e) {
			e.Handled = false; 
			if (e.ActionId == ImeAction.Go && model.InputIsValid (emailTextField.Text)) {
				ContinueButton.PerformClick ();
				e.Handled = true;   
			}
		}
	}
}