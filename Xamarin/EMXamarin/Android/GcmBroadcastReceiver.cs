using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Support.V4.App;
using Gcm.Client;
using Com.EM.Android;
using em;
using EMXamarin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Android.Support.V4.Content;
using Com.Squareup.Okhttp;

namespace Emdroid {

	[BroadcastReceiver (Permission = Gcm.Client.Constants.PERMISSION_GCM_INTENTS)]
	[IntentFilter (new [] { Gcm.Client.Constants.INTENT_FROM_GCM_MESSAGE }, Categories = new [] { "Android.Android" })]
	[IntentFilter (new [] { Gcm.Client.Constants.INTENT_FROM_GCM_REGISTRATION_CALLBACK }, Categories = new [] { "Android.Android" })]
	[IntentFilter (new [] { Gcm.Client.Constants.INTENT_FROM_GCM_LIBRARY_RETRY }, Categories = new [] { "Android.Android" })]
	public class GcmBroadcastReceiver : GcmBroadcastReceiverBase<GcmService> {
		public static string[] SENDER_IDS = { "773868909362" };
	}

	[Service] //Must use the service tag
	public class GcmService : GcmServiceBase {
		
		private string MEDIA_URL_KEY = "mediaRef";
		private string GUID_KEY = "guid";
		private string SENDER_KEY = "sender";
		private string SENDER_SERVER_ID_KEY = "serverID";
		private string SENDER_THUMB_URL_KEY = "thumbnailURL";
		private string SENDER_ATTRIBUTES_KEY = "attributes";
		private string SENDER_COLOR_KEY = "color";

		private int MAX_LARGE_NOTIFICATION_SIZE;

		public static string NOTIFICATION_GUID_INTENT_KEY = "guid64";

		private Bitmap largeIcon;
		private Bitmap LargeIcon {
			get {
				if (this.largeIcon == null) {
					BitmapFactory.Options opts = new BitmapFactory.Options ();
					opts.InJustDecodeBounds = true;
					BitmapFactory.DecodeResource (Resources, Resource.Drawable.iconLarge, opts);

					// Get the max size of a notification icon based on density.
					int maxSize = DensityHelper.MaxLargeNotificationIconSizeFromDensity ();

					// Basic scaling.
					int inSampleSize = 1;
					int outWidth = opts.OutWidth;
					int outHeight = opts.OutHeight;
					while ((outWidth / inSampleSize) > maxSize
					       || (outHeight / inSampleSize) > maxSize) {
						inSampleSize *= 2;
					}

					opts.InJustDecodeBounds = false;
					opts.InSampleSize = inSampleSize;
					opts.InScaled = false; // DecodeResource will scale the resulting bitmap, ignore this value so that it doesn't get scaled.

					this.largeIcon = BitmapFactory.DecodeResource (Resources, Resource.Drawable.iconLarge, opts);
					int bytecount = this.largeIcon.ByteCount;
				}

				return this.largeIcon;
			}
			set {
				this.largeIcon = value;
			}
		}

		private bool LoggedIn { get; set; }

		private Bitmap ContentPicture { get; set; }

		public GcmService () : base (GcmBroadcastReceiver.SENDER_IDS) {
			
		}

		protected override void OnRegistered (Context context, string registrationId) {
			EMApplication app = EMApplication.GetInstance ();
			if (app != null) {
				app.appModel.platformFactory.getDeviceInfo ().SetPushToken (registrationId);
				app.appModel.account.RegisterForPushNotifications ();
			}
		}

		protected override void OnUnRegistered (Context context, string registrationId) {
			//Receive notice that the app no longer wants notifications
		}

		const int ON_MESSAGE_ID = 312313; // This message id might be different for each person. Hardcoded for now.

		// The reason we use 1 and not 0 is because when a notification is displayed, 1 is the lowest amount of notifications we have.
		// The 0th case doesn't occur.
		const int DEFAULT_UNREAD_COUNT = 1;

		static int unreadCount = DEFAULT_UNREAD_COUNT;
		public static int UnreadCount {
			get { return unreadCount; }
			set { unreadCount = value; }
		}

		public static void ResetUnreadCount () {
			GcmService.UnreadCount = DEFAULT_UNREAD_COUNT;
		}

		public static void ClearNotificationsFromStatusBar () {
			var notificationManager = (NotificationManager)EMApplication.Instance.GetSystemService (NotificationService);
			notificationManager.CancelAll ();
		}

