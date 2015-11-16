using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Notifications {
	class NotificationItemTemplate {
		private NotificationEntry Entry { get; set; }

		public BitmapImage Image {
			get {
				BitmapImage bm = ImageManager.Shared.GetImage (this.Entry.counterparty);
				return bm;
			}
		}

		public string Text {
			get {
				return this.Entry.Title;
			}
		}

		public NotificationItemTemplate (NotificationEntry entry) {
			this.Entry = entry;
		}
	}
}
