using System;
using UIKit;

namespace iOS
{
	public class UINavigationControllerHelper
	{
		// This method should be called in viewWillDisappear
		// based on http://stackoverflow.com/questions/1816614/viewwilldisappear-determine-whether-view-controller-is-being-popped-or-is-showi
		public static bool IsViewControllerBeingPopped(UIViewController controller) {
			if (controller.NavigationController == null) {
				return true;
			}
			UIViewController[] stack = controller.NavigationController.ViewControllers;
			int count = stack.Length;
			if (count > 1 && stack [count - 2] == controller)
				return false;

			foreach (UIViewController vc in stack)
				if (vc == controller)
					return false;

			return true;
		}
	}
}

