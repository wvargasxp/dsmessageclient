using System;
using UIKit;
using UIDevice_Extension;
using Foundation;

namespace iOS {
	public class BasicBubbleView : UIView {
		public UILabel FromLabel { get; set; }
		public UITextView MessageTextView { get; set; }
		protected UIImageView BubbleBackground { get; private set; }

		public static UIFont NameFont = FontHelper.DefaultBoldFontWithSize (UIFont.SystemFontSize - 4);
		public static UIFont MessageFont = FontHelper.DefaultFontWithSize (UIFont.SystemFontSize);
		public static UIFont EnlargedMessageFont = FontHelper.DefaultFontWithSize (84);

		public BasicBubbleView () {
			if (this.BubbleBackground == null) {
				this.BubbleBackground = new UIImageView (Bounds);
				this.BubbleBackground.Tag = 0x1A;
			}

			this.Add (this.BubbleBackground);

			this.FromLabel = new UILabel ();
			this.FromLabel.Tag = 0x1B;
			this.FromLabel.Font = NameFont;
			this.FromLabel.LineBreakMode = UILineBreakMode.Clip;
			this.FromLabel.TextAlignment = UIDevice.CurrentDevice.IsRightLeftLanguage () ? UITextAlignment.Right : UITextAlignment.Left;
			this.Add (this.FromLabel);

			this.MessageTextView = new BasicBubbleTextView ();
			this.MessageTextView.Tag = 0x1D;
			this.MessageTextView.Font = MessageFont;
			this.MessageTextView.TextContainer.LineBreakMode = UILineBreakMode.WordWrap;
			this.MessageTextView.TextContainer.LineFragmentPadding = 0;
			this.MessageTextView.TextContainerInset = UIEdgeInsets.Zero;
			this.MessageTextView.Editable = false;
			this.MessageTextView.ScrollEnabled = false;
			this.MessageTextView.DataDetectorTypes = UIDataDetectorType.All;
			this.MessageTextView.BackgroundColor = UIColor.Clear;
			this.MessageTextView.Selectable = true;
			this.MessageTextView.ShouldInteractWithUrl = ShouldInteractWithUrl;
			this.MessageTextView.TextAlignment = UIDevice.CurrentDevice.IsRightLeftLanguage () ? UITextAlignment.Right : UITextAlignment.Left;
			this.Add (this.MessageTextView);
		}

		bool ShouldInteractWithUrl (UITextView textView, NSUrl url, NSRange characterRange) {
			return true;
		}

		public override void LayoutSubviews() {
			base.LayoutSubviews ();
		}

		public void UpdateAttributedString () {
			// https://stackoverflow.com/questions/19121367/uitextviews-in-a-uitableview-link-detection-bug-in-ios-7/19589680#19589680
			// gist: attributed string stores link information, so we need to reset it everytime to get the right link
			UIStringAttributes defaultAttributes = new UIStringAttributes {
				ForegroundColor = this.MessageTextView.TextColor,
				Font = this.MessageTextView.Font
			};

			NSMutableAttributedString prettyString = new NSMutableAttributedString (this.MessageTextView.Text, defaultAttributes);
			this.MessageTextView.AttributedText = prettyString;
		}
	}
}

