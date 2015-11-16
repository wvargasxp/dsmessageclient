using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace em {
	public class GroupUpdateOutbound {
		public string name { get; set; }
		public string serverID { get; set; }
		public string fromAlias { get; set; }
		public IList<GroupMember> members { get; set; }
		public JToken attributes { get; set; }

		public GroupUpdateOutbound () {
		}
	}
}

