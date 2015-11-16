using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.Notifications {
	class NotificationItemViewModel {
		public IList<NotificationItemTemplate> Items { get; private set; }

		public static NotificationItemViewModel From (IList<NotificationEntry> entries) {
			NotificationItemViewModel v = new NotificationItemViewModel ();

			if (entries == null) return v;

			IList<NotificationItemTemplate> items = new List<NotificationItemTemplate> ();

			foreach (NotificationEntry entry in entries) {
				items.Add (new NotificationItemTemplate (entry));
			}

			v.Items = items;
			return v;
		}
	}
}
