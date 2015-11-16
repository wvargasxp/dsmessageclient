using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Preferences;
using em;
using Google.Analytics.Tracking;

namespace Emdroid {
	[Activity (Theme = "@style/Theme.Splash", MainLauncher = true, NoHistory = true)]
	//[IntentFilter (new[]{Intent.ActionView}, Categories=new[]{Intent.CategoryDefault, Intent.CategoryBrowsable}, DataScheme="em")]
	public class SplashActivity : Activity {
		protected override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			// http://www.jjask.com/363784/avoid-click-android-shortcut-create-instance-and-kill-the-one
			// https://stackoverflow.com/questions/11296203/on-click-of-shortcut-on-homescreen-launching-from-spalsh-screen-in-android
			if (!this.IsTaskRoot) {
				this.Finish ();
				return;
			}
			//

			EMApplication.SetCurrentActivity (this);
			EMTask.DispatchBackground( () => {
				#region Google Analytics Campaign and Referrer Tracking
				// Get the intent that started this Activity.
				var intent = Intent;
				bool setGACampaignOrReferrer = false;

				string referrer = GoogleInterceptor.GetReferrer (EMApplication.GetMainContext().GetDir("em", FileCreationMode.Private));
				if (referrer != null) {
					recordReferrer (referrer);
					setGACampaignOrReferrer = true;
				} else if (intent != null) {
					Android.Net.Uri uri = intent.Data;

					if (uri != null) {
						System.Diagnostics.Debug.WriteLine("GA URI in Splash Activity: " + uri);

						string query = uri.Query;
						string path = uri.Path;

						if (!string.IsNullOrEmpty (query)) {
							recordReferrer (query);
							setGACampaignOrReferrer = true;
						} else if (!string.IsNullOrEmpty (path)) {
							recordReferrer (path);
							setGACampaignOrReferrer = true;
						}
					} else {
						System.Diagnostics.Debug.WriteLine("GA URI is null in Splash Activity.");
					}
				}

				if(!setGACampaignOrReferrer) {
					setGAContext();
				}
				#endregion
			});

			//add a shortcut to EM on home screen if one does not already exist
			//AddShortcutIfDoesNotExistAsync (EMApplication.GetMainContext ());

			ApplicationModel applicationModel = EMApplication.GetInstance ().appModel;
			Dictionary<string, object> sessionInfo = applicationModel.GetSessionInfo ();
			if ((bool)sessionInfo["isOnboarding"]) {
				StartActivity (typeof(OnboardingActivity));
			} else {
				StartActivity (typeof(MainActivity));
			}
		}

		protected override void OnResume () {
			base.OnResume ();
			AndroidAdjustHelper.Shared.Resume ();
		}

		protected override void OnPause () {
			base.OnPause ();
			AndroidAdjustHelper.Shared.Pause ();
		}

		protected override void OnDestroy () {
			MemoryUtil.ClearReferences (this);
			base.OnDestroy ();
		}

		static void setGAContext() {
			EasyTracker.GetInstance (EMApplication.GetMainContext ()).Set (Fields.TrackingId, EMApplication.TrackingId);
		}

		static void recordReferrer(string tracking) {
			setGAContext ();

			EMApplication.GetInstance ().appModel.account.RecordReferrer(tracking, success => {
				if (success) {
					System.Diagnostics.Debug.WriteLine ("GATRACKING Success recording tracking, now deleting file.");
					GoogleInterceptor.DeleteReferrer (EMApplication.GetMainContext ().GetDir ("em", FileCreationMode.Private));
				} else {
					System.Diagnostics.Debug.WriteLine ("GATRACKING Error recording tracking, not deleting file.");
				}
			});

			if (tracking.Contains ("utm_source=")) {
				int startIndex = tracking.IndexOf ("utm_source=") + 11;
				int endIndex = tracking.IndexOf ("&", startIndex);

				if (endIndex < startIndex)
					endIndex = tracking.Length - startIndex;

				var source = tracking.Substring (startIndex, endIndex);

				AnalyticsHelper.SendField (Fields.CampaignSource, source);
			}
		}

		void AddShortcutIfDoesNotExistAsync(Context context) {
			EMTask.DispatchBackground (() => {
				var appPreferences = PreferenceManager.GetDefaultSharedPreferences (context);
				var doesShortcutExist = appPreferences.GetBoolean ("doesShortcutExist", false);

				if (!doesShortcutExist) {
					var shortcut = new Intent ("com.android.launcher.action.INSTALL_SHORTCUT");

					ApplicationInfo appInfo = context.ApplicationInfo;

					// Shortcut name
					shortcut.PutExtra (Intent.ExtraShortcutName, "EM");
					shortcut.PutExtra ("duplicate", false); // Just create once

					// Setup activity shoud be shortcut object 
					var component = new ComponentName (appInfo.PackageName, this.Class.Name);
					shortcut.PutExtra (Intent.ExtraShortcutIntent, new Intent (Intent.ActionMain).SetComponent (component));

					// Set shortcut icon
					Intent.ShortcutIconResource iconResource = Intent.ShortcutIconResource.FromContext (context, appInfo.Icon);
					shortcut.PutExtra (Intent.ExtraShortcutIconResource, iconResource);

					context.SendBroadcast (shortcut);

					//Make preference true
					ISharedPreferencesEditor editor = appPreferences.Edit ();
					editor.PutBoolean ("doesShortcutExist", true);
					editor.Commit ();
				}
			});
		}

	}
}