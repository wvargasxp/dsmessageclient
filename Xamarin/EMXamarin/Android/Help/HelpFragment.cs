using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Content;
using em;

namespace Emdroid
{
	public class HelpFragment : Fragment {

		Button leftBarButton;
		TextView gettingstarted, tips, faq, support;

		public static HelpFragment NewInstance () {
			return new HelpFragment ();
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View view = inflater.Inflate(Resource.Layout.help, container, false);

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

			View.FindViewById<TextView> (Resource.Id.appTitle).Typeface = FontHelper.DefaultBoldFont;

			View.FindViewById<TextView> (Resource.Id.titleTextView).Text = "HELP_TITLE".t ();
			View.FindViewById<TextView> (Resource.Id.titleTextView).Typeface = FontHelper.DefaultFont;

			leftBarButton = View.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += (sender, e) => FragmentManager.PopBackStack ();
			leftBarButton.Typeface = FontHelper.DefaultFont;

			gettingstarted = View.FindViewById<TextView> (Resource.Id.gettingstarted);
			gettingstarted.Typeface = FontHelper.DefaultFont;
			gettingstarted.Clickable = true;
			gettingstarted.Click += (sender, e) => goToInternalWebView ("GETTING_STARTED".t (), "GETTING_STARTED_URL".t ());

			tips = View.FindViewById<TextView> (Resource.Id.tips);
			tips.Typeface = FontHelper.DefaultFont;
			tips.Clickable = true;
			tips.Click += (sender, e) => goToInternalWebView ("TIPS".t (), "TIPS_URL".t ());

			faq = View.FindViewById<TextView> (Resource.Id.faq);
			faq.Typeface = FontHelper.DefaultFont;
			faq.Clickable = true;
			faq.Click += (sender, e) => goToInternalWebView ("FAQ".t (), "FAQ_URL".t ());

			support = View.FindViewById<TextView> (Resource.Id.support);
			support.Typeface = FontHelper.DefaultFont;
			support.Clickable = true;
			support.Click += (sender, e) => goToExternalWebView ("SUPPORT_URL".t ());

			AnalyticsHelper.SendView ("Help View");
		}

		void goToInternalWebView(string title, string url) {
			var fragment = HelpWebFragment.NewInstance (title, url);

			Activity.FragmentManager.BeginTransaction ()
				.SetTransition (FragmentTransit.FragmentOpen)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit();
		}

		void goToExternalWebView(string url) {
			var ah = new AnalyticsHelper();
			ah.SendEvent(AnalyticsConstants.CATEGORY_UI_ACTION, AnalyticsConstants.ACTION_BUTTON_PRESS, string.Format(AnalyticsConstants.SUPPORT_LINK, AndroidDeviceInfo.Language()), 0);

			var uri = Android.Net.Uri.Parse (url);
			var intent = new Intent (Intent.ActionView, uri); 
			StartActivity (intent);
		}
	}
}