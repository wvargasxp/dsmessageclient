using System;

namespace em {
	public class NotificationEntry : BaseDataR {

		public int NotificationEntryID { get; set; }

		CounterParty c;
		public CounterParty counterparty {
			get { return c; }
			set {
				CounterParty oldContact = c;
				c = value;
					
				// unregister from prior contact
				RemoveDelegates (oldContact);

				// register current contact
				AddDelegates (c);
			}
		}

		string aid;
		public string ActionID { 
			get { return aid; }
			set { aid = value; }
		}

		DateTime nd;
		public DateTime NotificationDate { 
			get { return nd; } 
			set { nd = value.ToUniversalTime (); } 
		}

		public string NotificationDateString {
			get {
				var offset = new DateTimeOffset(NotificationDate, TimeSpan.Zero);
				return offset.ToString(Constants.ISO_DATE_FORMAT, Preference.usEnglishCulture);
			}
			set { 
				// We need to set the Assume Universal flag here because without it, the value that will be set to previewDate will be of Kind 'Unspecified'. 
				// Converting this 'Unspecified' value to UTC changes its value (in the setter function of previewDate).
				NotificationDate = DateTime.ParseExact (value, 
					Constants.ISO_DATE_FORMAT, 
					Preference.usEnglishCulture, 
					System.Globalization.DateTimeStyles.AdjustToUniversal|System.Globalization.DateTimeStyles.AssumeUniversal);
			}
		}
			
		public string FormattedNotificationDate {
			get {
				string notificationDateString = appModel.platformFactory.GetFormattedDate (NotificationDate, DateFormatStyle.PartialDate);
				return notificationDateString;
			}
		}

		public string Title { get; set; }
		public string Url { get; set; }

		bool r;
		public bool Read {
			get { return r; }
			set { r = value; }
		}
		public string ReadString {
			get { return Read ? "Y" : "N"; }
			set { Read = value.Equals ("Y"); }
		}

		bool d;
		public bool Deleted {
			get { return d; }
			set { d = value; }
		}
		public string DeletedString {
			get { return Deleted ? "Y" : "N"; }
			set { Deleted = value.Equals ("Y"); }
		}

		public delegate void DidCompleteContactThumbnailDownload(CounterParty cp);
		public DidCompleteContactThumbnailDownload DelegateDidCompleteContactThumbnailDownload = (CounterParty cp) => { };

		public NotificationList NotificationList { get; set; }

		public static NotificationEntry FromNotificationInput(ApplicationModel _appModel, NotificationInput input) {
			var retVal = new NotificationEntry ();
			retVal.isPersisted = false;
			retVal.appModel = _appModel;
			retVal.NotificationList = _appModel.notificationList;

			retVal.NotificationEntryID = input.serverID;
			retVal.ActionID = input.actionID;
			retVal.NotificationDate = input.timestamp;
			retVal.Title = input.title;
			retVal.Url = input.url;
			retVal.Read = input.read;
			retVal.Deleted = input.deleted;

			return retVal;
		}

		public static NotificationEntry FindNotificationByServerID(ApplicationModel _appModel, int serverID) {
			System.Diagnostics.Debug.Assert (_appModel != null, "App Model is NULL when trying to find notification by server id");

			lock (_appModel.daoConnection) {
				var ne = _appModel.notificationEntryDao.FindNotificationEntry (serverID);
				if (ne != null) {
					ne.appModel = _appModel;
					ne.NotificationList = _appModel.notificationList;
				}	

				return ne;
			}
		}

		public void Save() {
			if (!Deleted) {
				//update local database
				if (isPersisted)
					appModel.notificationEntryDao.UpdateNotificationEntry (this);
				else
					appModel.notificationEntryDao.InsertNotificationEntry (this);

				//add to list
				if (NotificationList == null)
					NotificationList = appModel.notificationList;

				NotificationList.SaveNotificationEntry (this);

				//TODO: call delegate that it was added
				EMTask.DispatchMain (() => {
					//DelegateDidAddMessage (pos);
				});
			}
		}

		~NotificationEntry() {
			RemoveDelegates ();
		}

		public void Delete() {
			//remove from list
			if (NotificationList == null)
				NotificationList = appModel.notificationList;

			NotificationList.RemoveNotificationEntryAtAsync (this);

			RemoveDelegates ();
		}

		public BackgroundColor ColorTheme {
			get { return counterparty != null ? counterparty.colorTheme : BackgroundColor.Default; }
		}

		public Media Thumbnail {
			get { return counterparty != null ? counterparty.media : null; }
		}

		void ContactDidChangeThumbnail(CounterParty cp) {
			EMTask.DispatchMain (() => {
				if (NotificationList != null)
					NotificationList.DelegateDidChangeNotificationEntryContactThumbnailSource (this);
			});
		}

		void ContactDidLoadThumbnail(CounterParty cp) {
			EMTask.DispatchMain (() => {
				DelegateDidCompleteContactThumbnailDownload (cp);
				if(NotificationList != null)
					NotificationList.DelegateDidDownloadNotificationEntryContactThumbnail (this);
			});
		}

		void BackgroundContactDidFailToDownloadThumbnail (Notification notification) {
			var cp = notification.Source as CounterParty;
			if (cp != null)
				ContactDidChangeThumbnail (cp);
		}

		void ContactDidChangeColorTheme(CounterParty cp) {
			EMTask.DispatchMain (() => {
				if (NotificationList != null)
					NotificationList.DelegateDidChangeNotificationEntryColorTheme (this);
			});
		}

		public void AddDelegates() {
			AddDelegates (counterparty);
		}

		public void AddDelegates(CounterParty cnt) {
			if(cnt != null) {
				cnt.DelegateDidChangeColorTheme += ContactDidChangeColorTheme;
				cnt.DelegateDidChangeThumbnailMedia += ContactDidChangeThumbnail;
				cnt.DelegateDidDownloadThumbnail += ContactDidLoadThumbnail;

				NotificationCenter.DefaultCenter.AddWeakObserver (cnt, Constants.Counterparty_DownloadFailed, BackgroundContactDidFailToDownloadThumbnail);
			}
		}

		public void RemoveDelegates() {
			RemoveDelegates (counterparty);
		}

		public void RemoveDelegates(CounterParty cnt) {
			if(cnt != null) {
				cnt.DelegateDidChangeColorTheme -= ContactDidChangeColorTheme;
				cnt.DelegateDidChangeThumbnailMedia -= ContactDidChangeThumbnail;
				cnt.DelegateDidDownloadThumbnail -= ContactDidLoadThumbnail;

				NotificationCenter.DefaultCenter.RemoveObserverAction (cnt, Constants.Counterparty_DownloadFailed, BackgroundContactDidFailToDownloadThumbnail);
			}
		}
	}
}