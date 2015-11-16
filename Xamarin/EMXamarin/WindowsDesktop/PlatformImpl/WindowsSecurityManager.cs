using em;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.PlatformImpl {
	class WindowsSecurityManager : ISecurityManager {

		private static WindowsSecurityManager _shared;
		public static WindowsSecurityManager Shared {
			get {
				if (_shared == null) {
					_shared = new WindowsSecurityManager ();
				}

				return _shared;
			}
		}

		private string SystemPathForName (string name) {
			string path = Environment.CurrentDirectory;
			string secPath = Path.Combine (path, name);
			return secPath;
		}

		private Dictionary<string, string> DictionaryFromFilePath (string systemPath) {
			if (File.Exists (systemPath)) {
				string[] lines = File.ReadAllLines (systemPath);
				Dictionary<string, string> dict = lines.Select (l => l.Split ('=')).ToDictionary (a => a[0], a => a[1]);
				return dict;
			} else {
				Dictionary<string, string> t = new Dictionary<string, string> ();
				WriteDictionaryToFilePath (t, systemPath);
				return t;
			}
		}

		private void WriteDictionaryToFilePath (Dictionary<string, string> dict, string systemPath) {
			string[] lines = dict.Select (kvp => kvp.Key + "=" + kvp.Value).ToArray ();
			if (File.Exists (systemPath)) {
				File.Delete (systemPath);
			}
			File.WriteAllLines (systemPath, lines);
		}

		public void storeSecureField (string fieldName, string fieldValue) {
			string path = SystemPathForName (fieldName);
			Dictionary<string, string> dict = DictionaryFromFilePath (path);

			if (dict.ContainsKey (fieldName)) {
				dict[fieldName] = fieldValue;
			} else {
				dict.Add (fieldName, fieldValue);
			}

			WriteDictionaryToFilePath (dict, path);
		}

		public string retrieveSecureField (string fieldName) {
			string path = SystemPathForName (fieldName);
			Dictionary<string, string> dict = DictionaryFromFilePath (path);
			if (dict.ContainsKey (fieldName)) {
				return dict[fieldName];
			}

			return null;
		}

		public bool removeSecureField (string fieldName) {
			string path = SystemPathForName (fieldName);
			Dictionary<string, string> dict = DictionaryFromFilePath (path);
			if (dict.ContainsKey (fieldName)) {
				dict.Remove (fieldName);
				WriteDictionaryToFilePath (dict, path);
				return true;
			}

			return false;
		}

		public void SaveSecureKeyValue (string key, string value) {
			Dictionary<string, string> dict = GetSecureDictionary ();
			if (dict.ContainsKey (key)) {
				dict[key] = value;
			} else {
				dict.Add (key, value);
			}

			SaveSecureDictionary (dict);
		}

		public void SaveSecureKeyValue (Dictionary<string, string> keyValuePairs) {
			if (keyValuePairs == null || keyValuePairs.Count < 1) {
				Debug.WriteLine ("Trying to store null or empty dictionary!");
				return;
			}

			Dictionary<string, string> dict = GetSecureDictionary ();
			var dictEnumerator = keyValuePairs.GetEnumerator ();
			while (dictEnumerator.MoveNext ()) {
				string key = dictEnumerator.Current.Key;
				string value = dictEnumerator.Current.Value;
				if (dict.ContainsKey (key)) {
					dict[key] = value;
				} else {
					dict.Add (key, value);
				}
			}

			SaveSecureDictionary (dict);
		}

		public string GetSecureKeyValue (string key) {
			if (key == null) return null;

			Dictionary<string, string> dict = GetSecureDictionary ();
			if (dict.ContainsKey (key)) {
				return dict[key];
			}

			return null;
		}

		public void RemoveSecureKeyValue (string key) {
			Dictionary<string, string> dict = GetSecureDictionary ();
			if (dict.ContainsKey (key)) {
				dict.Remove (key);
				SaveSecureDictionary (dict);
			}
		}

		private Dictionary<string, string> GetSecureDictionary () {
			string path = SystemPathForName ("BLAHBLAHBLAH");
			if (File.Exists (path)) {
				Dictionary<string, string> dict = DictionaryFromFilePath (path);
				return dict;
			}

			Dictionary<string, string> newDict = new Dictionary<string, string> ();
			WriteDictionaryToFilePath (newDict, path);
			return newDict;
		}

		private void SaveSecureDictionary (Dictionary<string, string> dict) {
			string path = SystemPathForName ("BLAHBLAHBLAH");
			WriteDictionaryToFilePath (dict, path);
		}

		public string CalculateMD5Hash (string input) {
			// step 1, calculate MD5 hash from input
			MD5 md5 = MD5.Create ();
			byte[] inputBytes = Encoding.UTF8.GetBytes (input);
			byte[] hash = md5.ComputeHash (inputBytes);

			// step 2, convert byte array to hex string
			var sb = new StringBuilder ();
			for (int i = 0; i < hash.Length; i++)
				sb.Append (hash[i].ToString ("X2"));

			return sb.ToString ();
		}


		public byte[] MD5StreamToBytes (Stream stream) {
			throw new NotImplementedException ();
		}
	}
}
