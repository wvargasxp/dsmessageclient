using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.Contacts {
	class AddressBookItemViewModel {
		public IList<AddressBookItemTemplate> Items { get; private set; }

		public static AddressBookItemViewModel From (IList<AggregateContact> list) {
			AddressBookItemViewModel v = new AddressBookItemViewModel ();
			if (list == null) return v;

			IList<AddressBookItemTemplate> items = new List<AddressBookItemTemplate> ();

			foreach (AggregateContact item in list) {
				items.Add (new AddressBookItemTemplate (item));
			}

			v.Items = items;
			return v;
		}
	}
}
