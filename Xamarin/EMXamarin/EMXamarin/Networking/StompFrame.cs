using System;
using System.Collections.Generic;

namespace em {
	public class StompFrame {
		public string command;
		public Dictionary<string, string> headers;
		public object body;

		public StompFrame () {}
	}
}

