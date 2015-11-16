using System;

namespace EMXamarin
{
	public class UIComponentFactory
	{
		public UIComponentFactory ()
		{
		}

		public UIComponent CreateUIComponent(string classname)
		{
			return new UIComponent ();
		}
	}
}

