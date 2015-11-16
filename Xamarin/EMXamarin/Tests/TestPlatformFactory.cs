using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SQLite.Net.Interop;
using SQLite.Net.Platform.XamarinIOS;
using MonoTouch.Foundation;
using em;
using EMXamarin;


namespace Tests
{
	public class TestPlatformFactory : PlatformFactory
	{
		IAddressBook addressBook = null;
		IDeviceInfo deviceInfo = null;
		private string accountID;
		private string pwd;
		private int userIndex;

		public string GetFilePathForAccountInfo() {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			path = Path.Combine (path, "account");
			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);
			path = Path.Combine (path, "accountInfo.json");

			return path;
		}

		public void storeSecureField(String fieldName, String fieldValue) {
			if (fieldName.Equals("accountID"))
				accountID = fieldValue;
			if (fieldName.Equals ("password"))
				pwd = fieldValue;
			/*
			var rec = new SecRecord (SecKind.GenericPassword){
				Generic = NSData.FromString ("foo")
			};

			SecStatusCode res;
			var match = SecKeyChain.QueryAsRecord (rec, out res);
			if (res == SecStatusCode.Success)
				DisplayMessage ("Key found, password is: {0}", match.ValueData);
			else
				DisplayMessage ("Key not found: {0}", res);

			var s = new SecRecord (SecKind.GenericPassword) {
				Label = "Item Label",
				Description = "Item description",
				Account = "Account",
				Service = "Service",
				Comment = "Your comment here",
				ValueData = NSData.FromString ("my-secret-password"),
				Generic = NSData.FromString ("foo")
			};

			var err = SecKeyChain.Add (s);

			if (err != SecStatusCode.Success && err != SecStatusCode.DuplicateItem)
				DisplayMessage ("Error adding record: {0}", err);
			*/
		}

		public string retrieveSecureField(String fieldName) {

			if (fieldName.Equals ("accountID"))
				return accountID;
			else if (fieldName.Equals("password"))
				return pwd;

			return null;
			/*
			var rec = new SecRecord (SecKind.GenericPassword){
				Generic = NSData.FromString (fieldName)
			};

			SecStatusCode res;
			var match = SecKeyChain.QueryAsRecord (rec, out res);
			if (res == SecStatusCode.Success)
				DisplayMessage ("Key found, password is: {0}", match.ValueData);
			else
				DisplayMessage ("Key not found: {0}", res);

			var s = new SecRecord (SecKind.GenericPassword) {
				Label = "Item Label",
				Description = "Item description",
				Account = "Account",
				Service = "Service",
				Comment = "Your comment here",
				ValueData = NSData.FromString ("my-secret-password"),
				Generic = NSData.FromString ("foo")
			};

			var err = SecKeyChain.Add (s);

			if (err != SecStatusCode.Success && err != SecStatusCode.DuplicateItem)
				DisplayMessage ("Error adding record: {0}", err);
*/
		}

		public PlatformType getPlatformType() {
			return PlatformType.IOSPlatform;
		}

		public IDeviceInfo getDeviceInfo() {
			if (deviceInfo == null)
				deviceInfo = new TestDeviceInfo (userIndex);

			return deviceInfo;
		}

		public IAddressBook getAddressBook() {
			if ( addressBook == null )
				addressBook = new TestAddressBook(userIndex);
			return addressBook;
		}

		public WebSocketClient GetWebSocket () {
			return new TestWebSocketClient (userIndex);
		}

		public string getSQLPath() {
			var sqliteFilename = "TaskDB.db3";
			// we need to put in /Library/ on iOS5.1 to meet Apple's iCloud terms
			// (they don't want non-user-generated data in Documents)
			string documentsPath = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			string libraryPath = Path.Combine (documentsPath, "../Library/"); // Library folder
			var path = Path.Combine(libraryPath, sqliteFilename);
			return path;
		}

		public HttpClientHandler GetHttpHandler() {
			return new StreamingClientHandler ();
		}

		public string GetFilePathForChatEntryUri (Uri uri, ChatEntry chatEntry) {
			string absoluteUri = uri == null ? "" : uri.AbsoluteUri;
			if (absoluteUri.StartsWith ("file://"))
				return uri.LocalPath;

			string storagepath = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			string mediaDir = Path.Combine (storagepath, "media");
			string chatsDir = Path.Combine (mediaDir, "chats");
			string convoDir = Path.Combine (chatsDir, chatEntry.chatEntryID.ToString ());

			// if no ur then just the directory for the chatEntry
			if (uri == null)
				return convoDir;

			string fileName = Path.GetFileName (absoluteUri);
			string fullPath = Path.Combine (convoDir, fileName);

			return fullPath;
		}

