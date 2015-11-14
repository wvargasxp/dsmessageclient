using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Telephony;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using em;

namespace Emdroid {
	public class MobileSignInFragment : SignInFragment {

		MobileSignInModel model;

		#region UI
		Button leftBarButton;
		TextView phonePrefix;
		EditText mobileNumberTextField;
		#endregion

		public static MobileSignInFragment NewInstance () {
			return new MobileSignInFragment ();
		}

		#region lifecycle - sorted order
		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			model = new MobileSignInModel ();
			model.DidFailToRegister += () => {
				var dialog = new AndroidModalDialogs ();
				dialog.ShowBasicOKMessage ("APP_TITLE".t (), "ERROR_REGISTER_MOBILE_EXPLAINATION".t (), (sender, args) => { });
			};

			model.ShouldPauseUI = PauseUI;
			model.ShouldResumeUI = ResumeUI;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.MobileSignInPage, container, false);
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
				KeyboardUtil.HideKeyboard (this.mobileNumberTextField);
				FragmentManager.PopBackStack ();
			};
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			titleTextView.Text = "MOBILE_TITLE".t ();
			#endregion

			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState); 

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			phonePrefix = View.FindViewById<TextView> (Resource.Id.phonePrefix);
			phonePrefix.Typeface = FontHelper.DefaultFont;
			phonePrefix.Text = "+" + CurrentCountry.phonePrefix;

			mobileNumberTextField = View.FindViewById<EditText> (Resource.Id.mobileNumberTextField);

			string actualPhoneNumber = null;
			var tMgr = (TelephonyManager) EMApplication.GetCurrentActivity().GetSystemService(Context.TelephonyService);
			var number = tMgr.Line1Number;
			if(string.IsNullOrEmpty(number) && Profile != null && Profile.PossiblePhoneNumbers ().Count > 0)
				number = Profile.PossiblePhoneNumbers () [0];

			if(!string.IsNullOrEmpty(number)) {
				if (number.Contains (phonePrefix.Text))
					actualPhoneNumber = number.Replace (phonePrefix.Text, "");
				else {
					string prefix = phonePrefix.Text.Replace (" ", "");
					if (number.Contains (prefix))
						actualPhoneNumber = number.Replace (prefix, "");
				}
			}

			if(actualPhoneNumber != null) {
				mobileNumberTextField.Text = actualPhoneNumber;
				//mobileNumberTextField.SetSelectAllOnFocus (true);
			}
			
			mobileNumberTextField.TextChanged += (sender, e) => UpdateContinueButtonStatus ();
			mobileNumberTextField.EditorAction += HandleGoAction;
			mobileNumberTextField.FocusChange += (sender, e) => {
				if (!e.HasFocus) {
					KeyboardUtil.HideKeyboard (this.mobileNumberTextField); //dismiss keyboard when clicking outside
				}
			};

			ContinueButton.Click += (sender, e) => {
				if (AppEnv.SKIP_ONBOARDING) {
					EMApplication _application = EMApplication.GetInstance ();
					model.Register (_application.appModel.account, mobileNumberTextField.Text, SelectedCountry.countryCode, SelectedCountry.phonePrefix, HandleRegistrationComplete);
					return;
				}

				var title = "VERIFICATION_TITLE".t ();
				var numberToMessage = string.Format("+{0}{1}", SelectedCountry.phonePrefix, mobileNumberTextField.Text);
				var message = string.Format("SEND_VALIDATION_CODE_EXPLAINATION".t (), numberToMessage);
				var action = "YES".t ();

				var builder = new AlertDialog.Builder (Activity);

				builder.SetTitle (title);
				builder.SetMessage (message);
				builder.SetPositiveButton(action, (s1, dialogClickEventArgs) => {
					EMApplication application = EMApplication.GetInstance ();
					model.Register (application.appModel.account, mobileNumberTextField.Text, SelectedCountry.countryCode, SelectedCountry.phonePrefix, HandleRegistrationComplete);
				});
				builder.SetNegativeButton("EDIT_BUTTON".t (), (s2, dialogClickEventArgs) => { });
				builder.Create ();
				builder.Show ();
			};

			UpdateContinueButtonStatus ();
			AnalyticsHelper.SendView ("Mobile Sign In View");

			if (AppEnv.SKIP_ONBOARDING) {
				mobileNumberTextField.Text = AppEnv.SkipOnboardingMobileToRegisterWith;
				ContinueButton.Enabled = true;
				ContinueButton.PerformClick ();
			}
		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnResume () {
			base.OnResume ();
			KeyboardUtil.ShowKeyboard (this.mobileNumberTextField);
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

		private void HandleRegistrationComplete (string accountId) {
			this.Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.onboarding_frame, MobileVerificationFragment.NewInstance (accountId))
				.AddToBackStack (null)
				.Commit ();
		}

		public override void DidSelectCountry(object sender, AdapterView.ItemSelectedEventArgs e) {
			base.DidSelectCountry (sender, e);

			phonePrefix.Text = "+" + SelectedCountry.phonePrefix;

			UpdateContinueButtonStatus ();
		}

		void UpdateContinueButtonStatus () {
			ContinueButton.Enabled = model.InputIsValid (SelectedCountry.countryCode, mobileNumberTextField.Text);
		}

		void HandleGoAction(object sender, TextView.EditorActionEventArgs e) {
			e.Handled = false; 
			if (e.ActionId == ImeAction.Go && model.InputIsValid (SelectedCountry.countryCode, mobileNumberTextField.Text)) {
				ContinueButton.PerformClick ();
				e.Handled = true;   
			}
		}
	}
}