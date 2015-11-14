using System;
using System.Collections.Generic;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Nhaarman.Listviewanimations.Appearance.Simple;
using Com.Nhaarman.Listviewanimations.Itemmanipulation;
using em;
using EMXamarin;

namespace Emdroid {
	public class ProfileListFragment : Fragment {

		public ChatEntry chatEntry;
		public IList<Contact> contacts;

		private HiddenReference<SharedProfileListController> _shared;
		private SharedProfileListController sharedProfileListController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedProfileListController> (value); }
		}

		ProfileListAdapter profileListAdapter;
		Button leftBarButton, rightBarButton;

		public int ThumbnailSizePixels { get; set; }

		bool isActive;
		public bool Active {
			get { return isActive; }
			set { isActive = value; }
		}

		public static ProfileListFragment NewInstance (ChatEntry ce) {
			ProfileListFragment f = new ProfileListFragment ();
			f.sharedProfileListController = new SharedProfileListController (EMApplication.Instance.appModel, ce);
			f.chatEntry = ce;
			f.contacts = ce.contacts;
			return f;
		}

		public ProfileListFragment () {}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			DisplayMetrics displayMetrics = Resources.DisplayMetrics;
			ThumbnailSizePixels = (int) (Android_Constants.ROUNDED_THUMBNAIL_SIZE / displayMetrics.Density);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			var v = inflater.Inflate(Resource.Layout.profile_list, container, false);
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

			profileListAdapter = new ProfileListAdapter(this);

			#region animation
			var listView = View.FindViewById<DynamicListView>(Resource.Id.ProfileListView);
			var animationAdapter = new AlphaInAnimationAdapter(profileListAdapter);
			animationAdapter.SetAbsListView(listView);
			listView.Adapter = animationAdapter;
			listView.ItemClick += DidTapProfile;
			#endregion

			leftBarButton = View.FindViewById<Button> (Resource.Id.LeftBarButton);
			leftBarButton.Click += (sender, e) => FragmentManager.PopBackStack ();
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			View.FindViewById<TextView> (Resource.Id.starterLabel).Typeface = FontHelper.DefaultFont;

			rightBarButton = View.FindViewById<Button> (Resource.Id.RightBarButton);
			rightBarButton.Click += DidTapRemoveMeButton;
			rightBarButton.Enabled = chatEntry.IsAdHocGroupWeCanLeave();
			ViewClickStretchUtil.StretchRangeOfButton (rightBarButton);

			this.Active = true;

			AnalyticsHelper.SendView ("Profile List View");
		}

		public override void OnResume () {
			this.Active = true;
			base.OnResume ();

			if (profileListAdapter != null)
				profileListAdapter.NotifyDataSetChanged ();
		}

		public override void OnPause() {
			this.Active = false;
			base.OnPause ();
		}

		public override void OnDestroy() {
			sharedProfileListController.Dispose ();
			base.OnDestroy ();
		}

		protected void DidTapProfile(object sender, AdapterView.ItemClickEventArgs eventArgs) {
			Contact contact = sharedProfileListController.Profiles[eventArgs.Position];

			ProfileFragment fragment = ProfileFragment.NewInstance (contact);

			Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit();
		}

		protected void DidTapRemoveMeButton(object sender, EventArgs eventArgs) {
			var builder = new AlertDialog.Builder (Activity);
			builder.SetTitle ("LEAVE_CONVERSATION".t ());
			builder.SetMessage ("LEAVE_CONVERSATION_EXPAINATION".t ());
			builder.SetPositiveButton ("LEAVE".t (), (sender2, dialogClickEventArgs) => {
				sharedProfileListController.RemoveFromAdHocGroupAsync ();

				//pop off twice
				FragmentManager.PopBackStack (); //gets back to the chat conversation
				FragmentManager.PopBackStack (); //gets back to the prior fragment
			});
			builder.SetNegativeButton ("CANCEL_BUTTON".t (), (sender2, dialogClickEventArgs) => { });
			builder.Create ();
			builder.Show ();
		}

		class ProfileListAdapter : BaseAdapter<Contact> {
			readonly ProfileListFragment profileListFragment;

			public ProfileListAdapter(ProfileListFragment fragment) {
				profileListFragment = fragment;
			}

			public override long GetItemId(int position) {
				return position;
			}

			public override Contact this[int index] {  
				get { return profileListFragment.sharedProfileListController.Profiles == null ? null : profileListFragment.sharedProfileListController.Profiles[index]; }
			}

			public override int Count {
				get { return profileListFragment.sharedProfileListController.Profiles == null ? 0 : profileListFragment.sharedProfileListController.Profiles.Count; }
			}

			public override View GetView(int position, View convertView, ViewGroup parent) {
				ViewHolder currVH = convertView != null ? (ViewHolder)convertView.Tag : new ViewHolder (profileListFragment);

				Contact contact = this [position];
				currVH.Position = position;
				currVH.SetContact (contact);
				currVH.SetEven (position % 2 == 0);

				return currVH.view;
			}
		}

		class ViewHolder: EMBitmapViewHolder {
			ProfileListFragment profileListFragment;

			public bool inflationNeeded { get; set; }
			public View view { get; set; }
			public bool isInsertion { get; set; }
			public TextView name { get; set; }
			public ImageView thumbnailImageview { get; set; }
			public ImageView photoFrame { get; set; }
			public ImageView aliasIcon { get; set; }
			public View colorTrimView { get; set; }

			public ViewHolder(ProfileListFragment fragment) {
				profileListFragment = fragment;
				view = null;
				isInsertion = false;
				inflationNeeded = true;
			}

			void Inflate() {
				view = View.Inflate (profileListFragment.Activity, Resource.Layout.profile_entry, null);
				view.Tag = this;
				name = view.FindViewById<TextView> (Resource.Id.NameTextView);
				thumbnailImageview = view.FindViewById<ImageView> (Resource.Id.thumbnailImageView);
				photoFrame = view.FindViewById<ImageView> (Resource.Id.photoFrame);
				aliasIcon = view.FindViewById<ImageView> (Resource.Id.aliasIcon);
				colorTrimView = view.FindViewById<View> (Resource.Id.trimView);

				name.Typeface = FontHelper.DefaultFont;

				inflationNeeded = false;
			}

			public void SetContact(Contact contact) {
				if (inflationNeeded)
					Inflate ();

				name.Text = contact == null ? "" : contact.displayName;

				BackgroundColor colorTheme = contact == null ? BackgroundColor.Default : contact.colorTheme;
				Color trimAndTextColor = colorTheme.GetColor ();
				colorTrimView.SetBackgroundColor (trimAndTextColor);
				colorTheme.GetPhotoFrameLeftResource ((string filepath) => {
					if (photoFrame != null && profileListFragment != null) {
						BitmapSetter.SetBackgroundFromFile (photoFrame, profileListFragment.Resources, filepath);
					}
				});
				if (contact != null) {
					BitmapSetter.SetThumbnailImage (this, contact, profileListFragment.Resources, thumbnailImageview, Resource.Drawable.userDude, profileListFragment.ThumbnailSizePixels);
				}

				if(contact != null && contact.fromAlias != null) {
					var alias = EMApplication.GetInstance().appModel.account.accountInfo.AliasFromServerID (contact.fromAlias);
					if(alias != null) {
						aliasIcon.Visibility = ViewStates.Visible;
						BitmapSetter.SetImage (this, alias.iconMedia, profileListFragment.Resources, aliasIcon, Resource.Drawable.Icon, 100);
					} else {
						aliasIcon.Visibility = ViewStates.Invisible;
					}
				} else {
					aliasIcon.Visibility = ViewStates.Invisible;
				}
			}

			public void SetEven(bool isEven) {
				BasicRowColorSetter.SetEven (isEven, view);
			}
		}

		class SharedProfileListController : AbstractProfileController {

			public SharedProfileListController(ApplicationModel appModel, ChatEntry ce) : base (appModel, ce) {
				
			}

			public override void DidChangeTempProperty () {

			}

			public override void DidChangeBlockStatus (Contact c) {
				
			}

			public override void TransitionToChatController (ChatEntry chatEntry) {

			}
		}
	}
}