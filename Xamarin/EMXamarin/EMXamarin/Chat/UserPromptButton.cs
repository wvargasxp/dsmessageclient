using System;
using Newtonsoft.Json.Linq;

namespace em
{
	public class UserPromptButton
	{
		public string label { get; set; }
		public string destination { get; set; }
		public JToken responseAction { get; set; }

		public string Response {
			get {
				if (this.responseAction == null) {
					return string.Empty;
				}

				return this.responseAction.ToString ();
			}
		}
	}
}

