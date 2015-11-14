using System;
using Android.Views;
using Android.Widget;
using em;
using Android.Graphics;

namespace Emdroid {
	public static class FontHelper {
		static Typeface defaultFont;
		static Typeface defaultFontItalic;
		static Typeface defaultBoldFont;
		static Typeface defaultBoldItalicFont;

		public static Typeface DefaultFont {
			get {
				if (defaultFont == null)
					defaultFont = Typeface.CreateFromAsset (EMApplication.GetInstance ().BaseContext.Assets, Constants.FONT_FOR_DEFAULT + ".ttf");
				return defaultFont;
			}

			set {
				defaultFont = value;
			}
		}

		public static Typeface DefaultFontItalic {
			get {
				if (defaultFontItalic == null)
					defaultFontItalic = Typeface.CreateFromAsset (EMApplication.GetInstance ().BaseContext.Assets, Constants.FONT_LIGHT_ITALIC + ".ttf");
				return defaultFontItalic;
			}

			set {
				defaultFontItalic = value;
			}
		}

		public static Typeface DefaultBoldFont {
			get {
				if (defaultBoldFont == null)
					defaultBoldFont = Typeface.CreateFromAsset (EMApplication.GetInstance ().BaseContext.Assets, Constants.FONT_FOR_DEFAULT_BOLD + ".ttf");
				return defaultBoldFont;
			}

			set {
				defaultBoldFont = value;
			}
		}

		public static Typeface DefaultBoldItalicFont {
			get {
				if (defaultBoldItalicFont == null)
					defaultBoldItalicFont = Typeface.CreateFromAsset (EMApplication.GetInstance ().BaseContext.Assets, Constants.FONT_BOLD_ITALIC + ".ttf");
				return defaultBoldItalicFont;
			}

			set {
				defaultBoldItalicFont = value;
			}
		}

		public static void SetFontOnAllViews (ViewGroup root) {
			int childCount = root.ChildCount;
			for (int i=0; i<childCount; i++) {
				View v = root.GetChildAt (i);
				Type viewType = v.GetType ();
				if (viewType == typeof(TextView))
					((TextView)v).Typeface = DefaultFont;
				else if (viewType == typeof(EditText))
					((EditText)v).Typeface = DefaultFont;
				else if (viewType == typeof(Button))
					((Button)v).Typeface = DefaultFont;
				else if (viewType == typeof(ViewGroup))
					SetFontOnAllViews ((ViewGroup)v);
			}
		}
	}
}