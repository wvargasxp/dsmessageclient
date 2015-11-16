using CoreGraphics;
using Foundation;
using UIKit;
using String_UIKit_Extension;

namespace iOS {
	public class SideMenuTableViewCell : UITableViewCell {
		
		public static readonly NSString Key = new NSString ("SideMenuTableViewCell");

		UIImageView iconView;
		UILabel titleLabel;
		UIImageView accessoryIcon;
		UILabel accessoryLabel;
		UIView topLineView;
		UIView bottomLineView;

		public UIView topDropShadowLine;
		public UIView bottomDropShadowLine;

		public UIImageView IconView {
			get { return iconView; }
			set { iconView = value; }
		}

		public UILabel TextOnCell {
			get { return titleLabel; }
			set { titleLabel = value; }
		}

		public UIImageView AccessoryIcon {
			get { return accessoryIcon; }
			set { accessoryIcon = value; }
		}

		public UILabel AccessoryLabel {
			get { return accessoryLabel; }
			set { accessoryLabel = value; }
		}

		public SideMenuTableViewCell (UITableViewCellStyle style, string key) : base (style, key) {
			iconView = new UIImageView (new CGRect (0, 0, 35, 20));
			titleLabel = new UILabel (new CGRect (0, 0, ContentView.Frame.Width, 40));
			accessoryIcon = new UIImageView (new CGRect (0, 0, 30, 30));
			accessoryLabel = new UILabel (new CGRect (0, 0, 20, 20));
			accessoryLabel.Font = FontHelper.DefaultFontWithSize (14);

			// hiding the accessories by default
			HideAccessories ();

			#region top and bottom lines
			const float lineHeight = .5f;
			topLineView = new UINavigationBarLine (new CGRect (0, 0, ContentView.Frame.Width, lineHeight));
			topLineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			topLineView.BackgroundColor = iOS_Constants.BLACK_COLOR;
			bottomLineView = new UINavigationBarLine (new CGRect (0, 0, ContentView.Frame.Width, lineHeight));
			bottomLineView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			bottomLineView.BackgroundColor = iOS_Constants.BLACK_COLOR;

			topDropShadowLine = new UINavigationBarLine (topLineView.Frame);
			bottomDropShadowLine = new UINavigationBarLine (bottomLineView.Frame);

			SetDropShadowOnLines ();
			#endregion

			ContentView.Add (iconView);
			ContentView.Add (titleLabel);
			ContentView.Add (accessoryIcon);
			ContentView.Add (topLineView);
			ContentView.Add (bottomLineView);
			ContentView.Add (topDropShadowLine);
			ContentView.Add (bottomDropShadowLine);
			accessoryIcon.Add (accessoryLabel);
		}

		public static SideMenuTableViewCell Create () {
			return new SideMenuTableViewCell (UITableViewCellStyle.Default, Key);
		}

		public override void LayoutSubviews () {
			base.LayoutSubviews ();
			iconView.Frame = new CGRect (UI_CONSTANTS.SMALL_MARGIN, ContentView.Frame.Height / 2 - iconView.Frame.Height / 2, iconView.Frame.Width, iconView.Frame.Height);
			titleLabel.Frame = new CGRect (iconView.Frame.X + iconView.Frame.Width + UI_CONSTANTS.TINY_MARGIN, ContentView.Frame.Height / 2 - titleLabel.Frame.Height / 2, titleLabel.Frame.Width, titleLabel.Frame.Height);
			accessoryIcon.Frame = new CGRect (iOS_Constants.LEFT_DRAWER_WIDTH - accessoryIcon.Frame.Width - UI_CONSTANTS.SMALL_MARGIN + 1 /*spacing the accessory */, ContentView.Frame.Height / 2 - accessoryIcon.Frame.Height / 2, accessoryIcon.Frame.Width, accessoryIcon.Frame.Height);

			//center the unread notification count if text exists
			if(string.IsNullOrEmpty(accessoryLabel.Text))
				accessoryLabel.Frame = new CGRect (UI_CONSTANTS.SMALL_MARGIN, accessoryIcon.Frame.Height / 2 - accessoryLabel.Frame.Height / 2, accessoryLabel.Frame.Width, accessoryLabel.Frame.Height);
			else {
				CGSize sizeACL = accessoryLabel.Text.SizeOfTextWithFontAndLineBreakMode (accessoryLabel.Font, new CGSize (accessoryIcon.Frame.Width, accessoryIcon.Frame.Height), UILineBreakMode.Clip);
				sizeACL = new CGSize ((float)((int)(sizeACL.Width + 1.5)), (float)((int)(sizeACL.Height + 1.5)));
				var xCoordACL = (accessoryIcon.Frame.Width - sizeACL.Width) / 2;
				accessoryLabel.Frame = new CGRect (new CGPoint(xCoordACL, accessoryIcon.Frame.Height / 2 - accessoryLabel.Frame.Height / 2), sizeACL);
			}

			topLineView.Frame = new CGRect (0, 0, topLineView.Frame.Width, topLineView.Frame.Height);
			bottomLineView.Frame = new CGRect (0, ContentView.Frame.Height-bottomLineView.Frame.Height, bottomLineView.Frame.Width, bottomLineView.Frame.Height);
			topDropShadowLine.Frame = topLineView.Frame;
			bottomDropShadowLine.Frame = bottomLineView.Frame;
		}

		public void SetDropShadowOnLines () {
			const float shadowOpacity = .23f;
			topDropShadowLine.Layer.ShadowColor = iOS_Constants.BLACK_COLOR.CGColor;
			topDropShadowLine.Layer.ShadowOpacity = shadowOpacity;
			topDropShadowLine.Layer.ShadowOffset = new CGSize (0, 2);
			var _shadowPath = new CGRect (topDropShadowLine.Bounds.X - 10, topDropShadowLine.Layer.Bounds.Height - 2, topDropShadowLine.Bounds.Width + 20, 3);
			topDropShadowLine.Layer.ShadowPath = UIBezierPath.FromRect (_shadowPath).CGPath;
			topDropShadowLine.Layer.ShouldRasterize = true;
			topDropShadowLine.Layer.MasksToBounds = false;

			bottomDropShadowLine.Layer.ShadowColor = iOS_Constants.BLACK_COLOR.CGColor;
			bottomDropShadowLine.Layer.ShadowOpacity = shadowOpacity;
			bottomDropShadowLine.Layer.ShadowOffset = new CGSize (0, 2);
			var shadowPath = new CGRect (bottomDropShadowLine.Bounds.X - 10, bottomDropShadowLine.Layer.Bounds.Height - 2, bottomDropShadowLine.Bounds.Width + 20, 3);
			bottomDropShadowLine.Layer.ShadowPath = UIBezierPath.FromRect (shadowPath).CGPath;
			bottomDropShadowLine.Layer.ShouldRasterize = true;
			bottomDropShadowLine.Layer.MasksToBounds = false;
		}

		public void HideBottomLine () {
			bottomLineView.Alpha = 0;
			bottomDropShadowLine.Alpha = 0;
		}

		public void HideAccessories() {
			accessoryIcon.Alpha = 0;
			accessoryLabel.Alpha = 0;
		}

		public void ShowAccessories() {
			accessoryIcon.Alpha = 1;
			accessoryLabel.Alpha = 1;
		}
	}
}