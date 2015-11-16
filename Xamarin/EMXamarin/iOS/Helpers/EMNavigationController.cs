using System;
using UIKit;
using System.Collections.Generic;
using em;

namespace iOS {

	// https://stackoverflow.com/questions/1214965/setting-action-for-back-button-in-navigation-controller/19132881#19132881
	public class EMNavigationController : UINavigationController, IUINavigationBarDelegate {
		/*
		 * There's two flows to how a controller can be popped.
		 * Press back button or programatically call PopViewController.
		 * If you press the back button, ShouldPopItem gets called first.
		 * If it's a programmatic Pop, PopViewController is called first.
		 * For the case where PopViewController is called first, we want a vanilla ShouldPopItem.
		 * If ShouldPopItem happens first, we want to be able control if the controller is popped off or not.
		 */ 
		private bool PopViewControllerCalledFirst { get; set; }


		public EMNavigationController (UIViewController rootViewController) : base (rootViewController) {
			this.PopViewControllerCalledFirst = false;
		}

		public override UIViewController PopViewController (bool animated) {
			this.PopViewControllerCalledFirst = true;
			return base.PopViewController (animated);
		}

		[Foundation.Export ("navigationBar:shouldPopItem:")]
		public bool ShouldPopItem (UIKit.UINavigationBar navigationBar, UIKit.UINavigationItem item) {

			// If PopViewController called first, we should pop the item as it was a programmatic PopViewController
			if (this.PopViewControllerCalledFirst) {

				// Reset flag back to false.
				this.PopViewControllerCalledFirst = false;
				return true;
			}
			
			UIViewController controller = this.TopViewController;
			IPopListener iPopListener = controller as IPopListener;
			if (iPopListener == null || iPopListener.ShouldPopController ()) {
				EMTask.DispatchMain (() => {
					this.PopViewController (true);

					// Set flag to false since this is the flow where PopViewController was not called first.
					// We want to do it after we call PopViewController because PopViewController sets the flag to be true.
					this.PopViewControllerCalledFirst = false;
				});

				return true;
			} else {
				EMTask.DispatchMain (() => {
					// Reset back button alpha.
					UINavigationBar nBar = this.NavigationBar;
					if (nBar == null) return;
					foreach (UIView subview in nBar.Subviews) {
						UIView.Animate (1.0f, () => {
							subview.Alpha = 1.0f;
						});
					}

					// Run the interface's action.
					iPopListener.RunNonPopAction ();
				});

				return false;
			}
		}
	}

	public interface IPopListener {
		bool ShouldPopController ();
		void RunNonPopAction ();
	}
}

