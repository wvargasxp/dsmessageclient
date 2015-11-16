using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.IO;
using System.Net;

namespace em {
	public class OutgoingQueue {
		public ApplicationModel appModel { get; set; }
		static readonly int MAX_RETRIES = 10;

		private OutgoingQueueStrategy queueStrategy = null;

		public void DidLaunchApp() {
			this.appModel.queueDao.MarkAllQueueEntriesPending ();
			RemoveAllMarkedForDeletion ();

			this.queueStrategy = new DefaultOutgoingQueueStrategy (this, appModel.queueDao);
			SendPendingVideoEncodingEntries ();

			NotificationCenter.DefaultCenter.AddWeakObserver (null, Constants.EMAccount_EMHttpAuthorizedResponseNotification, HandleEmHttpAuthorizedResponse);
		}

		public void DidConnectToServer () {
			this.queueStrategy.Start ();
		}

		public void DidDisconnectFromServer () {
			this.queueStrategy.Stop ();

			this.queueStrategy.Enqueue (new ActionQueueInstruction (() => {
				if (this.appModel != null && this.appModel.queueDao != null) {
					this.appModel.queueDao.MarkAllWebsocketQueueEntriesAsPending ();
				}
			}));
		}

		public void HandleEmHttpAuthorizedResponse (Notification notif) {
			this.RetryUnauthorizedQueueEntries ();
		}

		private void SendPendingVideoEncodingEntries () {
			// Find all video queue entries that are pending and mark their media state, then get the oldest entry and encode that one.
			IList<QueueEntry> videoEncodingQueueEntries = this.appModel.queueDao.FindPendingVideoEncodingQueueEntries ();
			foreach (QueueEntry entry in videoEncodingQueueEntries) {
				this.queueStrategy.Send (new QueueEntryInstruction (entry, null));
			}
		}

		public void AppendQueueEntry(QueueEntry queueEntry) {
			this.appModel.queueDao.InsertQueueEntry (queueEntry);
		}

		public static Message FindMesssageFromQueueEntry (ApplicationModel appModel, QueueEntry queueEntry) {
			Message message = null;
			foreach (QueueEntryContents qec in queueEntry.contents) {
				if (qec.localID != null) {
					string guid = Message.GUIDFromLocalID (qec.localID);
					string fromAlias = Message.FromAliasFromLocalID (qec.localID);
					message = Message.FindMessageByMessageGUID (appModel, guid, fromAlias);
				}
			}
			return message;
		}

		public void AckQueueEntry (string receiptId) {
			QueueEntry queueEntry = this.appModel.outgoingQueue.FindQueueEntry (Convert.ToInt32 (receiptId));
			if (queueEntry != null) {
				this.appModel.outgoingQueue.RemoveQueueEntry (queueEntry, true);
			}
		}

		private void RemoveAllMarkedForDeletion () {
			IList<QueueEntry> pendingDeletes = this.appModel.queueDao.FindPendingDeletions ();

			Debug.WriteLine ("culling {0} entries marked for deletion", pendingDeletes.Count);

			foreach (QueueEntry queueEntry in pendingDeletes) {
				RemoveQueueEntry (queueEntry);
			}
		}

		public void MarkForDeletion (QueueEntry queueEntry) {
			queueEntry.entryState = QueueEntryState.PendingDelete;
			this.appModel.queueDao.UpdateStatus (queueEntry);

			UpdateOutgoingQueueAndModel (queueEntry, successOfSend: true);
		}

		virtual public void RemoveQueueEntry (QueueEntry queueEntry, bool successOfSend) {
			RemoveQueueEntry (queueEntry);

			UpdateOutgoingQueueAndModel (queueEntry, successOfSend);
		}

		private void RemoveQueueEntry (QueueEntry queueEntry) {
			foreach (QueueEntryContents contents in queueEntry.contents) {
				if (contents.deleteOnRemoval) {
					EMTask.DispatchBackground (() => {
						this.appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (contents.localPath);
					});
				}
			}

			this.appModel.queueDao.DeleteQueueEntry (queueEntry);
		}

		private void UpdateOutgoingQueueAndModel (QueueEntry queueEntry, bool successOfSend) {
			if (successOfSend) {
				this.queueStrategy.Enqueue (new QueueEntryEvent (QueueEntryEventType.Acked, queueEntry));
			} else {
				if (queueEntry.destination.Equals (StompPath.kSendMessage) ||
					queueEntry.destination.Equals ("sendMessage") ||
					queueEntry.destination.Equals ("/uploadFiles/sendMessage")) {

					lock (this.appModel.OLock) {
						Message message = FindMesssageFromQueueEntry (this.appModel, queueEntry);

						if (message != null && message.UpdateMessageStatus (MessageStatus.failed)) {
							message.Save ();
						}
					}
				}

				this.queueStrategy.Enqueue (new QueueEntryEvent (QueueEntryEventType.Removed, queueEntry));
			}
		}

