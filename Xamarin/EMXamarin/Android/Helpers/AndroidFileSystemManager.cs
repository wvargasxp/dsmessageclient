using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using em;

namespace Emdroid {
	public class AndroidFileSystemManager : IFileSystemManager {
		
		readonly PlatformFactory platformFactory;

		public AndroidFileSystemManager (PlatformFactory platformFactory) {
			this.platformFactory = platformFactory;
		}

		public string GetSystemPathForFolder (string folderName) {
			throw new NotImplementedException ();
		}

		public string ResolveSystemPathForUri (string virtualPath) {
			var systemPath = virtualPath;

			return systemPath;
		}

		public void RemoveFilesForChatEntry(ChatEntry chatEntry) {
			string path = platformFactory.GetUriGenerator ().GetFilePathForChatEntryUri (null, chatEntry);

			try {
				foreach (string file in Directory.EnumerateFiles(path)) {
					if (file != null)
						File.Delete (file);
				}
				Directory.Delete (path);
			}
			catch (DirectoryNotFoundException) {
				// okay, just means no files were saved
			}
			catch (Exception e) {
				Debug.WriteLine ("Exception removing files for chat entry: " + e);
			}
		}

		public string GetFilePathForSharingAlias (AliasInfo alias) {
			if (Android.OS.Environment.ExternalStorageDirectory != null && Android.OS.Environment.ExternalStorageDirectory.CanRead() && Android.OS.Environment.ExternalStorageDirectory.CanWrite()) {
				string path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

				path = Path.Combine (path, "alias");
				path = Path.Combine (path, alias.displayName);

				CreateParentDirectories (path);

				var oldPath = Path.Combine (path, "shareAlias.jpg");
				RemoveFileAtPath (oldPath);

				//put in the _1 to indicate version of the image generated
				path = Path.Combine (path, "shareAlias_1.jpg");
				return path;
			}

			return null;
		}

		public Stream GetReadOnlyFileStreamFromPath(string path) {
			Stream fileStream = File.OpenRead (path);
			return fileStream;
		}

		public void RemoveFileAtPath(string path) {
			if (path != null) {
				if (File.Exists (path)) {
					File.Delete (path);
				}

				InvalidateFileCacheForScaledMedia (path);
			}
		}

		public void InvalidateFileCacheForScaledMedia(string path) {
			if (path == null)
				return;

			string thumbnailDir = platformFactory.GetUriGenerator ().GetDirectoryPathForScaledMedia (path);
			if (Directory.Exists (thumbnailDir)) {
				foreach (string f in Directory.EnumerateFiles (thumbnailDir))
					File.Delete (f);

			}
		}

		public void CreateParentDirectories (string path) {
			string parentDirectory = Path.GetDirectoryName (path);
			if (!Directory.Exists (parentDirectory)) {
				Directory.CreateDirectory (parentDirectory);
			}
		}

		public void CopyBytesToPath (string path, byte[] incomingBytes, Action<double> progressCallback) {
			using (MemoryStream incomingStream = new MemoryStream (incomingBytes)) {
				long expectedLength = incomingBytes.Length;
				CopyBytesToPath (path, incomingStream, expectedLength, progressCallback);
			}
		}

		public void CopyBytesToPath (string path, Stream incomingStream, Action<double> progressCallback) {
			CopyBytesToPath (path, incomingStream, FileSystemManager_Constants.UNKNOWN_BYTE_LENGTH, progressCallback);
		}

