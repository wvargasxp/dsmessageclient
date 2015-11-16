using System;
using System.IO;

namespace em {
	public interface IFileSystemManager {
		void CreateParentDirectories (string path);
		string GetSystemPathForFolder (string folderName);
		string ResolveSystemPathForUri (string virtualPath);
		void RemoveFilesForChatEntry(ChatEntry chatEntry);
		string GetFilePathForSharingAlias(AliasInfo alias);
		Stream GetReadOnlyFileStreamFromPath(string path);
		void RemoveFileAtPath(string path);
		void InvalidateFileCacheForScaledMedia (string path);
		void CopyBytesToPath (string path, byte[] incomingBytes, Action<double> progressCallback);
		void CopyBytesToPath (string path, Stream incomingStream, Action<double> progressCallback);
		void CopyBytesToPath (string path, Stream incomingStream, long expectedLength, Action<double> progressCallback);
		void MoveFileAtPath(string srcPath, string destPath);
		void CopyFileAtPath(string srcPath, string destPath);
		byte[] ContentsOfFileAtPath(string path);
		bool FileExistsAtPath (string path);
		string MoveStagingContentsToFileStore (string stagingPath, ChatEntry chatEntry);
		string MoveStagingContentsToFileStore (string stagingPath, Message message);
		string GetFilePathForNotificationCenterMedia (string guid);
	}
}