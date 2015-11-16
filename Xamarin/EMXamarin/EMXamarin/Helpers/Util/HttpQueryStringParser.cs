using System;
using System.Collections.Generic;

namespace em {
	public class HttpQueryStringParser {

		public Dictionary<string, string> Parse(string query) {
			var parameterMap = new Dictionary<String, String> ();

			string queryString = SanitizeQuery (query);

			string[] parameters = queryString.Split ('&');

			if (parameters != null && parameters.Length > 0) {
				for (int i = 0; i < parameters.Length; i++) {
					string p = parameters [i];
					string[] kv = p.Split ('=');
					if (kv.Length != 2) {
						System.Diagnostics.Debug.WriteLine ("Misformed query string! Split key-value length isn't 2. QueryString: " + query + " Parameters: " + parameters + " KV Length: " + kv.Length);
						return parameterMap;
					}

					string k = UrlDecode (kv [0]);
					string v = UrlDecode (kv [1]);

					parameterMap.Add (k, v);
				}
			}

			return parameterMap;
		}

		static string SanitizeQuery(string query) {
			if (query.StartsWith ("?")) {
				return query.Substring (1);
			}

			return query;
		}

		/// <summary>
		/// UrlDecodes a string without requiring System.Web
		/// </summary>
		/// <param name="text">String to decode.</param>
		/// <returns>decoded string</returns>
		public static string UrlDecode(string text) {
			// pre-process for + sign space formatting since System.Uri doesn't handle it
			// plus literals are encoded as %2b normally so this should be safe
			text = text.Replace("+", " ");
			return Uri.UnescapeDataString(text);
		}
	}
}