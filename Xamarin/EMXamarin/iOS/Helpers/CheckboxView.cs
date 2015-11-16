using System;
using UIKit;
using CoreGraphics;

namespace iOS {
	public class CheckboxView : UIView {
		private UILabel CheckMark { get; set; }

		private bool _isOn = false;
		public bool IsOn { 
			get { return this._isOn; }
			set {
				this._isOn = value;
				this.CheckMark.Hidden = !this.IsOn;
			}
		}

		public static nfloat DefaultCheckBoxSize = 20;

		private UIColor Color { get; set; }

		private const float Size = 20f;
		private const float StrokeWidth = .7f;
		private const float AlphaC = .2f;
		private const float Radius = 3.0f;

		public bool ShouldDrawBorder { get; set; }

		public CheckboxView (CGRect frame) : base (frame) {
			this.Color = UIColor.FromWhiteAlpha (0, AlphaC);
			this.BackgroundColor = UIColor.Clear;

			this.CheckMark = new UILabel (new CGRect (StrokeWidth, StrokeWidth, Size - 2 * StrokeWidth, Size - 2 * StrokeWidth));
			this.CheckMark.Font = UIFont.SystemFontOfSize (18f);
			this.CheckMark.Text = "\u2713";
			this.CheckMark.BackgroundColor = UIColor.Clear;
			this.CheckMark.TextAlignment = UITextAlignment.Center;
			this.Add (this.CheckMark);
			this.CheckMark.Hidden = true;
			this.IsOn = false;
			this.ShouldDrawBorder = false;
		}

		public override void Draw (CGRect rect) {
			base.Draw (rect);
			CGRect _rect = new CGRect (StrokeWidth, StrokeWidth, Size - 2 * StrokeWidth, Size - 2 * StrokeWidth);
			if (this.ShouldDrawBorder) {
				DrawRoudnedRect (_rect, UIGraphics.GetCurrentContext ());
			}
		}

		private void DrawRoudnedRect (CGRect rect, CGContext context) {
			context.BeginPath ();
			context.SetLineWidth (StrokeWidth);
			context.SetStrokeColor (this.Color.CGColor);
			context.MoveTo (rect.GetMinX () + Radius, rect.GetMinY ());
			context.AddArc (rect.GetMaxX () - Radius, rect.GetMinY () + Radius, Radius, (nfloat)(3 * Math.PI / 2), 0, false);
			context.AddArc (rect.GetMaxX () - Radius, rect.GetMaxY () - Radius, Radius, 0, (nfloat)(Math.PI / 2), false);
			context.AddArc (rect.GetMinX () + Radius, rect.GetMaxY () - Radius, Radius, (nfloat)(Math.PI / 2), (nfloat)Math.PI, false);
			context.AddArc (rect.GetMinX () + Radius, rect.GetMinY () + Radius, Radius, (nfloat)(Math.PI), (nfloat)(3 * Math.PI / 2), false);
			context.ClosePath ();
			context.StrokePath ();
		}

		public void SetBoundedRectFill (UIColor color) {
			this.CheckMark.BackgroundColor = color;
		}
	}
}

