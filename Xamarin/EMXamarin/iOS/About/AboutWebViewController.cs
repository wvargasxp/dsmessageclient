using System;
using CoreGraphics;
using Foundation;
using GoogleAnalytics.iOS;
using MBProgressHUD;
using UIKit;
using em;

namespace iOS
{
	public class AboutWebViewController : UIViewController {

		UIView lineView;
		UIWebView webView;

		MTMBProgressHUD progressHud;

		string title { get; set; }
		string url { get; set; }

		bool visible;
		public bool Visible {
			get { return visible; }
			set { visible = value; }
		}

		public AboutWebViewController(string title, string url) {
			this.title = title;
			this.url = url;
		}

		void ThemeController (UIInterfaceOrientation orientation) {
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			var mainColor = appDelegate.applicationModel.account.accountInfo.colorTheme;
			mainColor.GetBackgroundResourceForOrientation (orientation, (UIImage image) => {
				if (View != null && lineView != null) {
					View.BackgroundColor = UIColor.FromPatternImage (image);
					lineView.BackgroundColor = mainColor.GetColor ();
				}
			});


			if(NavigationController != null)
				UINavigationBarUtil.SetDefaultAttributesOnNavigationBar (NavigationController.NavigationBar);
		}

		public override void LoadView () {
			base.LoadView ();

			progressHud = new MTMBProgressHUD (View) {
				LabelText = "LOADING".t (),
				LabelFont = FontHelper.DefaultFontForLabels(),
				RemoveFromSuperViewOnHide = true
			};
			View.AddSubview (progressHud);
			progressHud.Show (true);

			lineView = new UINavigationBarLine (new CGRect (0, 0, View.Frame.Width, 1));
			lineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			View.Add (lineView);
			View.BringSubviewToFront (lineView);

			webView = new UIWebView (new CGRect (0, 1, View.Frame.Width, View.Frame.Height - lineView.Frame.Height));
			webView.AutoresizingMask = UIViewAutoresizing.FlexibleHeight | UIViewAutoresizing.FlexibleWidth;

			var req = new NSUrlRequest(NSUrl.FromString ( url ), NSUrlRequestCachePolicy.ReloadIgnoringLocalAndRemoteCacheData, 5);
			webView.LoadRequest (req);
			webView.Opaque = false;
			webView.BackgroundColor = UIColor.Clear;
			webView.LoadFinished += WeakDelegateProxy.CreateProxy<object, EventArgs> (HandlerWebViewLoadFinished).HandleEvent<object, EventArgs>;
			View.Add (webView);

			View.BringSubviewToFront (progressHud);
		}

		private void HandlerWebViewLoadFinished (object sender, EventArgs e) {
			progressHud.Hide (true);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			ThemeController (InterfaceOrientation);
			UINavigationBarUtil.SetBackButtonToHaveNoText (NavigationItem);

			Title = title;

			View.AutosizesSubviews = true;
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);
			this.Visible = true;
			// This screen name value will remain set on the tracker and sent with
			// hits until it is set to a new value or to null.
			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "About Web View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void ViewDidLayoutSubviews () {
			base.ViewDidLayoutSubviews ();

			ThemeController (InterfaceOrientation);

			nfloat displacement_y = this.TopLayoutGuide.Length;
			lineView.Frame = new CGRect (0, displacement_y, lineView.Frame.Width, lineView.Frame.Height);

			webView.Frame = new CGRect (0, displacement_y + lineView.Frame.Height, View.Frame.Width, View.Frame.Height - (displacement_y + lineView.Frame.Height));
		}

		public override void ViewDidDisappear (bool animated) {
			base.ViewDidDisappear (animated);
			this.Visible = false;
		}

		public override void WillAnimateRotation (UIInterfaceOrientation toInterfaceOrientation, double duration) {
			base.WillAnimateRotation (toInterfaceOrientation, duration);
			ThemeController (toInterfaceOrientation);
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}
	}
}