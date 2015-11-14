using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidHUD;
using Com.EM.Android;
using em;


namespace Emdroid {
	public class SignInFragment : Fragment {

		public AccountUtils.UserProfile Profile { get; set; }

		public CountryCode SelectedCountry { get; set; }
		public CountryCode CurrentCountry { get; set; }
		public ImageView CountryImageButton { get; set; }
		public TextView CountryText { get; set; }
		public Spinner CountryCodeSpinner { get; set; }
		public ImageButton ContinueButton { get; set; }

		#region lifecycle - sorted
		public override void OnAttach (Activity activity) {
			base.OnAttach (activity);
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			return base.OnCreateView (inflater, container, savedInstanceState);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			Profile = AccountUtils.GetUserProfile (this.Activity.ApplicationContext);

			string locale = Java.Util.Locale.Default.Country.ToLower ();
			CurrentCountry = CountryCode.getCountryFromCode (locale) ?? CountryCode.getCountryFromCode ("us");
			var imageResource = Activity.BaseContext.Resources.GetIdentifier ("drawable/" + CurrentCountry.photoUrl.Replace(".png", ""), null, Activity.BaseContext.PackageName);
			SelectedCountry = CurrentCountry;

			CountryCodeSpinner = View.FindViewById<Spinner> (Resource.Id.CountrySpinner);
			CountryCodeSpinner.Visibility = ViewStates.Invisible;
			CountryCodeSpinner.Adapter = new CountryArrayAdapter (Activity.BaseContext, CountryCode.countries);
			CountryCodeSpinner.ItemSelected += DidSelectCountry;
			CountryCodeSpinner.SetSelection( CountryCode.getIndexFromCountryCode(SelectedCountry));

			CountryImageButton = View.FindViewById<ImageView> (Resource.Id.CountryImageButton);
			CountryImageButton.SetImageResource (imageResource);
			CountryImageButton.Click += (sender, e) => CountryCodeSpinner.PerformClick ();

			CountryText = View.FindViewById<TextView> (Resource.Id.countryCodeLabel);
			CountryText.Typeface = FontHelper.DefaultFont;
			CountryText.Text = CurrentCountry.translationKey.t ();
			CountryText.Click += (sender, e) => CountryCodeSpinner.PerformClick ();

			ContinueButton = View.FindViewById<ImageButton> (Resource.Id.continueButton);
			var states = new StateListDrawable ();
			EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme.GetChatSendButtonResource ((string filepath) => {
				if (states != null && ContinueButton != null) {
					states.AddState (new int[] {Android.Resource.Attribute.StateEnabled}, Drawable.CreateFromPath (filepath));
					states.AddState (new int[] {}, Resources.GetDrawable (Resource.Drawable.iconSendDisabled));
					ContinueButton.SetImageDrawable (states);

					TouchDelegateComposite.ExpandClickArea (this.ContinueButton, View, 30);
				}
			});

		}

		public override void OnStart () {
			base.OnStart ();
		}

		public override void OnResume () {
			base.OnResume ();
		}

		public override void OnPause () {
			base.OnPause ();
		}

		public override void OnStop () {
			base.OnStop ();
		}

		public override void OnDestroyView () {
			base.OnDestroyView ();
		}

		public override void OnDestroy () {
			base.OnDestroy ();
		}

		public override void OnDetach () {
			base.OnDetach ();
		}
		#endregion

		public void PauseUI () {
			EMTask.DispatchMain (() => {
				ContinueButton.Enabled = false;
				AndHUD.Shared.Show (this.Activity, null, -1, MaskType.Clear, default(TimeSpan?), null, true, null);
			});
		}

		public void ResumeUI () {
			EMTask.DispatchMain (() => {
				ContinueButton.Enabled = true;
				AndHUD.Shared.Dismiss (this.Activity);
			});
		}

		public virtual void DidSelectCountry(object sender, AdapterView.ItemSelectedEventArgs e) {
			SelectedCountry = CountryCode.countries [e.Position];

			var imageResource = Activity.BaseContext.Resources.GetIdentifier ("drawable/" + SelectedCountry.photoUrl.Replace(".png", ""), null, Activity.BaseContext.PackageName);
			CountryImageButton.SetImageResource (imageResource);

			CountryText.Text = SelectedCountry.translationKey.t ();
		}

		public class CountryArrayAdapter : BaseAdapter<CountryCode>, ISpinnerAdapter {
			Context context;
			readonly IList<CountryCode> countries;

			class ViewHolder : EMBitmapViewHolder {
				ImageView countryImage;
				public ImageView CountryImage {
					get { return countryImage; }
					set { countryImage = value; }
				}

				TextView countryName;
				public TextView CountryName {
					get { return countryName; }
					set { countryName = value; }
				}
			}

			public CountryArrayAdapter(Context theContext, IList<CountryCode> theCountries) {
				context = theContext;
				countries = theCountries;
			}

			public override View GetView (int position, View convertView, ViewGroup parent) {
				ViewHolder holder;

				if (convertView != null)
					holder = (ViewHolder)convertView.Tag;
				else {
					convertView = View.Inflate (context, Resource.Layout.country_code, null);
					holder = new ViewHolder ();
					holder.CountryImage = convertView.FindViewById<ImageView> (Resource.Id.CountryImage);
					holder.CountryName = convertView.FindViewById<TextView> (Resource.Id.CountryName);
					convertView.Tag = holder;
				}

				holder.Position = position;

				var cc = CountryCode.getCountryFromPosition (position);
				var imageResource = context.Resources.GetIdentifier ("drawable/" + cc.photoUrl.Replace(".png", ""), null, context.PackageName);
				EMNativeBitmapWrapper.SetFlagImageOnListItem (holder, context.Resources, holder.CountryImage, imageResource);
				holder.CountryName.Text = cc.translationKey.t ();
				return convertView;
			}

			public override View GetDropDownView (int position, View convertView, ViewGroup parent) {
				return GetView (position, convertView, parent);
			}

			public override int Count {
				get { return countries.Count; }
			}

			public override long GetItemId (int position) {
				return (long) position;
			}

			public override CountryCode this [int index] { 
				get { return CountryCode.getCountryFromPosition (index); }
			}
		}
	}
}