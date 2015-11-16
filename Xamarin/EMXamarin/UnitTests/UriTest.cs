using System;
using NUnit.Framework;

namespace em {

	[TestFixture]
	public class UriTest {
		UriPlatformResolverStrategy mockStrategy = new UriResolverStrategyMock();
		PlatformUriGenerator uriGenerator;

		[SetUp]
		public void setUp() {
			uriGenerator = new TestPlatformUriGenerator (mockStrategy);
		}

		[Test]
		public void TestAccountUri () {
			string accountInfoFilePath = uriGenerator.GetFilePathForAccountInfo ();
			Assert.AreEqual ("APPLICATION_FOLDER/account/accountInfo.json", accountInfoFilePath);

			string thumbnailPath = uriGenerator.GetStagingPathForAccountInfoThumbnailLocal ();
			Assert.AreEqual ("APPLICATION_FOLDER/account/accountInfoThumbnail.jpg", thumbnailPath);
		}

		[Test]
		public void TestGroupUri () {
			Group mockGroup = new Group ();
			mockGroup.serverID = "PR:1001:97";

			string oldThumbnailPath = uriGenerator.GetStagingPathForGroupThumbnailLocal ();
			Assert.AreEqual ("APPLICATION_FOLDER/staging/group/groupthumbnail.jpg", oldThumbnailPath);

			string newThumbnailPath = uriGenerator.GetStagingPathForGroupThumbnailServer (mockGroup);
			Assert.AreEqual ("APPLICATION_FOLDER/group/PR:1001:97/groupthumbnail.jpg", newThumbnailPath);
		}

		[Test]
		public void TestAliasUri () {
			AliasInfo mockAlias = new AliasInfo ();
			mockAlias.displayName = "myalias";

			string oldThumbnailPath = uriGenerator.GetStagingPathForAliasThumbnailLocal ();
			Assert.AreEqual ("APPLICATION_FOLDER/staging/alias/aliasThumbnail.jpg", oldThumbnailPath);

			string newThumbnailPath = uriGenerator.GetStagingPathForAliasThumbnailServer (mockAlias);
			Assert.AreEqual ("APPLICATION_FOLDER/alias/myalias/aliasThumbnail.jpg", newThumbnailPath);

			string oldIconPath = uriGenerator.GetStagingPathForAliasIconThumbnailLocal ();
			Assert.AreEqual ("APPLICATION_FOLDER/staging/alias/aliasIcon.jpg", oldIconPath);

			string newIconPath = uriGenerator.GetStagingPathForAliasIconThumbnailServer (mockAlias);
			Assert.AreEqual ("APPLICATION_FOLDER/alias/myalias/aliasIcon.jpg", newIconPath);
		}

		[Test]
		public void TestCacheFileUri () {

			// Addressbook Uri
			AssertEqualsCacheFileUri ("APPLICATION_FOLDER/em/addressbook/101.jpeg", "addressbook://" + "101" + "/thumbnail");
			AssertEqualsCacheFileUri ("APPLICATION_FOLDER/em/addressbook/101.jpeg", "addressbook://" + "101" + "/thumbnail?cr=" + "ESCAPED_URI_STRING");

			// Local Uri
			AssertEqualsCacheFileUri ("/there/3weff.jpg", "file://chats/there/3weff.jpg");

			// Remote Uri
			AssertEqualsCacheFileUri ("APPLICATION_FOLDER/em/6-sentPhoto47f83190-caca-11e4-ac1f-3c1900001fbd.jpeg", "http://localhost:8080/file/6-sentPhoto47f83190-caca-11e4-ac1f-3c1900001fbd.jpeg");
		}

		private void AssertEqualsCacheFileUri (string cachePath, string uriPath) {
			Uri uri = new Uri (uriPath);
			string thumbnailPath = uriGenerator.GetCachedFilePathForUri (uri);
			Assert.AreEqual (cachePath, thumbnailPath);
		}

		[Test]
		public void TestChatEntryUri () {
			ChatEntry chatEntryMock = new ChatEntry ();
			chatEntryMock.chatEntryID = 1001;

			AssertEqualsChatEntryUri ("APPLICATION_FOLDER/media/chats/1001", null, chatEntryMock);
			AssertEqualsChatEntryUri ("/local/folder/somefile.jpeg", "file://some/local/folder/somefile.jpeg", chatEntryMock);
			AssertEqualsChatEntryUri ("APPLICATION_FOLDER/media/chats/1001/6-sentPhoto47f83190-caca-11e4-ac1f-3c1900001fbd.jpeg", "http://localhost:8080/file/6-sentPhoto47f83190-caca-11e4-ac1f-3c1900001fbd.jpeg", chatEntryMock);
		}

		private void AssertEqualsChatEntryUri (string finalPath, string path, ChatEntry entry) {
			Uri uri = path == null ? null : new Uri (path);
			string uriPath = uriGenerator.GetFilePathForChatEntryUri (uri, entry);
			Assert.AreEqual (finalPath, uriPath);
		}

