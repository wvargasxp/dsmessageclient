using System;
using UIKit;
using em;
using Foundation;

namespace iOS {
	public class iOS_Constants {
		public static readonly int CHAT_MESSAGE_BUBBLE_PADDING = 10;

		public static readonly UIColor BLUE_COLOR = UIColor.FromRGB (Constants.RGB_BLUE_COLOR [0],
																	 	Constants.RGB_BLUE_COLOR [1],
																	 	Constants.RGB_BLUE_COLOR [2]);

		public static readonly UIColor ORANGE_COLOR = UIColor.FromRGB (Constants.RGB_ORANGE_COLOR [0],
																	   	Constants.RGB_ORANGE_COLOR [1],
																	   	Constants.RGB_ORANGE_COLOR [2]);

		public static readonly UIColor PINK_COLOR = UIColor.FromRGB (Constants.RGB_PINK_COLOR [0], 
																	 	Constants.RGB_PINK_COLOR [1], 
			 														 	Constants.RGB_PINK_COLOR [2]);

		public static readonly UIColor GREEN_COLOR = UIColor.FromRGB (Constants.RGB_GREEN_COLOR [0], 
																	  	Constants.RGB_GREEN_COLOR [1], 
															 		  	Constants.RGB_GREEN_COLOR [2]);

		public static readonly UIColor GRAY_COLOR = UIColor.FromRGB (Constants.RGB_GRAY_COLOR [0], 
																	 	Constants.RGB_GRAY_COLOR [1], 
																     	Constants.RGB_GRAY_COLOR [2]);

		public static readonly UIColor WHITE_COLOR = UIColor.FromRGB (Constants.RGB_WHITE_COLOR [0], 
																      	Constants.RGB_WHITE_COLOR [1], 
																	  	Constants.RGB_WHITE_COLOR [2]);

		public static readonly UIColor BLACK_COLOR = UIColor.FromRGB (Constants.RGB_BLACK_COLOR [0],
					                                             		Constants.RGB_BLACK_COLOR [1],
						                                             	Constants.RGB_BLACK_COLOR [2]);

		public static readonly UIColor PURPLE_COLOR = UIColor.FromRGB (Constants.RGB_PURPLE_COLOR [0],
																		Constants.RGB_PURPLE_COLOR[1], 
																		Constants.RGB_PURPLE_COLOR[2]);

		public static readonly UIColor INBOX_ROW_SEPERATOR_COLOR = UIColor.FromRGB (Constants.RGB_INBOX_ROW_SEPERATOR_COLOR [0], 
																		Constants.RGB_INBOX_ROW_SEPERATOR_COLOR [1], 
																		Constants.RGB_INBOX_ROW_SEPERATOR_COLOR [2]);

		public static readonly UIColor ODD_COLOR = UIColor.FromRGB (Constants.RGB_ODD_INBOX_ROW[0], 
																		Constants.RGB_ODD_INBOX_ROW[1], 
																		Constants.RGB_ODD_INBOX_ROW[2]);
		public readonly static UIColor EVEN_COLOR = UIColor.FromRGB (Constants.RGB_EVEN_INBOX_ROW [0],
																		Constants.RGB_EVEN_INBOX_ROW [1],
																		Constants.RGB_EVEN_INBOX_ROW [2]);

		public readonly static UIColor NEUTRAL_COLOR = UIColor.FromRGB (Constants.RGB_NEUTRAL_COLOR [0],
																			Constants.RGB_NEUTRAL_COLOR [1],
																			Constants.RGB_NEUTRAL_COLOR [2]);

		public static readonly int COLOR_BAND_WIDTH = 5;
		public static readonly int APP_CELL_ROW_HEIGHT = 75; // row height used by inboxcell, contact cell, notifcell, groupcells
		public static readonly string DEFAULT_NO_IMAGE = "userDude.png";
		public static readonly int LEFT_DRAWER_WIDTH = 200;
		public static readonly nfloat DEFAULT_CORNER_RADIUS = 3.0f;
		public static readonly nfloat EDIT_ALIAS_VIEW_CORNER_RADIUS = 6.0f;

		public static readonly int RECORDING_BUTTON_BUBBLE_SIZE = 24;
		public static readonly int RECORDING_BUTTON_BUBBLE_RIGHT_PADDING = 5;

		public static readonly float AKA_MASK_ICON_HEIGHT = 12;
		public static readonly float AKA_MASK_ICON_WIDTH = 21;

		static string aliasIconPath = null;
		public static string DEFAULT_ALIAS_ICON_IMAGE {
			get {
				if (aliasIconPath == null)
					aliasIconPath = NSBundle.MainBundle.PathForResource ("Icon", "png");
				return aliasIconPath;
			}
		}

		#region custom selectors
		public const string RemoteDeleteSelector = "RemoteDeleteSelector:";
		#endregion

		public static string NOTIFICATION_REMOTE_DELETE_SELECTED = "RemoteDeleteSelectedNotification";
		public static string NOTIFICATION_CHAT_COPY_SELECTED = "ChatCopySelectedNotification";
		public static string NOTIFICATION_CHAT_TEXTVIEW_TAPPED = "ChatTextViewTappedNotification";
		public static string NOTIFICATION_TEXTVIEW_BECAME_FIRST_RESPONDER = "TextViewBecameFirstResponderNotification";

		public static readonly string DID_RECEIVE_MEMORY_WARNING = "appdidreceivememorywarning";

		public static readonly float FADE_ANIMATION_DURATION = ((float)Constants.FADE_ANIMATION_DURATION_MILLIS) / 1000f;

		public static readonly float ODD_EVEN_BACKGROUND_COLOR_CORRECTION_PAUSE = ((float)Constants.ODD_EVEN_BACKGROUND_COLOR_CORRECTION_PAUSE) / 1000f;
	}
}

