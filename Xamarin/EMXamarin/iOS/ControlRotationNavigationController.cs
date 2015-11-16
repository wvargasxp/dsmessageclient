using System;
using UIKit;
using Foundation;

namespace iOS {
	public class ControlRotationNavigationController : UINavigationController {

		private bool allowRotate;

		public ControlRotationNavigationController (UIViewController rootViewController) : base (rootViewController) {
			this.allowRotate = true;
			this.NavigationBarHidden = true;
		}

		public override bool ShouldAutorotate () {
			return this.allowRotate;
		}

		public void AllowRotate () {
			this.allowRotate = true;
		}

		public void DisableRotate () {
			this.allowRotate = false;
		}

		public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations () {
			return UIInterfaceOrientationMask.AllButUpsideDown;
		}
	}
}

