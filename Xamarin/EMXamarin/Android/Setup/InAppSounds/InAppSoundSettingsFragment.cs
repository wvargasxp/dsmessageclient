using System;
using Android.OS;
using Android.Views;
using em;

namespace Emdroid {
	public class InAppSoundSettingsFragment : BasicSettingsFragment {
		public SharedInAppSoundSettingsController Shared { get; set; }

		public static InAppSoundSettingsFragment NewInstance () {
			InAppSoundSettingsFragment f = new InAppSoundSettingsFragment ();
			return f;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			this.Shared = new SharedInAppSoundSettingsController (this);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			return base.OnCreateView (inflater, container, savedInstanceState);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);
			this.TitleTextView.Text = "SOUNDS_TITLE".t ();
			SetupListView ();

			AnalyticsHelper.SendView ("In App Sound Settings View");
		}

		protected override void SetupListView () {
			base.SetupListView ();
			InAppSoundSettingsListAdapter adapter = new InAppSoundSettingsListAdapter (this);
			adapter.ItemClick += Adapter_ItemClick;
			adapter.CheckBoxClick += Adapter_CheckBoxClick;
			this.ListView.SetAdapter (adapter);
		}

		private void Adapter_CheckBoxClick (object sender, SettingChange<SoundSetting> e) {
			this.Shared.HandlePushSettingChangeResult (e);
		}

		private void Adapter_ItemClick (object sender, int e) {}

	}

	public class SharedInAppSoundSettingsController : AbstractInAppSoundSettingsController {
		private WeakReference _r = null;
		private InAppSoundSettingsFragment Self {
			get { return this._r != null ? this._r.Target as InAppSoundSettingsFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedInAppSoundSettingsController (InAppSoundSettingsFragment f) : base (EMApplication.Instance.appModel) {
			this.Self = f;
		}
	}
}