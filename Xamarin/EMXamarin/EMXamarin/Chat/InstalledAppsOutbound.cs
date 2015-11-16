using System;
using System.Collections.Generic;

namespace em
{
	public class InstalledAppsOutbound
	{
		public List<AppDescriptionOutbound> appDescriptions { get; set; }

		public InstalledAppsOutbound() {
			appDescriptions = new List<AppDescriptionOutbound> ();
		}
	}
}

