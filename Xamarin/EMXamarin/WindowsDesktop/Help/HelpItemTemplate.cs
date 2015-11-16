using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.Help {
	class HelpItemTemplate {
		private HelpInfo HelpInfo { get; set; }

		public HelpItemTemplate (HelpInfo entry) {
			this.HelpInfo = entry;
		}

		public string Text {
			get {
				return this.HelpInfo.Title;
			}
		}
	}
}
