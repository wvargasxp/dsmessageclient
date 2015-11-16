using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using AFNetworkingLibrary;
using em;
using EMXamarin;
using Foundation;
using System.Globalization;
using UIKit;


namespace iOS {
	public class iOSHttpClient : NSObject, IHttpInterface {

		#region properties
		AFLibrary library = null;
		protected AFLibrary Library {
			get {
				if (library == null) {
					library = new AFLibrary ();
				}

				return library;
			}
		}
		#endregion

		public iOSHttpClient () {}

		#region basic requests
		public void SendRequestAsync (
			string address, 
			string json, 
			HttpMethod method, 
			string contentType, 
			Action<EMHttpResponse> completionHandler
		) {
			SendRequestAsync (address, json, method, contentType, completionHandler, null);
		}

		public void SendRequestAsync (
			string address, 
			string json, 
			HttpMethod method, 
			string contentType, 
			Action<EMHttpResponse> completionHandler,
			int? timeoutInSeconds
		) {
			EMTask.Dispatch (() => {
				KeepAliveManager.Shared.Add ();
				if (timeoutInSeconds.HasValue) {
					this.Library.SendRequest (address, json, method.Method, contentType, timeoutInSeconds.Value, (AFResponse response) => {
						HandleResponse (response, address, completionHandler);
					});
				} else {
					this.Library.SendRequest (address, json, method.Method, contentType, (AFResponse response) => {
						HandleResponse (response, address, completionHandler);
					});
				}
			}, EMTask.HTTP_REQUEST_QUEUE);
		}

		private void HandleResponse (AFResponse response, string address, Action<EMHttpResponse> completionHandler) {
			Debug.Assert (response != null, "Response should never be NULL. Address: " + address);

			var clientDate = DateTime.Now;
			DateTime serverDate = default(DateTime);
			NSObject serverDateObj = null;

			serverDateObj = response.Headers.ValueForKey (new NSString ("Date"));

			if (serverDateObj != null) {
				serverDate = DateTime.Parse (serverDateObj.ToString (), Preference.usEnglishCulture);
			}

			if (!response.Success) {
				// TODO: we could also do something with the Error property on response.Error
				Debug.WriteLine ("SendRequest error is " + response.Error.Description);

				HttpResponseHelper.CheckUnauthorized (response.StatusCode);

				if (serverDate == default(DateTime)) {
					completionHandler (EMHttpResponse.FromStatusCode (response.StatusCode));
				} else {
					completionHandler (EMHttpResponse.FromStatusCode (response.StatusCode, serverDate, clientDate));
				}
			} else {
				if (serverDate == default(DateTime)) {
					completionHandler (EMHttpResponse.FromString (response.Response));
				} else {
					completionHandler (EMHttpResponse.FromString (response.Response, serverDate, clientDate));
				}
			}

			KeepAliveManager.Shared.Remove ();
		}
		#endregion

		#region downloads
		public void SendDownloadMediaRequest (Media media) {
			MediaDownloadRequestHelper requestHelper = MediaDownloadRequestHelper.FromMediaObject (media);
			this.Library.SendMediaRequest (requestHelper);
		}

		public void SendDownloadRequestWithHandler (string address, Action<Stream, long> callback) {
			throw new NotImplementedException ();
		}
		#endregion

		#region uploads
		public void SendUploadMediaRequest (QueueEntry queueEntry, Action failureCallback, Action<EMHttpResponse> callback) {
			this.Library.SendUploadMediaRequest (MediaUploadRequestHelper.FromQueueEntry (queueEntry, failureCallback, callback));
		}
		#endregion

		#region image search
		public void SendImageSearchRequest (string address, string basicAuthKey, Action<EMHttpResponse> callback) {
			// The reason this code looks like this is because it's handling two cases.
			// One is the initial request using Bing's API to get a list of search images. 
			// The second is the case where we're actually making a GET request to get the image.
			// In the first case, we want a string reprsentation, in the second case, as bytes.
			EMTask.Dispatch (() => {
				KeepAliveManager.Shared.Add ();

				// Todo, this is more like a hack than anything. Should just use a separate call to get the image out.
				if (address.Contains ("datamarket")) {
					this.Library.SendImageSearchRequest (address, basicAuthKey, (AFResponse response) => {
						if (response.Success) {
							callback (EMHttpResponse.FromString (response.Response));
						} else {
							callback (EMHttpResponse.GenericException ());
						}

						KeepAliveManager.Shared.Remove ();
					});
				} else {
					this.Library.SendImageSearchRequest (address, null, (AFResponse response) => {
						if (response.Success) {
							NSData data = response.Data;
							byte[] bytes = data.ToByteArray ();
							if (bytes != null) {
								callback (EMHttpResponse.FromBytes (bytes));
							} else {
								callback (EMHttpResponse.GenericException ());
							}
						} else {
							callback (EMHttpResponse.GenericException ());
						}

						KeepAliveManager.Shared.Remove ();
					});
				}

			}, EMTask.HTTP_REQUEST_QUEUE);
		}
		#endregion
	}

