using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PCLWebUtility;

namespace em {
	public class EMHttpClient {

		const long HTTP_REQUEST_TIMEOUT_MILLIS = 5 * 60000;

		readonly DateTime UNIX_START_DATETIME = new DateTime (1970, 1, 1, 0, 0, 0);

		IHttpInterface httpClient;
		IHttpInterface imageSearchClient;

		public CookieContainer CookieContainer = new CookieContainer ();

		ApplicationModel appModel;

		public EMHttpClient (ApplicationModel applicationModel) {
			appModel = applicationModel;
			appModel.platformFactory.OpenServicePoint ();
			httpClient = appModel.platformFactory.GetNativeHttpClient ();
			imageSearchClient = appModel.platformFactory.GetNativeHttpClient ();
		}

		public void SendRequestAsync (string absoluteResourceAddress, object jsonObject, HttpMethod method, string contentType, Action<EMHttpResponse> completionHandler, int? timeoutInSeconds = null) {
			SendRequestAsync (absoluteResourceAddress, true, jsonObject, method, contentType, completionHandler, timeoutInSeconds);
		}
			
		public void SendRequestAsync (string absoluteResourceAddress, bool appendDeviceInfo, object jsonObject, HttpMethod method, string contentType, Action<EMHttpResponse> completionHandler, int? timeoutInSeconds = null) {
			SendRequest (absoluteResourceAddress, appendDeviceInfo, jsonObject, method, contentType, completionHandler, timeoutInSeconds);
		}

		private void SendRequest (string absoluteResourceAddress, bool appendDeviceInfo, object jsonObject, HttpMethod method, string contentType, Action<EMHttpResponse> completionHandler, int? timeoutInSeconds = null) {
			string address = FinalResourceAddress (absoluteResourceAddress, appendDeviceInfo);
			string json = JsonStringFromJObject (jsonObject);
			httpClient.SendRequestAsync (address, json, method, contentType, completionHandler, timeoutInSeconds);
		}
			
		public void SendMediaRequestAsync (Media media) {
			httpClient.SendDownloadMediaRequest (media);
		}

		public void SendRequestAsAsyncWithCallback (string url, Action<Stream, long> callback) {
			httpClient.SendDownloadRequestWithHandler (url, callback);
		}

		public void SendUploadMediaRequestAsync (QueueEntry queueEntry, Action failureCallback, Action<EMHttpResponse> callback) {
			httpClient.SendUploadMediaRequest (queueEntry, failureCallback, callback);
		}
			
		public void SendApiRequestAsync (string resourceAddress, object jsonObject, HttpMethod method, string contentType, Action<EMHttpResponse> completionHandler, int? timeoutInSeconds = null) {
			string address = AppEnv.HTTP_BASE_ADDRESS + "/" + resourceAddress;
			SendRequestAsync (address, jsonObject, method, contentType, completionHandler, timeoutInSeconds);
		}

		public void DoLoginAsync(string username, string password, DateTime messagesSince, bool getMissedMessages, Action<EMHttpResponse> completionHandler) {
			EMTask.Dispatch (() => DoLogin (username, password, messagesSince, getMissedMessages, completionHandler), EMTask.LOGIN_QUEUE);
		}

		void DoLogin(string username, string password, DateTime messagesSince, bool getMissedMessages, Action<EMHttpResponse> completionHandler) {
			if (appModel.platformFactory.OnMainThread )
				throw new Exception ("Don't call network code from the the main thread! ");

			username = WebUtility.UrlEncode (username);
			password = WebUtility.UrlEncode (password);
			long messageSinceUnix = UnixTimeMillis (messagesSince);

			string deviceInformationJson = appModel.account.deviceInfo.DeviceJSONString ();
			string base64DeviceInformation = Convert.ToBase64String (Encoding.UTF8.GetBytes (deviceInformationJson));
			string resourceAddress = String.Format ("{0}/login.html?username={1}&password={2}&deviceInformation={3}&messagesSince={4}&getMissedMessages={5}", AppEnv.HTTP_BASE_ADDRESS, username, password, base64DeviceInformation, messageSinceUnix, getMissedMessages);

			httpClient.SendRequestAsync (resourceAddress, null, HttpMethod.Post, "application/json", completionHandler);
		}

		public void ImageSearchRequest (string address, Action<EMHttpResponse> completionHandler) {
			string accountKey = BingSearch.API_KEY;
			string key = "Basic " + Convert.ToBase64String (Encoding.UTF8.GetBytes (accountKey + ":" + accountKey));
			imageSearchClient.SendImageSearchRequest (address, key, completionHandler);
		}

		#region helpers
		static string JsonStringFromJObject (object jsonObject) {
			string json = null;
			if (jsonObject != null) {
				if (jsonObject is JObject)
					json = jsonObject.ToString ();
				else if (jsonObject is byte[]) {
					var bytes = jsonObject as byte[];
					json = Encoding.UTF8.GetString (bytes, 0, bytes.Length);
					JsonConvert.DeserializeObject<Dictionary<string, object>> (json);
				} else {
					json = JsonConvert.SerializeObject (jsonObject);
					JsonConvert.SerializeObject (jsonObject);
				}
			}

			return json;
		}

		string FinalResourceAddress (string absoluteResourceAddress, bool appendDeviceInfo) { 
			string address = null;
			if (!appendDeviceInfo)
				address = absoluteResourceAddress;
			else {
				string deviceInformationJson = appModel.account.deviceInfo.DeviceJSONString ();
				string base64DeviceInformation = Convert.ToBase64String (Encoding.UTF8.GetBytes (deviceInformationJson));
				if(absoluteResourceAddress.Contains("?"))
					address = absoluteResourceAddress + "&deviceInformation=" + base64DeviceInformation;
				else
					address = absoluteResourceAddress + "?deviceInformation=" + base64DeviceInformation;
			}

			return address;
		}

		long UnixTimeMillis(DateTime timestamp) {
			var timeSpan = (timestamp - UNIX_START_DATETIME);
			var millis = (long)timeSpan.TotalSeconds * 1000L;

			return millis > 0 ? millis : 0;
		}
		#endregion
	}
}