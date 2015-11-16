using System;
using System.Collections.Generic;
using System.Text;
using em;
using EMXamarin;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace WindowsDesktop.Networking {
    class WindowsHttpClient : IHttpInterface {
		private HttpClient _client = null;
		private HttpClient Client {
			get {
				if (this._client == null) {
					this.Handler.UseCookies = true;
					this.Handler.CookieContainer = this.CookieContainer;
					this.Handler.AllowAutoRedirect = true;
					this._client = new HttpClient (this.Handler);
					this._client.Timeout = TimeSpan.FromMilliseconds (60000); // todo
				}

				return this._client;
			}
		}

		private HttpClient _streamingClient = null;
		private HttpClient StreamingClient {
			get {
				if (this._streamingClient == null) {
					this.StreamingHandler.UseCookies = true;
					this.StreamingHandler.CookieContainer = this.CookieContainer;
					this.StreamingHandler.AllowAutoRedirect = true;
					this._streamingClient = new HttpClient (this.StreamingHandler);
					this._streamingClient.Timeout = TimeSpan.FromMilliseconds (60000); // todo
				}

				return this._streamingClient;
			}
		}

		private HttpClientHandler _handler = null;
		private HttpClientHandler Handler {
			get {
				if (this._handler == null) {
					this._handler = new HttpClientHandler ();
				}

				return this._handler;
			}
		}

		private HttpClientHandler _streamingHandler = null;
		private HttpClientHandler StreamingHandler {
			get {
				if (this._streamingHandler == null) {
					this._streamingHandler = new StreamingClientHandler ();
				}

				return this._streamingHandler;
			}
		}

		private CookieContainer _cookieContainer = null;
		private CookieContainer CookieContainer {
			get {
				if (this._cookieContainer == null) {
					this._cookieContainer = new CookieContainer ();
				}

				return this._cookieContainer;
			}
		}

		#region basic
		private async void BackgroundSendRequest (string address, string json, HttpMethod method, string contentType, Action<EMHttpResponse> completionHandler, int? timeoutInSeconds /* unused */) {
			HttpRequestMessage request = new HttpRequestMessage (method, address);
			if (!string.IsNullOrWhiteSpace (json)) {
				request.Content = new StringContent (json, Encoding.UTF8, contentType);
				this.Client.DefaultRequestHeaders.Accept.Add (new MediaTypeWithQualityHeaderValue (contentType));
			} else {
				request.Content = null;
			}

			HttpResponseMessage response = await this.Client.SendAsync (request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait (false);
			if (response.IsSuccessStatusCode) {
				string g = await response.Content.ReadAsStringAsync ();
				completionHandler (EMHttpResponse.FromString (g));
			} else {
				completionHandler (EMHttpResponse.GenericException ());
			}
		}
		#endregion

		#region downloads
		private async void BackgroundSendMediaRequest (Media media) {
			string address = media.uri.AbsoluteUri;
			HttpMethod method = HttpMethod.Get;
			HttpRequestMessage request = new HttpRequestMessage (method, address);
			HttpResponseMessage response = await this.Client.SendAsync (request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait (false);

			if (!response.IsSuccessStatusCode) {
				// todo handle error
			} else {
				HttpContentHeaders httpHeaders = response.Content.Headers;
				long? length = httpHeaders.ContentLength;
				using (Stream g = await response.Content.ReadAsStreamAsync ()) {
					HandleDownloadStream (g, length, media);
				}
			}
		}

		private void HandleDownloadStream (Stream stream, long? length, Media media) {
			// Note: This code is almost a 1:1 copy of how Android handles the stream.
			ApplicationModel applicationModel = App.Instance.Model;
			media.BackgroundDownloadStartEvent ();
			IFileSystemManager fileManager = applicationModel.platformFactory.GetFileSystemManager ();

			string finalPath = media.GetPathForUri (applicationModel.platformFactory);
			string tempPath = applicationModel.uriGenerator.GetFilePathForStagingMediaDownload (finalPath);

			// need to do a cleanup first if the download previously failed due to dirty app termination.
			if (fileManager.FileExistsAtPath (tempPath)) {
				fileManager.RemoveFileAtPath (tempPath);
			}

			fileManager.CopyBytesToPath (tempPath, stream, (length != null ? (long)length : -1L), delegate(double compPerc) {
				media.BackgroundDownloadProgressEvent (compPerc);
			});

			// check if file download is incomplete
			using (Stream fileStream = applicationModel.platformFactory.GetFileSystemManager ().GetReadOnlyFileStreamFromPath (tempPath)) {
				if (fileStream.Length != length) {
					String errorMessage = String.Format ("file download incomplete {0}/{1}", fileStream.Length, length);
					Debug.WriteLine (errorMessage);
					fileManager.RemoveFileAtPath (finalPath);
					media.BackgroundDownloadErrorEvent (applicationModel, true);
					return;
				} 
			}

			fileManager.MoveFileAtPath (tempPath, finalPath);
			media.BackgroundDownloadFinishEvent (finalPath, applicationModel);
		}
		#endregion

		#region uploads
		private async void BackgroundUploadMediaRequest (QueueEntry queueEntry, Action failureCallback, Action<EMHttpResponse> callback) {
			ApplicationModel appModel = App.Instance.Model;

			string destination = queueEntry.destination;

			string address = AppEnv.UPLOAD_HTTP_BASE_ADDRESS + destination;
			string deviceInformationJson = appModel.account.deviceInfo.DeviceJSONString ();
			string base64DeviceInformation = Convert.ToBase64String (Encoding.UTF8.GetBytes (deviceInformationJson));
			if(address.Contains("?"))
				address = address + "&deviceInformation=" + base64DeviceInformation;
			else
				address = address + "?deviceInformation=" + base64DeviceInformation;

			HttpRequestMessage request = new HttpRequestMessage (HttpMethod.Post, address);
			var multiPartContent = new MultipartFormDataContent ("----Abs23AawqrrqTbbSWpppo8--");

			IList<QueueEntryContents> contents = queueEntry.contents;
			IFileSystemManager fileManager = appModel.platformFactory.GetFileSystemManager ();
			ProgressStream largestStream = null;

			try {
				for (int i = 0; i < contents.Count; i++) {
					QueueEntryContents content = contents [i];
					Stream stream = fileManager.GetReadOnlyFileStreamFromPath (content.localPath);
					ProgressStream progressStream = new ProgressStream (stream, content.fileName);

					if (largestStream == null) {
						largestStream = progressStream;
					} else if (largestStream.Length < progressStream.Length) {
						largestStream = progressStream;
					}

					StreamContent streamContent = new StreamContent (progressStream);
					streamContent.Headers.Add ("Content-Type", content.mimeType);
					streamContent.Headers.Add ("Content-Length", stream.Length.ToString ());

					HttpContent httpContent = streamContent;
					string name = content.name;
					string fileName = content.fileName;
					multiPartContent.Add (httpContent, name, fileName);
				}
			} catch (Exception e) {
				Debug.WriteLine ("{0}", e);
				failureCallback ();
				return;
			}

			if (largestStream != null) {
				largestStream.CompletionCallback = delegate (double compPerc) {
					queueEntry.DelegateDidUploadPercentage (compPerc);
				};
			}

			request.Content = multiPartContent;

			queueEntry.DelegateWillStartUpload ();

			HttpResponseMessage response = await this.StreamingClient.SendAsync (request);

			if (response.IsSuccessStatusCode) {
				queueEntry.DelegateDidCompleteUpload ();
				string g = await response.Content.ReadAsStringAsync ();
				callback (EMHttpResponse.FromString (g));
			} else {
				queueEntry.DelegateDidFailUpload ();
				callback (EMHttpResponse.GenericException ());
			}
		}
		#endregion

		public void SendRequestAsync (string address, string json, HttpMethod method, string contentType, Action<EMHttpResponse> completionHandler) {
			this.SendRequestAsync (address, json, method, contentType, completionHandler, null);
		}

		public void SendRequestAsync (string address, string json, HttpMethod method, string contentType, Action<EMHttpResponse> completionHandler, int? timeoutInSeconds) {
			EMTask.Dispatch (() => {
				this.BackgroundSendRequest (address, json, method, contentType, completionHandler, timeoutInSeconds);
			});
		}

		public void SendDownloadMediaRequest (Media media) {
			EMTask.Dispatch (() => {
				this.BackgroundSendMediaRequest (media);
			}, EMTask.DOWNLOAD_QUEUE);
		}

		public void SendUploadMediaRequest (QueueEntry queueEntry, Action failureCallback, Action<EMHttpResponse> callback) {
			EMTask.Dispatch (() => {
				this.BackgroundUploadMediaRequest (queueEntry, failureCallback, callback);	
			}, EMTask.HTTP_UPLOAD_QUEUE);
		}

		void IHttpInterface.SendImageSearchRequest (string address, string basicAuthKey, Action<EMHttpResponse> callback) {
			throw new NotImplementedException ();
		}

		void IHttpInterface.SendDownloadRequestWithHandler (string address, Action<Stream, long> callback) {
			throw new NotImplementedException ();
		}
	}
}
