using CoreGraphics;
using UIKit;
using System.Collections.Generic;
using System.Diagnostics;
using Foundation;
using System;
using System.Globalization;

namespace String_UIKit_Extension {
	public static class String_UIKit_Extension_Text_Size {

		static Dictionary<string, CGSize> d;
		static Dictionary<string, CGSize> SizeFDict {
			get { 
				if (d == null)
					d = new Dictionary<string, CGSize> ();
				return d; 
			}
			set { d = value; }
		}

		public static CGSize SizeOfTextWithFontAndLineBreakMode(this string media, UIFont font, CGSize constrainedToSize, UILineBreakMode lineBreakMode) {
			if (media == null)
				return CGSize.Empty;

			string key = font + "-" + media + "-" + constrainedToSize.Width + "-" + constrainedToSize.Height;
			if (String_UIKit_Extension_Text_Size.SizeFDict.ContainsKey (key)) {
				return SizeFDict [key];
			}

			CGSize s = media.SizeWithMaxWidthAndHeight (constrainedToSize.Width, constrainedToSize.Height, font, lineBreakMode);

			String_UIKit_Extension_Text_Size.SizeFDict.Add (key, s);
			return s;
		}

		public static CGSize SizeOfTextWithFontAndLineBreakMode(this string media, UIFont font, nfloat width, UILineBreakMode lineBreakMode) {
			if (media == null)
				return CGSize.Empty;

			string key = font + "-" + media + "-" + width;
			if (String_UIKit_Extension_Text_Size.SizeFDict.ContainsKey (key)) {
				return String_UIKit_Extension_Text_Size.SizeFDict [key];
			}

			var mockLabel = new UILabel();
			mockLabel.Font = font;
			mockLabel.LineBreakMode = lineBreakMode;
			mockLabel.Lines = 0;
			CGSize s = media.StringSize (font, width, lineBreakMode);

			String_UIKit_Extension_Text_Size.SizeFDict.Add (key, s);
			return s;
		}

		public static byte[] ConvertHexStringToByteArray (this string hexString) {
			if (hexString.Length % 2 != 0) {
				throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
			}

			byte[] HexAsBytes = new byte[hexString.Length / 2];
			for (int index = 0; index < HexAsBytes.Length; index++) {
				string byteValue = hexString.Substring(index * 2, 2);
				HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}

			return HexAsBytes; 
		}

		public static nfloat HeightFromWidthAndFont (this string stringToCalculate, nfloat widthValue, UIFont font) {
			CGSize size = stringToCalculate.SizeWithMaxWidthAndHeight (widthValue, float.MaxValue, font);
			return size.Height;
		}

		public static nfloat WidthFromHeightAndFont (this string stringToCalculate, nfloat heightValue, UIFont font) {
			CGSize size = stringToCalculate.SizeWithMaxWidthAndHeight (float.MaxValue, heightValue, font);
			return size.Width;
		}

		public static CGSize SizeWithMaxWidthAndHeight (this string stringToCalculate, nfloat widthValue, nfloat heightValue, UIFont font) {
			return stringToCalculate.SizeWithMaxWidthAndHeight (widthValue, heightValue, font, UILineBreakMode.WordWrap);
		}

		public static CGSize SizeWithMaxWidthAndHeight (this string stringToCalculate, nfloat widthValue, nfloat heightValue, UIFont font, UILineBreakMode lineBreakMode) {
			NSString text = new NSString (stringToCalculate);
			NSTextStorage textStorage = new NSTextStorage ();
			textStorage.SetString (new NSAttributedString (text));
			NSTextContainer textContainer = new NSTextContainer (new CGSize (widthValue, heightValue));
			textContainer.LineBreakMode = lineBreakMode;
			NSLayoutManager layoutManager = new NSLayoutManager ();
			layoutManager.AddTextContainer (textContainer);
			textStorage.AddLayoutManager (layoutManager);
			textStorage.AddAttribute (UIStringAttributeKey.Font, font, new NSRange (0, textStorage.Length));
			textContainer.LineFragmentPadding = 0.0f;
			layoutManager.GetGlyphRange (textContainer);
			CGRect result = layoutManager.GetUsedRectForTextContainer (textContainer);
			return new CGSize (result.Width, result.Height);
		}
	}
}