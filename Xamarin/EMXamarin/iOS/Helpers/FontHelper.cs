using UIKit;
using em;
using System;
using Foundation;

namespace iOS {
	public static class FontHelper {

		static string defaultFont;
		static string defaultFontItalic;
		static string defaultFontBold;
		static string defaultFontBoldItalic;

		public static string DEFAULT_FONT {
			get {
				if (defaultFont == null)
					defaultFont = Constants.FONT_FOR_DEFAULT;
				return defaultFont;
			}

			set {
				defaultFont = value;
			}
		}

		public static string DEFAULT_FONT_ITALIC {
			get {
				if (defaultFontItalic == null)
					defaultFontItalic = Constants.FONT_LIGHT_ITALIC;
				return defaultFontItalic;
			}

			set {
				defaultFontItalic = value;
			}
		}

		public static string DEFAULT_FONT_BOLD {
			get {
				if (defaultFontBold == null)
					defaultFontBold = Constants.FONT_FOR_DEFAULT_BOLD;
				return defaultFontBold;
			}

			set {
				defaultFontBold = value;
			}
		}

		public static string DEFAULT_FONT_BOLD_ITALIC {
			get {
				if (defaultFontBoldItalic == null)
					defaultFontBoldItalic = Constants.FONT_BOLD_ITALIC;
				return defaultFontBoldItalic;
			}

			set {
				defaultFontBoldItalic = value;
			}
		}

		public static nfloat DefaultFontSizeForTitles {
			get {
				return 20f;
			}
		}

		public static nfloat DefaultFontSize {
			get {
				return 17f;
			}
		}

		public static nfloat DefaultFontSizeForTextFields {
			get {
				return 17f;
			}
		}

		public static nfloat DefaultFontSizeForLabels {
			get {
				return 16f;
			}
		}

		public static nfloat DefaultFontSizeForButtons {
			get {
				return 17f;
			}
		}

		public static UIFont DefaultFontWithSize (nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT, customSize);
		}

		public static UIFont DefaultBoldFontWithSize (nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT_BOLD, customSize);
		}

		public static UIFont DefaultFontForTextFields () {
			return UIFont.FromName (DEFAULT_FONT, DefaultFontSizeForTextFields);
		}

		public static UIFont DefaultFontForTextFields (nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT, customSize);
		}

		public static UIFont DefaultFontForLabels () {
			return UIFont.FromName (DEFAULT_FONT, DefaultFontSizeForLabels);
		}

		public static UIFont DefaultFontForLabels (nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT, customSize);
		}

		public static UIFont DefaultFontForButtons () {
			return UIFont.FromName (DEFAULT_FONT, DefaultFontSizeForButtons);
		}

		public static UIFont DefaultFontForButtons (nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT, customSize);
		}

		public static UIFont DefaultBoldFontForButtons () {
			return UIFont.FromName (DEFAULT_FONT_BOLD, DefaultFontSizeForButtons);
		}

		public static UIFont DefaultBoldFontForButtons (nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT_BOLD, customSize);
		}

		public static UIFont DefaultFontForTitles () {
			return UIFont.FromName (DEFAULT_FONT, DefaultFontSizeForTitles);
		}

		public static UIFont DefaultFontForTitles (nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT, customSize);
		}

		public static UIFont DefaultItalicFont() {
			return UIFont.FromName (DEFAULT_FONT_ITALIC, DefaultFontSize);
		}

		public static UIFont DefaultItalicFont(nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT_ITALIC, customSize);
		}

		public static UIFont DefaultBoldItalicFont() {
			return UIFont.FromName (DEFAULT_FONT_BOLD_ITALIC, DefaultFontSize);
		}

		public static UIFont DefaultBoldItalicFont(nfloat customSize) {
			return UIFont.FromName (DEFAULT_FONT_BOLD_ITALIC, customSize);
		}

		public static UIColor DefaultTextColor () {
			return iOS_Constants.WHITE_COLOR;
		}

		public static UIColor BlackTextColor () {
			return iOS_Constants.BLACK_COLOR;
		}

		public static UITextAttributes DefaultNavigationAttributes() {
			var uiBarButtonAttributes = new UITextAttributes();
			uiBarButtonAttributes.Font = FontHelper.DefaultFontForButtons ();
			return uiBarButtonAttributes;
		}
	}
}