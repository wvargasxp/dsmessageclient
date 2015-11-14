using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Provider;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Ortiz.Touch;
using em;
using EMXamarin;
using Android.Support.V13.App;
using Com.EM.Android;

namespace Emdroid {

	public class MediaGalleryFragment : Fragment {
		public const string CurrentPageNotificationKey = "current_page_notification_key";

		private RelativeLayout titleBarLayout;
		public RelativeLayout TitleBarLayout {
			get { return titleBarLayout; }
			set { titleBarLayout = value; }
		}

		private TextView titleTextView;
		public TextView TitleTextView {
			get { return titleTextView; }
			set { titleTextView = value; }
		}

		private GalleryViewPager ViewPager { get; set; }
		private FragmentPagerAdapter FragmentAdapter { get; set; }

		private HiddenReference<SharedMediaGalleryController> _shared;
		public SharedMediaGalleryController Shared { 
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedMediaGalleryController> (value); }
		}

		public static MediaGalleryFragment NewInstance (IMediaMessagesProvider provider) {
			MediaGalleryFragment fragment = new MediaGalleryFragment ();
			fragment.Shared = new SharedMediaGalleryController (fragment, provider);
			return fragment;
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
		}

		public override void OnResume () {
			base.OnResume ();
			this.Shared.AddObservers ();
		}
			
		public override void OnPause () {
			base.OnPause ();
			this.Shared.RemoveObservers ();
			NotificationCenter.DefaultCenter.PostNotification (Constants.MediaGallery_Paused);
		}
			
		public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View view = inflater.Inflate (Resource.Layout.media_gallery, container, false);
			this.TitleBarLayout = view.FindViewById<RelativeLayout> (Resource.Id.titlebarlayout);
			this.TitleTextView = this.TitleBarLayout.FindViewById <TextView> (Resource.Id.titleTextView);
			Button leftBarButton = this.TitleBarLayout.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += (object sender, EventArgs e) => this.FragmentManager.PopBackStack ();

			Button rightBarButton = this.TitleBarLayout.FindViewById<Button> (Resource.Id.rightBarButton);
			rightBarButton.Visibility = ViewStates.Visible;
			rightBarButton.Click += ShowMenuOptionSaveMediaToGallery;

			// Modying the right button layout.
			RelativeLayout.LayoutParams r = (RelativeLayout.LayoutParams)rightBarButton.LayoutParameters;
			r.TopMargin = 10.DpToPixelUnit ();
			r.RightMargin = 5.DpToPixelUnit ();
			r.Width = 25.DpToPixelUnit ();
			r.Height = 25.DpToPixelUnit ();

			// Expand hit area.
			TouchDelegateComposite.ExpandClickArea (rightBarButton, view, 30);

			BitmapSetter.SetBackground (rightBarButton, this.Resources, Resource.Drawable.iconAddImage);

