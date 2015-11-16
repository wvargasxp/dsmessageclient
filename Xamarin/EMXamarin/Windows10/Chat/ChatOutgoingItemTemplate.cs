using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsDesktop.Utility;
using em;

namespace WindowsDesktop.Chat {
	class ChatOutgoingItemTemplate : ChatItemTemplate {
		public override BitmapImage ThumbnailImage {
			get {
				BitmapImage bm = ImageManager.Shared.GetImage (this.Message.chatEntry.SenderCounterParty);
				return bm;
			}
		}

		public override string SenderName {
			get {
				return this.Message.chatEntry.SenderName;
			}
		}

		public ChatOutgoingItemTemplate (Message message) : base (message) { }
	}
}
