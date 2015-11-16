using System;
using UIKit;
using CoreGraphics;
using CoreAnimation;
using Foundation;
using System.Drawing;

namespace iOS {
	
	public class DHImageCropperOverlayView : UIView {

		NSNumber kInnerMargin = new NSNumber(0);
		NSNumber kLandscapeInnerMargin = new NSNumber(0);

		public UIBezierPath CroppingPath { get; set; }

		CGRect cropFrame;

		public Action<bool> Completion { get; set; }

		CGRect cropRect;
		UILabel moveAndScale;
		UIButton chooseButton;
		UIButton cancelButton;
		CAShapeLayer maskLayer;

		public CGRect CropFrame () {
			nfloat width = this.Bounds.Size.Width - kInnerMargin.FloatValue;
			nfloat height = this.Bounds.Size.Height;

			if (UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeLeft || UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeRight) {
				width = this.Bounds.Size.Width;
				height = this.Bounds.Size.Height - kLandscapeInnerMargin.FloatValue;
			}

			width = NMath.Min(width, height);
			height = width;

			nfloat x = kInnerMargin.FloatValue * 0.5f;
			nfloat y = (this.Bounds.Size.Height / 2f) - (height * .5f);
			if (UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeLeft || UIApplication.SharedApplication.StatusBarOrientation == UIInterfaceOrientation.LandscapeRight) {
				x = NMath.Floor((this.Bounds.Size.Width / 2f) - (width * 0.5f));
				y = NMath.Floor(kLandscapeInnerMargin.FloatValue * 0.5f);
			}

			cropFrame = new CGRect (x, y, width, height);

			cropRect = cropFrame.Integral ();

			return cropRect;
		}

		void ChooseButtonTouched (object sender, EventArgs args) {
			if (Completion != null)
				Completion(true);
		}

		void CancelButtonTouched (object sender, EventArgs args) {
			if (Completion != null)
				Completion (false);
		}

		public override UIView HitTest (CGPoint point, UIEvent e) {
			UIView hitView = null;
			UIView[] subViews = this.Subviews;

			for (var idx = subViews.Length - 1; idx >= 0; --idx) {
				UIView v = subViews [idx];

				if (v == this)
					continue;

				CGPoint convertedPoint = v.ConvertPointFromView (point, this); //[view convertPoint:point fromView:self];
				hitView = v.HitTest (convertedPoint, e);

				if (hitView == chooseButton || hitView == cancelButton)
					return v;

				if (hitView == this)
					return null;
				
				if (hitView != null)
					return hitView;
			}

			return hitView;
		}

		public override void LayoutSubviews () {
			AddMask ();
			UpdateConstraints ();

			// https://stackoverflow.com/questions/24731552/assertion-failure-in-myclass-layoutsublayersoflayer
			// call base last
			base.LayoutSubviews ();
		}

		public override void UpdateConstraints () {
			base.UpdateConstraints ();

			if (this.Constraints.Length > 0) {
				this.RemoveConstraints (this.Constraints);
			}

			NSNumber labelTopMargin = NSNumber.FromNFloat (NMath.Floor(this.Center.X - (moveAndScale.Frame.Size.Width / 2f)));
			NSNumber bottomMargin = NSNumber.FromNFloat (NMath.Floor(chooseButton.Bounds.Size.Height * .5f));

			NSNumber labelWidth = NSNumber.FromNFloat (moveAndScale.Bounds.Size.Width);
			NSNumber labelHeight = NSNumber.FromNFloat (moveAndScale.Bounds.Size.Height);
			NSNumber buttonWidth = NSNumber.FromNFloat (chooseButton.Bounds.Size.Width);
			NSNumber buttonHeight = NSNumber.FromNFloat (chooseButton.Bounds.Size.Height);

			NSDictionary views = new NSDictionary (
				"_chooseButton", chooseButton,
				"_cancelButton", cancelButton,
				"_moveAndScale", moveAndScale,
				"superview", this);
			NSDictionary metrics = new NSDictionary (
				"bottomMargin", bottomMargin, 
				"horzMargin", kInnerMargin, 
				"labelTopMargin", labelTopMargin,
				"labelWidth", labelWidth,
				"labelHeight", labelHeight,
				"buttonWidth", buttonWidth,
				"buttonHeight", buttonHeight);

			UpdateChooseButtonConstraintsWithViews (views, metrics);
			UpdateCancelButtonConstraintsWithViews (views, metrics);
			UpdateLabelConstraintsWithViews (views, metrics);
		}

