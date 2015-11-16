using System;
using em;

namespace iOS {
	public class iOSUriPlatformResolverStrategy : UriPlatformResolverStrategy {

		public string VirtualPathToPlatformPath (string virtualParentPath) {
			switch (virtualParentPath) {
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_CACHE:
				return Environment.GetFolderPath (Environment.SpecialFolder.InternetCache);
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ACCOUNT_THUMBNAIL:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ACCOUNT_INFO:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_GROUP_THUMBNAIL:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ALIAS_THUMBNAIL:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ALIAS_ICON_THUMBNAIL:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_MEDIA:
			case PlatformUriGenerator.VIRTUAL_PARENT_CHAT_ENTRY:
			case PlatformUriGenerator.VIRTUAL_PARENT_STAGING_CONTENTS:
			case PlatformUriGenerator.VIRTUAL_PARENT_OUTGOING_QUEUE_STAGING:
			default:
				return iOSFileSystemManager_Constants.FOLDER_MYDOCUMENTS;
			}
		}
	}
}

