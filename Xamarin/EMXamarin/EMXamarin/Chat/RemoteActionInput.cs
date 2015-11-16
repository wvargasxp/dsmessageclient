using System;
using Newtonsoft.Json.Linq;

namespace em {
	public class RemoteActionInput: MessageInput {
		public string action { get; set; }
		public JToken parameters { get; set; }
		public string responseDestination { get; set; }
		public JToken responseAction { get; set; }

		public AppAction AppAction {
			get {
				try {
					AppAction appAction;
					return Enum.TryParse<AppAction>(action, out appAction) ? appAction : AppAction.unknown;
				}
				catch (Exception e) {
					return AppAction.unknown;
				}
			}
		}
	}
}

