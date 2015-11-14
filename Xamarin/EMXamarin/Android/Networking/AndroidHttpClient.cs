using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using em;
using EMXamarin;
using Java.Net;
using Com.Squareup.Okhttp;
using Okio;

namespace Emdroid {
	public class AndroidHttpClient : IHttpInterface  {

		const int TIMEOUT = 300;

		#region properties
		private OkHttpClient client;
		protected OkHttpClient This {
			get { 
				if (client == null) {
					client = new OkHttpClient ();
					client.SetConnectTimeout (TIMEOUT, Java.Util.Concurrent.TimeUnit.Seconds);
					client.SetReadTimeout (TIMEOUT, Java.Util.Concurrent.TimeUnit.Seconds);
					client.SetWriteTimeout (TIMEOUT, Java.Util.Concurrent.TimeUnit.Seconds);
					client.SetCookieHandler (this.CookieManager);
				}

				return client;
			}
		}

		private CookieManager cookieManager;
		protected CookieManager CookieManager {
			get {
				if (cookieManager == null) {
					cookieManager = new CookieManager ();
					cookieManager.SetCookiePolicy (CookiePolicy.AcceptAll);
				}

				return cookieManager;
			}
		}
		#endregion

		#region basic request

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
			// TODO: We might want to put these requests in its own thread pool.
			EMTask.DispatchBackground (() => {
				try {
					BackgroundSendRequest (address, json, method, contentType, completionHandler, timeoutInSeconds);
				} catch (Exception e) {
					completionHandler (EMHttpResponse.GenericException ());
				}
			});
		}

		private void BackgroundSendRequest (
			string address, 
			string json, 
			HttpMethod method, 
			string contentType, 
			Action<EMHttpResponse> completionHandler,
			int? timeoutInSeconds
		) {
			if (string.IsNullOrEmpty (contentType))
				contentType = string.Empty;

			MediaType mediaType = MediaType.Parse (contentType);

			if (string.IsNullOrEmpty (json))
				json = "";

			RequestBody body = RequestBody.Create (mediaType, json);
			Request request = CreateRequest (address, method, body);
			OkHttpClient httpClient = this.This;

			if (timeoutInSeconds.HasValue) {
				httpClient = this.This.Clone ();
				httpClient.SetReadTimeout ((long)timeoutInSeconds.Value * 1000 /* seconds -> milliseconds */, Java.Util.Concurrent.TimeUnit.Milliseconds);
			}

			Response response = httpClient.NewCall (request).Execute ();
			int statusCode = response.Code ();

			DateTime clientDate = DateTime.Now;
			string serverDateString = response.Header ("Date");
			DateTime serverDate = DateTime.Parse (serverDateString, Preference.usEnglishCulture);

			if (!response.IsSuccessful) {
				HttpResponseHelper.CheckUnauthorized (statusCode);

				completionHandler (EMHttpResponse.FromStatusCode (statusCode, serverDate, clientDate));
			} else {
				string responseString = response.Body ().String ();
				completionHandler (EMHttpResponse.FromString (responseString, serverDate, clientDate));
			}
		}
		#endregion
			
		#region image search
		public void SendImageSearchRequest (string address, string basicAuthKey, Action<EMHttpResponse> callback) {
			EMTask.Dispatch (() => {
				try {
					BackgroundSendImageSearchRequest (address, basicAuthKey, callback);
				} catch (Exception e) {
					callback (EMHttpResponse.GenericException ());
				}
			}, EMTask.DOWNLOAD_QUEUE);
		}

		private void BackgroundSendImageSearchRequest (string address, string basicAuthKey, Action<EMHttpResponse> callback) {
			// The reason this code looks like this is because it's handling two cases.
			// One is the initial request using Bing's API to get a list of search images. 
			// The second is the case where we're actually making a GET request to get the image.
			// In the first case, we want a strign reprsentation, in the second case, as bytes.
			var builder = new Request.Builder ();
			builder.Url (address);

			bool isBingRequest = false;
			if (address.Contains ("datamarket")) {
				builder.AddHeader ("Authorization", basicAuthKey);
				isBingRequest = true;
			}

			Request request = builder.Build ();

			Response response = this.This.NewCall (request).Execute ();
			if (response.IsSuccessful) {
				if (isBingRequest) {
					string responseString = response.Body ().String ();
					callback (EMHttpResponse.FromString (responseString));
				} else {
					byte[] bytes = response.Body ().Bytes ();
					callback (EMHttpResponse.FromBytes (bytes));
				}
			} else {
				callback (EMHttpResponse.GenericException ());
			}
		}
		#endregion

