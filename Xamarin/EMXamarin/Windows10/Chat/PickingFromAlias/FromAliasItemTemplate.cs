using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsDesktop.Utility;
using em;

namespace WindowsDesktop.Chat.PickingFromAlias {
	class FromAliasItemTemplate : BasicEmRowTemplate {
		public FromAliasItemTemplate (AliasInfo info) : base (info) { }
		public FromAliasItemTemplate (AccountInfo info) : base (info) { }
	}
}
