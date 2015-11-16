using Newtonsoft.Json.Linq;

namespace em {
	public class ContactInput {
		public string clientID { get; set; }
		public string serverID { get; set; }
		public string displayName { get; set; }
		public string thumbnailURL { get; set; }
		public string toAlias { get; set; }
		public bool group { get; set; }
		public bool me { get; set; }
		public JToken attributes { get; set; }
		public bool requiresAlias { get; set; }
		public string label { get; set; }
		public string description { get; set; }
		public string lifecycle { get; set; }
		public string addressBookLifeCycle { get; set; }
		public string groupMemberLifecycle { get; set; }
		public string identifierType { get; set; }
		public string phoneNumberType { get; set; }
		public bool preferredContact { get; set; }
		public string contactID { get; set; }

		public string memberStatus { get; set; }
		public string blockStatus { get; set; }
	}
}