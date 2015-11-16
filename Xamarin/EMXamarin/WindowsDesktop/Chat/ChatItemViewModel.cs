using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.Chat {
	class ChatItemViewModel {
		public IList<ChatItemTemplate> Items { get; private set; }

		public static ChatItemViewModel From (IList<Message> list) {
			if (list == null) return new ChatItemViewModel ();

			ChatItemViewModel v = new ChatItemViewModel ();
			IList<ChatItemTemplate> items = new List<ChatItemTemplate> ();

			foreach (Message item in list) {
				ChatItemTemplate t = TemplateForMessage (item);
				items.Add (t);
			}

			v.Items = items;
			return v;
		}

		private static ChatItemTemplate TemplateForMessage (Message message) {
			ChatItemTemplate template = null;
			if (message.IsInbound ()) {
				if (message.HasMedia ()) {
					template = new ChatIncomingMediaItemTemplate (message);
				} else {
					template = new ChatIncomingItemTemplate (message);
				}
			} else {
				if (message.HasMedia ()) {
					template = new ChatOutgoingMediaItemTemplate (message);
				} else {
					template = new ChatOutgoingItemTemplate (message);
				}
			}

			return template;
		}
	}
}
