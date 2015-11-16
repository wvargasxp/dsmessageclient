using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Chat {
	class ChatIncomingItemTemplate : ChatItemTemplate {
		public override BitmapImage ThumbnailImage {
			get {
				BitmapImage bm = ImageManager.Shared.GetImage (this.Message.fromContact);
				return bm;
			}
		}

		public override string SenderName {
			get {
				return this.Message.fromContact.displayName;
			}
		}

		public ChatIncomingItemTemplate (Message message) : base (message) { }
	}
}