	#region downloads - helper
	public class MediaDownloadRequestHelper : DownloadRequestHelper {
		protected WeakReference Ref { get; set; }
		private string Address { get; set; }

		public static MediaDownloadRequestHelper FromMediaObject (Media media) {
			MediaDownloadRequestHelper listener = new MediaDownloadRequestHelper ();
			listener.Address = media.uri.AbsoluteUri;
			listener.Ref = new WeakReference (media);
			return listener;
		}

		public override void HasProgress (NSNumber progress) {
			double fractionCompleted = progress.DoubleValue;
			Media media = this.Ref.Target as Media;
			if (media != null) {
				media.BackgroundDownloadProgressEvent (fractionCompleted);
			}
		}

		public override string MediaAddress {
			get {
				return this.Address;
			}
		}

		public override void Begin () {
			KeepAliveManager.Shared.Add ();

			Media media = this.Ref.Target as Media;
			if (media != null) {
				media.BackgroundDownloadStartEvent ();
			}
		}

		public override void End (NSError error, NSUrlResponse response, NSUrl filePath) {
			Media media = this.Ref.Target as Media;
			if (media != null) {
				ApplicationModel applicationModel = AppDelegate.Instance.applicationModel;
				bool success = error == null;
				if (success) {
					PlatformFactory platformFactory = AppDelegate.Instance.applicationModel.platformFactory;
					string finalMediaPath = media.GetPathForUri (platformFactory);
					string downloadedLocation = filePath.AbsoluteString;

					Uri uri = new Uri (downloadedLocation);
					string localDownloadedPath = uri.LocalPath;

					platformFactory.GetFileSystemManager ().MoveFileAtPath (localDownloadedPath, finalMediaPath);

					media.BackgroundDownloadFinishEvent (finalMediaPath, applicationModel);
				} else {
					Debug.WriteLine ("Failed to download file at path: {0} error: {1}", filePath, error);
					NSHttpUrlResponse httpResponse = response as NSHttpUrlResponse;
					if (httpResponse == null) {
						media.BackgroundDownloadErrorEvent (applicationModel, false); // httpResponse shouldn't be null, lets just say this download shouldn't be retried
					} else {
						nint statusCode = httpResponse.StatusCode;
						if (statusCode >= 400 && statusCode != 503) {
							HttpResponseHelper.CheckUnauthorized ((int)statusCode);

							media.BackgroundDownloadErrorEvent (applicationModel, false); // on a 400 or 500 (/w 503 as an exception) level response, we let the media know, it shouldnt retry the download
						} else {
							media.BackgroundDownloadErrorEvent (applicationModel, true);
						}
					}
				}
			}

			KeepAliveManager.Shared.Remove ();
		}
	}
	#endregion

	#region uploads - helper
	public class MediaUploadRequestHelper : UploadRequestHelper {
		private WeakReference queueEntryRef;
		protected WeakReference Ref {
			get { return queueEntryRef; }
			set { queueEntryRef = value; }
		}

		public string Destination { get; set; }
		private Action<EMHttpResponse> Completion { get; set; }
		private Action FailureCallback { get; set; }

        private double _lastPercentage = 0.0; // keep track of the last percentage 
        private double LastPercentage { get { return this._lastPercentage; } set { this._lastPercentage = value; } } 

        private double _uploadPercentageDifference = 0.0;
		private double UploadPercentageDifference { get { return this._uploadPercentageDifference; } set { this._uploadPercentageDifference = value; } }

		private const double MinimumDifference = .01;