		public QueueEntry FindQueueEntry (int queueEntry) {
			return this.appModel.queueDao.FindQueueEntry (queueEntry);
		}

		public void RetrySend (QueueEntry queueEntry, Action<EMHttpResponse> responseCallback) {
			Debug.WriteLine (String.Format ("outgoing queue: message ID = {0}; try {1} of {2}", queueEntry.messageID, queueEntry.retryCount, MAX_RETRIES));

			this.queueStrategy.Enqueue (new QueueEntryInstruction (queueEntry, responseCallback));
		}

		public void EnqueueAndSend (QueueEntry queueEntry) {
			EnqueueAndSend (queueEntry, null);
		}

		public void EnqueueAndSend (QueueEntry queueEntry, Action<EMHttpResponse> callback) {
			queueEntry.entryState = QueueEntryState.Pending;
			this.appModel.outgoingQueue.AppendQueueEntry (queueEntry);

			this.queueStrategy.Enqueue (new QueueEntryInstruction (queueEntry, callback));
		}

		public void Send (QueueEntry queueEntry, Action<EMHttpResponse> callback) {
			switch (queueEntry.route) {
			case QueueRoute.Websocket:
				this.appModel.outgoingQueue.SendViaWebsocket (queueEntry);
				break;

			case QueueRoute.Rest:
				this.appModel.outgoingQueue.SendViaRest (queueEntry, callback);
				break;

			case QueueRoute.VideoEncoding:
				this.appModel.outgoingQueue.EncodeVideoFromQueueEntry (queueEntry);
				break;

			default:
				// should never happen
				this.appModel.outgoingQueue.RemoveQueueEntry(queueEntry, false);
				break;
			}
		}

		ThreadSafeHashSet<Timer> stompMessageTimeouts = new ThreadSafeHashSet<Timer> ();

		protected void SendViaWebsocket (QueueEntry queueEntry) {
			if (queueEntry.contents.Count != 1) {
				Debug.WriteLine ("Trying to send via websocket with entry that has the wrong amount of contents");
				RemoveQueueEntry (queueEntry, false);
				return;
			} else {
				QueueEntryContents contents = queueEntry.contents [0];
				byte[] messageBody = this.appModel.platformFactory.GetFileSystemManager ().ContentsOfFileAtPath (contents.localPath);
				if (messageBody == null) {
					if (this.appModel.queueDao.FindQueueEntry (queueEntry.messageID) == null) {
						Debug.WriteLine (String.Format ("aborting attempt to send message {0} that has been already removed from the queue", queueEntry.messageID));
						this.queueStrategy.Enqueue (new QueueEntryEvent (QueueEntryEventType.Removed, queueEntry));
						return;
					}

					RemoveQueueEntry (queueEntry, false);
					Debug.WriteLine (String.Format ("aborting attempt to send message because existing queue entry {0} missing message body", queueEntry.messageID));
					return;
				}

				string json = System.Text.Encoding.UTF8.GetString (messageBody, 0, messageBody.Length);
				try {
					this.appModel.liveServerConnection.SendToDestinationAsync (json, queueEntry.destination, queueEntry.messageID);
				} catch (Exception e) {
					Debug.WriteLine (e.Message);
					DidFailToSendMessage (queueEntry, enqueueForRetry: true);
				}
			}

			Timer messageTimer = null;
			messageTimer = new Timer (o => {
				QueueEntry entry = FindQueueEntry (queueEntry.messageID);
				if (entry == null) {
					stompMessageTimeouts.Remove (messageTimer);  // un-pin, so timer can be gc'ed
					return;
				}

				DidFailToSendMessage (entry, enqueueForRetry: true);

				stompMessageTimeouts.Remove (messageTimer);  // un-pin, so timer can be gc'ed
			}, null, Constants.TIMER_SEND_CHAT_MESSAGE_VIA_WEBSOCKET_TIMEOUT, Timeout.Infinite);

			stompMessageTimeouts.Add (messageTimer); // pin, to prevent timer from being gc'ed
		}
			
		ThreadSafeList<int> queueEntry401sList = new ThreadSafeList<int> ();

