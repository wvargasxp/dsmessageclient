using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Nhaarman.Listviewanimations.Appearance.Simple;
using Com.Nhaarman.Listviewanimations.Itemmanipulation;
using EMXamarin;
using Java.IO;
using em;
using Xamarin.Social.Services;
using Xamarin.Social;

namespace Emdroid {

	public class AliasFragment : Fragment {

		private HiddenReference<ApplicationModel> _appModel;
		private ApplicationModel appModel {
			get { return this._appModel != null ? this._appModel.Value : null; }
			set { this._appModel = new HiddenReference<ApplicationModel> (value); }
		}

		private HiddenReference<SharedAliasController> _shared;
		private SharedAliasController sharedAliasController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedAliasController> (value); }
		}

		AliasListAdapter aliasListAdapter;

		Button leftBarButton, rightBarButton;

		public static AliasFragment NewInstance () {
			var fragment = new AliasFragment ();
			fragment.appModel = EMApplication.GetInstance ().appModel;
			return fragment;
		}

		public int ThumbnailSizePixels { get; set; }

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			sharedAliasController = new SharedAliasController (this);

			DisplayMetrics displayMetrics = Resources.DisplayMetrics;
			ThumbnailSizePixels = (int) (Android_Constants.ROUNDED_THUMBNAIL_SIZE / displayMetrics.Density);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			var v = inflater.Inflate(Resource.Layout.alias, container, false);
			ThemeController (v);
			return v;
		}

		public void ThemeController () {
			ThemeController (this.View);
		}

		public void ThemeController (View v) {
			if (this.IsAdded && v != null) {
				EMApplication.GetInstance ().appModel.account.accountInfo.colorTheme.GetBackgroundResource ((string file) => {
					if (v != null && this.Resources != null) {
						BitmapSetter.SetBackgroundFromFile(v, this.Resources, file);
					}
				});
			}
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			leftBarButton = View.FindViewById<Button> (Resource.Id.LeftBarButton);
			leftBarButton.Click += (sender, e) => FragmentManager.PopBackStack ();
			leftBarButton.Typeface = FontHelper.DefaultFont;
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			rightBarButton = View.FindViewById<Button> (Resource.Id.RightBarButton);
			rightBarButton.Typeface = FontHelper.DefaultFont;
			rightBarButton.Click += DidTapAddAlias;

			ToggleNewAliasButtons ();

			aliasListAdapter = new AliasListAdapter(this);

			#region animation
			var listView = View.FindViewById<DynamicListView>(Resource.Id.AliasListView);
			var animationAdapter = new AlphaInAnimationAdapter(aliasListAdapter);
			animationAdapter.SetAbsListView(listView);
			listView.Adapter = animationAdapter;
			listView.ItemClick += DidTapAlias;
			#endregion

			View.FindViewById<TextView> (Resource.Id.starterLabel).Typeface = FontHelper.DefaultFont;

			AnalyticsHelper.SendView ("Alias View");
		}

		public override void OnResume () {
			base.OnResume ();

			if (aliasListAdapter != null)
				aliasListAdapter.NotifyDataSetChanged ();

			ToggleNewAliasButtons ();
		}

		public override void OnPause () {
			base.OnPause ();
		}

		public override void OnDestroy() {
			sharedAliasController.Dispose ();
			base.OnDestroy ();
		}

		protected void ToggleNewAliasButtons() {
			rightBarButton.Enabled = sharedAliasController.EnableAddNewAliasButton ();
			rightBarButton.SetTextColor (sharedAliasController.EnableAddNewAliasButton () ? Android_Constants.BLACK_COLOR : Android_Constants.GRAY_COLOR);
		}

		protected void DidTapAddAlias(object sender, EventArgs eventArgs) {
			if(sharedAliasController.EnableAddNewAliasButton ()) {
				EditAliasFragment fragment = EditAliasFragment.NewInstance (null);
				Activity.FragmentManager.BeginTransaction ()
					.SetTransition (FragmentTransit.FragmentOpen)
					.Replace (Resource.Id.content_frame, fragment)
					.AddToBackStack (null)
					.Commit();
			}
		}

		protected void DidTapAlias(object sender, AdapterView.ItemClickEventArgs eventArgs) {
			AliasInfo alias = sharedAliasController.Aliases[eventArgs.Position];

			if (alias.lifecycle == ContactLifecycle.Active) {
				EditAliasFragment fragment = EditAliasFragment.NewInstance (alias.serverID);

				Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit ();
			} else {
				var title = "ALIAS_DELETED_TITLE".t ();
				var message = "ALIAS_DELETED_MESSAGE".t ();
				var action = "ALIAS_DELETED_REACTIVATE".t ();

				var builder = new AlertDialog.Builder (Activity);

				builder.SetTitle (title);
				builder.SetMessage (message);
				builder.SetPositiveButton(action, (s1, dialogClickEventArgs) => sharedAliasController.ReactivateAlias (alias));
				builder.SetNegativeButton("OK_BUTTON".t (), (s2, dialogClickEventArgs) => { });
				builder.Create ();
				builder.Show ();
			}
		}

		class AliasListAdapter : BaseAdapter<AliasInfo> {
			readonly AliasFragment aliasFragment;

			public AliasListAdapter(AliasFragment fragment) {
				aliasFragment = fragment;
			}

			public override long GetItemId(int position) {
				return position;
			}

			public override AliasInfo this[int index] {  
				get { return aliasFragment.sharedAliasController.Aliases == null ? null : aliasFragment.sharedAliasController.Aliases[index]; }
			}

			public override int Count {
				get { return aliasFragment.sharedAliasController.Aliases == null ? 0 : aliasFragment.sharedAliasController.Aliases.Count; }
			}

			public override View GetView(int position, View convertView, ViewGroup parent) {
				AliasViewHolder currVH = convertView != null ? (AliasViewHolder)convertView.Tag : new AliasViewHolder (aliasFragment);

				AliasInfo alias = this [position];
				currVH.Position = position;
				currVH.SetAlias (alias);
				currVH.SetEven (position % 2 == 0);
				currVH.PossibleShowProgressIndicator (alias);

				return currVH.view;
			}
		}

		class AliasViewHolder: EMBitmapViewHolder {
			AliasFragment aliasFragment;
			AliasInfo alias;
			public bool InflationNeeded { get; set; }
			public View view { get; set; }
			public bool IsInsertion { get; set; }
			public TextView AliasName { get; set; }
			public ImageView ThumbnailImageview { get; set; }
			public ImageView PhotoFrame { get; set; }
			public ImageView AliasIcon { get; set; }
			public ImageView ShareButton { get; set; }
			public TextView ShareText { get; set; }
			public View ColorTrimView { get; set; }
			public ProgressBar ProgressBar { get; set; }

			public AliasViewHolder(AliasFragment fragment) {
				aliasFragment = fragment;
				view = null;
				IsInsertion = false;
				InflationNeeded = true;
			}

			void Inflate() {
				view = View.Inflate (aliasFragment.Activity, Resource.Layout.alias_entry, null);
				view.Tag = this;
				this.AliasName = view.FindViewById<TextView> (Resource.Id.AliasTextView);
				this.ThumbnailImageview = view.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				this.PhotoFrame = view.FindViewById<ImageView> (Resource.Id.photoFrame);
				this.AliasIcon = view.FindViewById<ImageView> (Resource.Id.aliasIcon);
				this.ShareButton = view.FindViewById<ImageView> (Resource.Id.ShareButton);
				this.ShareText = view.FindViewById<TextView> (Resource.Id.ShareButtonLabel);
				this.ColorTrimView = view.FindViewById<View> (Resource.Id.trimView);
				this.ProgressBar = view.FindViewById<ProgressBar> (Resource.Id.ProgressBar);

				this.AliasName.Typeface = FontHelper.DefaultFont;

				this.ShareButton.Click += (sender, e) => {
					var ah = new AnalyticsHelper();
					ah.SendEvent(AnalyticsConstants.CATEGORY_UI_ACTION, AnalyticsConstants.ACTION_BUTTON_PRESS, AnalyticsConstants.SHARE_PROFILE, 0);

					string imagePath = aliasFragment.appModel.platformFactory.GetFileSystemManager ().GetFilePathForSharingAlias(alias);
					string actualPath = aliasFragment.appModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri(imagePath);
					var media = new File(actualPath);
					
					if(media.Exists()) {
						string text = string.Format("INSTAGRAM_SHARE_DEFAULT_MESSAGE".t (), AliasName.Text);

						/*
						if(AndroidDeviceInfo.IsPackageInstalled("com.facebook.katana", view.Context)) {
							var facebook = new FacebookService();

							var item = new Item();
							item.Text = text;
							ImageData img = new ImageData(actualPath);
							item.Images.Insert(0, img);

							facebook.GetShareUI(aliasFragment.Activity, item, result => {
								// result lets you know if the user shared the item or canceled
							});
						}
						*/

						// Create the new Intent using the 'Send' action.
						var share = new Intent(Intent.ActionSend);

						//if Instagram is installed, make that the default app to use to share
						if(AndroidDeviceInfo.IsPackageInstalled("com.instagram.android", view.Context))
							share.SetPackage("com.instagram.android");

						// Set the MIME type
						share.SetType("image/*");

						// Create the URI from the media
						var uri = Android.Net.Uri.FromFile(media);

						share.PutExtra(Intent.ExtraStream, uri);
						share.PutExtra(Intent.ExtraText, text);

						// Broadcast the Intent.
						aliasFragment.Activity.StartActivity(Intent.CreateChooser(share, "SHARE_TO".t ()));
					} else {
						EMTask.DispatchBackground (() => ShareHelper.GenerateInstagramSharableFile (aliasFragment.appModel, alias, ClickShareButton));
					}
				};

				this.InflationNeeded = false;
			}

			public void PossibleShowProgressIndicator (CounterParty c) {
				if (BitmapSetter.ShouldShowProgressIndicator (c)) {
					this.ProgressBar.Visibility = ViewStates.Visible;
					this.PhotoFrame.Visibility = ViewStates.Invisible;
					this.ThumbnailImageview.Visibility = ViewStates.Invisible;
				} else {
					this.ProgressBar.Visibility = ViewStates.Gone;
					this.PhotoFrame.Visibility = ViewStates.Visible;
					this.ThumbnailImageview.Visibility = ViewStates.Visible;
				}
			}

			public void SetAlias(AliasInfo a) {
				alias = a;

				if (InflationNeeded)
					Inflate ();

				if (alias.lifecycle == ContactLifecycle.Active) {
					view.Alpha = 1;
					ShareButton.Visibility = ViewStates.Visible;
					ShareButton.Enabled = true;
					ShareText.Visibility = ViewStates.Visible;
				} else {
					view.Alpha = 0.3f;
					ShareButton.Visibility = ViewStates.Gone;
					ShareButton.Enabled = false;
					ShareText.Visibility = ViewStates.Gone;
				}

				this.AliasName.Text = alias.displayName;

				BackgroundColor colorTheme = alias.colorTheme;
				Color trimAndTextColor = colorTheme.GetColor ();
				this.ColorTrimView.SetBackgroundColor (trimAndTextColor);
				colorTheme.GetPhotoFrameLeftResource ((string filepath) => {
					if (PhotoFrame != null && aliasFragment != null) {
						BitmapSetter.SetBackgroundFromFile (PhotoFrame, aliasFragment.Resources, filepath);
					}
				});
				BitmapSetter.SetThumbnailImage (this, alias, aliasFragment.Resources, ThumbnailImageview, Resource.Drawable.userDude, aliasFragment.ThumbnailSizePixels);
				BitmapSetter.SetImage (this, alias.iconMedia, aliasFragment.Resources, AliasIcon, Resource.Drawable.Icon, 100);
				colorTheme.GetShareImageResource ((string filepath) => {
					if (this.ShareButton != null) {
						BitmapSetter.SetBackgroundFromFile (this.ShareButton, aliasFragment.Resources, filepath);
					}
				});
			}

			public void SetEven(bool isEven) {
				BasicRowColorSetter.SetEven (isEven, view);
			}

			void ClickShareButton() {
				EMTask.DispatchMain (() => ShareButton.PerformClick ());
			}
		}

		class SharedAliasController : AbstractAliasController {
			private WeakReference _r = null;
			private AliasFragment Self {
				get { return this._r != null ? this._r.Target as AliasFragment : null; }
				set { this._r = new WeakReference (value); }
			}

			public SharedAliasController(AliasFragment fragment) : base(EMApplication.GetInstance().appModel) {
				this.Self = fragment;
			}

			public override void DidChangeAliasList() {
				AliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.aliasListAdapter.NotifyDataSetChanged ();
				self.ToggleNewAliasButtons ();
			}

			public override void DidChangeColorTheme () {
				AliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.ThemeController ();
			}

			public override void DidChangeThumbnailMedia () {
				AliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.aliasListAdapter.NotifyDataSetChanged ();
			}

			public override void DidDownloadThumbnail () {
				AliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.aliasListAdapter.NotifyDataSetChanged ();
			}

			public override void DidChangeIconMedia () {
				AliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.aliasListAdapter.NotifyDataSetChanged ();
			}

			public override void DidDownloadIcon () {
				AliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.aliasListAdapter.NotifyDataSetChanged ();
			}

			public override void DidUpdateLifecycle () {
				AliasFragment self = this.Self;
				if (GCCheck.ViewGone (self)) return;
				self.aliasListAdapter.NotifyDataSetChanged ();
				self.ToggleNewAliasButtons ();
			}
		}
	}
}