		protected override void OnMessage (Context context, Intent intent) {
			//Push Notification arrived - print out the keys/values
			if (intent == null || intent.Extras == null)
				foreach (var key in intent.Extras.KeySet())
					Debug.WriteLine (String.Format ("Key: {0}, Value: {1}", key, intent.Extras.Get (key)));
			else {
				MAX_LARGE_NOTIFICATION_SIZE = DensityHelper.MaxLargeNotificationIconSizeFromDensity ();
				string message = intent.Extras.GetString ("message", null);
				// oddly the integer unread in the json, is retrieve as a string from the intent.
				GcmService.UnreadCount = Convert.ToInt32 (intent.Extras.GetString ("unread", Convert.ToString (GcmService.UnreadCount)));
				Debug.WriteLine ("Received: " + message);
				em.EMAccount account = EMApplication.Instance.appModel.account;
				// an unread count of zero signals we should clear prior
				// notifications.
				if (GcmService.UnreadCount == 0)
					ClearNotificationsFromStatusBar ();
					
				if (message != null) {
					PendingIntent resultPendingIntent = null;
					string guidInBase64 = (string)(intent.Extras.Get (GUID_KEY));
					string senderString = (string)(intent.Extras.Get (SENDER_KEY));
					UserSettings userSettings = account.UserSettings;
					string photoURL = (string)(intent.Extras.Get (MEDIA_URL_KEY));

					// Create the PendingIntent with the back stack
					// When the user clicks the notification, SecondActivity will start up.
					Android.OS.Bundle bundle = new Android.OS.Bundle ();
					bundle.PutString (NOTIFICATION_GUID_INTENT_KEY, guidInBase64);
					if (account.IsLoggedIn) {
						return;
					}
					var resultIntent = new Intent (this.ApplicationContext, typeof(MainActivity));
					resultIntent.PutExtras (bundle);
					Android.Support.V4.App.TaskStackBuilder stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create (this.ApplicationContext);
					stackBuilder.AddParentStack (Java.Lang.Class.FromType (typeof(MainActivity)));
					stackBuilder.AddNextIntent (resultIntent);
					resultPendingIntent = stackBuilder.GetPendingIntent (0, (int)PendingIntentFlags.UpdateCurrent);
					Android.Net.Uri incomingMessageUri = Android.Net.Uri.Parse ("android.resource://" + this.PackageName + "/" + Android_Constants.IncomingMessageResource);
					// There's an accompanying media object that comes with this message. Proceed to download the media
					if (photoURL != null) {
						string downloadPath = EMApplication.Instance.FilesDir.AbsolutePath + "/notification/" + guidInBase64 + ".temporary";
						string tempPath = downloadPath + ".download";
						AndroidFileSystemManager fsm = (AndroidFileSystemManager)ApplicationModel.SharedPlatform.GetFileSystemManager ();
						if (!(fsm.FileExistsAtPath (downloadPath))) {
							// File doesn't exist, download the media at photoURL
							account.httpClient.SendRequestAsAsyncWithCallback (photoURL, (Stream stream, long length) => {
								// Need to do a cleanup first if the download previously failed due to dirty app termination.
								if (fsm.FileExistsAtPath (tempPath)) {
									fsm.RemoveFileAtPath (tempPath);
								}

								fsm.CopyBytesToPath (tempPath, stream, (length != null ? (long)length : -1L), null);
								// Check if file download is incomplete
								using (Stream fileStream = fsm.GetReadOnlyFileStreamFromPath (tempPath)) {
									if (fileStream.Length != length) {
										String errorMessage = String.Format ("file download incomplete {0}/{1}", fileStream.Length, length);
										Debug.WriteLine (errorMessage);
										fsm.RemoveFileAtPath (downloadPath);
										return;
									} 
								}

								// Move temporary file to real path. On relaunch of the app, any attempt to redownload media associated with
								// this message will go look in downloadPath first to avoid double-download
								fsm.MoveFileAtPath (tempPath, downloadPath);
								this.ContentPicture = BitmapFactory.DecodeFile (downloadPath);
								GenerateSenderThumbnail (senderString, account, userSettings, resultPendingIntent, message, incomingMessageUri);
							});
						} else {
							// File already exists. Use bitmap from file.
							this.ContentPicture = BitmapFactory.DecodeFile (downloadPath);
							GenerateSenderThumbnail (senderString, account, userSettings, resultPendingIntent, message, incomingMessageUri);
						}
					} else {
						// No bitmap to be generated as this is pure text.
						GenerateSenderThumbnail (senderString, account, userSettings, resultPendingIntent, message, incomingMessageUri);
					}
				}
			}
		}