		protected void SendViaRest (QueueEntry queueEntry, Action<EMHttpResponse> callback) {
			if (queueEntry.methodType == QueueRestMethodType.Post) {
				byte[] bytes = queueEntry.contents.Count == 0 ? new byte[0] : this.appModel.platformFactory.GetFileSystemManager ().ContentsOfFileAtPath (queueEntry.contents[0].localPath);

				this.appModel.account.httpClient.SendApiRequestAsync (queueEntry.destination, bytes, HttpMethod.Post, queueEntry.contents.Count == 0 ? "application/json" : queueEntry.contents[0].mimeType, (EMHttpResponse response) => {
					if (response.IsSuccess)
						RemoveQueueEntry (queueEntry, true);
					else if (response.IsRetryable)
						DidFailToSendMessage (queueEntry, enqueueForRetry:true);
					else
						DidFailToSendMessage (queueEntry, enqueueForRetry:false);

					if ( callback != null )
						callback (response);
				});
			} else if (queueEntry.methodType == QueueRestMethodType.MultiPartPost) {
				Message message = FindMesssageFromQueueEntry (this.appModel, queueEntry);
				if (message != null) {
					message.SetQueueEntryUploadDelegates (queueEntry);
				}

				this.appModel.account.httpClient.SendUploadMediaRequestAsync (queueEntry, () => {
					DidFailToSendMessage (queueEntry, enqueueForRetry: true);
				}, (EMHttpResponse response) => {
					if (response.IsSuccess) {
						MarkForDeletion (queueEntry);
					} else if (response.IsRetryable) {
						DidFailToSendMessage (queueEntry, enqueueForRetry:true);
					} else {
						DidFailToSendMessage (queueEntry, enqueueForRetry:false);

						if (response.EMHttpStatusCode == EMHttpStatusCode.AuthorizationException) {
							queueEntry401sList.Add (queueEntry.messageID);
							RetryUnauthorizedQueueEntries ();
						}
					}

					if (callback != null) {
						callback (response);
					}
				});
			}
		}

		private void RetryUnauthorizedQueueEntries () {
			ResetUnauthorizedQueueEntries ();

			this.queueStrategy.Start ();
		}

		private void ResetUnauthorizedQueueEntries () {
			EMTask.ExecuteNowIfMainOrDispatchMain (() => {
				
				if (!this.appModel.account.IsLoggedIn) {
					return;
				}

				EMTask.DispatchBackground (() => {
					IList<int> messageIDs = queueEntry401sList.Drain ();
					if (messageIDs.Count == 0) {
						return;
					}

					OutgoingQueueDao queueDao = this.appModel.queueDao;

					foreach (int messageID in messageIDs) {
						QueueEntry queueEntry = queueDao.FindQueueEntry (messageID);
						if (queueEntry == null) {
							continue;
						}

						if (queueEntry.route != QueueRoute.Rest) {
							continue;
						}

						queueEntry.entryState = QueueEntryState.UploadPending;
						queueDao.UpdateStatus (queueEntry);
					}
				});
			});
		}

		protected void EncodeVideoFromQueueEntry (QueueEntry queueEntry) {
			IVideoConverter converter = this.appModel.platformFactory.GetVideoConverter ();
			foreach (QueueEntryContents content in queueEntry.contents) {
				if (ContentTypeHelper.IsVideo (ContentTypeHelper.FromString (content.mimeType))) {

					Message message = OutgoingQueue.FindMesssageFromQueueEntry (this.appModel, queueEntry);
					message.media.MediaState = MediaState.Encoding;

					ConvertVideoInstruction instruction = new ConvertVideoInstruction (content.localPath, (bool success, QueueEntry entry) => {
						EMTask.Dispatch (() => {
							this.queueStrategy.Enqueue (new QueueEntryEvent (QueueEntryEventType.EndEncoding, entry));
						}, EMTask.OUTGOING_QUEUE_QUEUE);
					}, queueEntry);

					converter.ConvertVideo (instruction);
				}
			}
		}

		protected void DidFailToSendMessage (QueueEntry queueEntry, bool enqueueForRetry) {
			Debug.Assert (queueEntry.route != QueueRoute.VideoEncoding, "DidFailToSendMessage- Unexpected queueEntry route VideoEncoding");
			queueEntry.retryCount++;
			if ( queueEntry.retryCount >= MAX_RETRIES ) {
				Debug.WriteLine ("Failed to resend QueueEntry after " + MAX_RETRIES + " retries will drop " + queueEntry.destination);
				RemoveQueueEntry (queueEntry, false);
			}
			else {
				switch (queueEntry.route) {
				case QueueRoute.Websocket:
					queueEntry.entryState = QueueEntryState.Pending;
					break;
				case QueueRoute.Rest:
					if (enqueueForRetry) {
						// we keep the queueEntry in the Sending state so they're not immediately retried. The case here only applies to REST queue entries...
						queueEntry.entryState = QueueEntryState.UploadPending;
					}
					break;
				}

				this.appModel.queueDao.UpdateStatusAndRetryCount (queueEntry);
			}

			this.queueStrategy.Enqueue (new QueueEntryEvent (QueueEntryEventType.Failed, queueEntry));
		}

	}

