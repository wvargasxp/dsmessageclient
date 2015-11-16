using UIKit;
using CoreGraphics;

namespace iOS {
	public sealed class OnboardingTextField : UITextField {
		public OnboardingTextField (CGRect frame) : base (frame) {
			BorderStyle = UITextBorderStyle.None;
			Background = UIImage.FromFile ("chat/text-field.png").StretchableImage (UI_CONSTANTS.ONBOARDING_TEXT_FIELD_LEFT_CAP, UI_CONSTANTS.ONBOARDING_TEXT_FIELD_TOP_CAP);
			Font = FontHelper.DefaultFontForTextFields ();
			AutocorrectionType = UITextAutocorrectionType.No;
			AutocapitalizationType = UITextAutocapitalizationType.None;
			ReturnKeyType = UIReturnKeyType.Go;
			ClearButtonMode = UITextFieldViewMode.WhileEditing;
			VerticalAlignment = UIControlContentVerticalAlignment.Center;

			/* set left and right padding to support LTR & RTL langauges */
			var paddingView = new UIView (new CGRect (0, 0, 10 /* padding */, Frame.Height));
			RightView = paddingView;
			RightViewMode = UITextFieldViewMode.Always;
			LeftView = paddingView;
			LeftViewMode = UITextFieldViewMode.Always;
		}
	}
}