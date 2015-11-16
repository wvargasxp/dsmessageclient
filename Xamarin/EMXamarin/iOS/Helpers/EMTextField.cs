using UIKit;
using CoreGraphics;
using Foundation;

namespace iOS {
	public sealed class EMTextField : UITextField {
		public EMTextField (CGRect frame, string placeholder, UITextAutocorrectionType autocorrect, UITextAutocapitalizationType autocapitalize, UIReturnKeyType returnkey) : base (frame) {
			Background = UIImage.FromFile ("chat/text-field.png").StretchableImage (UI_CONSTANTS.ONBOARDING_TEXT_FIELD_LEFT_CAP, UI_CONSTANTS.ONBOARDING_TEXT_FIELD_TOP_CAP);

			if(placeholder != null)
				AttributedPlaceholder = new NSAttributedString(placeholder, FontHelper.DefaultFontForTextFields (), iOS_Constants.BLACK_COLOR, null, null, null, NSLigatureType.Default, 0, 0, null, 0, 0);

			AutoresizingMask = UIViewAutoresizing.FlexibleWidth;
			Font = FontHelper.DefaultFontForTextFields ();
			ClipsToBounds = true;
			BorderStyle = UITextBorderStyle.None;
			TextColor = iOS_Constants.BLACK_COLOR;
			TextAlignment = UITextAlignment.Center;
			VerticalAlignment = UIControlContentVerticalAlignment.Center;
			AutocorrectionType = autocorrect;
			AutocapitalizationType = autocapitalize;
			ReturnKeyType = returnkey;
			ClearButtonMode = UITextFieldViewMode.WhileEditing;

			/* set left and right padding to support LTR & RTL langauges */
			var paddingView = new UIView (new CGRect (0, 0, 10 /* padding */, Frame.Height));
			RightView = paddingView;
			RightViewMode = UITextFieldViewMode.Always;
			LeftView = paddingView;
			LeftViewMode = UITextFieldViewMode.Always;
		}
	}
}