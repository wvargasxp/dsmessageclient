using System;
using System.Diagnostics;
using System.IO;
using em;
using Foundation;
using UIKit;

namespace iOS {
	public class iOSFileSystemManager : IFileSystemManager {

		readonly PlatformFactory platformFactory;

		public iOSFileSystemManager (PlatformFactory platformFactory) {
			this.platformFactory = platformFactory;
		}

		public void CreateParentDirectories (string path) {
			string systemPath = ResolveSystemPathForUri (path);
			string parentDirectory = Path.GetDirectoryName (systemPath);

			if (!NSFileManager.DefaultManager.FileExists (parentDirectory)) {
				NSFileManager.DefaultManager.CreateDirectory (parentDirectory, true, null);
			}
		}

		public void CopyBytesToPath (string path, byte[] incomingBytes, Action<double> progressCallback) {
			using (MemoryStream incomingStream = new MemoryStream (incomingBytes)) {
				long expectedLength = incomingStream.Length;
				CopyBytesToPath (path, incomingStream, expectedLength, progressCallback);
			}
		}

		public void CopyBytesToPath (string path, Stream incomingStream, Action<double> progressCallback) {
			CopyBytesToPath (path, incomingStream, FileSystemManager_Constants.UNKNOWN_BYTE_LENGTH, progressCallback);
		}

		public void CopyBytesToPath(string path, Stream incomingStream, long expectedLength, Action<double> progressCallback) {
			var systemPath = ResolveSystemPathForUri (path);

			nint taskId = UIApplication.BackgroundTaskInvalid;
			if (UIApplication.SharedApplication != null) {
				taskId = UIApplication.SharedApplication.BeginBackgroundTask (() => {
					if (taskId != UIApplication.BackgroundTaskInvalid) {
						UIApplication.SharedApplication.EndBackgroundTask (taskId);
						taskId = UIApplication.BackgroundTaskInvalid;
					}
				});
			}

			CreateParentDirectories (systemPath);

			try {
				// first create an empty file with default file protection attributes, to take advantage of iOS' data protection feature
				CreateFileWithDefaultProtection (systemPath);

				FileMode fm = File.Exists(systemPath) ? FileMode.Append : FileMode.Create;

				using (var writeStream = new FileStream(systemPath, fm, FileAccess.Write)) {
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
									progressCallback( (double)totalRead/(double)expectedLength);
							}

							bytesRead = incomingStream.Read(buffer, 0, buffer.Length);
						}
					}
				}

