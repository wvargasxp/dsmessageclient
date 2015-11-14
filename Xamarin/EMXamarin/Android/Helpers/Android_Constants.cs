using System;
using Android.Graphics;
using em;

namespace Emdroid {
	public static class Android_Constants {
		public static Color OddColor = Color.Rgb(Constants.RGB_ODD_INBOX_ROW[0], Constants.RGB_ODD_INBOX_ROW[1], Constants.RGB_ODD_INBOX_ROW[2]);
		public static Color EvenColor = Color.Rgb(Constants.RGB_EVEN_INBOX_ROW[0], Constants.RGB_EVEN_INBOX_ROW[1], Constants.RGB_EVEN_INBOX_ROW[2]);
		public static Color SelectedOddColor = Color.Rgb(Constants.RGB_ODD_INBOX_ROW[0]-20, Constants.RGB_ODD_INBOX_ROW[1]-20, Constants.RGB_ODD_INBOX_ROW[2]-20);
		public static Color SelectedEvenColor = Color.Rgb(Constants.RGB_EVEN_INBOX_ROW[0]-20, Constants.RGB_EVEN_INBOX_ROW[1]-20, Constants.RGB_EVEN_INBOX_ROW[2]-20);

		public static readonly Color BLUE_COLOR = Color.Rgb (Constants.RGB_BLUE_COLOR [0], Constants.RGB_BLUE_COLOR [1], Constants.RGB_BLUE_COLOR [2]);
		public static readonly Color ORANGE_COLOR = Color.Rgb (Constants.RGB_ORANGE_COLOR [0], Constants.RGB_ORANGE_COLOR [1], Constants.RGB_ORANGE_COLOR [2]);
		public static readonly Color PINK_COLOR = Color.Rgb (Constants.RGB_PINK_COLOR [0], Constants.RGB_PINK_COLOR [1], Constants.RGB_PINK_COLOR [2]);
		public static readonly Color GREEN_COLOR = Color.Rgb (Constants.RGB_GREEN_COLOR [0], Constants.RGB_GREEN_COLOR [1], Constants.RGB_GREEN_COLOR [2]);
		public static readonly Color GRAY_COLOR = Color.Rgb (Constants.RGB_GRAY_COLOR [0], Constants.RGB_GRAY_COLOR [1], Constants.RGB_GRAY_COLOR [2]);
		public static readonly Color WHITE_COLOR = Color.Rgb (Constants.RGB_WHITE_COLOR [0], Constants.RGB_WHITE_COLOR [1], Constants.RGB_WHITE_COLOR [2]);
		public static readonly Color BLACK_COLOR = Color.Rgb (Constants.RGB_BLACK_COLOR [0], Constants.RGB_BLACK_COLOR [1], Constants.RGB_BLACK_COLOR [2]);
		public static readonly Color PURPLE_COLOR = Color.Rgb (Constants.RGB_PURPLE_COLOR [0], Constants.RGB_PURPLE_COLOR [1], Constants.RGB_PURPLE_COLOR [2]);

		/* Resolution to load thumbnails in width x height square */
		public static readonly int ROUNDED_THUMBNAIL_SIZE = 600;

		/* Resolution to load alias icons in width x height square */
		public static readonly int ALIAS_ICON_SIZE = 50;

		// sleightly inflate thumbnail sizes on Android due to the tendency for the screens
		// to be larger.  But still tying it back to the same original constant.
		public static readonly int PORTRAIT_CHAT_THUMBNAIL_HEIGHT = (int) (Constants.PORTRAIT_CHAT_THUMBNAIL_HEIGHT);
		public static readonly int LANDSCAPE_CHAT_THUMBNAIL_WIDTH = (int) (Constants.LANDSCAPE_CHAT_THUMBNAIL_WIDTH);

		// Width of the waveform will always be wider than its height so no need for two separate values like the constants for thumbnail height.
		public static readonly int AUDIO_WAVEFORM_THUMBNAIL_HEIGHT = 30;

		public static int IncomingMessageResource {
			get {
				return Resource.Raw.m;
			}
		}

		public static int InAppIncomingMessageResource {
			get {
				return Resource.Raw.InAppIncomingMessage;
			}
		}

		public static readonly long DELETE_ANIMATION_DURATION_MILLIS = Constants.FADE_ANIMATION_DURATION_MILLIS;
		public static readonly long INSERT_ANIMATION_DURATION_MILLIS = Constants.FADE_ANIMATION_DURATION_MILLIS;
		public static readonly long MOVE_ANIMATION_DURATION_MILLIS = Constants.FADE_ANIMATION_DURATION_MILLIS;
		public static readonly int DELAY_BEFORE_CHECKING_LISTVIEW_ANIMATION_RUNNING = 200; 

		public static readonly int JPEG_CONVERSION_QUALITY = (int) (Constants.JPEG_CONVERSION_QUALITY * 100.0);

		public static readonly long VIBRATE_DURATION_MILLIS = 300;

		public static readonly double SOUND_RECORDING_DISTANCE_DELTA_THRESHOLD = 100;

		public static string AndroidSoundRecordingRecorder_HasUpdatedAmplitude = "AndroidSoundRecordingRecorder_HasUpdatedAmplitude";
		public static string AndroidSoundRecordingRecorder_LastAmplitudeKey = "AndroidSoundRecordingRecorder_LastAmplitudeKey";
	}
}

