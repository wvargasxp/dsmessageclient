using System;
using Android.OS;
using Android.Views;
using em;

namespace Emdroid {
	public class PushSettingsFragment : BasicSettingsFragment {
		public SharedPushSettingsController Shared { get; set; }

		public static PushSettingsFragment NewInstance () {
			PushSettingsFragment f = new PushSettingsFragment ();
			return f;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
			this.Shared = new SharedPushSettingsController (this);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			return base.OnCreateView (inflater, container, savedInstanceState);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);
			this.TitleTextView.Text = "OFFLINE_NOTIFICATIONS_TITLE".t ();
			SetupListView ();

			AnalyticsHelper.SendView ("Push Settings View");
		}

		protected override void SetupListView () {
			base.SetupListView ();
			PushSettingsListAdapter adapter = new PushSettingsListAdapter (this);
			adapter.ItemClick += Adapter_ItemClick;
			adapter.CheckBoxClick += Adapter_CheckBoxClick;
			this.ListView.SetAdapter (adapter);
		}

		private void Adapter_CheckBoxClick (object sender, SettingChange<PushSetting> result) {
			this.Shared.HandlePushSettingChangeResult (result);
		}

		private void Adapter_ItemClick (object sender, int e) {}
	}

	public class SharedPushSettingsController : AbstractPushSettingsController {
		private WeakReference _r = null;
		private PushSettingsFragment Self {
			get { return this._r != null ? this._r.Target as PushSettingsFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedPushSettingsController (PushSettingsFragment f) : base (EMApplication.Instance.appModel) {
			this.Self = f;
		}
	}
}