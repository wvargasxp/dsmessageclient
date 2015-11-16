using System;
using Android.App;
using Android.OS;
using Android;
using Android.Content;
using Android.Content.PM;
using System.IO;
using em;
using EMXamarin;
using Android.Widget;

namespace Emdroid {
	[Activity (Label="EM", Theme = "@style/Theme.Splash")]
	// IMAGES
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="image/bmp")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="image/png")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="image/jpeg")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="image/gif")]
	// VIDEO
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="video/quicktime")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="video/mpeg")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="video/mp4")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="video/MP2T")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="video/H264")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="video/3gpp")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="application/x-mpegURL")]
	// AUDIO
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="audio/mp3")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="audio/mpeg")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="audio/aac")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="audio/amr")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="audio/ac3")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="audio/ogg")]
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="audio/3gpp")]
	// PLAINTEXT
	[IntentFilter (new[] {Intent.ActionSend}, Categories=new[]{Intent.CategoryDefault}, DataMimeType="text/plain")]

	public class ShareIntentActivity : Activity {

		public static string TEXT_INTENT_KEY = "ShareIntentText";
		public static string MEDIA_INTENT_KEY = "ShareIntentMedia";

		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
			if (savedInstanceState == null)
				savedInstanceState = new Bundle ();

			bool isOnboarding = (bool)EMApplication.GetInstance ().appModel.GetSessionInfo ()["isOnboarding"];
			PackageManager manager = this.PackageManager;

			EMTask.DispatchBackground (() => {
				Intent launchIntent = manager.GetLaunchIntentForPackage("me.emwith");
				launchIntent.AddCategory(Intent.CategoryLauncher);
				var mainIntent = new Intent (this, typeof (MainActivity));
				mainIntent.SetAction (Intent.ActionSend);

				if (Intent.Extras.Get (Intent.ExtraText) != null) {
					savedInstanceState.PutString (ShareIntentActivity.TEXT_INTENT_KEY, (string)(Intent.Extras.Get (Intent.ExtraText)));
				} else {
					Android.Net.Uri androidURI = Android.Net.Uri.Parse ((string)(Intent.Extras.Get (Intent.ExtraStream)));
					savedInstanceState.PutString (ShareIntentActivity.MEDIA_INTENT_KEY, androidURI.ToString ());
				}

				mainIntent.PutExtras (savedInstanceState);
				mainIntent.SetFlags (ActivityFlags.ClearTask);

				EMTask.DispatchMain (() => {
					this.StartActivity (launchIntent);
					if (!isOnboarding) {
						this.StartActivity (mainIntent);
					}
					else {
						Toast onboardNotify = Toast.MakeText (this, "SHARE_MEDIA_REGISTER_FIRST".t (), ToastLength.Short);
						onboardNotify.Show ();
					}
				});
			});

			Finish ();
		}
	}
}

