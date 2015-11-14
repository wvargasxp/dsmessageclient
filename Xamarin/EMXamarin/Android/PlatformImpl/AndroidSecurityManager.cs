using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using em;
using Javax.Crypto;
using Javax.Crypto.Spec;

namespace Emdroid.PlatformImpl {
	class AndroidSecurityManager : ISecurityManager {
		static object static_mutex = new object ();

		static AndroidSecurityManager _shared = null;
		public static AndroidSecurityManager Shared {
			get {
				if (_shared == null) {
					_shared = new AndroidSecurityManager ();
				}

				return _shared;
			}
		}

		IFileSystemManager _fileSystemManger = null;
		IFileSystemManager FileSystemManger {
			get {
				if (this._fileSystemManger == null) {
					this._fileSystemManger = ApplicationModel.SharedPlatform.GetFileSystemManager ();
				}

				return this._fileSystemManger;
			}
		}

		Dictionary<string, string> cache;

		public void storeSecureField(String fieldName, String fieldValue) {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			string secPath = Path.Combine (path, fieldName);
			this.FileSystemManger.CreateParentDirectories (secPath);

			using (var w = new StreamWriter(secPath)) {
				w.WriteLine(fieldValue);
			}
		}

		public string retrieveSecureField(String fieldName) {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			string secPath = Path.Combine (path, fieldName);
			if (File.Exists (secPath)) {
				using (var r = new StreamReader(secPath)) {
					string s = r.ReadLine();
					return s;
				}
			}

			return null;
		}

		public bool removeSecureField (String fieldName) {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			string secPath = Path.Combine (path, fieldName);
			if (File.Exists (secPath)) {
				File.Delete (secPath);

				return true;
			}

			return false;
		}

		public void SaveSecureKeyValue (string key, string value) {
			if (key == null || value == null) {
				Debug.WriteLine ("Trying to store null key or value! key: " + key + " value: " + value);
				return;
			}

			var dictionary = GetSecureDictionary ();

			dictionary[key] = value;
			cache [key] = value;

			SaveSecureDictionary (dictionary);
		}

		public void SaveSecureKeyValue (Dictionary<string, string> keyValuePairs) {
			if (keyValuePairs == null || keyValuePairs.Count < 1) {
				Debug.WriteLine ("Trying to store null or empty dictionary!");
				return;
			}

			var dictionary = GetSecureDictionary ();

			var dictEnum = keyValuePairs.GetEnumerator ();
			while (dictEnum.MoveNext ()) {
				dictionary[dictEnum.Current.Key] = dictEnum.Current.Value;
				cache [dictEnum.Current.Key] = dictEnum.Current.Value;
			}

			SaveSecureDictionary (dictionary);
		}

		public string GetSecureKeyValue (string key) {
			if (key == null) {
				Debug.WriteLine ("Trying to get null key!");
				return null;
			}

			var dictionary = GetSecureDictionary ();

			if (dictionary != null && dictionary.ContainsKey (key))
				return dictionary[key];

			return null;
		}

		public void RemoveSecureKeyValue (string key) {
			if (key == null) {
				Debug.WriteLine ("Trying to remove with null key!");
				return;
			}

			var dictionary = GetSecureDictionary ();
			if (dictionary != null && dictionary.ContainsKey (key)) {
				if (dictionary.Remove (key)) {
					var updatedDictionary = CloneDictionaryCloningValues (dictionary);
					cache = CloneDictionaryCloningValues (dictionary);
					SaveSecureDictionary (updatedDictionary);
				} else
					Debug.WriteLine ("When removing " + key + ", which exists, the remove operation on the dictionary was not successful!");
			}
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
			MD5 md5 = MD5.Create ();
			return md5.ComputeHash (stream);
		}

		Dictionary<string, string> GetSecureDictionary () {
			if (cache != null)
				return cache;
			
			var path = GetSecureDirectoryPath ();

			if (!this.FileSystemManger.FileExistsAtPath (path))
				EncryptFile (path, new string[0], true);

			var dictionary = new Dictionary<string, string> ();
			string[] lines = DecryptFile (path);
			if (lines != null)
				dictionary = lines.Select (l => l.Split (new[] { '=' }, 2)).ToDictionary (a => a[0], a => a[1]);

			cache = CloneDictionaryCloningValues (dictionary);

			return dictionary;
		}

