using CoreGraphics;
using UIKit;
using em;
using String_UIKit_Extension;
using System.Collections.Generic;

namespace iOS {
	public class RemoteMessageTypingView : UIView {

		bool visible;

		#region UI
		CGPoint greenDotPoint;
		#endregion

		UIImageView greenDotImageView;

		UILabel typingLabel;

		BackgroundColor currentColor;

		public RemoteMessageTypingView (CGRect f, BackgroundColor color) : base (f) {

			visible = false;

			AutosizesSubviews = true;
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			BackgroundColor = UIColor.Clear;

			UIImage greenDot = ImageSetter.GetResourceImage ("chat/dotGreen.png");
			greenDotImageView = new UIImageView(greenDot);

			typingLabel = new UILabel ();
			typingLabel.TextColor = iOS_Constants.WHITE_COLOR;
			typingLabel.Font = FontHelper.DefaultFontWithSize (10);
			CGSize sizeF = CGSize.Empty;

			this.Alpha = 0;

			currentColor = color;
			this.BackgroundColor = currentColor.GetColor ();

			this.AutosizesSubviews = false;
			this.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleRightMargin;

			greenDotPoint = new CGPoint (0, 5);
			var greenDotImageViewFrame = new CGRect(greenDotPoint, greenDot.Size);
			greenDotImageView.Frame = greenDotImageViewFrame;
			this.Add (greenDotImageView);

			var typingLabelFrame = new CGRect (new CGPoint (greenDot.Size.Width + 3, 3), sizeF);
			typingLabel.Frame = typingLabelFrame;
			this.Add (typingLabel);


			/* this looks to be unused
			var containerSize = new SizeF (typingLabelFrame.Location.X + typingLabelFrame.Size.Width,
				Math.Max(greenDotImageViewFrame.Location.Y + greenDotImageViewFrame.Size.Height,
					typingLabelFrame.Location.Y + typingLabelFrame.Size.Height));
			RectangleF frame = containerView.Frame;
			*/
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();
			greenDotImageView.Frame = new CGRect(greenDotPoint.X, greenDotPoint.Y, greenDotImageView.Frame.Width, greenDotImageView.Frame.Height);
			if (visible)
				UpdateRemoteTypingMessage (typingLabel.Text, true, currentColor);
		}

		public void HideRemoteTypingMessage (bool animated) {
			if (visible) {
				if (animated) {
					UIView.BeginAnimations ("Hiding");
					UIView.SetAnimationDuration (0.2);
				}

				this.Alpha = 0;
				visible = false;

				if ( animated )
					UIView.CommitAnimations ();
			}
		}

		Dictionary<string, CGSize> msgTypingCache = new Dictionary<string, CGSize> ();

		public void UpdateRemoteTypingMessage (string msg, bool animated, BackgroundColor color) {	
			currentColor = color;
			this.BackgroundColor = currentColor.GetColor ();
			typingLabel.Text = msg;
			if (msg != null) {
				CGSize sizeF;
				if (msgTypingCache.ContainsKey (msg)) {
					sizeF = msgTypingCache [msg];
				} else {
					sizeF = msg.StringSize (typingLabel.Font);
					msgTypingCache.Add (msg, sizeF);
				}

				CGSize dotSize = greenDotImageView.Bounds.Size;

				float totalWidth = (float) (sizeF.Width + dotSize.Width);
				var greenDotFrame = new CGRect (new CGPoint ((this.Bounds.Width - totalWidth) / 2, 5), dotSize); 
				greenDotImageView.Frame = greenDotFrame;
				typingLabel.Frame = new CGRect (new CGPoint (greenDotFrame.Location.X + greenDotFrame.Width + 3, 3), sizeF); 
			}

			if (animated) {
				UIView.BeginAnimations ("Updating");
				UIView.SetAnimationDuration (0.2);
			}

			if (!visible) {
				this.Alpha = 1;
				visible = true;
			}

			if ( animated )
				UIView.CommitAnimations ();
		}
	}
}