		public void RemoveFilesForChatEntry(ChatEntry chatEntry) {
			string path = GetFilePathForChatEntryUri (null, chatEntry);

			try {
				foreach (string file in Directory.EnumerateFiles(path)) {
					if (file != null)
						File.Delete (file);
				}

				Directory.Delete (path);
			}
			catch (DirectoryNotFoundException e) {
				// okay, just means no files were saved
			}
			catch (Exception e) {

			}
		}

		public string GetCachedFilePathForUri(Uri uri) {
			string absoluteUri = uri == null ? "" : uri.AbsoluteUri;
			if (absoluteUri.StartsWith ("file://"))
				return uri.LocalPath;

			string fileName = null;
			if (!absoluteUri.StartsWith ("addressbook:"))
				fileName = Path.GetFileName (uri.AbsolutePath);
			else {
				// address book thumbnail
				string[] split = absoluteUri.Split (new char[] { '/' });
				fileName = "addressBook" + split [split.Length - 2] + ".jpeg";
			}

			string storagepath = Environment.GetFolderPath (Environment.SpecialFolder.InternetCache);
			string emCacheDir = Path.Combine (storagepath, "em");

			string path = emCacheDir;
			path = Path.Combine (path, fileName.Length > 0 ? fileName [0].ToString() : "0");
			path = Path.Combine (path, fileName.Length > 1 ? fileName [1].ToString() : "0");
			path = Path.Combine (path, fileName);

			return path;
		}

		public Stream GetReadOnlyFileStreamFromPath(string path) {
			Stream fileStream = File.OpenRead (path);
			return fileStream;
		}

		public void CopyBytesToPath(string path, Stream incomingStream, long expectedLength, Action<double> completionCallback) {
			string dirs = Path.GetDirectoryName (path);
			if (!Directory.Exists (dirs))
				Directory.CreateDirectory (dirs);

			try {
				FileMode fm = File.Exists(path) ? FileMode.Append : FileMode.Create;
				using (FileStream writeStream = new FileStream(path, fm, FileAccess.Write)) {
					using(BinaryWriter writeBinay = new BinaryWriter(writeStream)) {
						long totalRead = 0;
						if ( completionCallback != null )
							completionCallback(0);

						byte[] buffer = new byte[Constants.LARGE_COPY_BUFFER];
						int bytesRead = incomingStream.Read(buffer, 0, buffer.Length);
						while ( bytesRead > 0 ) {
							writeBinay.Write(buffer, 0, bytesRead);
							if ( expectedLength != -1 ) {
								totalRead += bytesRead;
								if ( completionCallback != null )
									completionCallback( (double)totalRead/(double)expectedLength);
							}

							bytesRead = incomingStream.Read(buffer, 0, buffer.Length);
						}
					}
				}

				// if we don't know the length, indicate completion
				if ( expectedLength == -1 && completionCallback != null)
					completionCallback(1);
			}
			catch (Exception ex) {
				Debug.WriteLine(ex.ToString());
			}
		}

		public byte[] ContentsOfFileAtPath(string path) {
			return File.ReadAllBytes (path);
		}

		public string GetNewMediaFileNameForStagingContents() {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			path = Path.Combine (path, "staging");
			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);
			path = Path.Combine (path, Path.GetRandomFileName ());

			return path;
		}

		public string GetOutgoingQueueTempName() {
			string path = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			path = Path.Combine (path, "outgoing-queue");
			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);
			path = Path.Combine (path, Path.GetRandomFileName ());

			return path;
		}

		public string MoveStagingContentsToFileStore (string stagingPath, ChatEntry chatEntry) {
			string filename = Path.GetFileName (stagingPath);

			string spoofed = "http://foo.com/" + filename;
			string finalPath = GetFilePathForChatEntryUri (new Uri (spoofed), chatEntry);

			string dir = Path.GetDirectoryName (finalPath);
			if (!Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			File.Move (stagingPath, finalPath);
			return finalPath;
		}

		public void RemoveFileAtPath(string path) {
			File.Delete (path);
		}

		public ISQLitePlatform getSQLitePlatform() {
			return new SQLitePlatformIOS ();
		}

		public void runOnMainThread(Action someAction) {
			Task.Factory.StartNew (delegate {
				someAction ();
			});
		}

		public void PlayIncomingMessageSound() {

		}

		public TestPlatformFactory (int userArrayIndex)
		{
			userIndex = userArrayIndex;
		}
	}
}