			return view;
		}

		public override void OnDestroy () {
			base.OnDestroy ();
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);
			this.ViewPager = View.FindViewById<GalleryViewPager> (Resource.Id.viewpager);

			this.FragmentAdapter = new FragmentPagerAdapter (this, this.FragmentManager);

			this.ViewPager.Adapter = this.FragmentAdapter;

			SetInitialController ();

			this.ViewPager.PageSelected += (object sender, ViewPager.PageSelectedEventArgs e) => {
				this.Shared.CurrentPage = e.Position;
				UpdateTitleBar ();
				Dictionary<string, int> extra = new Dictionary<string, int> ();
				extra [CurrentPageNotificationKey] = this.Shared.CurrentPage;
				NotificationCenter.DefaultCenter.PostNotification (null, Constants.MediaGallery_PageChangedNotification, extra);
			};

			UpdateTitleBar ();

			AnalyticsHelper.SendView ("Media Gallery View");
		}

		public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig) {
			base.OnConfigurationChanged (newConfig);
		}

		public void ShowMenuOptionSaveMediaToGallery (object sender, EventArgs e) {
			em.Message m = this.Shared.MessageForCurrentPage;

			string localPath = EMApplication.Instance.appModel.uriGenerator.GetFilePathForChatEntryUri (m.media.uri, m.chatEntry);

			PopupMenu popupMenu = new PopupMenu (this.Activity, (View)sender);
			popupMenu.Inflate (Resource.Menu.popup_gallery_save_options);

			popupMenu.MenuItemClick += (s1, arg1) => {
				IMenuItem item = arg1.Item;
				switch ( item.ItemId ) {
				case Resource.Id.Save: {
						ContentType type = ContentType.Unknown;
						if (m.contentType != null) {
							type = ContentTypeHelper.FromString (m.contentType);
						}

						if ((type != ContentType.Unknown && ContentTypeHelper.IsVideo (type)) || ContentTypeHelper.IsVideo (m.media.uri.AbsolutePath)) {
							var intent = new Intent (Intent.ActionMediaScannerScanFile, Android.Net.Uri.FromFile (new Java.IO.File (localPath)));
							this.Activity.SendBroadcast (intent);
						} else {
							FragmentStatePagerAdapter adapter = this.ViewPager.Adapter as FragmentStatePagerAdapter;
							if (adapter != null) {
								AbstractGalleryItemFragment f = adapter.InstantiateItem (this.ViewPager, this.ViewPager.CurrentItem) as AbstractGalleryItemFragment;
								if (f != null) {
									TouchImageView imageView = f.Image;
									Bitmap bitmapToSave = ((BitmapDrawable)imageView.Drawable).Bitmap;
									MediaStore.Images.Media.InsertImage (this.Activity.ContentResolver, bitmapToSave, "EM Image", "EM Image");
								}
							}
						}
					}

					break;

				default:
					break;
				}
			};

			// Android 4 now has the DismissEvent
			popupMenu.DismissEvent += (s2, arg2) => System.Diagnostics.Debug.WriteLine ("menu dismissed");

			popupMenu.Show ();
		}

		#region shared impl
		public void SetInitialController () {
			this.ViewPager.CurrentItem = this.Shared.CurrentPage;
		}

		public void UpdateDatasourceAndDelegates () {
			this.FragmentAdapter.NotifyDataSetChanged ();
		}

		public void UpdateTitleBar () {
			this.TitleTextView.Text = string.Format ("MEDIA_GALLERY_COUNT".t (), this.Shared.CurrentPage + 1, this.Shared.MediaMessages.Count);
		}
		#endregion

		protected override void Dispose (bool disposing) {
			this.Shared.Dispose ();
			this.Shared = null;
			base.Dispose (disposing);
		}
	}

	public class FragmentPagerAdapter : FragmentStatePagerAdapter {
		WeakReference weakRef;
		MediaGalleryFragment Fragment {
			get { return weakRef != null ? weakRef.Target as MediaGalleryFragment : null; }
			set { weakRef = new WeakReference (value); }
		}

		public FragmentPagerAdapter (MediaGalleryFragment f, FragmentManager fm) : base (fm) {
			this.Fragment = f;
		}

		public override Fragment GetItem (int position) {
			MediaGalleryFragment f = this.Fragment;
			AbstractGalleryItemFragment x = null;
			if (f == null) return x;

			SharedMediaGalleryController shared = f.Shared;
			shared.CheckIfNeedsToRequestMoreMessages ();

			em.Message mediaMessage = shared.MediaMessages [position];

			ContentType type = ContentTypeHelper.FromMessage (mediaMessage);
			int currentPage = shared.CurrentPage;

			if (ContentTypeHelper.IsVideo (type)) {
				x = VideoPlaybackFragment.NewInstance (mediaMessage, position, currentPage);
			} else if (ContentTypeHelper.IsAudio (type)) {
				x = AudioPlaybackFragment.NewInstance (mediaMessage, position, currentPage);
			} else {
				x = PhotoPlaybackFragment.NewInstance (mediaMessage, position, currentPage);
			}

			return x;
		}

		public override int Count {
			get {
				MediaGalleryFragment f = this.Fragment;

				if (f != null) {
					SharedMediaGalleryController shared = f.Shared;
					return shared.MediaMessages.Count;
				} else {
					return 0;
				}
			}
		}
	}

	public class SharedMediaGalleryController : AbstractMediaGalleryController {
		private WeakReference _r = null;
		private MediaGalleryFragment Self {
			get { return this._r != null ? this._r.Target as MediaGalleryFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedMediaGalleryController (MediaGalleryFragment self, IMediaMessagesProvider provider) : base (provider, EMApplication.Instance.appModel) {
			this.Self = self;
		}

		#region implemented abstract members of AbstractMediaGalleryController
		public override void UpdateDatasourceAndDelegates () {
			MediaGalleryFragment self = this.Self;
			if (self == null) return;
			self.UpdateDatasourceAndDelegates ();
		}

		public override void SetInitialController () {
			MediaGalleryFragment self = this.Self;
			if (self == null) return;
			self.SetInitialController ();
		}

		public override void UpdateTitleBar () {
			MediaGalleryFragment self = this.Self;
			if (self == null) return;
			self.UpdateTitleBar ();
		}
		#endregion
		
	}
}