		#region downloads
		public void SendDownloadMediaRequest (Media media) {
			EMTask.Dispatch (() => {
				try {
					BackgroundSendDownloadMediaRequest (media);
				} catch (Exception e) {
					Debug.WriteLine ("SendDownloadMediaRequest: Exception thrown {0}", e);
					media.BackgroundDownloadErrorEvent (EMApplication.Instance.appModel, true);
				}
			}, EMTask.DOWNLOAD_QUEUE);
		}

		public void SendDownloadRequestWithHandler (string address, Action<Stream, long> callback) {
			EMTask.DispatchBackground (() => {
				try {
					BackgroundSendDownloadRequestWithHandler (address, callback);
				} catch (Exception e) {
					Debug.WriteLine ("BackgroundSendDownloadRequestWithHandler: Exception thrown {0}", e);
				}
			});
		}

		private void BackgroundSendDownloadMediaRequest (Media media) {
			string address = media.uri.AbsoluteUri;
			Request request = new Request.Builder ()
				.Url (address)
				.Build ();
			Response response = this.This.NewCall (request).Execute ();
			ApplicationModel applicationModel = EMApplication.Instance.appModel;

			if (!response.IsSuccessful) {
				int code = response.Code ();
				if (code >= 400 && code != 503) {
					HttpResponseHelper.CheckUnauthorized (code);

					media.BackgroundDownloadErrorEvent (applicationModel, false);  // on a 400 or 500 (/w 503 as an exception) level response, we let the media know, it shouldnt retry the download
				} else {
					media.BackgroundDownloadErrorEvent (applicationModel, true);
				}
			} else {
				using (Stream responseStream = response.Body ().ByteStream ()) {
					string length = response.Header ("Content-Length");
					long l = long.Parse (length);
					HandleDownloadStream (responseStream, l, media, applicationModel);
				}
			}
		}

		private void BackgroundSendDownloadRequestWithHandler (string address, Action<Stream, long> callback) {
			if (address == null)
				return;
			Request request = new Request.Builder ()
			.Url (address)
			.Build ();
			Response response = this.This.NewCall (request).Execute ();
			if (response.IsSuccessful) {
				using (Stream responseStream = response.Body ().ByteStream ()) {
					string length = response.Header ("Content-Length");
					long l = long.Parse (length);
					callback (responseStream, l);
				}
			}
		}

		/**
		 * This call handles a stream from a download media call.
		 * It first copies the stream's bytes to a temporary location.
		 * When the download is complete, it does the move operation into the final location.
		 */
		private void HandleDownloadStream (Stream stream, long length, Media media, ApplicationModel applicationModel) {
			media.BackgroundDownloadStartEvent ();

			string finalPath = media.GetPathForUri (applicationModel.platformFactory); 
			string tempPath = applicationModel.uriGenerator.GetFilePathForStagingMediaDownload (finalPath);

			// need to do a cleanup first if the download previously failed due to dirty app termination.
			if (applicationModel.platformFactory.GetFileSystemManager ().FileExistsAtPath (tempPath)) {
				applicationModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (tempPath);
			}
			media.tempPath = tempPath;
			applicationModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (tempPath, stream, (length != null ? (long)length : -1L), delegate(double compPerc) {
				media.BackgroundDownloadProgressEvent (compPerc);
			});

			// check if file download is incomplete
			using (Stream fileStream = applicationModel.platformFactory.GetFileSystemManager ().GetReadOnlyFileStreamFromPath (tempPath)) {
				if (fileStream.Length != length) {
					String errorMessage = String.Format ("file download incomplete {0}/{1}", fileStream.Length, length);
					Debug.WriteLine (errorMessage);
					applicationModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (finalPath);
					media.BackgroundDownloadErrorEvent (applicationModel, true);
					return;
				} 
			}

			applicationModel.platformFactory.GetFileSystemManager ().MoveFileAtPath (tempPath, finalPath);
			System.Diagnostics.Debug.WriteLine ("Finished downloading file at path" + finalPath);
			media.BackgroundDownloadFinishEvent (finalPath, applicationModel);
		}
		#endregion
			
