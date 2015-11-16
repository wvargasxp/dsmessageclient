using System;
using System.Collections.Generic;

namespace em {
	public class RemoveChatEntryOutbound {
		public string fromAlias { get; set; }
		public IList<string> replyTo { get; set; }
		public String createDate { get; set; }

		public void setCreateDate(DateTime datetime) {
			DateTimeOffset offset = new DateTimeOffset(datetime,TimeSpan.Zero);
			createDate = offset.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
		}
	}
}

