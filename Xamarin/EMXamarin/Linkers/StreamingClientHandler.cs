using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http {

	// A handler that streams uploads and downloads.
	// Pipeline for POST with multipart data: 
	//     Files to be uploaded on disk are streamed in during the construction of a multipart body. 
	//     The body is streamed out to a temp file on disk during construction progress.
	//     Finally, when the multipart body is finished, the handler has the HttpWebRequest stream the multipart body from disk over http.
	// Currently handles GET and POST requests.
	public class StreamingClientHandler : HttpClientHandler {

		public static readonly string CONNECTION_GROUP_NAME = "StreamingClientHandler";

		const int WEB_REQUEST_TIMEOUT_MILLIS = 100000;

		static readonly string CONTENT_ENCODING_HEADER = "content-encoding";
		static readonly string ACCEPT_ENCODING_HEADER = "accept-encoding";
		static readonly string GZIP_VALUE = "gzip";

		public StreamingClientHandler() {
			this.AutomaticDecompression = DecompressionMethods.GZip; // default to using gzip compression
		}
	
		protected void addHeaders(HttpWebRequest wrequest, HttpHeaders requestHeaders) {
			var headers = wrequest.Headers;
			foreach (var header in requestHeaders) {
				var key = header.Key;
				if (WebHeaderCollection.IsRestricted (key)) {
					var valueTable = new Hashtable ();

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

					if (success)
						Debug.WriteLine ("added restricted header {0}: {1}", key, valuesString);
					else
						Debug.WriteLine ("failed to add restricted header {0}: {1} ", key, valuesString);

				} else { 
					foreach (var value in header.Value)
						headers.Add (header.Key, value);
				}
			}

			if (this.SupportsAutomaticDecompression) {
				headers.Add (ACCEPT_ENCODING_HEADER, GZIP_VALUE);
			}
		}

		bool ResponseIsGzipCompressed (WebHeaderCollection headers) {
			for (int i = 0; i < headers.Count; ++i) {
				var key = headers.GetKey (i);
				var values = headers.GetValues (i);

				if (key.ToLower ().Equals (CONTENT_ENCODING_HEADER)) {
					foreach (string v in values) {
						if (v.ToLower ().Equals (GZIP_VALUE)) {
							return true;
						}
					}
				}
			}

			return false;
		}

		internal virtual HttpWebRequest CreateWebRequest (HttpRequestMessage request) {
			var wr = (HttpWebRequest)HttpWebRequest.Create (request.RequestUri);
			wr.ConnectionGroupName = CONNECTION_GROUP_NAME;
			wr.Method = request.Method.Method;
			wr.ProtocolVersion = request.Version;
			wr.AllowAutoRedirect = true;
			wr.MaximumAutomaticRedirections = 10;
			wr.AllowWriteStreamBuffering = false;
			wr.AllowReadStreamBuffering = false;      // note: only one of the two Allow..Buffering flags need to be set for both to be set, but we have it here for clarity.

			wr.CookieContainer = CookieContainer;
				
			addHeaders (wr, request.Headers);

			return wr;
		}

		HttpResponseMessage CreateResponseMessage (HttpWebResponse wr, HttpRequestMessage requestMessage) {	
			Debug.WriteLine ("Creating Response Message");
			HttpResponseMessage response;

			response = new HttpResponseMessage (wr.StatusCode);
			response.RequestMessage = requestMessage;
			response.ReasonPhrase = wr.StatusDescription;

			var headers = wr.Headers;

			bool isGzipCompressed = ResponseIsGzipCompressed (headers);
			if (isGzipCompressed) {
				response.Content = new StreamContent (new GZipStream (wr.GetResponseStream (), CompressionMode.Decompress));
			} else {
				response.Content = new StreamContent (wr.GetResponseStream ());
			}

			for (int i = 0; i < headers.Count; ++i) {
				var key = headers.GetKey (i);
				var values = headers.GetValues (i);

				response.Headers.TryAddWithoutValidation (key, values);
				response.Content.Headers.TryAddWithoutValidation (key, values);
			}
				
			return response;
		}

		async Task<HttpResponseMessage> sendAsyncAsync (HttpRequestMessage request, CancellationToken cancellationToken) {
			var wrequest = CreateWebRequest (request);

			if (request.Content != null) {
				wrequest.AllowWriteStreamBuffering = false;
				wrequest.ReadWriteTimeout = WEB_REQUEST_TIMEOUT_MILLIS;
				wrequest.Timeout = WEB_REQUEST_TIMEOUT_MILLIS;

				if (wrequest.Method.Equals ("POST")) {
					wrequest.SendChunked = true;
				}

				addHeaders (wrequest, request.Content.Headers);
				
				using (Stream stream = await wrequest.GetRequestStreamAsync ().ConfigureAwait (false)) {
					Debug.WriteLine ("copying request body out to web request stream");
					await request.Content.CopyToAsync (stream).ConfigureAwait (false);
				}
			}

			HttpWebResponse wresponse = null;

			using (cancellationToken.Register (l => ((HttpWebRequest) l).Abort (), wrequest)) {
				try {
					if (request.Content == null && !wrequest.Method.Equals ("GET")) {
						Debug.WriteLine ("starting GET request");
						wrequest.ContentLength = 0;
						await wrequest.GetRequestStreamAsync ();
					}
					Debug.WriteLine ("GET'ing response with AllowReadStreamBuffering: " + wrequest.AllowReadStreamBuffering);
					wresponse = (HttpWebResponse)await wrequest.GetResponseAsync ().ConfigureAwait (false);
				}
				catch (WebException we) {
					if (we.Status != WebExceptionStatus.RequestCanceled) {
						Debug.WriteLine (we.Message + " while requesting (" + request.RequestUri.AbsoluteUri + ").");
						if (we.Response == null) {
							throw;
						}

						wresponse = (HttpWebResponse)we.Response;
					}
				}

				if (cancellationToken.IsCancellationRequested) {
					var cancelled = new TaskCompletionSource<HttpResponseMessage> ();
					cancelled.SetCanceled ();
					return await cancelled.Task;
				}
			}

			return CreateResponseMessage (wresponse, request);
		}

		protected override Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken) {
			return sendAsyncAsync (request, cancellationToken);
		}
	}
}
