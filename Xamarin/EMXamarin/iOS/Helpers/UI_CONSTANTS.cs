using System;
using UIKit;
using em;

namespace iOS {
	public class UI_CONSTANTS {
		public static readonly int COLORED_SQUARE_SIZE = 47;

		public static readonly int STINY_MARGIN = 2; 
		public static readonly int TINY_MARGIN = 5; // used when we want a small space between elements
		public static readonly int SMALL_MARGIN = 10; // used to add some length/width to a button so that the button isn't hugging its uilabel
		public static readonly int EXTRA_MARGIN = 20; // some form of padding used when a specific number isn't needed, just used to space ui elements apart
		public static readonly int BUTTON_VERTICAL_PADDING = 20; // padding used to space text in UIButtons
		public static readonly int BUTTON_HORIZONTAL_PADDING = 20;

		public static readonly int LABEL_PADDING = 30;
		public static readonly int TEXTFIELD_HEIGHT = 40;

		public static readonly int ONBOARDING_TEXT_FIELD_LEFT_CAP = 20;
		public static readonly int ONBOARDING_TEXT_FIELD_TOP_CAP = 15;

		public static readonly int ADD_BUTTON_IMAGE_SIZE = 27;
		public static readonly int TO_LABEL_SIZE = 27; // the size of the label "To:" in ChatViewController and ContactsViewController
	}
}