		public static MediaUploadRequestHelper FromQueueEntry (QueueEntry queueEntry, Action failureCallback, Action<EMHttpResponse> completionHandler) {
			MediaUploadRequestHelper listener = new MediaUploadRequestHelper ();
			listener.Ref = new WeakReference (queueEntry);

			string destination = queueEntry.destination;
			string address = AppEnv.UPLOAD_HTTP_BASE_ADDRESS + destination;
			string deviceInformationJson = AppDelegate.Instance.applicationModel.account.deviceInfo.DeviceJSONString ();
			string base64DeviceInformation = Convert.ToBase64String (Encoding.UTF8.GetBytes (deviceInformationJson)); // slow?
			if(address.Contains("?"))
				address = address + "&deviceInformation=" + base64DeviceInformation;
			else
				address = address + "?deviceInformation=" + base64DeviceInformation;

			listener.Destination = address;

			foreach (QueueEntryContents contents in queueEntry.contents) {
				string systemPathForContent = AppDelegate.Instance.applicationModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (contents.localPath);
				listener.FilePaths.Add ((NSString)systemPathForContent);
				listener.Names.Add ((NSString)contents.name);
				listener.FileNames.Add ((NSString)contents.fileName);
				listener.MimeTypes.Add ((NSString)contents.mimeType);
			}

			listener.Completion = completionHandler;
			listener.FailureCallback = failureCallback;

			return listener;
		}

		private bool ShouldCallDelegate (double newPercentage) {
			double difference = newPercentage - this.LastPercentage;
			this.UploadPercentageDifference += difference;
			if (this.UploadPercentageDifference > MinimumDifference) {
				this.UploadPercentageDifference = 0.0; // reset the difference back to 0
				return true;
			} else {
				return false;
			}
		}

		public override void HasProgress (NSNumber progress) {
			double fractionCompleted = progress.DoubleValue;
			QueueEntry queueEntry = this.Ref.Target as QueueEntry;
			if (ShouldCallDelegate (fractionCompleted)) {
				if (queueEntry != null) {
					queueEntry.DelegateDidUploadPercentage (fractionCompleted);
				}
			}
		}

		public override string MediaAddress {
			get {
				return this.Destination;
			}
		}

		public override void Begin () {
			KeepAliveManager.Shared.Add ();
			QueueEntry queueEntry = this.Ref.Target as QueueEntry;
			if (queueEntry != null) {
				queueEntry.DelegateWillStartUpload ();
			}
		}

		public override void End (NSError error, NSUrlResponse response, NSObject responseObject) {
			bool success = error == null;
			QueueEntry queueEntry = this.Ref.Target as QueueEntry;
			if (queueEntry != null) {
				if (success) {
					queueEntry.DelegateDidCompleteUpload ();
				} else {
					queueEntry.DelegateDidFailUpload ();
				}
			}

			int statusCode = EMHttpStatusCodeHelper.INVALID_STATUS_CODE;
			if (response != null) {
				statusCode = (int)((NSHttpUrlResponse)response).StatusCode;
			}

			if (success && statusCode == 200) {
				NSDictionary responseDict = (NSDictionary)responseObject;
				NSError err; 
				NSData jsonData = NSJsonSerialization.Serialize (responseDict, NSJsonWritingOptions.PrettyPrinted, out err);

				if (err != null) {
					// handle case where serialization fails
					this.Completion (EMHttpResponse.GenericException ());
				} else {
					string jsonString = NSString.FromData (jsonData, NSStringEncoding.UTF8);
					this.Completion (EMHttpResponse.FromString (jsonString));
				}
			} else {
				HttpResponseHelper.CheckUnauthorized (statusCode);

				this.Completion (EMHttpResponse.FromStatusCode(statusCode));
			}

			KeepAliveManager.Shared.Remove ();
		}

		public override void EndWithError (NSError error) {
			this.FailureCallback ();
		}

		private NSMutableArray names;
		public override NSMutableArray Names {
			get {
				if (names == null) {
					names = new NSMutableArray ();
				}

				return names;
			}
		}

		private NSMutableArray fileNames;
		public override NSMutableArray FileNames {
			get {
				if (fileNames == null) {
					fileNames = new NSMutableArray ();
				}

				return fileNames;
			}
		}

		private NSMutableArray filePaths;
		public override NSMutableArray FilePaths {
			get {
				if (filePaths == null) {
					filePaths = new NSMutableArray ();
				}

				return filePaths;
			}
		}

		private NSMutableArray mimeTypes;
		public override NSMutableArray MimeTypes {
			get {
				if (mimeTypes == null) {
					mimeTypes = new NSMutableArray ();
				}

				return mimeTypes;
			}
		}
	}
	#endregion

}