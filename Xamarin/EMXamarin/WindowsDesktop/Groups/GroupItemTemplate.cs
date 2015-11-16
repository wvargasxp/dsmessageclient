using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using em;
using System.Windows.Media.Imaging;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Groups {
	class GroupItemTemplate : BasicEmRowTemplate {
		public GroupItemTemplate (Group contact) : base (contact) { }
	}
}