				// if we don't know the length, indicate completion
				if ( expectedLength == FileSystemManager_Constants.UNKNOWN_BYTE_LENGTH && progressCallback != null)
					progressCallback(1);
			}
			catch (Exception ex) {
				Debug.WriteLine(ex.ToString());
			} 
			finally {
				if (UIApplication.SharedApplication != null) {
					UIApplication.SharedApplication.EndBackgroundTask (taskId);
				}
			}
		}

		public void MoveFileAtPath (string srcPath, string destPath) {
			string srcSystemPath = ResolveSystemPathForUri (srcPath);
			string destSystemPath = ResolveSystemPathForUri (destPath);

			try {
				if (NSFileManager.DefaultManager.FileExists (srcSystemPath)) {

					CreateParentDirectories (destSystemPath);

					if (NSFileManager.DefaultManager.FileExists (destSystemPath)) {
						RemoveFileAtPath (destSystemPath);
					}

					NSError error = null;
					NSFileManager.DefaultManager.Move (srcSystemPath, destSystemPath, out error);
					if (error != null) {
						throw new IOException (error.LocalizedDescription);
					}
				}
			} catch (Exception e) {
				throw new IOException (String.Format ("issue moving file {0} to {1}. {2}", srcPath, destPath, e));
			}
		}

		public void CopyFileAtPath(string srcPath, string destPath) {
			string srcSystemPath = ResolveSystemPathForUri (srcPath);
			string destSystemPath = ResolveSystemPathForUri (destPath);

			try {
				if (NSFileManager.DefaultManager.FileExists (srcSystemPath)) {
					CreateParentDirectories (destSystemPath);

					if (NSFileManager.DefaultManager.FileExists (destSystemPath)) {
						RemoveFileAtPath (destSystemPath);
					}

					NSError error = null;
					NSFileManager.DefaultManager.Copy (srcSystemPath, destSystemPath, out error);
					if (error != null) {
						throw new IOException (error.LocalizedDescription);
					}
				}
			} catch (Exception e) {
				Debug.WriteLine (String.Format ("issue copying file {0} to {1}. {2}", srcPath, destPath, e));
			}
		}

		public byte[] ContentsOfFileAtPath(string path) {
			string systemPath = ResolveSystemPathForUri (path);
			return File.Exists (systemPath) ? File.ReadAllBytes (systemPath) : null;
		}

		public bool FileExistsAtPath (string path) {
			string systemPath = ResolveSystemPathForUri (path);
			return File.Exists (systemPath);
		}

		public void RemoveFilesForChatEntry(ChatEntry chatEntry) {
			string path = platformFactory.GetUriGenerator ().GetFilePathForChatEntryUri (null, chatEntry);
			string systemPath = this.ResolveSystemPathForUri (path);
			try {
				foreach (string file in Directory.EnumerateFiles(systemPath)) {
					if (file != null) {
						File.Delete (file);
					}
				}

				Directory.Delete (systemPath);
			}
			catch (DirectoryNotFoundException e) {
				// okay, just means no files were saved
				Debug.WriteLine ("RemoveFilesForChatEntry:SafeToIgnore:" + e);
			}
			catch (Exception e) {
				Debug.WriteLine ("iOSPlatformFactory:RemoveFilesForChatEntry :" + e);
			}
		}

		public string GetFilePathForSharingAlias (AliasInfo alias) {
			string path = iOSFileSystemManager_Constants.FOLDER_MYDOCUMENTS;
			path = Path.Combine (path, "alias");
			path = Path.Combine (path, alias.displayName);
			CreateParentDirectories (path);

			var oldPath = Path.Combine (path, "shareAlias.igo");
			RemoveFileAtPath (oldPath);

			//put in the _1 to indicate version of the image generated
			path = Path.Combine (path, "shareAlias_1.igo");

			return path;
		}

		public Stream GetReadOnlyFileStreamFromPath(string path) {
			string systemPath = ResolveSystemPathForUri (path);
			Stream fileStream = File.OpenRead (systemPath);

			return fileStream;
		}

		public string MoveStagingContentsToFileStore (string stagingPath, ChatEntry chatEntry) {
			string systemStagingPath = ResolveSystemPathForUri (stagingPath);
			string filename = Path.GetFileName (systemStagingPath);

			string spoofed = "http://foo.com/" + filename;
			string finalPath = platformFactory.GetUriGenerator ().GetFilePathForChatEntryUri (new Uri (spoofed), chatEntry);
			string systemFinalPath = ResolveSystemPathForUri (finalPath);

			this.CreateParentDirectories (systemFinalPath);

			File.Move (systemStagingPath, systemFinalPath);

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

		public void RemoveFileAtPath(string path) {
			if (path != null) {
				var systemPath = ResolveSystemPathForUri (path);

				if (File.Exists (systemPath)) {
					File.Delete (systemPath);
				}

				InvalidateFileCacheForScaledMedia (path);
			}
		}

		public void InvalidateFileCacheForScaledMedia(string path) {
			if (path == null)
				return;

			var systemPath = ResolveSystemPathForUri (path);

			string thumbnailDir = platformFactory.GetUriGenerator ().GetDirectoryPathForScaledMedia (systemPath);
			if (Directory.Exists (thumbnailDir)) {
				foreach (string f in Directory.EnumerateFiles (thumbnailDir))
					File.Delete (f);

			}
		}

		public string GetSystemPathForFolder (string folderName) {
			switch (folderName) {
			case iOSFileSystemManager_Constants.FOLDER_MYDOCUMENTS:
				return Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			case iOSFileSystemManager_Constants.FOLDER_INTERNET_CACHE:
				return Environment.GetFolderPath (Environment.SpecialFolder.InternetCache);
			}

			// Default path to use
			return Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
		}

		public string ResolveSystemPathForUri (string virtualPath) {
			string folder = null;
			if (virtualPath == null) {
				return null;
			}
			foreach (string f in iOSFileSystemManager_Constants.FOLDERS) {
				if (virtualPath.Contains (f)) {
					folder = f;
					break;
				}
			}

			if (folder == null) {
				return virtualPath;
			}

			var systemFolderPath = GetSystemPathForFolder (folder);
			return virtualPath.Replace (folder, systemFolderPath);
		}

		private void CreateFileWithDefaultProtection(string systemPath) {
			bool succeeded = NSFileManager.DefaultManager.CreateFile (systemPath, new NSData(), NSDictionary.FromObjectAndKey(NSFileManager.FileProtectionCompleteUntilFirstUserAuthentication, NSFileManager.FileProtectionKey));
			if (!succeeded) {
				throw new IOException (String.Format ("cannot create protected file at path {0}", systemPath));
			}
		}

		public string GetFilePathForNotificationCenterMedia (string guid) {
			return null;
		}
	}
}

