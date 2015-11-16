using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Chat {
	class ChatOutgoingMediaItemTemplate : ChatOutgoingItemTemplate {

		public BitmapImage MediaImage {
			get {
				Media media = this.Message.media;
				BitmapImage bm = ImageManager.Shared.GetImageFromMedia (media);
				return bm;
			}
		}

		public ChatOutgoingMediaItemTemplate (Message message) : base (message) { }
	}
}
