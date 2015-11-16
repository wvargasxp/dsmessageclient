using System.Collections.Generic;

namespace em {
	public class RegisterContactsResponse {
		public int contactsVersion { get; set; }
		public IList<GroupInput> contacts { get; set; }
	}
}