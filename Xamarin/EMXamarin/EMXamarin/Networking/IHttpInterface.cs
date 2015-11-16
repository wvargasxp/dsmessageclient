using System;
using System.Net.Http;
using System.IO;

namespace em {
	public interface IHttpInterface {
		void SendRequestAsync (
			string address, 
			string json, 
			HttpMethod method, 
			string contentType, 
			Action<EMHttpResponse> completionHandler
		);

		void SendRequestAsync (
			string address, 
			string json, 
			HttpMethod method, 
			string contentType, 
			Action<EMHttpResponse> completionHandler,
			int? timeoutInSeconds
		);

		void SendDownloadMediaRequest (Media media);
		void SendUploadMediaRequest (QueueEntry queueEntry, Action failureCallback, Action<EMHttpResponse> callback);
		void SendImageSearchRequest (string address, string basicAuthKey, Action<EMHttpResponse> callback);
		void SendDownloadRequestWithHandler (string address, Action<Stream, long> callback);
	}
}