		[Test]
		public void TestScaledThumbnailUri () {
			int heightMock = 10;
			AssertEqualsScaledThumbnailUri ("CACHE_FOLDER/63616c2f666f6c646572/somefile.jpeg/10.thumb", "/local/folder/somefile.jpeg", heightMock);
			AssertEqualsScaledThumbnailUri ("CACHE_FOLDER/63616c2f666f6c646572/someotherfile.jpeg/10.thumb", "/local/folder/someotherfile.jpeg", heightMock);

			int heightMock2 = 200;
			AssertEqualsScaledThumbnailUri ("CACHE_FOLDER/63616c2f666f6c646572/somefile.jpeg/200.thumb", "/local/folder/somefile.jpeg", heightMock2);


			AssertEqualsScaledThumbnailUri ("CACHE_FOLDER/63686174732f31303031/6-sentPhoto47f83190-caca-11e4-ac1f-3c1900001fbd.jpeg/10.thumb", "APPLICATION_FOLDER/media/chats/1001/6-sentPhoto47f83190-caca-11e4-ac1f-3c1900001fbd.jpeg", heightMock);
		}

		[Test]
		public void TestScaledThumbnailUriMaxDirectoryLength () {
			int heightMock = 10;

			WhenMaxThumbnailDirectoryLengthIs (5);
			AssertEqualsScaledThumbnailUri ("CACHE_FOLDER/46572/somefile.jpeg/10.thumb", "/local/folder/somefile.jpeg", heightMock);
		}

		private void WhenMaxThumbnailDirectoryLengthIs (int length) {
			var pug = (PlatformUriGenerator) uriGenerator;
			pug.MaxThumbnailDirectoryLength = length;
		}

		private void AssertEqualsScaledThumbnailUri (string finalPath, string pathToTest, int height) {
			string uriPath = uriGenerator.GetFilePathForScaledMedia (pathToTest, height);
			Assert.AreEqual (finalPath, uriPath);
		}

		[Test]
		public void TestScaledThumbnailDirectory () {
			AssertEqualsScaledThumbnailDirectory ("CACHE_FOLDER/63616c2f666f6c646572/somefile.jpeg", "/local/folder/somefile.jpeg");
			AssertEqualsScaledThumbnailDirectory ("CACHE_FOLDER/63616c2f666f6c646572/someotherfile.jpeg", "/local/folder/someotherfile.jpeg");
		}

		private void AssertEqualsScaledThumbnailDirectory (string finalPath, string pathToTest) {
			string directoryPath = uriGenerator.GetDirectoryPathForScaledMedia (pathToTest);
			Assert.AreEqual (finalPath, directoryPath);
		}

		[Test]
		public void TestsStagingPathForMediaDownloads () {
			AssertEqualsStagingPathForMediaDownloads ("CACHE_FOLDER/63616c2f666f6c646572/somefile.jpeg.download", "/local/folder/somefile.jpeg");
			AssertEqualsStagingPathForMediaDownloads ("CACHE_FOLDER/63686174732f31303031/6-sentPhoto47f83190-caca-11e4-ac1f-3c1900001fbd.jpeg.download", "APPLICATION_FOLDER/media/chats/1001/6-sentPhoto47f83190-caca-11e4-ac1f-3c1900001fbd.jpeg");
		}

		private void AssertEqualsStagingPathForMediaDownloads (string finalPath, string downloadPath) {
			string stagingPath = uriGenerator.GetFilePathForStagingMediaDownload (downloadPath);

			Assert.AreEqual (finalPath, stagingPath);
		}

		[Test]
		public void TestGetNewMediaFileNameForStagingContents () {
			Assert.AreEqual ("APPLICATION_FOLDER/staging/aRandomFileName", uriGenerator.GetNewMediaFileNameForStagingContents ());
		}

		private class UriResolverStrategyMock : UriPlatformResolverStrategy {
			public string VirtualPathToPlatformPath (string virtualParentPath) {
				switch (virtualParentPath) {
				case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ACCOUNT_INFO:
				case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ACCOUNT_THUMBNAIL:
				case PlatformUriGenerator.VIRTUAL_PARENT_PATH_GROUP_THUMBNAIL:
				case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ALIAS_THUMBNAIL:
				case PlatformUriGenerator.VIRTUAL_PARENT_PATH_ALIAS_ICON_THUMBNAIL:
				case PlatformUriGenerator.VIRTUAL_PARENT_CHAT_ENTRY:
				case PlatformUriGenerator.VIRTUAL_PARENT_PATH_MEDIA:
				case PlatformUriGenerator.VIRTUAL_PARENT_STAGING_CONTENTS:
					return "APPLICATION_FOLDER";
				case PlatformUriGenerator.VIRTUAL_PARENT_PATH_CACHE:
					return "CACHE_FOLDER";
				}

				return "DEFAULT_FOLDER";
			}
		}

		private class TestPlatformUriGenerator : PlatformUriGenerator {
			public TestPlatformUriGenerator (UriPlatformResolverStrategy strategy) : base (strategy) {
			}

			protected override string GetRandomFileName () {
				return "aRandomFileName";
			}
		}
	}
}