		#region helpers
		private Request CreateRequest (string address, HttpMethod method, RequestBody body) {
			var builder = new Request.Builder ();
			builder.Url (address);

			if (method == HttpMethod.Post) {
				builder.Post (body);
			} else if (method == HttpMethod.Delete) {
				builder.Delete ();
			} else if (method == HttpMethod.Put) {
				builder.Put (body);
			}

			Request request = builder.Build ();
			return request;
		}

		public void PrintCookies (string address) {
			ICookieStore cookieStore = this.CookieManager.CookieStore;
			IList<HttpCookie> cookies = cookieStore.Cookies;
			System.Diagnostics.Debug.WriteLine ("Http: address is " + address);
			foreach (HttpCookie cookie in cookies) {
				System.Diagnostics.Debug.WriteLine ("cookie domain: {0} name: {1} value: {2}", cookie.Domain, cookie.Name, cookie.Value);
			}
		}
		#endregion

		#region uploads
		public void SendUploadMediaRequest (QueueEntry queueEntry, Action failureCallback, Action<EMHttpResponse> callback) {
			EMTask.DispatchBackground (() => {
				try {
					BackgroundSendMediaUploadRequest (queueEntry, callback);
				} catch (Exception e) {
					failureCallback ();
				}
			});
		}

		private void BackgroundSendMediaUploadRequest (QueueEntry queueEntry, Action<EMHttpResponse> callback) {
			queueEntry.DelegateWillStartUpload ();

			ApplicationModel appModel = EMApplication.Instance.appModel;

			string destination = queueEntry.destination;

			string address = AppEnv.UPLOAD_HTTP_BASE_ADDRESS + destination;
			string base64DeviceInformation = appModel.account.deviceInfo.DeviceBase64String ();
			if(address.Contains("?"))
				address = address + "&deviceInformation=" + base64DeviceInformation;
			else
				address = address + "?deviceInformation=" + base64DeviceInformation;

			var builder = new MultipartBuilder ();
			builder.Type (MultipartBuilder.Form);

			// We are figuring out the size of the largest part in the multipart.
			// So we can track the progress of the upload based on that part alone.
			long totalSize = 0; 
			string pathOfBiggestFile = string.Empty;
			var fileDict = new Dictionary<string, Java.IO.File> ();

			foreach (QueueEntryContents contents in queueEntry.contents) {
				string path = appModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (contents.localPath);
				var file = new Java.IO.File (path);
				long lengthInBytes = file.Length ();

				// This if statement is simply checking to see if the current file is larger than the last.
				// We just want to check the progress of the largest file.
				if (lengthInBytes > totalSize) {
					totalSize = lengthInBytes;
					pathOfBiggestFile = path;
				}

				// The file dictionary is used so we don't have to create a new File again when the path comes up when we iterate over the contents again.
				fileDict.Add (path, file);
			}

			// Building up the multipart.
			foreach (QueueEntryContents contents in queueEntry.contents) {
				string path = appModel.platformFactory.GetFileSystemManager ().ResolveSystemPathForUri (contents.localPath);
				string name = contents.name;
				string filename = contents.fileName;
				string mimeType = contents.mimeType;
				Java.IO.File file = fileDict [path];

				Headers contentDisposition = Headers.Of ("Content-Disposition", string.Format ("form-data; name=\"{0}\"; filename=\"{1}\"", name, filename));

				if (path.Equals (pathOfBiggestFile)) {
					// For the multipart that is the largest part, we use a subclassed RequestBody to check upload progress.
					builder.AddPart (contentDisposition, new CountingFileRequestBody (file, mimeType, MediaUploadProgressListener.FromQueueEntry (queueEntry, totalSize)));
				} else {
					builder.AddPart (contentDisposition, RequestBody.Create (MediaType.Parse (mimeType), file));
				}
			}

			RequestBody requestBody = builder.Build ();

			Com.Squareup.Okhttp.Request.Builder requestBuilder = new Request.Builder (); // Note; Do not pull namespaces out. Can conflict with Android's namespace if/when Android's namespace is pulled in.
			requestBuilder.Url (address);
			requestBuilder.Post (requestBody);
			Request request = requestBuilder.Build ();

			try {
				Response response = this.This.NewCall (request).Execute ();
				int statusCode = response.Code ();

				if (response.IsSuccessful) {
					queueEntry.DelegateDidCompleteUpload ();
					string responseString = response.Body ().String ();
					callback (EMHttpResponse.FromString (responseString));
				} else {
					HttpResponseHelper.CheckUnauthorized (statusCode);

					queueEntry.DelegateDidFailUpload ();
					callback (EMHttpResponse.FromStatusCode (statusCode));
				}
			} catch (Java.Net.SocketException e) {
				// Workaround code where some devices throw an exception on 401.
				// As a workaround, we (one time only), throw a 401 instead to trigger the 401 recovery flow.
				// If it truly was an exception (SocketException) that we got, then the next time we'll just go with the normal exception handling by throwing.
				if (this.SocketExceptionUrls.Contains (address)) {
					this.SocketExceptionUrls.Remove (address);
					throw e;
				} else {
					this.SocketExceptionUrls.Add (address);

					int spoofedStatusCode = 401;
					int statusCode = spoofedStatusCode;

					HttpResponseHelper.CheckUnauthorized (statusCode);

					queueEntry.DelegateDidFailUpload ();
					callback (EMHttpResponse.FromStatusCode (statusCode));
				}
			}

		}