		private Random rgen = new Random ();
		private Object rgenLock = new Object ();

		private void GenerateSenderThumbnail (string senderString, em.EMAccount account, UserSettings userSettings, PendingIntent resultPendingIntent, string message, Android.Net.Uri incomingMessageUri) {
			Debug.Assert (senderString != null, "Payload in the GCM detailing sender's information is null.");
			senderString = HttpQueryStringParser.UrlDecode (senderString);
			Dictionary<string, Object> senderDictionary = (Dictionary<string,Object>)JsonConvert.DeserializeObject (senderString, typeof(Dictionary<string, Object>));
			Debug.Assert (senderDictionary != null, "Was not able to deserialize sender's information");
			string senderServerID = (string)senderDictionary [SENDER_SERVER_ID_KEY];
			object attributesObject = null;
			senderDictionary.TryGetValue (SENDER_ATTRIBUTES_KEY, out attributesObject);

			if (attributesObject == null) { // coming from someone who's not an EM user; don't generate thumbnail
				CreateNotification (userSettings, resultPendingIntent, message, incomingMessageUri);
				return;
			}

			string attributes = attributesObject.ToString ();
			Dictionary<string, Object> colorDictionary = (Dictionary<string,Object>)JsonConvert.DeserializeObject (attributes, typeof(Dictionary<string, Object>));
			string colorHex = (string)colorDictionary [SENDER_COLOR_KEY];
			BackgroundColor senderColor = BackgroundColor.FromHexString (colorHex);
			Contact contactWithServerID = account.accountInfo.appModel.contactDao.ContactWithServerID (senderServerID);
			// TODO: Remove first half of if condition when GCM message is fixed
			// Code path for when there is no media associated with this contact
			PlatformFactory plat = account.applicationModel.platformFactory;
			if (contactWithServerID == null || contactWithServerID.media == null || 
				!plat.GetFileSystemManager ().FileExistsAtPath (contactWithServerID.media.GetPathForUri (plat))) {

				string thumbnailURL = (string)senderDictionary [SENDER_THUMB_URL_KEY];
				if (thumbnailURL != null) {
					AndroidFileSystemManager fsm = (AndroidFileSystemManager)ApplicationModel.SharedPlatform.GetFileSystemManager ();
					string downloadPath = EMApplication.Instance.FilesDir.AbsolutePath + "/notification/" + senderServerID;
					if (fsm.FileExistsAtPath (downloadPath)) {
						senderColor.GetLargePhotoBackgroundResource ((string file) => {
							this.LargeIcon = BitmapThumbnailHelper.CreateBitmapForNotifications (downloadPath, file, MAX_LARGE_NOTIFICATION_SIZE);
							CreateNotification (userSettings, resultPendingIntent, message, incomingMessageUri);
						});
					} else {
						string tempPath = "";
						lock (rgenLock) {
							tempPath = EMApplication.Instance.FilesDir.AbsolutePath + "/notification/" + rgen.Next (10000) + ".tmp";
							while (fsm.FileExistsAtPath (tempPath)) {
								tempPath = EMApplication.Instance.FilesDir.AbsolutePath + "/notification/" + rgen.Next (10000) + ".tmp";
							}
						}
						// Download picture associated with sender's thumbnail
						account.httpClient.SendRequestAsAsyncWithCallback (thumbnailURL, (Stream stream, long length) => {
							fsm.CopyBytesToPath (tempPath, stream, (length != null ? (long)length : -1L), null);
							// Check if file download is incomplete
							using (Stream fileStream = fsm.GetReadOnlyFileStreamFromPath (tempPath)) {
								if (fileStream.Length != length) {
									String errorMessage = String.Format ("file download incomplete {0}/{1}", fileStream.Length, length);
									Debug.WriteLine (errorMessage);
									fsm.RemoveFileAtPath (downloadPath);
									return;
								} 
							}
							// File doesn't exist. All clear to move temp file to permanent path
							if (!fsm.FileExistsAtPath (downloadPath)) {
								fsm.MoveFileAtPath (tempPath, downloadPath);
							}
							// File already exists. Probably generated by another thread. Work was wasted,
							// and no need to overwrite progress made by other thread. Just delete the
							// temp and use the permanent file.
							else {
								fsm.RemoveFileAtPath (tempPath);
							}
							senderColor.GetLargePhotoBackgroundResource ((string file) => {
								this.LargeIcon = BitmapThumbnailHelper.CreateBitmapForNotifications (downloadPath, file, MAX_LARGE_NOTIFICATION_SIZE);
								CreateNotification (userSettings, resultPendingIntent, message, incomingMessageUri);
							});
						});
					}
				} else {
					CreateNotification (userSettings, resultPendingIntent, message, incomingMessageUri);
				}
			} else {
				senderColor.GetLargePhotoBackgroundResource ((string file) => {
					string senderThumbnailPath = contactWithServerID.media.GetPathForUri (account.applicationModel.platformFactory);
					if (senderThumbnailPath != null) {
						this.largeIcon = BitmapThumbnailHelper.CreateBitmapForNotifications (senderThumbnailPath, file, MAX_LARGE_NOTIFICATION_SIZE);
					}
					CreateNotification (userSettings, resultPendingIntent, message, incomingMessageUri);
				});
			}
		}

