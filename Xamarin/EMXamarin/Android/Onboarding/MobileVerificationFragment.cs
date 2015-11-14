using System;
using System.Collections.Generic;
using Android.App;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidHUD;
using em;
using EMXamarin;

namespace Emdroid {
	
	public class MobileVerificationFragment : Fragment {

		public string accountID;

		private SharedVerificationController Shared { get; set; }

		#region UI
		EditText verificationTextField;
		Button leftBarButton;
		ImageButton continueButton;
		#endregion

		public static MobileVerificationFragment NewInstance (string acctId) {
			var fragment = new MobileVerificationFragment ();
			fragment.accountID = acctId;
			return fragment;
		}

		#region lifecyle - sorted
		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			if (savedInstanceState != null)
				accountID = savedInstanceState.GetString ("acct");

			this.Shared = new SharedVerificationController (EMApplication.Instance.appModel, this);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.MobileVerificationPage, container, false);
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
				KeyboardUtil.HideKeyboard (verificationTextField);
				FragmentManager.PopBackStack ();
			};
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			titleTextView.Text = "VERIFICATION_TITLE".t ();
			#endregion

			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			this.Shared.AccountID = accountID;

			TextView identifierLabel = View.FindViewById<TextView> (Resource.Id.identifierLabel);
			identifierLabel.Text = accountID;

			verificationTextField = View.FindViewById<EditText> (Resource.Id.verificationTextField);
			verificationTextField.InputType = Android.Text.InputTypes.TextFlagNoSuggestions;
			verificationTextField.TextChanged += (sender, e) => UpdateContinueButtonStatus ();
			verificationTextField.EditorAction += HandleGoAction;
			verificationTextField.FocusChange += (sender, e) => {
				if (e.HasFocus) {
					KeyboardUtil.ShowKeyboard (this.verificationTextField);
				} else {
					KeyboardUtil.HideKeyboard (this.verificationTextField);
				}
			};

			ApplicationModel applicationModel = EMApplication.GetInstance ().appModel;

			continueButton = View.FindViewById<ImageButton> (Resource.Id.continueButton);
			var states = new StateListDrawable ();
			EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme.GetChatSendButtonResource ((string filepath) => {
				if (states != null && continueButton != null) {
					states.AddState (new int[] {Android.Resource.Attribute.StateEnabled}, Drawable.CreateFromPath (filepath));
					states.AddState (new int[] {}, Resources.GetDrawable (Resource.Drawable.iconSendDisabled));
					continueButton.SetImageDrawable (states);
					continueButton.Click += (sender, e) => {
						verificationTextField.ClearFocus();
						verificationTextField.RequestFocus();
						this.Shared.TryToLogin (verificationTextField.Text);
					};
				}
			});
			UpdateContinueButtonStatus ();

			// reset verification code parameter here to prevent confusing behavior when the verfiy url is triggered while the app on a page other than verification.
			ISecurityManager security2 = applicationModel.platformFactory.GetSecurityManager ();
			security2.RemoveSecureKeyValue (Constants.URL_QUERY_VERIFICATION_CODE_KEY);
			security2.removeSecureField (Constants.URL_QUERY_VERIFICATION_CODE_KEY);

			if (AppEnv.SKIP_ONBOARDING) {
				verificationTextField.Text = AppEnv.SkipOnboardingVerificationCode;
				continueButton.Enabled = true;
				continueButton.PerformClick ();
			} else {
				var dialog = new AndroidModalDialogs ();
				dialog.ShowBasicOKMessage ("APP_TITLE".t (), "SEND_VERIFICATION_EXPLAINATION".t (), (sender, args) => { });
			}

			AnalyticsHelper.SendView ("Mobile Verification View");
		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnResume () {
			base.OnResume ();
			verificationTextField.RequestFocus ();
			this.Shared.CheckVerificationCodeReceivedViaUrl ();
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

		public override void OnSaveInstanceState (Bundle outState) {
			outState.PutString ("acct", accountID);
			base.OnSaveInstanceState (outState);
		}

		// Similar/same to SignInFragment
		#region blocking/unblocking ui
		public void PauseUI () {
			continueButton.Enabled = false;
			AndHUD.Shared.Show (Activity, null, -1, MaskType.Clear, default(TimeSpan?), null, true, null);
		}

		public void ResumeUI () {
			continueButton.Enabled = true;
			AndHUD.Shared.Dismiss (Activity);
		}
		#endregion

		void UpdateContinueButtonStatus () {
			continueButton.Enabled = this.Shared.InputIsValid (verificationTextField.Text);
		}

		void HandleGoAction(object sender, TextView.EditorActionEventArgs e) {
			e.Handled = false; 
			if (e.ActionId == ImeAction.Go && this.Shared.InputIsValid (verificationTextField.Text)) {
				continueButton.PerformClick ();
				e.Handled = true;   
			}
		}

		public void TriggerContinueButton () {
			this.continueButton.CallOnClick ();
		}

		public void UpdateTextFieldWithText (string text) {
			this.verificationTextField.Text = text;
		}

		public void DisplayAccountError () {
			var dialog = new AndroidModalDialogs ();
			dialog.ShowBasicOKMessage ("APP_TITLE".t (), "ERROR_VERIFY_ACCOUNT_EXPLAINATION".t (), (sender, args) => { });
		}

		public void DismissControllerAndFinishOnboarding () {
			//if this is an existing user, ask if they would like to get their historical messages
			MainActivity.AskToGetHistoricalMessages = true;
			//if this is an existing user, don't send user to Account Setup - just send them into inbox
			this.Activity.StartActivity (typeof(MainActivity));
			this.Activity.Finish ();
		}

		public void GoToAccountController () {
			// This flow starts the account activity.
			MainActivity.IsOnboarding = true;
			this.Activity.StartActivity (typeof(MainActivity));
			this.Activity.Finish ();
		}
	}

	class SharedVerificationController : AbstractMobileVerificationController {
		private WeakReference _r = null;
		private MobileVerificationFragment Controller {
			get { return this._r != null ? this._r.Target as MobileVerificationFragment : null; }
			set {
				this._r = new WeakReference (value);
			}
		}

		public SharedVerificationController (ApplicationModel g, MobileVerificationFragment t) : base (g) {
			this.Controller = t;
		}
			
		public override void ShouldPauseUI () {
			MobileVerificationFragment c = this.Controller;
			if (c != null) {
				c.PauseUI ();
			}
		}

		public override void ShouldResumeUI () {
			MobileVerificationFragment c = this.Controller;
			if (c != null) {
				c.ResumeUI ();
			}
		}

		public override void TriggerContinueButton () {
			MobileVerificationFragment c = this.Controller;
			if (c != null) {
				c.TriggerContinueButton ();
			}
		}

		public override void UpdateTextFieldWithText (string text) {
			MobileVerificationFragment c = this.Controller;
			if (c != null) {
				c.UpdateTextFieldWithText (text);
			}
		}

		public override void DisplayAccountError () {
			MobileVerificationFragment c = this.Controller;
			if (c != null) {
				c.DisplayAccountError ();
			}
		}

		public override void DismissControllerAndFinishOnboarding () {
			MobileVerificationFragment c = this.Controller;
			if (c != null) {
				c.DismissControllerAndFinishOnboarding ();
			}
		}

		public override void GoToAccountController () {
			MobileVerificationFragment c = this.Controller;
			if (c != null) {
				c.GoToAccountController ();
			}
		}
	}
}