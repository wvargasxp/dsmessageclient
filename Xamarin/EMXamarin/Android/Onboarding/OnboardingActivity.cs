using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using Google.Analytics.Tracking;
using em;

namespace Emdroid {
	[Activity (Label = "OnboardingActivity", NoHistory = false, Theme = "@style/AppTheme.Dark", ScreenOrientation = ScreenOrientation.Portrait, AlwaysRetainTaskState = false, LaunchMode = LaunchMode.SingleTask)]			
	[IntentFilter (new[]{Intent.ActionView}, Categories=new[]{Intent.CategoryDefault, Intent.CategoryBrowsable}, DataScheme="em")]
	public class OnboardingActivity : Activity {

		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
			EMApplication.SetCurrentActivity (this);
			ApplicationModel appModel = EMApplication.GetInstance ().appModel;

			SetContentView (Resource.Layout.OnboardingPage);

			// Don't do a replace if there's already a fragment showing.
			var onScreenFragment = FragmentManager.FindFragmentById (Resource.Id.onboarding_frame);
			if (onScreenFragment == null) {
				
				Window.SetSoftInputMode (Android.Views.SoftInput.StateAlwaysHidden|Android.Views.SoftInput.AdjustResize);

				FragmentManager.BeginTransaction ()
					.SetTransition (FragmentTransit.FragmentOpen)
					.Replace (Resource.Id.onboarding_frame, LandingPageFragment.NewInstance ())
					.Commit();
			}

			EasyTracker.GetInstance (EMApplication.GetMainContext ()).ActivityStart (this);
		}

		protected override void OnResume () {
			base.OnResume ();
			EMApplication.SetCurrentActivity (this);
			AndroidAdjustHelper.Shared.Resume ();
		}

		/** hook that gets triggered by em:// calls
		 */
		protected override void OnNewIntent (Intent intent) {
			base.OnNewIntent (intent);

			if (intent.Action != null && intent.Action == Intent.ActionView) {
				Uri url = intent.Data;
				var customUrl = new System.Uri (url.ToString ());

				ApplicationModel appModel = EMApplication.GetInstance ().appModel;
				appModel.customUrlSchemeController.Handle (customUrl);
			}
		}

		protected override void OnPause () {
			base.OnPause ();
			AndroidAdjustHelper.Shared.Pause ();
		}

		protected override void OnDestroy () {
			MemoryUtil.ClearReferences (this);
			base.OnDestroy ();

			EasyTracker.GetInstance (EMApplication.GetMainContext ()).ActivityStop (this);
		}

		public void SetSoftInputToAlwaysShow() {
			Window.SetSoftInputMode (Android.Views.SoftInput.StateAlwaysVisible|Android.Views.SoftInput.AdjustResize);
		}
	}
}