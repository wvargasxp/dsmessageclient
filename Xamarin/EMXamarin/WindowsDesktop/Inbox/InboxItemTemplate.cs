using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Inbox {
	class InboxItemTemplate {

		public ChatEntry ChatEntry { get; set; }

		public InboxItemTemplate (ChatEntry entry) {
			this.ChatEntry = entry;
		}

		public string ContactsLabel {
			get {
				return this.ChatEntry.ContactsLabel;
			}
		}

		public string Preview {
			get {
				return this.ChatEntry.preview;
			}
		}

		public BitmapImage Image {
			get {
				BitmapImage bm = ImageManager.Shared.GetImage (this.ChatEntry.FirstContactCounterParty);
				return bm;
			}
		}
	}
}