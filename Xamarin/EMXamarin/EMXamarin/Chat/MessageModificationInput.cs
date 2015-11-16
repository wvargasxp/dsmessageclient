using Newtonsoft.Json.Linq;

namespace em {
	public class MessageModificationInput {
		public string messageGUID { get; set; }
		public string toAlias { get; set; }
		public string message { get; set; }
		public string mediaRef { get; set; }
		public string contentType { get; set; }
		public string messageLifecycle { get; set; }
		public JToken attributes { get; set; }
	}
}