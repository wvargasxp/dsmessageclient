using Android.Graphics;
using Com.EM.Android;
using em;
using System;
using Android.App;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Android.Graphics.Drawables;

namespace Emdroid {
	public static class BackgroundColor_Extension {

		private static string internalDir = Application.Context.FilesDir.AbsolutePath;
		private static string externalDir = Application.Context.ExternalCacheDir.AbsolutePath;
		private static string CACHE_DIR = internalDir + "/resources/";

		public static Color GetColor (this BackgroundColor color) {
			int[] rgbArray = color.GetRGB ();
			return new Color (rgbArray [0], rgbArray [1], rgbArray [2]);
		}
		// Image resources that are in progress of being generated
		private static Dictionary<string, Semaphore> currentlyExecuting = new Dictionary<string, Semaphore>();
		// Image files that are in progress of being constructed
		private static Object mapLock = new Object ();
		private static Random rgen = new Random ();
		private static Object rgenLock = new Object ();

		public static void GetBackgroundResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, "background", callback);
		}

		private static string FormatResourceName (this BackgroundColor color, string resourceType, bool isNinePatch) {
			string pathname = CACHE_DIR;
			if (resourceType.Equals ("background")) {
				pathname += (resourceType + color.ToHexString () + ".jpg");
			} else {
				pathname += (EMApplication.Instance.Resources.GetResourceEntryName(int.Parse (resourceType)) + color.ToHexString ());
				if (isNinePatch) {
					pathname += ".9";
				}
				pathname += ".png";
			}
			return pathname;
		}
		private static void GetResource (this BackgroundColor color, string resourceType, Action<string> callback) {
			GetResource (color, resourceType, null, ((string filepath, byte[] chunk) => {
				callback (filepath);
			}));
		}

		private static void GetResource (this BackgroundColor color, string resourceType, byte[] ninePatchChunk, Action<string, byte[]> callback) {
			bool forBackground = resourceType.Equals ("background");
			string filePath = FormatResourceName (color, resourceType, ninePatchChunk != null);
			AndroidFileSystemManager fsm = (AndroidFileSystemManager)(ApplicationModel.SharedPlatform.GetFileSystemManager ());
			if (fsm.FileExistsAtPath (filePath)) {
				callback (filePath, ninePatchChunk);
			} else {
				if (forBackground) {
					if (!fsm.FileExistsAtPath (CACHE_DIR + "backgroundGray.jpg")) {
						fsm.CreateParentDirectories (CACHE_DIR + "backgroundGray.jpg");
						using (Stream inputStream = Application.Context.Resources.OpenRawResource (Resource.Drawable.bgGray)) {
							using (FileStream outputStream = new FileStream (CACHE_DIR + "backgroundGray.jpg", FileMode.Create)) {
								byte[] buffer = new byte[Constants.LARGE_COPY_BUFFER];
								int numRead = inputStream.Read (buffer, 0, buffer.Length);
								while (numRead > 0) {
									outputStream.Write (buffer, 0, numRead);
									numRead = inputStream.Read (buffer, 0, buffer.Length);
								}
							}
						}
					}
					callback (CACHE_DIR + "backgroundGray.jpg", ninePatchChunk);
				}
				EMTask.DispatchBackground (() => {
					Semaphore colorSem = null;
					lock (mapLock) {
						if (!currentlyExecuting.TryGetValue (filePath, out colorSem)) {
							colorSem = new Semaphore (1, 1);
							currentlyExecuting.Add (filePath, colorSem);
						}
					}
					colorSem.WaitOne ();
					if (!fsm.FileExistsAtPath (filePath)) {
						string tempPath = "";
						lock (rgenLock) {
							tempPath = CACHE_DIR + rgen.Next (100000) + ".tmp";
							while (fsm.FileExistsAtPath (tempPath)) {
								tempPath = CACHE_DIR + rgen.Next (100000) + ".tmp";
							}
						}
						fsm.CreateParentDirectories (tempPath);
						if (forBackground) {
							Android.Util.DisplayMetrics screenSize = Application.Context.Resources.DisplayMetrics;
							BitmapBackgroundHelper.InitializeBackground (color.ToHexString (), screenSize.WidthPixels, 
								screenSize.HeightPixels, tempPath);
						} else {
							BitmapIconHelper.InitializeIcon (int.Parse (resourceType), color.ToHexString (), tempPath, EMApplication.Instance.Resources);
						}
						fsm.MoveFileAtPath (tempPath, filePath);
					}
					colorSem.Release ();
					EMTask.DispatchMain (() => {
						callback (filePath, ninePatchChunk);
					});
				});
			}
		}

		public static void GetInboxFooterResource (this BackgroundColor color, Action<string, byte[]> callback) {
			int resourceID = Resource.Drawable.footerbluebackground;
			string filepath = FormatResourceName (color, resourceID.ToString (), true);
			byte[] chunk = BitmapIconHelper.GetNinePatchChunk (resourceID, EMApplication.Instance.Resources, filepath);
			GetResource (color, resourceID.ToString (), chunk, callback);
		}

		public static void GetInboxNewMessageResource (this BackgroundColor color, Action<string, string> callback) {
			GetResource (color, Resource.Drawable.iconWriteBlue1.ToString (), (string notPressed) => {
				GetResource (color, Resource.Drawable.iconWriteBlue2.ToString (), (string pressed) => {
					callback (notPressed, pressed);
				});
			});
		}

		public static void GetChatAddContactButtonResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.iconBlueAdd2).ToString (), callback);
		}

		public static int GetChatRecordingIndicatorResource (this BackgroundColor color) {
			return Resource.Drawable.RecordingIndicator;
		}

		public static void GetChatAttachmentsResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.AttachBlue).ToString (), callback);
		}

		public static void GetChatSendButtonResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.SendBlue).ToString (), callback);
		}

		public static void GetChatVoiceRecordingButtonResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.RecordBlue).ToString (), callback);
		}

		public static void GetRoundedRectangleResource (this BackgroundColor color, Action<string, byte[]> callback) {
			int resourceID = Resource.Drawable.roundedrectblue;
			string filepath = FormatResourceName (color, resourceID.ToString (), true);
			byte[] chunk = BitmapIconHelper.GetNinePatchChunk (resourceID, EMApplication.Instance.Resources, filepath);
			GetResource (color, Resource.Drawable.roundedrectblue.ToString (), chunk, callback);
		}

		public static void GetPhotoFrameRightResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.photoFrameBlueRight).ToString (), callback);
		}

		public static void GetPhotoFrameLeftResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.photoFrameBlueLeft).ToString (), callback);
		}

		public static void GetSoundRecordingControlPlayLineResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.RecordingPlayBlue).ToString (), callback);
		}

		public static void GetSoundRecordingControlStopLineResource (this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.RecordingStopBlue).ToString (), callback);
		}
			
		public static int GetStretchableColorSelectionSquareResource (this BackgroundColor color) {
			string hexString = color.HexString;
			if (hexString.Equals (BackgroundColor.Blue.HexString))
				return Resource.Drawable.stretchablesquareblue;
			else if (hexString.Equals (BackgroundColor.Orange.HexString))
				return Resource.Drawable.stretchablesquaregold;
			else if (hexString.Equals (BackgroundColor.Pink.HexString))
				return Resource.Drawable.stretchablesquarepink;
			else if (hexString.Equals (BackgroundColor.Green.HexString))
				return Resource.Drawable.stretchablesquaregreen;
			else if (hexString.Equals (BackgroundColor.Purple.HexString))
				return Resource.Drawable.stretchablesquarepurple;
			else
				return Resource.Drawable.stretchablesquaregray;
//			int resourceID = Resource.Drawable.stretchablesquareblue;
//			byte[] chunk = BitmapIconHelper.GetNinePatchChunk (resourceID, EMApplication.Instance.Resources);
//			GetResource (color, resourceID.ToString (), chunk, callback);
		}

		public static void GetLargePhotoBackgroundResource(this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.notDiscBlue).ToString (), callback);
		}

		public static void GetBlankPhotoAccountResource(this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.userBlankBlue).ToString (), callback);
		}

		public static void GetColorThemeSelectionImageResource(this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.changeThemeBlue).ToString (), callback);
		}

		public static void GetShareImageResource(this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.shareBlue).ToString (), callback);
		}

		public static void GetAddImageResource(this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.iconBlueAdd2).ToString (), callback);
		}

		public static void GetAkaMaskResource(this BackgroundColor color, Action<string> callback) {
			GetResource (color, (Resource.Drawable.iconAlias2Blue).ToString (), callback);
		}

		public static void GetButtonResource(this BackgroundColor color, Action<Drawable> callback) {
			GradientDrawable drawable = new GradientDrawable ();
			drawable.SetStroke (DPtoPixel (1), color.GetColor ());
			drawable.SetColor (Android.Graphics.Color.Transparent.ToArgb ());
			drawable.SetCornerRadius (DPtoPixel (10));
			callback (drawable);
		}

		private static int DPtoPixel (int dp) {
			float logicalDensity = EMApplication.Instance.Resources.DisplayMetrics.Density;
			return (int)Math.Ceiling (dp * logicalDensity);
		}
	}
}