	public enum QueueRoute {
		Websocket,
		Rest,
		VideoEncoding
	}

	public enum QueueEntryState {
		Pending,             // retry pending
		Sending,             // in normal queue being sent
		Reuploading,            // in re-upload queue being sent
		UploadPending,        // a re-upload of a media message is pending
		Encoding,			// video in the process of encoding, if not encoding and is a video, should be Pending state
		PendingDelete        // marked for deletion of queue entry. Deletion happens on app launch.
	}

	public enum QueueRestMethodType {
		Get,
		Post,
		MultiPartPost,
		Put,
		Delete,
		NotApplicable
	}

	public class QueueEntry {
		public int messageID { get; set; }
		public QueueRestMethodType methodType { get; set; }

		public delegate void WillStartUpload ();
		public delegate void DidUploadPercentage(double perc);
		public delegate void DidCompleteUpload();
		public delegate void DidFailUpload();

		public WillStartUpload DelegateWillStartUpload = () => { };
		public DidUploadPercentage DelegateDidUploadPercentage = (double perc) => {};
		public DidCompleteUpload DelegateDidCompleteUpload = () => {};
		public DidFailUpload DelegateDidFailUpload = () => {};

		public string methodTypeString {
			get {
				switch (methodType) {
				case QueueRestMethodType.Get:
					return "G";
				case QueueRestMethodType.Post:
					return "R";
				case QueueRestMethodType.MultiPartPost:
					return "M";
				case QueueRestMethodType.Put:
					return "P";
				case QueueRestMethodType.Delete:
					return "D";
				default:
					return "N";
				}
			}
			set {
				switch (value) {
				case "G":
					methodType = QueueRestMethodType.Get;
					break;
				case "R":
					methodType = QueueRestMethodType.Post;
					break;
				case "M":
					methodType = QueueRestMethodType.MultiPartPost;
					break;
				case "P":
					methodType = QueueRestMethodType.Put;
					break;
				case "D":
					methodType = QueueRestMethodType.Delete;
					break;
				default:
					methodType = QueueRestMethodType.NotApplicable;
					break;
				}
			}
		}

		public QueueRoute route { get; set; }
		public string routeString {
			get {
				switch (route) {
				case QueueRoute.Rest:
					return "R";
				case QueueRoute.VideoEncoding:
					return "V";
				case QueueRoute.Websocket:
				default: /* Websocket */
					return "W";
				}
			}

			set {
				if (value.Equals ("R")) {
					route = QueueRoute.Rest;
				} else if (value.Equals ("V")) {
					route = QueueRoute.VideoEncoding;
				} else { /* W */
					route = QueueRoute.Websocket;
				}
			}
		}

		DateTime sd;
		public DateTime sentDate { get { return sd; } set { sd = value.ToUniversalTime (); } }
		public string sendDateString {
			get {
				DateTimeOffset offset = new DateTimeOffset(sentDate,TimeSpan.Zero);
				return offset.ToString(Constants.ISO_DATE_FORMAT, Preference.usEnglishCulture);
			}
			set {
				sentDate = DateTime.ParseExact (value, Constants.ISO_DATE_FORMAT,
					Preference.usEnglishCulture);
			}
		}

		public string destination { get; set; }
		public int retryCount { get; set; }
		public QueueEntryState entryState { get; set; }
		public string entryStateString {
			get {
				switch (entryState) {
				case QueueEntryState.Pending:
					return "P";
				case QueueEntryState.Sending:
					return "S";
				case QueueEntryState.Reuploading:
					return "R";
				case QueueEntryState.UploadPending:
					return "U";
				case QueueEntryState.Encoding:
					return "E";
				case QueueEntryState.PendingDelete:
					return "D";
				default:
					return "P";
				}

			}

			set {
				switch (value) {
				case "P":
					entryState = QueueEntryState.Pending;
					break;
				case "S": 
					entryState = QueueEntryState.Sending;
					break;
				case "R":
					entryState = QueueEntryState.Reuploading;
					break;
				case "U":
					entryState = QueueEntryState.UploadPending;
					break;
				case "E":
					entryState = QueueEntryState.Encoding;
					break;
				case "D":
					entryState = QueueEntryState.PendingDelete;
					break;
				default:
					entryState = QueueEntryState.Pending;
					break;
				}
			}
		}

