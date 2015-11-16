using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Threading;

using em;

namespace System.Net.Http {

	// A handler that streams uploads and downloads.
	// Pipeline for POST with multipart data: 
	//     Files to be uploaded on disk are streamed in during the construction of a multipart body. 
	//     The body is streamed out to a temp file on disk during construction progress.
	//     Finally, when the multipart body is finished, the handler has the HttpWebRequest stream the multipart body from disk over http.
	// Currently handles GET and POST requests.
	public class TestStreamingClientHandler : HttpClientHandler {

		private static string TEMP_FILE_SUFFIX = ".TEMP";

		private PlatformFactory platformFactory;

		public TestStreamingClientHandler (PlatformFactory platformFactory) {
			this.platformFactory = platformFactory;
		}

		protected void addHeaders(HttpWebRequest wrequest, HttpHeaders requestHeaders) {
			var headers = wrequest.Headers;
			foreach (var header in requestHeaders) {
				var key = header.Key;
				if (WebHeaderCollection.IsRestricted (key)) {
					Hashtable valueTable = new Hashtable ();

					string valuesString = "";
					bool success = true;

					foreach (var value in header.Value) {
						if (!valueTable.Contains (key)) {
							valueTable.Add (key, value);
							valuesString += value + ";";
						}
					}

					switch (key) {
					case "Accept":
						wrequest.Accept = valuesString;
						break;
					case "Content-Type":
						wrequest.ContentType = valuesString;
						break;
					default:
						success = false;
						break;
					}

					if (success) {
						Debug.WriteLine ("added restricted header {0}: {1}", key, valuesString);
					} else {
						Debug.WriteLine ("failed to add restricted header {0}: {1} ", key, valuesString);
					}

				} else { 
					foreach (var value in header.Value) {
						headers.Add (header.Key, value);
					}
				}
			}

		}

		internal virtual HttpWebRequest CreateWebRequest (HttpRequestMessage request) {
			var wr = (HttpWebRequest)HttpWebRequest.Create (request.RequestUri);
			wr.ConnectionGroupName = "HttpEMHandler";
			wr.Method = request.Method.Method;
			wr.ProtocolVersion = request.Version;
			wr.AllowAutoRedirect = true;
			wr.MaximumAutomaticRedirections = 10;
			wr.AllowWriteStreamBuffering = false;
			wr.AllowReadStreamBuffering = false;      // note: only one of the two Allow..Buffering flags need to be set for both to be set, but we have it here for clarity.

			wr.CookieContainer = this.CookieContainer;

			addHeaders (wr, request.Headers);

			return wr;
		}

		HttpResponseMessage CreateResponseMessage (HttpWebResponse wr, HttpRequestMessage requestMessage) {	
			Debug.WriteLine ("Creating Response Message");
			HttpResponseMessage response = null;

			response = new HttpResponseMessage (wr.StatusCode);
			response.RequestMessage = requestMessage;
			response.ReasonPhrase = wr.StatusDescription;
			response.Content = new StreamContent (wr.GetResponseStream ());

			var headers = wr.Headers;
			for (int i = 0; i < headers.Count; ++i) {
				var key = headers.GetKey (i);
				var values = headers.GetValues (i);

				response.Headers.TryAddWithoutValidation (key, values);
				response.Content.Headers.TryAddWithoutValidation (key, values);
			}

			return response;
		}

		private async Task<HttpResponseMessage> sendAsyncAsync (HttpRequestMessage request) {
			var wrequest = CreateWebRequest (request);

			if (request.Content != null) {
				wrequest.AllowWriteStreamBuffering = false;
				wrequest.ReadWriteTimeout = 1000000;
				wrequest.Timeout = 1000000;

				addHeaders (wrequest, request.Content.Headers);

				// Save multipart being constructed to temp file before streaming over http
				// TODO might be able to skip this step if we chunk the request.
				string filePath = platformFactory.GetNewMediaFileNameForStagingContents () + TEMP_FILE_SUFFIX;
				FileStream fileStream = File.Create (filePath);
				Debug.WriteLine ("copying request body to temp file: " + filePath);

				await request.Content.CopyToAsync (fileStream);
				fileStream.Close ();

				Debug.WriteLine ("getting request stream from temp file: " + filePath);
				FileStream readStream = File.OpenRead (filePath);
				Debug.WriteLine ("setting Content-length: " + readStream.Length);
				wrequest.ContentLength = readStream.Length;
				Stream stream = await wrequest.GetRequestStreamAsync ().ConfigureAwait (false);

				Debug.WriteLine ("copying request body out to web request stream");
				await readStream.CopyToAsync (stream).ConfigureAwait (false);

				// clean up old temp files
				var stagingPath = Directory.GetParent (filePath).FullName;
				foreach (string f in Directory.EnumerateFiles (stagingPath, "*" + TEMP_FILE_SUFFIX)) {
					File.Delete (f);
					Debug.WriteLine ("deleted temp file: ", f);
				}
			}

			HttpWebResponse wresponse = null;

			try {
				if (request.Content == null && !wrequest.Method.Equals ("GET")) {
					Debug.WriteLine ("starting GET request");
					wrequest.ContentLength = 0;
					await wrequest.GetRequestStreamAsync ();
				}
				Debug.WriteLine ("GET'ing response with AllowReadStreamBuffering: " + wrequest.AllowReadStreamBuffering);
				wresponse = (HttpWebResponse)await wrequest.GetResponseAsync ().ConfigureAwait (false);
			} catch (WebException we) {
				if (we.Status != WebExceptionStatus.RequestCanceled) {
					Debug.WriteLine ("An exception was caught while getting the http reponse (" + request.RequestUri.AbsoluteUri + "). " + we.Message + ":" + we.StackTrace);
					if (we.Response == null) {
						throw;
					}

					wresponse = (HttpWebResponse)we.Response;
				}
			}

			return CreateResponseMessage (wresponse, request);
		}

		protected override Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken) {
			return sendAsyncAsync (request);
		}
	}
}
