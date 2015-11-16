using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsDesktop.Extensions;
using em;

namespace WindowsDesktop.Utility {
	public class ImageManager {
		private static ImageManager _shared = null;
		public static ImageManager Shared {
			get {
				if (_shared == null) {
					_shared = new ImageManager ();
				}

				return _shared;
			}
		}

		public BitmapImage GetImage (CounterParty counterparty) {
			if (counterparty == null) return null;
			// todo
			return GetImageFromMedia (counterparty.media);
		}

		public BitmapImage GetImageFromMedia (Media media) {
			if (media == null) return null;
			return media.LoadThumbnail ();
		}
	}
}