		public IList<QueueEntryContents> contents { get; set; }

		public QueueEntry() {
			contents = new List<QueueEntryContents> ();
		}
	}

	public class QueueEntryContents {
		public string localPath { get; set; }
		public bool deleteOnRemoval { get; set; }
		public string deleteOnRemovalString {
			get {
				return deleteOnRemoval ? "Y" : "N";
			}
			set {
				deleteOnRemoval = value.Equals ("Y") ? true : false;
			}
		}
		public string mimeType { get; set; }
		public string name { get; set; }
		public string fileName { get; set; }
		string lid;
		public string localID { 
			get { return lid; }
			set { lid = value; }
		}

		public static QueueEntryContents CreateTemporaryContents (ApplicationModel appModel, byte[] contents, string mimeType, string name, string fileName) {
			QueueEntryContents retVal = new QueueEntryContents ();

			string localTempFilePath = appModel.platformFactory.GetUriGenerator ().GetOutgoingQueueTempName ();
			appModel.platformFactory.GetFileSystemManager ().CreateParentDirectories (localTempFilePath);
			appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (localTempFilePath, contents, null);

			retVal.localPath = localTempFilePath;
			retVal.deleteOnRemoval = true;
			retVal.mimeType = mimeType;
			retVal.name = name;
			retVal.fileName = fileName;

			return retVal;
		}

		public static QueueEntryContents CreateTemporaryContentsFromLocalPath (string localPath, string mimeType, string name, string fileName) {
			QueueEntryContents retVal = CreateContentsFromLocalPath (localPath, mimeType, name, fileName);
			retVal.deleteOnRemoval = true;
			return retVal;
		}

		public static QueueEntryContents CreateContentsFromLocalPath (string localPath, string mimeType, string name, string fileName) {
			QueueEntryContents retVal = new QueueEntryContents ();

			retVal.localPath = localPath;
			retVal.deleteOnRemoval = false;
			retVal.mimeType = mimeType;
			retVal.name = name;
			retVal.fileName = fileName;

			return retVal;
		}
	}

	/**
	 * A readonly, unseekable stream wrapper that triggers a callback when bytes are read from the underlying finite stream.
	 * Used for reporting on the progress of file uploads.
	 */
	public class ProgressStream : Stream {

		private Stream content;

		public String Filename { get; set; }

		public Action<double> CompletionCallback { get; set; }

		private long totalBytesRead;

		/** ===== Stream class variables ===== */

		public override bool CanRead {
			get {
				return content.CanRead;
			}
		}
		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanTimeout {
			get {
				return content.CanTimeout;
			}
		}

		public override bool CanWrite {
			get {
				return false;
			}
		}

		public override long Length {
			get {
				return content.Length;
			}
		}

		public override long Position {
			get {
				return content.Position;
			}
			set {
				content.Position = value;
			}
		}

		public override int ReadTimeout {
			get {
				return content.ReadTimeout;
			}
			set {
				content.ReadTimeout = value;
			}
		}

		public override int WriteTimeout {
			get {
				return content.WriteTimeout;
			}
			set {
				content.WriteTimeout = value;
			}
		}

		public ProgressStream (Stream stream, String filename) {
			this.content = stream;
			this.Filename = filename;
			this.CompletionCallback = null;
			this.totalBytesRead = 0;
		}

		/** ===== Stream class methods ===== */

		public override int Read (byte[] buffer, int offset, int count) {
			int bytesRead = this.content.Read (buffer, 0, buffer.Length);
			if (this.content.Length != -1) {
				totalBytesRead += bytesRead;
				if (this.CompletionCallback != null && bytesRead > 0) {
					this.CompletionCallback ((double)totalBytesRead / (double)content.Length);
				}
			}
			return bytesRead;
		}

		public override void SetLength (long value) {
			this.content.SetLength (value);
		}

		public override void Flush () {
			this.content.Flush ();
		}

		public override int ReadByte () {
			return this.content.ReadByte ();
		}

		public override long Seek (long offset, SeekOrigin origin) {
			return this.content.Seek (offset, origin);
		}

		public override void Write (byte[] buffer, int offset, int count) {
			this.content.Write (buffer, offset, count);
		}

		public override void WriteByte (byte value) {
			this.content.WriteByte (value);
		}
	}
}