		private HashSet<string> _socketExceptionUrls;
		protected HashSet<string> SocketExceptionUrls {
			get { 
				if (this._socketExceptionUrls == null) {
					this._socketExceptionUrls = new HashSet<string> ();
				}

				return this._socketExceptionUrls; 
			}
		}

		/**
	 	 * This class is closely tied with the queue entry object it is assigned.
	 	 * Progress updates are obtained from the Transferred function.
	 	 */
		public class MediaUploadProgressListener : IOkioProgressListener {
			private QueueEntry QueueEntry { get; set; } // queue entry to pump delegate calls back to, mainly upload percentage
			private long MaxSize { get; set; } // the maxSize of the upload, we use this to determine the percentage done

            private double _lastPercentage = 0.0; // keep track of the last percentage 
            private double LastPercentage { get { return this._lastPercentage; } set { this._lastPercentage = value; } }

            private double _uploadPercentageDifference = 0.0;
			private double UploadPercentageDifference { get { return this._uploadPercentageDifference; } set { this._uploadPercentageDifference = value; } }

			private const double MinimumDifference = .01;

			public static MediaUploadProgressListener FromQueueEntry (QueueEntry queueEntry, long maxSize) {
				MediaUploadProgressListener listener = new MediaUploadProgressListener ();
				listener.QueueEntry = queueEntry;
				listener.MaxSize = maxSize;
				return listener;
			}

			public void Transferred (long num) { 
				double percentage = (double)num / (double)this.MaxSize;

				if (ShouldCallDelegate (percentage)) {
					this.QueueEntry.DelegateDidUploadPercentage (percentage);
				}

				this.LastPercentage = percentage;
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
		}

		/**
		 * https://gist.github.com/eduardb/dd2dc530afd37108e1ac
		 * By subclassing RequestBody and overriding WriteTo, we can check the progress of the request.
		 * WriteTo eventually calls the Listener's (subclassed by calling class) Transferred method which can report progress back.
		 */
		public class CountingFileRequestBody : Com.Squareup.Okhttp.RequestBody {
			
			const int SEGMENT_SIZE = 2048; // okio.Segment.SIZE

			Java.IO.File File { get; set; } // the relevant file we're uploading
			IOkioProgressListener Listener { get; set; } // listener where we pump progress updates to
			string Type { get; set; } // contentType / mimeType of the file request

			public CountingFileRequestBody (Java.IO.File file, string contentType, IOkioProgressListener listener) {
				this.File = file;
				this.Listener = listener;
				this.Type = contentType;
			}

			public override long ContentLength () {
				return this.File.Length ();
			}

			public override MediaType ContentType () {
				return MediaType.Parse (this.Type);
			}

			public override void WriteTo (Okio.IBufferedSink sink) {
				Okio.ISource source = null;

				try {
					source = Okio.Okio.Source (this.File);
					long total = 0;
					long read;

					while ((read = source.Read (sink.Buffer (), SEGMENT_SIZE)) != -1) {
						total += read;
						sink.Flush ();
						this.Listener.Transferred (total);
					}
				} finally {
					try {
						source.Close ();
					} catch (Exception e) {
						System.Diagnostics.Debug.WriteLine ("CountingFileRequestBody: Exception throwing closing source {0}", e);
					}
				}
			}
		}
	}

	/**
	 * Subclass this and pass it into a subclassed RequestBody to get progress updates.
	 */
	public interface IOkioProgressListener {
		void Transferred (long num);
	}

	#endregion
}