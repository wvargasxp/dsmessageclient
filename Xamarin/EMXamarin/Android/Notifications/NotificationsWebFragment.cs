using System;
using System.Text;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidHUD;
using em;
using Newtonsoft.Json;

namespace Emdroid {
	public class NotificationsWebFragment : Fragment {

		Button leftBarButton;

		WebView web_view;

		NotificationEntry notificationEntry;
		protected NotificationList notificationList;

		private HiddenReference<SharedNotificationsWebController> _shared;
		public SharedNotificationsWebController sharedNotificationsWebController {
			get { return this._shared != null ? this._shared.Value : null; }
			set { this._shared = new HiddenReference<SharedNotificationsWebController> (value); }
		}

		public static NotificationsWebFragment NewInstance (NotificationEntry ne) {
			var fragment = new NotificationsWebFragment ();
			fragment.notificationEntry = ne;
			fragment.notificationList = EMApplication.GetInstance ().appModel.notificationList;
			return fragment;
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
			View v = inflater.Inflate(Resource.Layout.notification_web, container, false);

			web_view = v.FindViewById<WebView> (Resource.Id.webview);
			web_view.Settings.JavaScriptEnabled = true;

			if (int.Parse (Build.VERSION.Sdk) < 18)
				//deprecated in API 18
				web_view.Settings.SetRenderPriority (WebSettings.RenderPriority.High);
			
			web_view.Settings.CacheMode = CacheModes.NoCache;
			web_view.Settings.AllowContentAccess = true;
			web_view.Settings.AllowFileAccessFromFileURLs = true;
			web_view.Settings.AllowUniversalAccessFromFileURLs = true;
			web_view.Settings.LoadsImagesAutomatically = true;

			if (int.Parse (Build.VERSION.Sdk) >= 21)
				//Note: This namespace, class, or member is supported only in version Added in API level 21 and later.
				web_view.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

			web_view.SetWebViewClient (new MyWebViewClient (this, notificationEntry, notificationList));
			string url = notificationEntry.Url.Replace ("http://localhost:8080", AppEnv.HTTP_BASE_ADDRESS);
			web_view.LoadUrl (url);
			web_view.SetBackgroundColor(Android.Graphics.Color.Transparent);

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

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			if (savedInstanceState != null) {
				int notificationEntryId = savedInstanceState.GetInt ("notificationEntryId");
				ApplicationModel appModel = EMApplication.GetInstance ().appModel;
				notificationEntry = appModel.notificationEntryDao.FindNotificationEntry (notificationEntryId);
				notificationList = EMApplication.GetInstance ().appModel.notificationList;
			}

		}

		public override void OnDestroy () {
			if (sharedNotificationsWebController != null)
				sharedNotificationsWebController.Dispose ();
			base.OnDestroy ();
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
		}

		public override void OnSaveInstanceState (Bundle outState) {
			// Called when fragment is destroyed and recreated.
			int notificationEntryId = notificationEntry.NotificationEntryID;
			outState.PutInt ("notificationEntryId", notificationEntryId);
			base.OnSaveInstanceState (outState);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			EMTask.DispatchMain (() => AndHUD.Shared.Show (Activity, "LOADING".t (), -1, MaskType.Clear, default(TimeSpan?), null, true, null));

			sharedNotificationsWebController = new SharedNotificationsWebController (this, EMApplication.GetInstance ().appModel);

			FontHelper.SetFontOnAllViews (View as ViewGroup);

			View.FindViewById<TextView> (Resource.Id.titleTextView).Text = "NOTIFICATION_TITLE".t ();
			View.FindViewById<TextView> (Resource.Id.titleTextView).Typeface = FontHelper.DefaultFont;

			leftBarButton = View.FindViewById<Button> (Resource.Id.leftBarButton);
			leftBarButton.Click += (sender, e) => FragmentManager.PopBackStack ();
			ViewClickStretchUtil.StretchRangeOfButton (leftBarButton);

			AnalyticsHelper.SendView ("Notification Web View");
		}

		public void TransitionToChatControllerUsingChatEntry (ChatEntry chatEntry) {
			ChatFragment chatFragment = ChatFragment.NewInstance (chatEntry);
			Bundle args = new Bundle ();
			ChatList chatList = EMApplication.Instance.appModel.chatList;
			int index = chatList.entries.IndexOf (chatEntry);
			args.PutInt ("Position", index >= 0 ? index : ChatFragment.NEW_MESSAGE_INITIATED_FROM_NOTIFICATION_POSITION);
			chatFragment.Arguments = args;
			this.Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, chatFragment, "chatEntry" + chatEntry.chatEntryID)
				.AddToBackStack ("chatEntry" + chatEntry.chatEntryID)
				.Commit ();
		}
	}

	public class MyWebViewClient : WebViewClient { 
		readonly NotificationsWebFragment fragment;
		readonly NotificationEntry notificationEntry;
		protected NotificationList notificationList;

		public MyWebViewClient (NotificationsWebFragment fragment, NotificationEntry notificationEntry, NotificationList notificationList) {
			this.fragment = fragment;
			this.notificationEntry = notificationEntry;
			this.notificationList = notificationList;
		}

		public override bool ShouldOverrideUrlLoading (WebView view, string url) {
			if(url.Contains("sendMessage")) {
				//record action initiated
				notificationList.MarkNotificationEntryActionInitiatedAsync(notificationEntry);

				int beginningIndex = url.IndexOf("c=");

				string rawData = url.Substring(beginningIndex + 2);
				string encodedBase64Contact = Uri.UnescapeDataString(rawData);

				byte[] decodedContactBytes = Convert.FromBase64String(encodedBase64Contact);
				string decodedContact = Encoding.UTF8.GetString(decodedContactBytes);

				ContactInput contactInput = JsonConvert.DeserializeObject<ContactInput>(decodedContact);
				var existing = Contact.FindOrCreateContact (EMApplication.GetInstance().appModel, contactInput);

				fragment.sharedNotificationsWebController.GoToNewOrExistingChatEntry (existing);
			}

			//don't return base... if you return false, it tries to load a URL. The proper thing to do here is to initiate a new message to the user/group
			return true;
		}

		public override void OnPageFinished (WebView view, string url) {
			base.OnPageFinished (view, url);

			AndHUD.Shared.Dismiss (fragment.Activity);
		}
	}

	public class SharedNotificationsWebController : AbstractNotificationsWebController {
		private WeakReference _r = null;
		private NotificationsWebFragment Self {
			get { return this._r != null ? this._r.Target as NotificationsWebFragment : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedNotificationsWebController (NotificationsWebFragment pc, ApplicationModel appModel) : base (appModel) {
			this.Self = pc;
		}

		public override void DidChangeColorTheme () {
			NotificationsWebFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.ThemeController ();
		}

		public override void GoToChatControllerUsingChatEntry (ChatEntry chatEntry) {
			NotificationsWebFragment self = this.Self;
			if (GCCheck.ViewGone (self)) return;
			self.TransitionToChatControllerUsingChatEntry (chatEntry);
		}
	}
}

