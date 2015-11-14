using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using em;
using EMXamarin;

namespace Emdroid
{
	public class AboutFragment : Fragment {

		Button leftBarButton;
		TextView version, credits, privacy, eula;
		ImageButton facebookButton, twitterButton;

		public static AboutFragment NewInstance () {
			return new AboutFragment ();
		}

		#region showing verbose status updates
        private int _secretTapCount = 0;
        private int SecretTapCount { get { return this._secretTapCount; } set { this._secretTapCount = value; } }

        private long _startMillis = 0;
		private long StartMillis { get { return this._startMillis; } set { this._startMillis = value; } }

		private void ViewTouchEvent (object sender, View.TouchEventArgs e) {
			MotionEventActions eventAction = e.Event.Action;
			if (eventAction == MotionEventActions.Up) {
				//get system current milliseconds
				long time = Java.Lang.JavaSystem.CurrentTimeMillis ();

				//if it is the first time, or if it has been more than 3 seconds since the first tap ( so it is like a new try), we reset everything 
				if (this.StartMillis == 0 || (time - this.StartMillis > 3000)) {
					this.StartMillis = time;
					this.SecretTapCount = 1;
				} else {
					//it is not the first, and it has been less than 3 seconds since the first
					this.SecretTapCount++;
				}

				if (this.SecretTapCount == 10) {
					EMApplication.Instance.appModel.ShowVerboseMessageStatusUpdates = true;
		
					EditText input = new EditText (this.Activity);
					input.InputType = Android.Text.InputTypes.ClassText;

					AlertDialog.Builder builder = new AlertDialog.Builder (this.Activity);

					builder.SetTitle ("APP_TITLE".t ());
					builder.SetMessage (" \t\n\n                              (_)(_)\n                             /     \\\n                            /       |\n                           /   \\  * |\n             ________     /    /\\__/\n     _      /        \\   /    /\n    / \\    /  ____    \\_/    /\n   //\\ \\  /  /    \\         /\n   V  \\ \\/  /      \\       /\n       \\___/        \\_____/");
					builder.SetNegativeButton("OK_BUTTON".t (), (gs, dialogClickEventArgs) => { 
						string text = input.Text;
						if (text.Length > 0) {
							AppEnv.SetDomainTo (text);
							AppEnv.SwitchHttpProtocolToHTTP ();
							AppEnv.SwitchSecureWebsocketsToUnsecured ();
							Toast.MakeText (this.Activity, "Changed ip to " + text  + " and changed https to http and changed wss to ws.", ToastLength.Short).Show ();
						}
					});
					builder.SetView (input);
					builder.Create ();
					builder.Show ();


					this.SecretTapCount = 0;
				}
			}
		}
		#endregion

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View view = inflater.Inflate(Resource.Layout.about, container, false);
			view.Touch += WeakDelegateProxy.CreateProxy<object, View.TouchEventArgs> (ViewTouchEvent).HandleEvent<object, View.TouchEventArgs>;
			ThemeController (view);
			return view;
		}

		public override void OnDestroy () {
			base.OnDestroy ();
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		public void ThemeController () {
			ThemeController (this.View);
		}

		public void ThemeController (View v) {
			if (this.IsAdded && v != null) {
				EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme.GetBackgroundResource ((string file) => {
					if (v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
					}
				});
			}
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			View.FindViewById<TextView> (Resource.Id.titleTextView).Text = "ABOUT_TITLE".t ();
			View.FindViewById<TextView> (Resource.Id.titleTextView).Typeface = FontHelper.DefaultFont;

			leftBarButton = View.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += (sender, e) => FragmentManager.PopBackStack ();
			leftBarButton.Typeface = FontHelper.DefaultFont;

			View.FindViewById<TextView> (Resource.Id.appTitle).Typeface = FontHelper.DefaultBoldFont;
			version = View.FindViewById<TextView> (Resource.Id.appVersion);
			version.Typeface = FontHelper.DefaultFont;
			PackageInfo info = EMApplication.GetMainContext ().PackageManager.GetPackageInfo (EMApplication.GetMainContext ().PackageName, 0);
			version.Text = string.Format ("VERSION".t (), info.VersionName + " (" + BranchInfo.BRANCH_NAME + ")");

			credits = View.FindViewById<TextView> (Resource.Id.credits);
			credits.Typeface = FontHelper.DefaultFont;
			credits.Clickable = true;
			credits.Click += (sender, e) => goToInternalWebView ("CREDITS".t (), "CREDITS_URL".t ());

			privacy = View.FindViewById<TextView> (Resource.Id.privacy);
			privacy.Typeface = FontHelper.DefaultFont;
			privacy.Clickable = true;
			privacy.Click += (sender, e) => goToInternalWebView ("PRIVACY_POLICY".t (), "PRIVACY_POLICY_URL".t ());

			eula = View.FindViewById<TextView> (Resource.Id.eula);
			eula.Typeface = FontHelper.DefaultFont;
			eula.Clickable = true;
			eula.Click += (sender, e) => goToInternalWebView ("EULA".t (), "EULA_URL".t ());

			facebookButton = View.FindViewById<ImageButton> (Resource.Id.facebook);
			facebookButton.Click += (sender, e) => goToUrl ("FACEBOOK_URL".t (), "com.facebook.katana", null, SocialMediaType.Facebook);

			twitterButton = View.FindViewById<ImageButton> (Resource.Id.twitter);
			twitterButton.Click += (sender, e) => goToUrl ("TWITTER_URL".t (), "com.twitter.android", "com.twitter.android.ProfileActivity", SocialMediaType.Twitter);

			AnalyticsHelper.SendView ("About View");
		}

		void goToInternalWebView(string title, string url) {
			var fragment = AboutWebFragment.NewInstance (title, url);

			Activity.FragmentManager.BeginTransaction ()
				.SetTransition (FragmentTransit.FragmentOpen)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit();
		}

		void goToUrl (String url, string package, string classname, SocialMediaType type) {
			if(AndroidDeviceInfo.IsPackageInstalled(package, View.Context)) {
				Intent intent = null;
				if(type == SocialMediaType.Facebook) {
					var id = AndroidDeviceInfo.Locale().ToLower().Equals("ar") ? Constants.EM_FACEBOOK_ID_ARABIA : Constants.EM_FACEBOOK_ID_DEFAULT;
					var uri = Android.Net.Uri.Parse("fb://page/" + id);
					intent = new Intent (Intent.ActionView, uri);
				} else if (type == SocialMediaType.Twitter) {
					intent = new Intent(Intent.ActionView);
					intent.SetClassName (package, classname);
					var id = AndroidDeviceInfo.Locale().ToLower().Equals("ar") ? Constants.EM_TWITTER_ID_ARABIA : Constants.EM_TWITTER_ID_DEFAULT;
					intent.PutExtra ("user_id", id);
				}

				this.Activity.StartActivity(intent);
			} else {
				Android.Net.Uri uriUrl = Android.Net.Uri.Parse (url);
				var launchBrowser = new Intent(Intent.ActionView, uriUrl);
				this.Activity.StartActivity (launchBrowser);
			}
		}
	}

	public enum SocialMediaType {
		Facebook,
		Twitter
	}
}