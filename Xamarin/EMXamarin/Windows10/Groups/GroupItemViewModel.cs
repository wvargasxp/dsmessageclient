using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.Groups {
	class GroupItemViewModel {
		public IList<GroupItemTemplate> Items { get; private set; }

		public static GroupItemViewModel From (IList<Group> list) {
			GroupItemViewModel v = new GroupItemViewModel ();

			if (list == null) return v;

			IList<GroupItemTemplate> items = new List<GroupItemTemplate> ();

			foreach (Group item in list) {
				items.Add (new GroupItemTemplate (item));
			}

			v.Items = items;
			return v;
		}
	}
}