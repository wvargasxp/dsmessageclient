using System;

namespace em {
	public enum MediaState {
		Unknown, // Unknown, needs to be resolved
		Absent, // Not on the filesystem
		Downloading, // Not on the filesystem, downloading
		Present, // On Filesystem
		FailedDownload, // Not on the filesystem, download failed
		Uploading, // On Filesystem, in the process of uploading
		FailedUpload, // On Filesystem, failed to upload
		Encoding // On Filesystem, encoding (video)
	}
}