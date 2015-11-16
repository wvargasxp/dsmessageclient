using UIKit;
using CoreGraphics;

namespace iOS {
	public sealed class EMButton : UIButton {
		public EMButton (CGRect frame, UIColor color, string title) : base(frame) {
			SetTitle (title, UIControlState.Normal);
			this.Font = FontHelper.DefaultFontForButtons ();
			ThemeButton (color);
		}

		public EMButton(UIButtonType type, UIColor color, string title) : base(type) {
			SetTitle (title, UIControlState.Normal);
			this.Font = FontHelper.DefaultFontForButtons ();
			this.ClipsToBounds = true;
			ThemeButton (color);
		}

		public EMButton (UIButtonType type) : base (type) {
			this.Font = FontHelper.DefaultFontForButtons ();
			this.ClipsToBounds = true;
		}

		public void ThemeButton (UIColor color) {
			if (color == iOS_Constants.WHITE_COLOR)
				SetTitleColor (color, UIControlState.Normal);
			else
				SetTitleColor (iOS_Constants.BLACK_COLOR, UIControlState.Normal);
			
			this.Layer.BorderColor = color.CGColor;
			this.Layer.CornerRadius = 10f;
			this.Layer.BorderWidth = 1f;
		}


		private const int ProgressSize = 50;
		private UIActivityIndicatorView _progress = null;
		private UIActivityIndicatorView Progress { 
			get {
				if (this._progress == null) {
					this._progress = new UIActivityIndicatorView (new CGRect (0, 0, ProgressSize, ProgressSize));
					this._progress.Color = UIColor.Gray;
				}	

				return this._progress;
			}
		}

		private bool _showProgress = false;
		public bool ShowProgress {
			get {
				return this._showProgress;
			}

			set {
				this._showProgress = value;

				if (value) {
					this.Add (this.Progress);
					this.Progress.StartAnimating ();
					this.Progress.Center = new CGPoint (this.Bounds.Width - this.Bounds.Height / 2, this.Bounds.Height / 2);
					this.Progress.Frame = new CGRect (0, this.Progress.Frame.Y, ProgressSize, ProgressSize);
				} else {
					this.Progress.RemoveFromSuperview ();
					this.Progress.StopAnimating ();
				}
			}
		}


		public override void LayoutSubviews () {
			base.LayoutSubviews ();
		}
	}
}