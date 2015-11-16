using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using em;
using Foundation;
using Security;

namespace iOS.PlatformImpl {
	class iOSSecurityManager : ISecurityManager {

		static iOSSecurityManager _shared = null;
		public static iOSSecurityManager Shared {
			get {
				if (_shared == null) {
					_shared = new iOSSecurityManager ();
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

		public void storeSecureField (String fieldName, String fieldValue) {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			string secPath = Path.Combine (path, fieldName);
			NSMutableDictionary dict = null;
			if (!File.Exists (secPath))
				dict = new NSMutableDictionary ();
			else {
				NSDictionary fromDisk = NSDictionary.FromFile (secPath);
				dict = new NSMutableDictionary (fromDisk);
			}

			dict.Add (new NSString (fieldName), new NSString (fieldValue));

			// first create an empty file with default file protection attributes, to take advantage of iOS' data protection feature
			CreateFileWithDefaultProtection (secPath);

			dict.WriteToFile (secPath, false);
		}

		public string retrieveSecureField (String fieldName) {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			string secPath = Path.Combine (path, fieldName);
			if (File.Exists (secPath)) {
				NSDictionary dict = NSDictionary.FromFile (secPath);
				var str = (NSString)dict.ObjectForKey (new NSString (fieldName));
				return str.ToString ();
			}

			return null;
		}

		public bool removeSecureField (String fieldName) {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
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

		Dictionary<string, string> GetSecureDictionary () {
			if (cache != null)
				return cache;

			var path = GetSecureDirectoryPath ();

			var dictionary = new Dictionary<string, string> ();
			string[] lines = DecryptFile (path);
			if (lines != null)
				dictionary = lines.Select (l => l.Split (new[] { '=' }, 2)).ToDictionary (a => a[0], a => a[1]);

			cache = CloneDictionaryCloningValues (dictionary);

			return dictionary;
		}

		void SaveSecureDictionary(Dictionary<string, string> dictionary) {
			var path = GetSecureDirectoryPath ();

			var lines = dictionary.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray();
			EncryptFile (path, lines);
		}

		string GetSecureDirectoryPath() {
			string root = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			IFileSystemManager fs = this.FileSystemManger;
			fs.CreateParentDirectories (root);

			string path = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), Constants.SECURE_FILE_NAME);
			fs.CreateParentDirectories (path);

			return path;
		}

		string GetTempSecureDirectoryPath (string path) {
			return path + ".txt";
		}

		void EncryptFile (string path, string[] inputData) {
			if (path == null || inputData == null || inputData.Length < 1) {
				Debug.WriteLine ("Trying to encrypt a file will null path or no data. path: " + path + " data: " + inputData);
				return;
			}

			lock (_fileSystemManger) {
				File.Delete (path);
				var tmpPath = GetTempSecureDirectoryPath (path);
				File.Delete (tmpPath);
				File.WriteAllLines (tmpPath, inputData);

				byte[] key = ReadEMBytes ();
				byte[] iv = WriteEMBytes ();

				using (var fileCrypt = new FileStream (path, FileMode.Create)) {
					using (var aes = new AesCryptoServiceProvider {
						Key = key,
						IV = iv,
						Mode = CipherMode.CBC,
						Padding = PaddingMode.PKCS7
					}) {
						using (var cs = new CryptoStream (fileCrypt, aes.CreateEncryptor (), CryptoStreamMode.Write)) {
							using (var fileInput = new FileStream (tmpPath, FileMode.Open)) {
								int data;

								while ((data = fileInput.ReadByte ()) != -1)
									cs.WriteByte ((byte)data);
							}
						}
					}
				}

				File.Delete (tmpPath);
			}
		}

		string[] DecryptFile (string path) {
			if (path == null) {
				Debug.WriteLine ("Trying to decrypt a file will null path");
				return null;
			}

			if (!NSFileManager.DefaultManager.FileExists (path)) {
				Debug.WriteLine ("File to decrypt does not exist! Path: " + path);
				return null;
			}

			lock (_fileSystemManger) {
				var tmpPath = GetTempSecureDirectoryPath (path);
				File.Delete (tmpPath);

				byte[] key = ReadEMBytes ();
				byte[] iv = ReadEMBytes2 ();

				using (var fileCrypt = new FileStream (path, FileMode.Open)) {
					using (var aes = new AesCryptoServiceProvider {
						Key = key,
						IV = iv,
						Mode = CipherMode.CBC,
						Padding = PaddingMode.PKCS7
					}) {
						using (var cs = new CryptoStream (fileCrypt, aes.CreateDecryptor (), CryptoStreamMode.Read)) {
							using (var fileOutput = new FileStream (tmpPath, FileMode.OpenOrCreate)) {
								int data;

								while ((data = cs.ReadByte ()) != -1)
									fileOutput.WriteByte ((byte)data);
							}
						}
					}
				}

				string[] lines = File.ReadAllLines (tmpPath);
				File.Delete (tmpPath);
				return lines;
			}
		}

		public Dictionary<TKey, TValue> CloneDictionaryCloningValues<TKey, TValue> (Dictionary<TKey, TValue> original) where TValue : ICloneable {
			var ret = new Dictionary<TKey, TValue> (original.Count, original.Comparer);

			foreach (KeyValuePair<TKey, TValue> entry in original)
				ret.Add (entry.Key, (TValue)entry.Value.Clone ());

			return ret;
		}

		static byte[] ReadEMBytes () {
			const string generic = "EMSK";
			byte[] KEY;

			var recKey = new SecRecord (SecKind.GenericPassword) {
				Generic = NSData.FromString (generic)
			};

			SecStatusCode resKey;
			var matchKey = SecKeyChain.QueryAsRecord (recKey, out resKey);
			if (resKey == SecStatusCode.Success) {
				KEY = Convert.FromBase64String (matchKey.ValueData.ToString ());
			} else {
				var provider = new AesCryptoServiceProvider {
					Mode = CipherMode.CBC,
					Padding = PaddingMode.PKCS7
				};
				provider.GenerateKey ();
				KEY = provider.Key;

				recKey = new SecRecord (SecKind.GenericPassword) {
					Label = generic,
					Account = "EMACTK",
					ValueData = NSData.FromString (Convert.ToBase64String (KEY)),
					Generic = NSData.FromString (generic)
				};

				var result = SecKeyChain.Add (recKey);

				Debug.Assert (result == SecStatusCode.Success, "Error inserting key");
			}

			return KEY;
		}

		static byte[] WriteEMBytes () {
			const string generic = "EMIV";

			var provider = new AesCryptoServiceProvider {
				Mode = CipherMode.CBC,
				Padding = PaddingMode.PKCS7
			};
			provider.GenerateIV ();
			byte[] IV = provider.IV;

			var recIV = new SecRecord (SecKind.GenericPassword) {
				Generic = NSData.FromString (generic)
			};

			SecStatusCode resIV;
			var query = SecKeyChain.QueryAsRecord (recIV, out resIV);
			if (resIV == SecStatusCode.Success) {
				var updatedRecord = new SecRecord (SecKind.Identity);
				updatedRecord.ValueData = NSData.FromString (Convert.ToBase64String (IV));
				var updateStatusCode = SecKeyChain.Update (recIV, updatedRecord);

				if (updateStatusCode != SecStatusCode.Success)
					IV = Convert.FromBase64String (query.ValueData.ToString ());
				
			} else {
				recIV = new SecRecord (SecKind.GenericPassword) {
					Label = generic,
					Account = "EMACTIV",
					ValueData = NSData.FromString (Convert.ToBase64String (IV)),
					Generic = NSData.FromString (generic)
				};

				var addStatusCode = SecKeyChain.Add (recIV);

				Debug.Assert (addStatusCode == SecStatusCode.Success, "Error adding IV");
			}

			return IV;
		}

		static byte[] ReadEMBytes2 () {
			const string generic = "EMIV";
			byte[] IV;

			var recIV = new SecRecord (SecKind.GenericPassword) {
				Generic = NSData.FromString (generic)
			};

			SecStatusCode resIV;
			var matchIV = SecKeyChain.QueryAsRecord (recIV, out resIV);
			if (resIV == SecStatusCode.Success) {
				IV = Convert.FromBase64String (matchIV.ValueData.ToString ());
			} else {
				var provider = new AesCryptoServiceProvider {
					Mode = CipherMode.CBC,
					Padding = PaddingMode.PKCS7
				};
				provider.GenerateIV ();
				IV = provider.IV;

				recIV = new SecRecord (SecKind.GenericPassword) {
					Label = generic,
					Account = "EMACTIV",
					ValueData = NSData.FromString (Convert.ToBase64String (IV)),
					Generic = NSData.FromString (generic)
				};

				var result = SecKeyChain.Add (recIV);

				Debug.Assert (result == SecStatusCode.Success, "Error adding IV 2");
			}

			return IV;
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

		// TODO: duplicated in PlatformFactory
		void CreateFileWithDefaultProtection (string systemPath) {
			bool succeeded = NSFileManager.DefaultManager.CreateFile (systemPath, new NSData (), NSDictionary.FromObjectAndKey (NSFileManager.FileProtectionCompleteUntilFirstUserAuthentication, NSFileManager.FileProtectionKey));
			if (!succeeded) {
				throw new IOException (String.Format ("cannot create protected file at path {0}", systemPath));
			}
		}
	}
}