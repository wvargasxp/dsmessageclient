using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Chat {
	abstract class ChatItemTemplate {
		public abstract BitmapImage ThumbnailImage { get; }
		public abstract string SenderName { get; }

		public string MessageContent {
			get {
				return this.Message.message;
			}
		}

		protected Message Message { get; set; }

		public ChatItemTemplate (Message message) {
			this.Message = message;
		}
	}
}
