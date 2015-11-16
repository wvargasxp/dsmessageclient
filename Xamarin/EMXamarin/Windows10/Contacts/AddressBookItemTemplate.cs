using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsDesktop.Utility;

namespace WindowsDesktop.Contacts {
	class AddressBookItemTemplate : BasicEmRowTemplate {
		public AddressBookItemTemplate (AggregateContact contact) : base (contact.ContactForDisplay) { }
	}
}
