using em;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WindowsDesktop.Extensions {
	public static class WindowsDesktop_Media_ImageExtension {
		public static BitmapImage LoadThumbnail (this Media media) {
			if (media == null) return null;

			ApplicationModel appModel = App.Instance.Model;
			PlatformFactory platformFactory = appModel.platformFactory;
			MediaManager mediaManager = appModel.mediaManager;

			BitmapImage thumbnail = media.GetThumbnail<BitmapImage> ();
			if (thumbnail != null) {
				return thumbnail;
			}

			mediaManager.ResolveState (media);
			if (mediaManager.MediaOnFileSystem (media)) {
				string systemPath = media.GetPathForUri (platformFactory);


				// todo
				// handle other media
				if (systemPath.Contains (".aac") || systemPath.Contains (".mp4") || systemPath.Contains (".3gp")) {
					return null;
				}

				Uri uri = new Uri (systemPath);
				thumbnail = new BitmapImage (uri);
				if (thumbnail != null) {
					media.SetThumbnail<BitmapImage> (thumbnail);
					return thumbnail;
				}
			}

			media.DownloadMedia (appModel);
			return null;
		}
	}
}
