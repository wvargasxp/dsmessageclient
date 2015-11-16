using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsDesktop.Aliases;
using em;

namespace WindowsDesktop.Chat.PickingFromAlias {
	class FromAliasItemViewModel {
		public IList<FromAliasItemTemplate> Items { get; private set; }

		public static FromAliasItemViewModel From (IList<AliasInfo> list) {
			FromAliasItemViewModel v = new FromAliasItemViewModel ();
			IList<FromAliasItemTemplate> items = new List<FromAliasItemTemplate> ();

			foreach (AliasInfo item in list) {
				items.Add (new FromAliasItemTemplate (item));
			}

			AccountInfo accountInfo = App.Instance.Model.account.accountInfo;
			items.Add (new FromAliasItemTemplate (accountInfo));

			v.Items = items;
			return v;
		}

		public AliasInfo InfoFromIndex (int index) {
			if (index >= this.Items.Count || index < 0) {
				return null;
			} else {
				FromAliasItemTemplate t = this.Items [index];
				return t.Counterparty as AliasInfo;
			}
		}
	}
}
