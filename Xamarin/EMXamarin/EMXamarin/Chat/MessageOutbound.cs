using Newtonsoft.Json.Linq;

namespace em {
	public class MessageOutbound {
		public string fromAlias { get; set; }
		public string[] destination { get; set; }
		public string messageGUID { get; set; }
		public string message { get; set; }
		public string mediaRef { get; set; }
		public string contentType { get; set; }
		public JToken attributes { get; set; }
		public string sentDate { get; set; }
	}
}