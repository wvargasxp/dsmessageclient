using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace em {
	public class PlatformUriGenerator : IUriGenerator {

		private const string THUMBNAIL_EXT = ".thumb";
		private const string DOWNLOAD_EXT = ".download";

		public const string VIRTUAL_PARENT_PATH_GROUP_THUMBNAIL = "VIRTUAL_GROUP_THUMBNAIL";
		public const string VIRTUAL_PARENT_PATH_ACCOUNT_THUMBNAIL = "VIRTUAL_ACCOUNT_THUMBNAIL";
		public const string VIRTUAL_PARENT_PATH_ACCOUNT_INFO = "VIRTUAL_ACCOUNT_INFO";
		public const string VIRTUAL_PARENT_PATH_ALIAS_THUMBNAIL = "VIRTUAL_ALIAS_THUMBNAIL";
		public const string VIRTUAL_PARENT_PATH_ALIAS_ICON_THUMBNAIL = "VIRTUAL_ALIAS_ICON_THUMBNAIL";
		public const string VIRTUAL_PARENT_PATH_MEDIA = "VIRTUAL_MEDIA";
		public const string VIRTUAL_PARENT_PATH_CACHE = "VIRTUAL_CACHE_FILE";
		public const string VIRTUAL_PARENT_CHAT_ENTRY = "VIRTUAL_CHAT_ENTRY";
		public const string VIRTUAL_PARENT_STAGING_CONTENTS = "VIRTUAL_STAGING_CONTENTS";
		public const string VIRTUAL_PARENT_OUTGOING_QUEUE_STAGING = "VIRTUAL_PARENT_OUTGOING_QUEUE_STAGING";

		private const int MAX_THUMBNAIL_DIRECTORY_LENGTH = 20;

		UriPlatformResolverStrategy uriplatformResolverStrategy;

		public int MaxThumbnailDirectoryLength {
			get;
			set;
		}

		public PlatformUriGenerator (UriPlatformResolverStrategy uriplatformResolverStrategy) {
			this.uriplatformResolverStrategy = uriplatformResolverStrategy;
			this.MaxThumbnailDirectoryLength = MAX_THUMBNAIL_DIRECTORY_LENGTH;
		}

		public string GetFilePathForAccountInfo () {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_ACCOUNT_INFO);
			string path = platformPath;
			path = Path.Combine (path, "account");
			path = Path.Combine (path, "accountInfo.json");

			return path;
		}

		public string GetStagingPathForAccountInfoThumbnailLocal () {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_ACCOUNT_THUMBNAIL);
			string path = platformPath;
			path = Path.Combine (path, "account");
			path = Path.Combine (path, "accountInfoThumbnail.jpg");

			return path;
		}

		public string GetStagingPathForGroupThumbnailLocal () {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_GROUP_THUMBNAIL);
			string path = platformPath;

			path = Path.Combine (path, "staging");
			path = Path.Combine (path, "group");
			path = Path.Combine (path, "groupthumbnail.jpg");

			return path;
		}

		public string GetStagingPathForGroupThumbnailServer (Group group) {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_GROUP_THUMBNAIL);
			string path = platformPath;

			path = Path.Combine (path, "group");
			path = Path.Combine (path, group.serverID);
			path = Path.Combine (path, "groupthumbnail.jpg");

			return path;
		}

		public string GetStagingPathForAliasThumbnailLocal () {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_ALIAS_THUMBNAIL);
			string path = platformPath;

			path = Path.Combine (path, "staging");
			path = Path.Combine (path, "alias");
			path = Path.Combine (path, "aliasThumbnail.jpg");

			return path;
		}

		public string GetStagingPathForAliasThumbnailServer (AliasInfo alias) {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_ALIAS_THUMBNAIL);
			string path = platformPath;;
			path = Path.Combine (path, "alias");
			path = Path.Combine (path, alias.displayName);
			path = Path.Combine (path, "aliasThumbnail.jpg");

			return path;
		}

		public string GetStagingPathForAliasIconThumbnailLocal () {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_ALIAS_ICON_THUMBNAIL);
			string path = platformPath;

			path = Path.Combine (path, "staging");
			path = Path.Combine (path, "alias");
			path = Path.Combine (path, "aliasIcon.jpg");

			return path;
		}

		public string GetStagingPathForAliasIconThumbnailServer (AliasInfo alias) {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_ALIAS_ICON_THUMBNAIL);
			string path = platformPath;
			path = Path.Combine (path, "alias");
			path = Path.Combine (path, alias.displayName);
			path = Path.Combine (path, "aliasIcon.jpg");

			return path;
		}

		public string GetCachedFilePathForUri(Uri uri) {
			string absoluteUri = uri == null ? "" : uri.AbsoluteUri;
			if (absoluteUri.StartsWith ("file://")) {
				return uri.LocalPath;
			}

			string fileName = null;
			if (!absoluteUri.StartsWith ("addressbook:"))
				fileName = Path.GetFileName (uri.AbsolutePath);
			else {
				// address book thumbnail
				string[] split = absoluteUri.Split (new char[] { '/' });
				fileName = Path.Combine ("addressbook", split [split.Length - 2] + ".jpeg");
			}

			fileName = Path.Combine ("em", fileName);

			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_MEDIA);

			string path = Path.Combine (platformPath, fileName);

			return path;
		}

		public string GetFilePathForChatEntryUri (Uri uri, ChatEntry chatEntry) {
			string absoluteUri = uri == null ? "" : uri.AbsoluteUri;
			if (absoluteUri.StartsWith ("file://"))
				return uri.LocalPath;

			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_CHAT_ENTRY);
			string mediaDir = Path.Combine (platformPath, "media");
			string chatsDir = Path.Combine (mediaDir, "chats");
			string convoDir = Path.Combine (chatsDir, chatEntry.chatEntryID.ToString ());

			// if no ur then just the directory for the chatEntry
			if (uri == null)
				return convoDir;

			string fileName = Path.GetFileName (absoluteUri);
			string fullPath = Path.Combine (convoDir, fileName);

			return fullPath;
		}

		public string GetFilePathForScaledMedia (string filePath, int height) {
			string scaledFilename = height + THUMBNAIL_EXT;
			string fullThumbnailDirectory = GetDirectoryPathForScaledMedia (filePath);
			string fullPath = Path.Combine (fullThumbnailDirectory, scaledFilename);

			return fullPath;
		}

		public string GetDirectoryPathForScaledMedia (string filePath) {
			string parentDirectory = Path.GetDirectoryName (filePath);

			string thumbnailDirFirstPart = DirectoryToEncodedDirectory (parentDirectory);
			string thumbnailDirSecondPart = Path.GetFileName (filePath);
			string thumbnailDir = Path.Combine (thumbnailDirFirstPart, thumbnailDirSecondPart);

			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_CACHE);

			string fullThumbnailDirectory = Path.Combine (platformPath, thumbnailDir);

			return fullThumbnailDirectory;
		}

		public string GetFilePathForStagingMediaDownload (string downloadPath) {
			string downloadPathDirectory = Path.GetDirectoryName (downloadPath);
			string cacheDirectory = DirectoryToEncodedDirectory (downloadPathDirectory);
			string stagingFileName = Path.GetFileName (downloadPath) + DOWNLOAD_EXT;
			string tempPath = Path.Combine (cacheDirectory, stagingFileName);

			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_PATH_CACHE);

			string fullTempPath = Path.Combine (platformPath, tempPath);

			return fullTempPath;
		}

		protected string DirectoryToEncodedDirectory (string directory) {
			string thumbnailDirectory = ByteToHex (Encoding.ASCII.GetBytes (directory));

			if (thumbnailDirectory.Length > this.MaxThumbnailDirectoryLength) {
				int i = thumbnailDirectory.Length - this.MaxThumbnailDirectoryLength;
				thumbnailDirectory = thumbnailDirectory.Substring (i);
			}

			return thumbnailDirectory;
		}

		private string ByteToHex (byte[] bytes) {
			StringBuilder sb = new StringBuilder(bytes.Length * 2);
			foreach (byte b in bytes)
			{
				sb.AppendFormat("{0:x2}", b);
			}

			return sb.ToString();
		}

		public string GetNewMediaFileNameForStagingContents () {
			string stagingFileName = GetRandomFileName ();
			string stagingPath = Path.Combine ("staging", stagingFileName);
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_STAGING_CONTENTS);
			string fullPath = Path.Combine (platformPath, stagingPath);

			return fullPath;
		}

		protected virtual string GetRandomFileName () {
			return Path.GetRandomFileName ();
		}

		public string GetOutgoingQueueTempName () {
			string platformPath = uriplatformResolverStrategy.VirtualPathToPlatformPath (VIRTUAL_PARENT_OUTGOING_QUEUE_STAGING);
			string fullPath = Path.Combine (platformPath, "outgoing-queue");
			fullPath = Path.Combine (fullPath, Path.GetRandomFileName ());

			return fullPath;
		}
	}
}

