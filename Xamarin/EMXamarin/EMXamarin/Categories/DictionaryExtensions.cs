using System;
using System.Collections.Generic;

// An Extension class similar to Categories in objc to extend a Dictionary.
namespace em {
	public static class DictionaryExtensions {
		public static bool IsEmpty<TKey, TValue> (this Dictionary<TKey, TValue> dictionary) {
			if (dictionary.Count == 0) {
				return true;
			} else {
				return false;
			}
		}

		public static bool NotEmpty<TKey, TValue> (this Dictionary<TKey, TValue> dictionary) {
			if (dictionary.Count > 0) {
				return true;
			} else {
				return false;
			}
		}
	}
}

