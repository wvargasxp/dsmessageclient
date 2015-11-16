using System;
using Android.App;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using em;

namespace Emdroid {
	public class BasicSettingsFragment : Fragment {

		protected RecyclerView ListView { get; set; }
		protected TextView TitleTextView { get; set; }
		protected Button LeftBarButton { get; set; }

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View view = inflater.Inflate (Resource.Layout.reuseable_nested_settings, container, false);
			ThemeController (view); 
			return view;
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			this.TitleTextView = this.View.FindViewById<TextView> (Resource.Id.titleTextView);
			this.LeftBarButton = this.View.FindViewById<Button> (Resource.Id.leftBarButton);
			this.ListView = this.View.FindViewById<RecyclerView> (Resource.Id.mainList);
			this.LeftBarButton.Click += WeakDelegateProxy.CreateProxy<object, EventArgs> (Exit).HandleEvent<object, EventArgs>;
			this.LeftBarButton.Typeface = FontHelper.DefaultFont;
			this.TitleTextView.Typeface = FontHelper.DefaultFont;

			View.FindViewById<TextView> (Resource.Id.appTitle).Typeface = FontHelper.DefaultBoldFont;
		}

		protected virtual void SetupListView () {
			LinearLayoutManager layoutMgr = new LinearLayoutManager (this.Activity);
			this.ListView.SetLayoutManager (layoutMgr);
			this.ListView.AddItemDecoration (new SimpleDividerItemDecoration (this.Activity));
		}

		protected void ThemeController (View v) {
			if (this.IsAdded && v != null) {
				BackgroundColor colorTheme = EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme;
				colorTheme.GetBackgroundResource ((string file) => {
					if (v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile (v, this.Resources, file);
					}
				});
			}
		}

		private void Exit (object obj, EventArgs args) {
			this.FragmentManager.PopBackStack ();
		}
	}
}