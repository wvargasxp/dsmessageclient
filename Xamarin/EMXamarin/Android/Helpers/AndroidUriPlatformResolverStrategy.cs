using System;
using System.IO;
using em;

namespace Emdroid {
	public class AndroidUriPlatformResolverStrategy : UriPlatformResolverStrategy {

		public string VirtualPathToPlatformPath (string virtualParentPath) {
			switch (virtualParentPath) {
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_CACHE:
				return EMApplication.GetInstance ().CacheDir.AbsolutePath;
			case PlatformUriGenerator.VIRTUAL_PARENT_OUTGOING_QUEUE_STAGING:
				return Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ACCOUNT_INFO:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ACCOUNT_THUMBNAIL:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_GROUP_THUMBNAIL:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ALIAS_THUMBNAIL:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ALIAS_ICON_THUMBNAIL:
			case PlatformUriGenerator.VIRTUAL_PARENT_PATH_MEDIA:
			case PlatformUriGenerator.VIRTUAL_PARENT_CHAT_ENTRY:
			case PlatformUriGenerator.VIRTUAL_PARENT_STAGING_CONTENTS:
			default:
				return Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);

			}
		}


	}
}

