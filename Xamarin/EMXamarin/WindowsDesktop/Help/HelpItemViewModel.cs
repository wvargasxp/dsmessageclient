using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.Help {
	class HelpItemViewModel {
		public IList<HelpItemTemplate> Items { get; private set; }

		public static HelpItemViewModel From (IList<HelpInfo> list) {
			HelpItemViewModel v = new HelpItemViewModel ();

			if (list == null) return v;

			IList<HelpItemTemplate> items = new List<HelpItemTemplate> ();

			foreach (HelpInfo item in list) {
				items.Add (new HelpItemTemplate (item));
			}

			v.Items = items;
			return v;
		}
	}
}