		void SaveSecureDictionary (Dictionary<string, string> dictionary) {
			var path = GetSecureDirectoryPath ();

			var lines = dictionary.Select (kvp => kvp.Key + "=" + kvp.Value).ToArray ();
			EncryptFile (path, lines, false);
		}

		string GetSecureDirectoryPath () {
			string root = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			this.FileSystemManger.CreateParentDirectories (root);

			string path = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), Constants.SECURE_FILE_NAME);
			this.FileSystemManger.CreateParentDirectories (path);

			return path;
		}

		string GetTempSecureDirectoryPath (string path) {
			return path + ".txt";
		}

		void EncryptFile (string path, string[] data, bool newFile) {
			if (path == null || (!newFile && (data == null || data.Length < 1))) {
				Debug.WriteLine ("Trying to encrypt a file will null path or no data. path: " + path + " data: " + data);
				return;
			}

			lock (static_mutex) {
				File.Delete (path);
				var tmpPath = GetTempSecureDirectoryPath (path);
				File.Delete (tmpPath);
				File.WriteAllLines (tmpPath, data);

				var fis = new Java.IO.FileInputStream (tmpPath);
				var fos = new FileStream (path, FileMode.OpenOrCreate);

				string _tmep = ReadTempCache ();
				var sks = new SecretKeySpec (GetBytes (_tmep), Constants.ENCRYPTION_TYPE);
				Cipher cipher = Cipher.GetInstance (Constants.ENCRYPTION_TYPE);
				cipher.Init (Javax.Crypto.CipherMode.EncryptMode, sks);

				var cos = new CipherOutputStream (fos, cipher);

				int b;
				var d = new byte[8];
				while ((b = fis.Read (d)) != -1) {
					cos.Write (d, 0, b);
				}

				cos.Flush ();
				cos.Close ();
				fis.Close ();

				File.Delete (tmpPath);
			}
		}

		string[] DecryptFile (string path) {
			if (path == null) {
				Debug.WriteLine ("Trying to decrypt a file will null path");
				return null;
			}

			if (!File.Exists (path)) {
				Debug.WriteLine ("File to decrypt does not exist! Path: " + path);
				return null;
			}

			lock (static_mutex) {
				var fis = new FileStream (path, FileMode.Open);

				var tmpPath = GetTempSecureDirectoryPath (path);
				File.Delete (tmpPath);

				var fos = new Java.IO.FileOutputStream (tmpPath);

				string _tmep = ReadTempCache ();
				var sks = new SecretKeySpec (GetBytes (_tmep), Constants.ENCRYPTION_TYPE);
				Cipher cipher = Cipher.GetInstance (Constants.ENCRYPTION_TYPE);
				cipher.Init (Javax.Crypto.CipherMode.DecryptMode, sks);

				var cis = new CipherInputStream (fis, cipher);

				int b;
				var d = new byte[8];
				while ((b = cis.Read (d)) != -1) {
					fos.Write (d, 0, b);
				}

				fos.Flush ();
				fos.Close ();
				cis.Close ();

				string[] lines = File.ReadAllLines (tmpPath);
				File.Delete (tmpPath);
				return lines;
			}
		}

		static string ReadTempCache () {
			var sb = new StringBuilder ();
			sb.Append (Constants.KEY_1);
			sb.Append (Reverse (Constants.KEY_2));
			sb.Append (Constants.KEY_3);
			return sb.ToString ();
		}

		public static string Reverse (string s) {
			char[] charArray = s.ToCharArray ();
			Array.Reverse (charArray);
			return new string (charArray);
		}

		public Dictionary<TKey, TValue> CloneDictionaryCloningValues<TKey, TValue> (Dictionary<TKey, TValue> original) where TValue : ICloneable {
			var ret = new Dictionary<TKey, TValue> (original.Count, original.Comparer);

			foreach (KeyValuePair<TKey, TValue> entry in original)
				ret.Add (entry.Key, (TValue)entry.Value.Clone ());

			return ret;
		}

		static byte[] GetBytes (string str) {
			var bytes = new byte[str.Length * sizeof (char)];
			Buffer.BlockCopy (str.ToCharArray (), 0, bytes, 0, bytes.Length);
			return bytes;
		}

		static string GetString (byte[] bytes) {
			var chars = new char[bytes.Length / sizeof (char)];
			Buffer.BlockCopy (bytes, 0, chars, 0, bytes.Length);
			return new string (chars);
		}
	}
}