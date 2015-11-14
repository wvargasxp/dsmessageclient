using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidHUD;
using em;

namespace Emdroid
{
	public class AboutWebFragment: Fragment {

		string title;
		string url;

		Button leftBarButton;
		WebView web_view;

		public static AboutWebFragment NewInstance (string title, string url) {
			var fragment = new AboutWebFragment ();
			fragment.title = title;
			fragment.url = url;
			return fragment;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.about_web, container, false);

			web_view = v.FindViewById<WebView> (Resource.Id.webview);
			web_view.SetWebViewClient (new LoadingWebViewClient(Activity));
			web_view.Settings.JavaScriptEnabled = true;

			if (int.Parse (Build.VERSION.Sdk) < 18)
				//deprecated in API 18
				web_view.Settings.SetRenderPriority (WebSettings.RenderPriority.High);
			
			web_view.Settings.CacheMode = CacheModes.NoCache;
			web_view.LoadUrl (url);
			web_view.SetBackgroundColor(Android.Graphics.Color.Transparent);

			ThemeController (v);
			return v;
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

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override void OnDestroy () {
			base.OnDestroy ();
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		public override void OnSaveInstanceState (Bundle outState) {
			base.OnSaveInstanceState (outState);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			EMTask.DispatchMain (() => AndHUD.Shared.Show (Activity, "LOADING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null));

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			View.FindViewById<TextView> (Resource.Id.titleTextView).Text = title;
			View.FindViewById<TextView> (Resource.Id.titleTextView).Typeface = FontHelper.DefaultFont;

			leftBarButton = View.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += (sender, e) => FragmentManager.PopBackStack ();
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			AnalyticsHelper.SendView ("About Web View");
		}
	}
}