using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.Aliases {
	class AliasItemViewModel {
		public IList<AliasItemTemplate> Items { get; private set; }

		public static AliasItemViewModel From (IList<AliasInfo> list) {
			AliasItemViewModel v = new AliasItemViewModel ();
			IList<AliasItemTemplate> items = new List<AliasItemTemplate> ();

			foreach (AliasInfo item in list) {
				items.Add (new AliasItemTemplate (item));
			}

			v.Items = items;
			return v;
		}
	}
}
