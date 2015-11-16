using System;

namespace SideMenuBinding
{
	public enum MMDrawerSide {
		None = 0,
		Left,
		Right
	}

	public enum MMDrawerOpenCenterInteractionMode {
		None,
		Full,
		NavigationBarOnly
	}

	public enum MMOpenDrawerGestureMode {
		None = 0,
		PanningNavigationBar = 1 << 1,
		PanningCenterView = 1 << 2,
		BezelPanningCenterView = 1 << 3,
		Custom = 1 << 4,
		All = PanningNavigationBar | PanningCenterView | BezelPanningCenterView | Custom
	}


	public enum MMCloseDrawerGestureMode {
		None = 0,
		PanningNavigationBar = 1 << 1,
		PanningCenterView = 1 << 2,
		BezelPanningCenterView = 1 << 3,
		TapNavigationBar = 1 << 4,
		TapCenterView = 1 << 5,
		PanningDrawerView = 1 << 6,
		Custom = 1 << 7,
		All = PanningNavigationBar | PanningCenterView | BezelPanningCenterView | TapNavigationBar | TapCenterView | PanningDrawerView | Custom
	}

}

