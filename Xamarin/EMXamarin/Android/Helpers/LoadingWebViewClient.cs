using Android.App;
using Android.Webkit;
using AndroidHUD;

namespace Emdroid {
	
	public class LoadingWebViewClient : WebViewClient {

		readonly Activity a;

		public LoadingWebViewClient(Activity activity) {
			a = activity;
		}

		public override void OnPageFinished (WebView view, string url) {
			base.OnPageFinished (view, url);

			AndHUD.Shared.Dismiss (a);
		}
	}
}