		private void CreateNotification (UserSettings userSettings, PendingIntent resultPendingIntent, string message, Android.Net.Uri incomingMessageUri) {
			NotificationCompat.Builder builder = new NotificationCompat.Builder (this);

			if (userSettings.PushShowInNotificationCenterEnabled) {
				builder.SetAutoCancel (true); // dismiss the notification from the notification area when the user clicks on it
				builder.SetContentTitle ("APP_TITLE".t ()); // Set the title
				builder.SetNumber (GcmService.UnreadCount++); // Display the count in the Content Info
				//.SetDefaults ((int)NotificationDefaults.Sound) // default system sound for notification
				builder.SetContentText (String.Format (message)); // the message to display.
				if (this.ContentPicture != null) {
					NotificationCompat.BigPictureStyle bps = new NotificationCompat.BigPictureStyle ();
					bps.BigPicture (this.ContentPicture);
					bps.SetSummaryText (String.Format (message));
					builder.SetStyle (bps);
				}
				builder.SetLargeIcon (this.LargeIcon);
				if (int.Parse (Android.OS.Build.VERSION.Sdk) >= 21 /* lollipop */) { 
					builder.SetSmallIcon (Resource.Drawable.IconStatusBar); // This is the icon to display

				} else {
					builder.SetSmallIcon (Resource.Drawable.Icon); // This is the icon to display
				}
				builder.SetContentIntent (resultPendingIntent); // start up this activity when the user clicks the intent.
			}


			//only play sound once every 5 seconds
			if (ShouldPlayNotificationSound () && userSettings.PushWithSoundEnabled)
				builder.SetSound (incomingMessageUri); // custom sound for notification, uncomment this and comment out .SetDefaults to use custom sound

			// Finally publish the notification
			var notificationManager = (NotificationManager)GetSystemService (NotificationService);
			Android.App.Notification notification = builder.Build ();
			notification.Flags |= NotificationFlags.HighPriority;
			notificationManager.Notify (ON_MESSAGE_ID, notification);
		}

		protected override bool OnRecoverableError (Context context, string errorId) {
			Debug.WriteLine ("Received a 'Recoverable' error trying to register for GCM " + errorId);
			EMApplication app = EMApplication.GetInstance ();
			if (app != null)
				app.appModel.account.RegisterForPushNotifications ();

			return true;
		}

		protected override void OnError (Context context, string errorId) {
			Debug.WriteLine ("Received an error trying to register for GCM " + errorId);
			EMApplication app = EMApplication.GetInstance ();
			if (app != null)
				app.appModel.account.RegisterForPushNotifications ();
		}

		static bool playSound;
		static Timer recentlyPlayedTimer;
		static readonly object recentlyPlayedTimerLock = new object ();

		static bool ShouldPlayNotificationSound () {
			lock (recentlyPlayedTimerLock) {
				if (recentlyPlayedTimer == null) {
					playSound = true;
					recentlyPlayedTimer = new Timer (o => {
						lock (recentlyPlayedTimerLock) {
							recentlyPlayedTimer = null;
						}
					}, null, em.Constants.TIMER_INTERVAL_BETWEEN_PLAYING_SOUNDS, Timeout.Infinite);
				} else
					playSound = false;
			}

			return playSound;
		}
	}
}