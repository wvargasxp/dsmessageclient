using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;

using System.IO;
using System.Diagnostics;

namespace Windows10.PlatformImpl
{
    class WindowsDesktopFileSystemManager : IFileSystemManager    
    {
        private PlatformFactory PlatformFactory { get; set; }

        public WindowsDesktopFileSystemManager(PlatformFactory platformFactory)
        {
            this.PlatformFactory = platformFactory;
        }

        public void CreateParentDirectories(string path)
        {
            string parentDirectory = Path.GetDirectoryName(path);
            if (!Directory.Exists(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }
        }

        public string GetSystemPathForFolder(string folderName)
        {
            throw new NotImplementedException();
        }

        public string ResolveSystemPathForUri(string virtualPath)
        {
            var systemPath = virtualPath;
            return systemPath;
        }

        public void RemoveFilesForChatEntry(ChatEntry chatEntry)
        {
            string path = this.PlatformFactory.GetUriGenerator().GetFilePathForChatEntryUri(null, chatEntry);

            try
            {
                foreach (string file in Directory.EnumerateFiles(path))
                {
                    if (file != null)
                        File.Delete(file);
                }
                Directory.Delete(path);
            }
            catch (DirectoryNotFoundException)
            {
                // okay, just means no files were saved
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception removing files for chat entry: " + e);
            }
        }

        public string GetFilePathForSharingAlias(AliasInfo alias)
        {
            throw new NotImplementedException();
        }

        public System.IO.Stream GetReadOnlyFileStreamFromPath(string path)
        {
            Stream fileStream = File.OpenRead(path);
            return fileStream;
        }

        public void RemoveFileAtPath(string path)
        {
            if (path != null)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                InvalidateFileCacheForScaledMedia(path);
            }
        }

        public void InvalidateFileCacheForScaledMedia(string path)
        {
            if (path == null)
                return;

            string thumbnailDir = this.PlatformFactory.GetUriGenerator().GetDirectoryPathForScaledMedia(path);
            if (Directory.Exists(thumbnailDir))
            {
                foreach (string f in Directory.EnumerateFiles(thumbnailDir))
                    File.Delete(f);
            }
        }

        public void CopyBytesToPath(string path, System.IO.Stream incomingStream, Action<double> completionCallback)
        {
            CopyBytesToPath(path, incomingStream, FileSystemManager_Constants.UNKNOWN_BYTE_LENGTH, completionCallback);
        }

        public void CopyBytesToPath(string path, System.IO.Stream incomingStream, long expectedLength, Action<double> completionCallback)
        {
            CreateParentDirectories(path);

            try
            {
                FileMode fm = File.Exists(path) ? FileMode.Append : FileMode.Create;
                using (var writeStream = new FileStream(path, fm, FileAccess.Write))
                {
                    using (var writeBinay = new BinaryWriter(writeStream))
                    {
                        long totalRead = 0;
                        if (completionCallback != null)
                            completionCallback(0);

                        var buffer = new byte[Constants.LARGE_COPY_BUFFER];
                        int bytesRead = incomingStream.Read(buffer, 0, buffer.Length);
                        while (bytesRead > 0)
                        {
                            writeBinay.Write(buffer, 0, bytesRead);
                            if (expectedLength != FileSystemManager_Constants.UNKNOWN_BYTE_LENGTH)
                            {
                                totalRead += bytesRead;
                                if (completionCallback != null)
                                    completionCallback((double)totalRead / (double)expectedLength);
                            }

                            bytesRead = incomingStream.Read(buffer, 0, buffer.Length);
                        }
                    }
                }

                // if we don't know the length, indicate completion
                if (expectedLength == FileSystemManager_Constants.UNKNOWN_BYTE_LENGTH && completionCallback != null)
                    completionCallback(1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            Debug.WriteLine("finish saving downloaded file to disk: " + path);
        }

        public void MoveFileAtPath(string srcPath, string destPath)
        {
            try
            {
                if (File.Exists(srcPath))
                {
                    CreateParentDirectories(destPath);
                    RemoveFileAtPath(destPath);
                    File.Move(srcPath, destPath);
                }
            }
            catch (Exception e)
            {
                throw new IOException(String.Format("issue moving file {0} to {1}. {2}", srcPath, destPath, e));
            }
        }

        public void CopyFileAtPath(string srcPath, string destPath)
        {
            try
            {
                if (File.Exists(srcPath))
                {
                    CreateParentDirectories(destPath);
                    RemoveFileAtPath(destPath);
                    File.Copy(srcPath, destPath);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(String.Format("issue copying file {0} to {1}. {2}", srcPath, destPath, e));
            }
        }

        public byte[] ContentsOfFileAtPath(string path)
        {
            if (File.Exists(path))
                return File.ReadAllBytes(path);
            return null;
        }

        public bool FileExistsAtPath(string path)
        {
            string systemPath = ResolveSystemPathForUri(path);
            return File.Exists(systemPath);
        }

        public string MoveStagingContentsToFileStore(string stagingPath, ChatEntry chatEntry)
        {
            string filename = Path.GetFileName(stagingPath);

            string spoofed = "http://foo.com/" + filename;
            string finalPath = this.PlatformFactory.GetUriGenerator().GetFilePathForChatEntryUri(new Uri(spoofed), chatEntry);

            CreateParentDirectories(finalPath);

            File.Move(stagingPath, finalPath);
            return finalPath;
        }


        public void CopyBytesToPath(string path, byte[] incomingBytes, Action<double> progressCallback)
        {
            using (MemoryStream incomingStream = new MemoryStream(incomingBytes))
            {
                long expectedLength = incomingStream.Length;
                CopyBytesToPath(path, incomingStream, expectedLength, progressCallback);
            }
        }

        public string MoveStagingContentsToFileStore(string stagingPath, Message message)
        {
            string finalPath = message.LocalPathFromMessageGUID(stagingPath);

            string systemStagingPath = ResolveSystemPathForUri(stagingPath);
            string systemFinalPath = ResolveSystemPathForUri(finalPath);

            this.CreateParentDirectories(systemFinalPath);

            File.Move(systemStagingPath, systemFinalPath);

            return finalPath;
        }

        public string GetFilePathForNotificationCenterMedia(string guid)
        {
            return null;
        }
    }
}
