namespace em {

	public enum ContentType {
		Bitmap,
		Jpeg,
		Png,
		Gif,
		Html,
		Xml,
		Quicktime,
		Mpeg,
		Mp4Video,
		Mp4audio,
		M3u8,
		Ts,
		H264,
		_3gppVideo,
		Mp3,
		Aac,
		Amr,
		Ac3,
		Ogg, 
		_3gppAudio,
		Json,
		Unknown
	}

	public static class ContentTypeHelper {

		public static ContentType FromString (string ct) {
			if (string.IsNullOrEmpty (ct))
				return ContentType.Unknown;
			
			switch(ct) {
			case "image/bmp":
				return ContentType.Bitmap;
			case "image/jpeg":
			case "image/jpg":
				return ContentType.Jpeg;
			case "image/png":
				return ContentType.Png;
			case "image/gif":
				return ContentType.Gif;
			case "text/html":
				return ContentType.Html;
			case "application/xml":
			case "text/xml":
				return ContentType.Xml;
			case "video/quicktime":
				return ContentType.Quicktime;
			case "video/mpeg":
				return ContentType.Mpeg;
			case "video/mp4":
				return ContentType.Mp4Video;
			case "audio/mp4":
				return ContentType.Mp4audio;
			case "application/x-mpegURL":
				return ContentType.M3u8;
			case "video/MP2T":
				return ContentType.Ts;
			case "video/H264":
				return ContentType.H264;
			case "video/3gpp":
				return ContentType._3gppVideo;
			case "audio/mpeg3":
			case "audio/mpeg":
				return ContentType.Mp3;
			case "audio/aac":
				return ContentType.Aac;
			case "audio/amr":
				return ContentType.Amr;
			case "audio/ac3":
				return ContentType.Ac3;
			case "audio/ogg":
				return ContentType.Ogg;
			case "audio/3gpp":
				return ContentType._3gppAudio;
			case "application/json":
			case "text/json":
				return ContentType.Json;
			default:
				return ContentType.Unknown;
			}
		}

		public static bool IsPhoto (ContentType ct) {
			switch (ct) {

			case ContentType.Bitmap:
			case ContentType.Jpeg:
			case ContentType.Png:
			case ContentType.Gif:
				return true;

			default:
				return false;
			}
		}

		public static bool IsPhoto (string path) {
			bool photo = path.EndsWith(".bmp") || path.EndsWith (".jpeg") || path.EndsWith (".jpg") || path.EndsWith (".png") || path.EndsWith(".gif");
			return photo;
		}

		public static bool IsVideo (ContentType ct) {
			switch(ct) {

			case ContentType.Quicktime:
			case ContentType.Mpeg:
			case ContentType.Mp4Video:
			case ContentType.M3u8:
			case ContentType.Ts:
			case ContentType.H264:
			case ContentType._3gppVideo:
				return true;

			default:
				return false;
			}
		}

		public static bool IsVideo (string path) {
			bool video = path.EndsWith (".mp4") || path.EndsWith (".mov") || path.EndsWith (".qt")
						 || path.EndsWith (".mpg") || path.EndsWith (".mpeg") || path.EndsWith(".mpeg4") || path.EndsWith (".m3u") || path.EndsWith (".m3u8")
			             || path.EndsWith (".ts") || path.EndsWith (".tsa") || path.EndsWith (".tsv") || path.EndsWith (".264") || path.EndsWith (".h264") || path.EndsWith (".3gp");
			return video;
		}

		public static bool IsAudio (ContentType ct) {
			switch(ct) {

			case ContentType.Mp4audio:
			case ContentType.Mp3:
			case ContentType.Aac:
			case ContentType.Amr:
			case ContentType.Ac3:
			case ContentType.Ogg:
			case ContentType._3gppAudio:
				return true;

			default:
				return false;
			}
		}

		public static bool IsAudio (string path) {
			bool audio = path.EndsWith (".3gpp") || path.EndsWith (".aac") || path.EndsWith (".amr") || path.EndsWith(".ac3") || path.EndsWith(".ogg") || path.EndsWith(".mp3");
			return audio;
		}

		public static string MimeToExtension (string mimeType) {
			int indexOf = mimeType.LastIndexOf ("/");
			string extension = "";
			if (indexOf != -1) {
				extension = mimeType.Substring (indexOf + 1);
				// The bitmap drawing code uses file extension to differentiate between
				// mime types. It treats .mpeg as video, so we just treat it as mp3 file here.
				if (extension.ToLower ().Equals ("mpeg")) {
					extension = "mp3";
				}
			}
			return extension;
		}

		/**
		 * Old school way to check when we relied on file extensions to tell us what the content type is
		 */
		public static string GetContentTypeFromPath (string path) {
			if (path.EndsWith (".bmp"))
				return "image/bmp";

			if (path.EndsWith (".jpg") || path.EndsWith(".jpeg"))
				return "image/jpeg";

			if (path.EndsWith (".png"))
				return "image/png";

			if (path.EndsWith (".gif"))
				return "image/gif";

			if (path.EndsWith (".mov") || path.EndsWith(".qt"))
				return "video/quicktime";

			if (path.EndsWith (".mpg") || path.EndsWith(".mpeg"))
				return "video/mpeg";

			if (path.EndsWith (".mp4"))
				return "video/mp4"; //this could be audio/mp4 as well...

			if (path.EndsWith (".m3u") || path.EndsWith(".m3u8"))
				return "application/x-mpegURL";

			if (path.EndsWith (".ts") || path.EndsWith(".tsa") || path.EndsWith(".tsv"))
				return "video/MP2T";

			if (path.EndsWith (".264") || path.EndsWith(".h264"))
				return "video/H264";

			if (path.EndsWith (".3gp"))
				return "video/3gpp";

			if (path.EndsWith (".mp3"))
				return "audio/mpeg";

			if (path.EndsWith (".aac"))
				return "audio/aac";

			if (path.EndsWith (".amr"))
				return "audio/amr";

			if (path.EndsWith (".ac3"))
				return "audio/ac3";

			if (path.EndsWith (".ogg"))
				return "audio/ogg";

			if (path.EndsWith (".3gpp"))
				return "audio/3gpp";

			System.Diagnostics.Debug.WriteLine ("Unknown file extension. Returning text/plain content type. " + path);
			
			return "text/plain";
		}

		public static ContentType FromMessage (Message message) {
			ContentType type = ContentType.Unknown;

			if(message != null) {
				if (message.contentType != null) {
					// handle case where the message has a content type field.
					// Newer messages store this field.
					type = ContentTypeHelper.FromString (message.contentType);
				}

				if (type == ContentType.Unknown) {
					// handle case where the content type isn't specified by the message. 
					// Older version of the message model do not specify a content type 
					// as the field is a recent addition to the message model.
					Media media = message.media;
					if (media != null) {
						string contentTypeString = ContentTypeHelper.GetContentTypeFromPath (media.uri.AbsolutePath);
						type = ContentTypeHelper.FromString (contentTypeString);
					}
				}
			}

			return type;
		}
	}
}