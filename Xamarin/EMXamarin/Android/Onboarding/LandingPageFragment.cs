using Android.App;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using em;

namespace Emdroid {
	public class LandingPageFragment : Fragment {
		LandingPageModel model;

		#region UI
		WebView preview;
		Button continueButton;

		Button mobileButton;
		Button emailButton;
		#endregion

		bool reloadWebView;
		public bool ReloadWebView {
			get { return reloadWebView; }
			set { reloadWebView = value; }
		}

		public static LandingPageFragment NewInstance () {
			var fragment = new LandingPageFragment ();
			return fragment;
		}

		#region lifecycle - sorted
		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			model = new LandingPageModel ();
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.LandingPage, container, false);
//			BitmapSetter.SetBackground (v, Application.Context.Resources.GetDrawable (Resource.Drawable.lander));
			preview = v.FindViewById<WebView> (Resource.Id.previewWebView);
			preview.SetWebViewClient (new WebViewClient ());
			preview.Settings.JavaScriptEnabled = true;
			if (int.Parse (Build.VERSION.Sdk) < 18)
				//deprecated in API 18
				preview.Settings.SetRenderPriority (WebSettings.RenderPriority.High);
			
			preview.Settings.CacheMode = CacheModes.NoCache;
			preview.LoadUrl (model.PreviewViewURL);
			preview.SetBackgroundColor(Android.Graphics.Color.Transparent);
			BackgroundColor mainColor = EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme;
			foreach (BackgroundColor color in BackgroundColor.AllColors) {
				color.GetBackgroundResource ((string file) => {
					if (color == mainColor && !mainColor.ToHexString ().Equals (BackgroundColor.Gray.ToHexString ()) && v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile (v, this.Resources, file);
					}
				});
			}
			continueButton = v.FindViewById<Button> (Resource.Id.continueButton);
			mobileButton = v.FindViewById<Button> (Resource.Id.mobileTypeButton);
			emailButton = v.FindViewById<Button> (Resource.Id.emailTypeButton);

			#region setting the title bar 
			RelativeLayout titlebarLayout = v.FindViewById<RelativeLayout> (Resource.Id.titlebarlayout);
			TextView titleTextView = titlebarLayout.FindViewById<TextView> (Resource.Id.titleTextView);
			Button leftBarButton = titlebarLayout.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Visibility = ViewStates.Gone;
			titleTextView.Text = "WELCOME_TITLE".t ();
			titleTextView.Typeface = FontHelper.DefaultFont;
			#endregion

			if (model.didClickGetStarted) {
				continueButton.Visibility = ViewStates.Gone;
				mobileButton.Visibility = ViewStates.Visible;
				emailButton.Visibility = ViewStates.Visible;
			}

			return v;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			mobileButton.Click += (sender, e) => {
				var oba = this.Activity as OnboardingActivity;
				oba.SetSoftInputToAlwaysShow();

				Activity.FragmentManager.BeginTransaction ()
					.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
					.Replace (Resource.Id.onboarding_frame, MobileSignInFragment.NewInstance ())
					.AddToBackStack (null)
					.Commit ();
			};
			emailButton.Click += (sender, e) => {
				var oba = this.Activity as OnboardingActivity;
				oba.SetSoftInputToAlwaysShow();

				Activity.FragmentManager.BeginTransaction ()
					.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
					.Replace (Resource.Id.onboarding_frame, EmailSignInFragment.NewInstance ())
					.AddToBackStack (null)
					.Commit ();
			};

			continueButton.Typeface = FontHelper.DefaultFont;
			continueButton.Click += delegate {
				model.didClickGetStarted = true;

				bool versionLessThanJellyBeans = EMApplication.SDK_VERSION < Android.OS.BuildVersionCodes.JellyBean;
				if (versionLessThanJellyBeans) {
					// TODO: Animate this?
					continueButton.Visibility = ViewStates.Gone;
					mobileButton.Visibility = ViewStates.Visible;
					emailButton.Visibility = ViewStates.Visible;
				} else {
					ViewPropertyAnimator startAnimator = continueButton.Animate ();
					startAnimator.SetDuration (500);
					startAnimator.TranslationX (- continueButton.Width);
					startAnimator.WithEndAction (new Java.Lang.Runnable (() => {
						continueButton.Visibility = ViewStates.Gone;
						mobileButton.Visibility = ViewStates.Visible;
						emailButton.Visibility = ViewStates.Visible;
					}));
				}

			};

			Button blueSquare = View.FindViewById<Button> (Resource.Id.blueSquare);
			Button orangeSquare = View.FindViewById<Button> (Resource.Id.orangeSquare);
			Button pinkSquare = View.FindViewById<Button> (Resource.Id.pinkSquare);
			Button greenSquare = View.FindViewById<Button> (Resource.Id.greenSquare);
			Button[] buttons = { blueSquare, orangeSquare, pinkSquare, greenSquare };

			BackgroundColorChanger.SetActionOnButtons (this, buttons, color => { });	
			AnalyticsHelper.SendView ("Landing Page View");

			if (AppEnv.SKIP_ONBOARDING) {
				mobileButton.PerformClick ();
			}

			EMApplication.Instance.appModel.ContactsManagerBeginProcessingContacts ();
		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnResume () {
			base.OnResume ();

			if(preview != null && ReloadWebView)
				preview.LoadUrl (model.PreviewViewURL);
		}

		public override void OnPause () {
			base.OnPause ();
			ReloadWebView = true;
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
	}
}