		public void CopyBytesToPath(string path, Stream incomingStream, long expectedLength, Action<double> progressCallback) {
			CreateParentDirectories (path);

			try {
				FileMode fm = File.Exists(path) ? FileMode.Append : FileMode.Create;
				using (var writeStream = new FileStream(path, fm, FileAccess.Write)) {
					using(var writeBinay = new BinaryWriter(writeStream)) {
						long totalRead = 0;
						if ( progressCallback != null )
							progressCallback(0);

						var buffer = new byte[Constants.LARGE_COPY_BUFFER];
						int bytesRead = incomingStream.Read(buffer, 0, buffer.Length);
						while ( bytesRead > 0 ) {
							writeBinay.Write(buffer, 0, bytesRead);
							if ( expectedLength != FileSystemManager_Constants.UNKNOWN_BYTE_LENGTH ) {
								totalRead += bytesRead;
								if ( progressCallback != null )
									progressCallback((double) totalRead/(double)expectedLength);
							}

							bytesRead = incomingStream.Read(buffer, 0, buffer.Length);
						}
					}
				}

				// if we don't know the length, indicate completion
				if ( expectedLength == FileSystemManager_Constants.UNKNOWN_BYTE_LENGTH && progressCallback != null )
					progressCallback(1);
			}
			catch (Exception ex) {
				Debug.WriteLine (ex);
			}

			Debug.WriteLine ("finish copying file to: " + path);
		}

		public void MoveFileAtPath (string srcPath, string destPath) {
			try {
				if (File.Exists (srcPath)) {
					CreateParentDirectories (destPath);
					RemoveFileAtPath (destPath);
					File.Move (srcPath, destPath);
				}
			} catch (Exception e) {
				throw new IOException (String.Format ("issue moving file {0} to {1}. {2}", srcPath, destPath, e));
			}
		}

		public void CopyFileAtPath (string srcPath, string destPath) {
			try {
				if (File.Exists (srcPath)) {
					CreateParentDirectories (destPath);
					RemoveFileAtPath (destPath);
					File.Copy (srcPath, destPath);
				}
			} catch (Exception e) {
				Debug.WriteLine (String.Format ("issue copying file {0} to {1}. {2}", srcPath, destPath, e));
			}
		}

		public byte[] ContentsOfFileAtPath(string path) {
			if (File.Exists (path))
				return File.ReadAllBytes (path);
			return null;
		}

		public bool FileExistsAtPath (string path) {
			string systemPath = ResolveSystemPathForUri (path);
			return File.Exists (systemPath);
		}

		public string MoveStagingContentsToFileStore (string stagingPath, ChatEntry chatEntry) {
			string filename = Path.GetFileName (stagingPath);

			string spoofed = "http://foo.com/" + filename;
			string finalPath = platformFactory.GetUriGenerator ().GetFilePathForChatEntryUri (new Uri (spoofed), chatEntry);

			CreateParentDirectories (finalPath);

			File.Move (stagingPath, finalPath);
			return finalPath;
		}

		public string MoveStagingContentsToFileStore (string stagingPath, Message message) {
			string finalPath = message.LocalPathFromMessageGUID (stagingPath);

			string systemStagingPath = ResolveSystemPathForUri (stagingPath);
			string systemFinalPath = ResolveSystemPathForUri (finalPath);

			this.CreateParentDirectories (systemFinalPath);

			File.Move (systemStagingPath, systemFinalPath);

			return finalPath;
		}

		// When GCM message comes in, the media is stored in some directory. Filepath based on 
		// message's GUID.
		public string GetFilePathForNotificationCenterMedia (string guid) {
			MD5 md5 = MD5.Create ();
			byte[] firstFourBytes = new byte[4];
			using (Stream stream = GenerateStreamFromString (guid)) {
				byte[] hash = md5.ComputeHash (stream);
				for (int i = 0; i < 4; i++) {
					firstFourBytes [i] = hash [i];
				}
			}
			string guidInBase64 = Convert.ToBase64String (firstFourBytes);
			return EMApplication.Instance.FilesDir.AbsolutePath + "/notification/" + guidInBase64 + ".temporary";
		}

		private Stream GenerateStreamFromString(string s) {
			MemoryStream stream = new MemoryStream();
			StreamWriter writer = new StreamWriter(stream);
			writer.Write(s);
			writer.Flush();
			stream.Position = 0;
			return stream;
		}
	}
}

