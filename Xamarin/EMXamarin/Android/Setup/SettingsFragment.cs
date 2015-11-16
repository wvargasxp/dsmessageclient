using System;
using Android.App;
using Android.OS;
using Android.Views;
using em;

namespace Emdroid {
	public class SettingsFragment : BasicSettingsFragment {

		private HiddenReference<SharedSettingsController> _shared;
		public SharedSettingsController Shared { 
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedSettingsController> (value); }
		}

		public static SettingsFragment NewInstance () {
			return new SettingsFragment ();
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
			this.Shared = new SharedSettingsController (this);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View view = inflater.Inflate (Resource.Layout.settings, container, false);
			ThemeController (view);
			return view;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);
			this.TitleTextView.Text = "SETTINGS_TITLE".t ();
			SetupListView ();

			AnalyticsHelper.SendView ("Settings View");
		}

		protected override void SetupListView () {
			base.SetupListView ();
			SettingsListAdapter adapter = new SettingsListAdapter (this);
			adapter.ItemClick += Adapter_ItemClick;
			this.ListView.SetAdapter (adapter);
		}

		private void Adapter_ItemClick (object sender, int e) {
			SettingMenuItem menuItemClicked = this.Shared.Settings [e];
			Fragment p = null;
			switch (menuItemClicked) {
			default:
			case SettingMenuItem.Push:
				{
					p = PushSettingsFragment.NewInstance ();
					break;
				}
			case SettingMenuItem.InAppSounds:
				{
					p = InAppSoundSettingsFragment.NewInstance ();
					break;
				}
			case SettingMenuItem.InAppSettings:
				{
					p = InAppSettingsFragment.NewInstance ();
					break;
				}
			}

			this.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, p)
				.AddToBackStack (null)
				.Commit ();
		}
	}

	public class SharedSettingsController : AbstractSettingsController {
		private WeakReference _r = null;
		private SettingsFragment Self {
			get { return this._r != null ? this._r.Target as SettingsFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedSettingsController (SettingsFragment f) : base (EMApplication.Instance.appModel) {
			this.Self = f;
		}
	}
}