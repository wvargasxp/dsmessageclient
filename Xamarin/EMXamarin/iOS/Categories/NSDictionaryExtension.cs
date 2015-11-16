using System;
using System.Collections.Generic;
using Foundation;

namespace iOS {
	public static class NSDictionaryExtension {
		public static Dictionary<string, string> StringDictionary (this NSDictionary dictionary) {
			Dictionary<string, string> ret = new Dictionary<string, string> ();
			foreach (NSString key in dictionary.Keys) {
				ret.Add ((string)key, (string)(NSString)dictionary [key]);
			}

			return ret;
		}
	}
}