		public void UpdateCancelButtonConstraintsWithViews (NSDictionary views, NSDictionary metrics) {
			if (cancelButton.Constraints.Length > 0) {
				cancelButton.RemoveConstraints (cancelButton.Constraints);
			}
			
			var constraints = NSLayoutConstraint.FromVisualFormat ("V:[_cancelButton]-bottomMargin-|", (NSLayoutFormatOptions)0, metrics, views);
			this.AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("H:|-horzMargin-[_cancelButton]", (NSLayoutFormatOptions)0, metrics, views);
			this.AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("H:[_cancelButton(buttonWidth)]", (NSLayoutFormatOptions)0, metrics, views);
			this.AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("V:[_cancelButton(buttonHeight)]", (NSLayoutFormatOptions)0, metrics, views);
			this.AddConstraints(constraints);
		}

		public void UpdateChooseButtonConstraintsWithViews (NSDictionary views, NSDictionary metrics) {
			if (chooseButton.Constraints.Length > 0) {
				chooseButton.RemoveConstraints (chooseButton.Constraints);
			}

			var constraints = NSLayoutConstraint.FromVisualFormat ("V:[_chooseButton]-bottomMargin-|", (NSLayoutFormatOptions)0, metrics, views);
			this.AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("H:[_chooseButton]-horzMargin-|", (NSLayoutFormatOptions)0, metrics, views);
			this.AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("H:[_chooseButton(buttonWidth)]", (NSLayoutFormatOptions)0, metrics, views);
			this.AddConstraints(constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("V:[_chooseButton(buttonHeight)]", (NSLayoutFormatOptions)0, metrics, views);
			this.AddConstraints(constraints);
		}

		public void UpdateLabelConstraintsWithViews (NSDictionary views, NSDictionary metrics) {
			if (moveAndScale.Constraints.Length > 0) {
				moveAndScale.RemoveConstraints (moveAndScale.Constraints);
			}

			var constraint = NSLayoutConstraint.Create (moveAndScale, NSLayoutAttribute.CenterX, NSLayoutRelation.Equal, this, NSLayoutAttribute.CenterX, 1f, 0f);
			AddConstraint (constraint);

			constraint = NSLayoutConstraint.Create (moveAndScale, NSLayoutAttribute.Top, NSLayoutRelation.Equal, this, NSLayoutAttribute.Top, 1f, cropRect.Y - (moveAndScale.Bounds.Size.Height * 1.25f));
			AddConstraint (constraint);

			var constraints = NSLayoutConstraint.FromVisualFormat ("H:[_moveAndScale(labelWidth)]", (NSLayoutFormatOptions)0, metrics, views);
			AddConstraints (constraints);

			constraints = NSLayoutConstraint.FromVisualFormat ("V:[_moveAndScale(labelHeight)]", (NSLayoutFormatOptions)0, metrics, views);
			AddConstraints (constraints);
		}

		public void Styling () {
			this.BackgroundColor = new UIColor (0f, 0f, 0f, 0f);
		}

		public void CommonSetup () {
			kInnerMargin = new NSNumber(10f);
			kLandscapeInnerMargin = new NSNumber(80f);

			//  self.translatesAutoresizingMaskIntoConstraints = NO;
		}

		public UIButton StyledButton () {
			var button = new UIButton (UIButtonType.RoundedRect);
			button.TranslatesAutoresizingMaskIntoConstraints = false;
			button.TintColor = UIColor.White;
			button.TitleLabel.Font = FontHelper.DefaultFontForButtons ();

			return button;
		}

		public void SetupChooseButton () {
			chooseButton = StyledButton();
			chooseButton.SetTitle ("Choose", UIControlState.Normal);
			chooseButton.SizeToFit();

			var frame = new CGRect(this.Bounds.Size.Width - (chooseButton.Frame.Size.Width + 10f),
				this.Bounds.Size.Height - NMath.Floor(chooseButton.Frame.Size.Height * 2f),
				chooseButton.Frame.Size.Width,
				chooseButton.Frame.Size.Height);

			chooseButton.Frame = frame;
			chooseButton.TouchUpInside += ChooseButtonTouched;
			this.AddSubview(chooseButton);
		}

		public void SetupCancelButton () {
			cancelButton = StyledButton();
			cancelButton.SetTitle ("Cancel", UIControlState.Normal);
			cancelButton.SizeToFit();

			var frame = new CGRect(10f,
				this.Bounds.Size.Height - NMath.Floor(cancelButton.Frame.Size.Height * 2f),
				cancelButton.Frame.Size.Width,
				cancelButton.Frame.Size.Height);

			cancelButton.Frame = frame;
			cancelButton.TouchUpInside += CancelButtonTouched;
			this.AddSubview(cancelButton);
		}

		public void SetupButtons () {
			SetupCancelButton();
			SetupChooseButton();
		}

		public void SetupLabel () {
			moveAndScale = new UILabel (CGRect.Empty);
			moveAndScale.TranslatesAutoresizingMaskIntoConstraints = false;
			moveAndScale.Text = "Move and Scale";
			moveAndScale.TextColor = UIColor.White;
			moveAndScale.TintColor = UIColor.White;
			moveAndScale.SizeToFit ();

			CGRect frame = moveAndScale.Frame;
			frame.Location = new CGPoint (this.Center.X - NMath.Floor (moveAndScale.Frame.Size.Width / 2f), 
				cropRect.Location.Y - moveAndScale.Frame.Size.Height);

			moveAndScale.Frame = frame;

			this.AddSubview(moveAndScale);
		}

		public void AddMask () {
			CropFrame();

			if (maskLayer == null) {
				maskLayer = new CAShapeLayer ();
				this.Layer.AddSublayer(maskLayer);

				maskLayer.FillRule = CAShapeLayer.FillRuleEvenOdd;
				maskLayer.FillColor = new UIColor(0f, 0f, 0f, .7f).CGColor;
			}

			UIBezierPath maskPath = UIBezierPath.FromPath (CroppingPath.CGPath);
			nfloat xScale = cropFrame.Size.Width / maskPath.Bounds.Size.Width;
			nfloat yScale = cropFrame.Size.Height / maskPath.Bounds.Size.Height;
			maskPath.ApplyTransform(CGAffineTransform.MakeScale(xScale, yScale));
			maskPath.ApplyTransform(CGAffineTransform.MakeTranslation(this.cropFrame.Location.X, this.cropFrame.Location.Y));

			UIBezierPath path = UIBezierPath.FromRect(this.Bounds);
			path.UsesEvenOddFillRule = true;
			path.AppendPath(maskPath);

			CABasicAnimation clipPathAnimation = CABasicAnimation.FromKeyPath("path");
			clipPathAnimation.Duration = CATransaction.AnimationDuration;
			if ( CATransaction.AnimationTimingFunction != null )
				clipPathAnimation.TimingFunction = CATransaction.AnimationTimingFunction;
			maskLayer.AddAnimation(clipPathAnimation, "path");

			maskLayer.Path = path.CGPath;
		}

		public override void MovedToSuperview () {
			base.MovedToSuperview ();

			Styling ();
			CommonSetup ();
			CropFrame ();
			AddMask ();
			SetupButtons ();
			SetupLabel ();
		}

		public DHImageCropperOverlayView (CGRect frame, UIBezierPath croppingPath) : base (frame) {
			this.CroppingPath = croppingPath;
		}

		public DHImageCropperOverlayView (CGRect frame) : base (frame) {
			this.CroppingPath = UIBezierPath.FromOval (new CGRect(0f, 0f, 10f, 10f));
		}

	}
}