using System;

namespace em {
	public interface IUriGenerator {
		string GetFilePathForAccountInfo ();
		string GetStagingPathForAccountInfoThumbnailLocal ();
		string GetStagingPathForGroupThumbnailLocal ();
		string GetStagingPathForGroupThumbnailServer (Group group);
		string GetStagingPathForAliasThumbnailLocal ();
		string GetStagingPathForAliasThumbnailServer (AliasInfo alias);
		string GetStagingPathForAliasIconThumbnailLocal ();
		string GetStagingPathForAliasIconThumbnailServer (AliasInfo alias);
		string GetCachedFilePathForUri(Uri uri);
		string GetFilePathForChatEntryUri (Uri uri, ChatEntry chatEntry);
		string GetFilePathForScaledMedia (string filePath, int height);
		string GetDirectoryPathForScaledMedia (string filePath);
		string GetFilePathForStagingMediaDownload (string finalPath);
		string GetNewMediaFileNameForStagingContents ();
		string GetOutgoingQueueTempName ();
	}
}