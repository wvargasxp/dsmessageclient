using em;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Aliases {
	class AliasItemTemplate : BasicEmRowTemplate {
		public AliasItemTemplate (AliasInfo alias) : base (alias) {
			// Debug.Assert (alias is AliasInfo, "Expected type AliasInfo");
		}
	}
}
