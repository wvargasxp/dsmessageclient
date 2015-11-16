using System;
using UIKit;
using CoreGraphics;
using em;

namespace iOS {
	public abstract class BasicThumbnailView : UIView {
		UIActivityIndicatorView spinner;
		protected UIActivityIndicatorView Spinner {
			get { return spinner; }
			set { spinner = value; }
		}

		public BasicThumbnailView () {}

		public void UpdateProgressIndicatorVisibility (bool showProgressIndicator) {
			if (showProgressIndicator) {
				if (this.Spinner == null) {
					this.Spinner = new UIActivityIndicatorView (UIActivityIndicatorViewStyle.WhiteLarge);
					this.AddSubview (this.Spinner);
					this.Spinner.TintColor = UIColor.Blue;
				}

				CGRect f = this.Spinner.Frame;
				f.Location = this.ProgressIndicatorLocation;
				this.Spinner.Frame = f;
				if (!this.Spinner.IsAnimating)
					this.Spinner.StartAnimating ();
				this.BringSubviewToFront (this.Spinner);

				UpdateVisibility (false);
			} else {
				if (this.Spinner != null) {
					this.Spinner.StopAnimating ();
					this.Spinner.RemoveFromSuperview ();
					this.Spinner = null;
				}

				UpdateVisibility (true);
			}
		}

		public void ShowDebuggingInformation (bool noErrorsSoFar) {
			if (AppEnv.DEBUG_MODE_ENABLED) {
				if (noErrorsSoFar) {
					this.BackgroundColor = UIColor.Clear;
				} else {
					this.BackgroundColor = UIColor.Red;
				}
			}
		}

		public abstract CGPoint ProgressIndicatorLocation { get; }
		public abstract void UpdateVisibility (bool showThumbnail);
	}
}

