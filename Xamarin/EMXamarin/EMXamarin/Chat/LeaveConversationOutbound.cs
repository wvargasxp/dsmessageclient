using System;
using Newtonsoft.Json.Linq;

namespace em {
	public class LeaveConversationOutbound {
		public string fromAlias { get; set; }
		public string[] destination { get; set; }
		public String createDate { get; set; }

		public void setCreateDate(DateTime datetime) {
			DateTimeOffset offset = new DateTimeOffset(datetime,TimeSpan.Zero);
			createDate = offset.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
		}
	}
}

