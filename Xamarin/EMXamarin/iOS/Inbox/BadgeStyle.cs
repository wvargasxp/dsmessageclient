using System;
using UIKit;

namespace iOS {

	public enum BadgeStyleFontType {
		BadgeStyleFontTypeHelveticaNeueMedium = 0,
		BadgeStyleFontTypeHelveticaNeueLight = 1	
	}

	public class BadgeStyle {

		public UIColor BadgeTextColor { get; set; }
		public UIColor BadgeInsetColor { get; set; }
		public UIColor BadgeFrameColor { get; set; }
		public bool BadgeFrame { get; set; }
		public bool BadgeShining { get; set; }
		public BadgeStyleFontType BadgeFontType { get; set; }
		public bool BadgeShadow { get; set; }

		public static BadgeStyle DefaultStyle {
			get {
				BadgeStyle b = new BadgeStyle ();
				b.BadgeFontType = BadgeStyleFontType.BadgeStyleFontTypeHelveticaNeueLight;
				b.BadgeTextColor = UIColor.White;
				b.BadgeInsetColor = UIColor.Red;
				b.BadgeFrameColor = null;
				b.BadgeShadow = false;
				b.BadgeShining = false;
				b.BadgeFrame = false;
				return b;
			}
		}

		public static BadgeStyle OldStyle {
			get {
				BadgeStyle b = new BadgeStyle ();
				b.BadgeFontType = BadgeStyleFontType.BadgeStyleFontTypeHelveticaNeueMedium;
				b.BadgeTextColor = UIColor.White;
				b.BadgeInsetColor = UIColor.Red;
				b.BadgeFrameColor = UIColor.White;
				b.BadgeFrame = true;
				b.BadgeShadow = true;
				b.BadgeShining = true;
				return b;
			}
		}

		public static BadgeStyle CustomStyle (
			BadgeStyleFontType badgeFontType,
			UIColor badgeTextColor,
			UIColor badgeInsetColor,
			UIColor badgeFrameColor,
			bool badgeFrame,
			bool badgeShadow,
			bool badgeShining
		) {
			BadgeStyle b = new BadgeStyle ();
			b.BadgeFontType = badgeFontType;
			b.BadgeTextColor = badgeTextColor;
			b.BadgeInsetColor = badgeInsetColor;
			b.BadgeFrameColor = badgeFrameColor;
			b.BadgeFrame = badgeFrame;
			b.BadgeShadow = badgeShadow;
			b.BadgeShining = badgeShining;
			return b;
		}

		public BadgeStyle () {}
	}
}

