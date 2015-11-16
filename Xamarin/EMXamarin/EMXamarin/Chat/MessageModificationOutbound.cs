using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace em {
	public class MessageModificationOutbound {
		public IList<string> destination { get; set; }
		public string messageGUID { get; set; }
		public string fromAlias { get; set; }
		public string message { get; set; }
		public string mediaRef { get; set; }
		public string contentType { get; set; }
		public MessageLifecycle messageLifecycle { get; set; }
		public JToken attributes { get; set; }
	}
}