using System;
using System.Diagnostics;
using System.IO;

namespace em {
	public class EMHttpResponse {
		EMHttpStatusCode emStatusCode;

		public string ResponseAsString { get; set; }
		public Stream ResponseAsStream { get; set; }
		public byte[] ResponseAsBytes { get; set; }
		public long? ContentLength { get; set; }

		public DateTime ServerTime { get; set; }
		public DateTime ClientTime { get; set; }

		public EMHttpStatusCode EMHttpStatusCode {
			get { return emStatusCode; }
		}

		EMHttpResponse (EMHttpStatusCode s) {
			emStatusCode = s;
		}

		public EMHttpResponse () {
			emStatusCode = EMHttpStatusCode.OrdinaryResponse;
		}

		public static EMHttpResponse GenericException () {
			var r = new EMHttpResponse (EMHttpStatusCode.GenericException);
			return r;
		}

		public static EMHttpResponse RetryableException () {
			var r = new EMHttpResponse (EMHttpStatusCode.RetryableException);
			return r;
		}

		public static EMHttpResponse GenericException (DateTime server, DateTime client) {
			var r = new EMHttpResponse (EMHttpStatusCode.GenericException);
			r.ServerTime = server;
			r.ClientTime = client;
			return r;
		}

		public static EMHttpResponse FromStatusCode (int statusCode) {
			EMHttpResponse r = new EMHttpResponse(EMHttpStatusCodeHelper.ToEMHttpStatusCode (statusCode));

			return r;
		}
		
		public static EMHttpResponse FromStatusCode (int statusCode, DateTime server, DateTime client) {
			EMHttpResponse r = FromStatusCode (statusCode);
			r.ServerTime = server;
			r.ClientTime = client;

			return r;
		}


		public static EMHttpResponse FromString (string re) {
			var r = new EMHttpResponse ();
			r.ResponseAsString = re;
			return r;
		}

		public static EMHttpResponse FromString (string re, DateTime server, DateTime client) {
			var r = new EMHttpResponse ();
			r.ResponseAsString = re;
			r.ServerTime = server;
			r.ClientTime = client;
			return r;
		}

		public static EMHttpResponse FromStream (Stream s, long? length) {
			var r = new EMHttpResponse ();
			r.ContentLength = length;
			r.ResponseAsStream = s;
			return r;
		}

		public static EMHttpResponse FromBytes (byte[] bytes) {
			var r = new EMHttpResponse ();
			r.ResponseAsBytes = bytes;
			return r;
		}

		public bool NameResolutionFailure {
			get { return emStatusCode == EMHttpStatusCode.NameResolutionFailure; }
		}

		public bool OrdinaryResponse {
			get { return emStatusCode == EMHttpStatusCode.OrdinaryResponse; }
		}

		public bool RetryableExeception {
			get { return emStatusCode == EMHttpStatusCode.RetryableException; }
		}

		public bool IsSuccess {
			get { return this.OrdinaryResponse; }
		}

		public bool IsRetryable {
			get { return this.RetryableExeception; }
		}
	}
}