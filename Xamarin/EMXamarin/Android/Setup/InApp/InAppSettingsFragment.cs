using System;
using Android.OS;
using Android.Views;
using em;

namespace Emdroid {
	public class InAppSettingsFragment : BasicSettingsFragment {
		
		public static string InAppSettingsTitle = "IN_APP_SETTINGS_TITLE".t ();
		public SharedInAppSettingsController Shared { get; set; }

		public static InAppSettingsFragment NewInstance () {
			InAppSettingsFragment f = new InAppSettingsFragment ();
			return f;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			this.Shared = new SharedInAppSettingsController (this);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			return base.OnCreateView (inflater, container, savedInstanceState);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);
			this.TitleTextView.Text = InAppSettingsTitle;
			SetupListView ();

			AnalyticsHelper.SendView ("In App Settings View");
		}

		protected override void SetupListView () {
			base.SetupListView ();
			InAppSettingsListAdapter adapter = new InAppSettingsListAdapter (this);
			adapter.ItemClick += Adapter_ItemClick;
			adapter.CheckBoxClick += Adapter_CheckBoxClick;
			this.ListView.SetAdapter (adapter);
		}

		private void Adapter_CheckBoxClick (object sender, SettingChange<InAppSetting> e) {
			this.Shared.HandlePushSettingChangeResult (e);
		}

		private void Adapter_ItemClick (object sender, int e) {}

	}

	public class SharedInAppSettingsController : AbstractInAppSettingsController {
		private WeakReference _r = null;
		private InAppSettingsFragment Self {
			get { return this._r != null ? this._r.Target as InAppSettingsFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedInAppSettingsController (InAppSettingsFragment f) : base (EMApplication.Instance.appModel) {
			this.Self = f;
		}
	}
}