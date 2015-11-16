using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;

namespace WindowsDesktop.Groups {
	class EditGroupItemViewModel {
		public IList<EditGroupItemTemplate> Items { get; private set; }

		public static EditGroupItemViewModel From (IList<Contact> list) {
			EditGroupItemViewModel v = new EditGroupItemViewModel ();

			if (list == null) return v;

			IList<EditGroupItemTemplate> items = new List<EditGroupItemTemplate> ();

			foreach (Contact item in list) {
				items.Add (new EditGroupItemTemplate (item));
			}

			v.Items = items;
			return v;
		}
	}
}
