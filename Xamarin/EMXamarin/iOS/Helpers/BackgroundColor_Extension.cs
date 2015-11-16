using em;
using UIKit;
using UIImageExtensions;
using Media_iOS_Extension;
using Foundation;
using CoreGraphics;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace iOS {
	public static class BackgroundColor_Extension {

		public static UIColor GetColor (this BackgroundColor color) {
			int[] rgbArray = color.GetRGB ();
			return (UIColor.FromRGB (rgbArray [0], rgbArray [1], rgbArray [2])); 
		}

		private static Dictionary<string, UIImage> resourceCache = new Dictionary<string, UIImage>();
		private static Dictionary<string, Semaphore> currentlyExecuting = new Dictionary<string, Semaphore>();
		private static Object mapLock = new Object();
		private static Object dictionaryLock = new Object ();

		private static string FormatColorFileName(BackgroundColor color, UIInterfaceOrientation ori) {
			string model = "";
			string scaleString = "";
			string orientation = "";
			if (UIDevice.CurrentDevice.Model.Contains ("iPad")) {
				model = "~ipad";
			}
			int scale = (int)AppDelegate.Instance.ScreenScale;

			if (scale > 1) {
				scaleString = "@" + scale + "x";
			}
			if (ori == UIInterfaceOrientation.LandscapeLeft || ori == UIInterfaceOrientation.LandscapeRight) {
				orientation = "_L";
			}

			string prefix = ApplicationModel.SharedPlatform.GetFileSystemManager ().GetSystemPathForFolder ("Documents");
			string documents = "Documents";
			prefix = prefix.Remove (prefix.Length - documents.Length) + "Library/Caches/background" + color.ToHexString () + orientation + scaleString + model + ".png";
			return prefix;
		}

		private static string FormatIconFileName (BackgroundColor color, string iconType) {
			string scaleString = "";
			int scale = (int)AppDelegate.Instance.ScreenScale;
			if (scale > 1) {
				scaleString = "@" + scale + "x";
			}
			string prefix = ApplicationModel.SharedPlatform.GetFileSystemManager ().GetSystemPathForFolder ("Documents");
			string documents = "Documents";
			prefix = prefix.Remove (prefix.Length - documents.Length) + "Library/Caches/" + iconType + color.ToHexString () + scaleString + ".png";
			return prefix;
		}

		public static void GetBackgroundResourceForOrientation (this BackgroundColor color, UIInterfaceOrientation orientation, Action<UIImage> action) {
			color.GetResource ("background", orientation, action);
		}

		public static void GetBackgroundResource (this BackgroundColor color, Action<UIImage> action) {
			color.GetResource ("background", UIInterfaceOrientation.Portrait, action);
		}

		public static void InitializeBackground (this BackgroundColor color, CGSize screenSize) {
			UIColor backgroundColor = color.GetColor();
			nfloat red, blue, green, alpha;
			backgroundColor.GetRGBA (out red, out green, out blue, out alpha);
			uint redInt = (uint)(red * 255);
			uint greenInt = (uint)(green * 255);
			uint blueInt = (uint)(blue * 255);
			Random rgen = new Random ();
			int midX = (int)((screenSize.Width * AppDelegate.Instance.ScreenScale) / 2 );
			int width = midX * 2;
			int height = (int)(screenSize.Height * AppDelegate.Instance.ScreenScale);
			int bytesPerRow = width * 4;
			int landscapeBPR = height * 4;
			int totalBytes = bytesPerRow * height;
			byte[] pixels = new byte[totalBytes];
			byte[] landscapePixels = new byte[totalBytes];
			for (int i = 0; i < midX; i++) {
				int rowPos2 = ((midX - 1) + Math.Abs (i - midX));
				int rowOffset = i << 2;
				int rowOffset2 = rowPos2 << 2;

				int landHeightOffset = (width - 1 - i) * landscapeBPR;
				int landHeightOffset2 = (width - 1 - rowPos2) * landscapeBPR;

				float temp = (float)((midX - Math.Abs (i - midX)) * 2 * Math.PI / 3) / width;
				float adjustment = (float)Math.Sin (temp + Math.PI / 6);
				int newRed = (int)(redInt * adjustment);
				int newGreen = (int)(greenInt * adjustment);
				int newBlue = (int)(blueInt * adjustment);
				newRed = (newRed > 245 ? 245 : newRed);
				newGreen = (newGreen > 245 ? 245 : newGreen);
				newBlue = (newBlue > 245 ? 245 : newBlue);

				for (int j = 0; j < height; j++) {
					int heightOffset = j * bytesPerRow;
					int landRowOffset = j << 2;
					int offset = heightOffset + rowOffset;
					int offset2 = heightOffset + rowOffset2;
					int landOffset = landHeightOffset + landRowOffset;
					int landOffset2 = landHeightOffset2 + landRowOffset;
					UInt32 grain = (uint)(rgen.Next (10));
					byte redByte = Convert.ToByte (newRed + grain);
					byte greenByte = Convert.ToByte (newGreen + grain);
					byte blueByte = Convert.ToByte (newBlue + grain);
					byte fullByte = Convert.ToByte (0xFF);
					landscapePixels[landOffset] = landscapePixels[landOffset2] = pixels[offset] = pixels[offset2] = redByte;
					landscapePixels[landOffset + 1] = landscapePixels[landOffset2 + 1] = pixels[offset + 1] = pixels[offset2 + 1] = greenByte;
					landscapePixels[landOffset + 2] = landscapePixels[landOffset2 + 2] = pixels[offset + 2] = pixels[offset2 + 2] = blueByte;
					landscapePixels[landOffset + 3] = landscapePixels[landOffset2 + 3] = pixels [offset + 3] = pixels[offset2 + 3] = fullByte;
				}
			}

			CGBitmapContext context = new CGBitmapContext (pixels, (nint)width, (nint)height, 8, bytesPerRow, CGColorSpace.CreateDeviceRGB (), CGImageAlphaInfo.PremultipliedLast);
			UIImage newBackground = UIImage.FromImage (context.ToImage());
			NSData imageData = newBackground.AsPNG ();

			CGBitmapContext landscapeContext = new CGBitmapContext(landscapePixels, (nint)height, (nint)width, 8, 
				landscapeBPR, CGColorSpace.CreateDeviceRGB (), CGImageAlphaInfo.PremultipliedLast);
			UIImage rotatedBackground = UIImage.FromImage(landscapeContext.ToImage ());
			NSData rotatedData = rotatedBackground.AsPNG ();

			context = null;
			landscapeContext = null;
			string portraitPath = FormatColorFileName(color, UIInterfaceOrientation.Portrait);
			string landscapePath = FormatColorFileName (color, UIInterfaceOrientation.LandscapeLeft);
			iOSFileSystemManager fileManager = new iOSFileSystemManager (ApplicationModel.SharedPlatform);
			byte[] portraitBytes = imageData.ToByteArray ();
			fileManager.CopyBytesToPath(portraitPath, portraitBytes, (double d) => { });
			byte[] landscapeBytes = rotatedData.ToByteArray ();
			fileManager.CopyBytesToPath (landscapePath, landscapeBytes, (double d) => { });

			lock (dictionaryLock) {
				if (!(resourceCache.ContainsKey(portraitPath))) {
					resourceCache.Add (portraitPath, UIImage.FromFile (portraitPath));
				}
				if (!(resourceCache.ContainsKey(landscapePath))) {
					resourceCache.Add (landscapePath, UIImage.FromFile (landscapePath));
				}
			}
		}

		private static void InitializeIcon (BackgroundColor color, string iconType) {
			UIImage mask = UIImage.FromFile ("icons/" + iconType + "Mask.png");
			CGImage maskRef = mask.CGImage;
			nfloat maskWidth = mask.Size.Width;
			nfloat maskHeight = mask.Size.Height;
			nint maskBPR = (nint)(maskWidth * 4);
			nint maskBPC = 8;
			byte[] alphaBuf = new byte[(int)(maskHeight * maskBPR)];
			Array.Clear (alphaBuf, 0, alphaBuf.Length);
			CGBitmapContext maskContext = new CGBitmapContext (alphaBuf, (nint)maskWidth, (nint)maskHeight, 
				maskBPC, maskBPR, maskRef.ColorSpace, CGImageAlphaInfo.PremultipliedLast);
			maskContext.DrawImage (new CGRect (0, 0, (nfloat)maskWidth, (nfloat)maskHeight), maskRef);
			int[] backgroundColor = color.GetRGB ();
			for (int j = 0; j < maskHeight; j++) {
				for (int i = 0; i < maskWidth; i++) {
					int offset = (int)(j * maskBPR + i * 4);
					int alpha = Convert.ToInt32 (alphaBuf [offset + 3]);
					int red = Convert.ToInt32 (alphaBuf [offset]);
					int green = Convert.ToInt32 (alphaBuf [offset + 1]);
					int blue = Convert.ToInt32 (alphaBuf [offset + 2]);
					if (alpha != 0) {
						red = (255 * red) / alpha;
						green = (255 * green) / alpha;
						blue = (255 * blue) / alpha;
					}
					if (alpha == 0 || (red > 200 && green > 200 && blue > 200)) {
					}
					else if (InRange(red, green, blue, 85)) {
						red = (int)(backgroundColor [0] * ((double)alpha) / 255);
						green = (int)(backgroundColor [1] * ((double)alpha) / 255);
						blue = (int)(backgroundColor [2] * ((double)alpha) / 255);
					}
					else if (alpha == 255) {
					}
					else {
						red = green = blue = 0;
					}
					alphaBuf [offset] = Convert.ToByte (red);
					alphaBuf [offset + 1] = Convert.ToByte (green);
					alphaBuf [offset + 2] = Convert.ToByte (blue);
					alphaBuf [offset + 3] = Convert.ToByte (alpha);
				}
			}
			CGImage newIconRef = maskContext.ToImage ();
			UIImage newIcon = UIImage.FromImage (newIconRef);
			// Only the RecordingPlay and RecordingStop didn't have 3x masks
			if (!iconType.Contains ("Recording")) {
				nint maxScale = 3; // This may change in the future
				newIcon = Media_iOS_Extension.Media_UIImage_Extension.ScaleImage (newIcon, (nint)(maskWidth / maxScale));
			}
			maskContext = null;
			NSData newIconData = newIcon.AsPNG ();
			string iconPath = FormatIconFileName (color, iconType);
			byte[] newIconDataBytes = newIconData.ToByteArray ();
			ApplicationModel.SharedPlatform.GetFileSystemManager ().CopyBytesToPath (iconPath, newIconDataBytes, (double d) => { });
			lock (dictionaryLock) {
				if (!(resourceCache.ContainsKey (iconPath))) {
					resourceCache.Add (iconPath, UIImage.FromFile (iconPath));
				}
			}
		}

		private static int maskRed = 25;
		private static int maskGreen = 156;
		private static int maskBlue = 228;

		private static bool InRange(int red, int blue, int green, int threshold) {
			int otherAverage = (int)((red + green + blue)/3);
			int thisAverage = (maskRed + maskGreen + maskBlue)/3;
			return Math.Abs(otherAverage - thisAverage) < threshold;
		}

		private static int NoOverflow(int color) {
			color = color < 0 ? 0 : color;
			color = color > 255 ? 255 : color;
			return color;
		}

		private static string GetIconResource (BackgroundColor color, string iconType) {
			string targetPath = FormatIconFileName (color, iconType);
			if (!ApplicationModel.SharedPlatform.GetFileSystemManager ().FileExistsAtPath (targetPath)) {
				InitializeIcon (color, iconType);
			}
			return targetPath;
		}

		private static string FormatResourceFileName (BackgroundColor color, string resourceName, UIInterfaceOrientation ori) {
			string filename = "";
			if (resourceName.Contains ("background")) {
				if (ori == UIInterfaceOrientation.Unknown) {
					ori = UIInterfaceOrientation.Portrait;
				}
				filename = FormatColorFileName (color, ori);	
			} else {
				filename = FormatIconFileName (color, resourceName);
			}
			return filename;
		}

		private static void GetResource (this BackgroundColor color, string resourceName, UIInterfaceOrientation ori, Action<UIImage> callback) {
			string targetPath = FormatResourceFileName (color, resourceName, ori);
			iOSFileSystemManager fileManager = (iOSFileSystemManager)ApplicationModel.SharedPlatform.GetFileSystemManager ();
			UIImage resource = null;
			if (ApplicationModel.SharedPlatform.GetFileSystemManager ().FileExistsAtPath (targetPath)) {
				lock (dictionaryLock) {
					if (!resourceCache.TryGetValue (targetPath, out resource)) {
						resource = UIImage.FromFile (targetPath);
						if (resource != null) {
							resourceCache.Add (targetPath, resource);
						}
					}
				}
				if (resource == null) {
					GenerateResource (color, resourceName, ori, callback);
				} else {
					callback (resource);
				}
			} else {
				GenerateResource (color, resourceName, ori, callback);
			}
		}

		private static void GenerateResource (this BackgroundColor color, string resourceName, UIInterfaceOrientation ori, Action<UIImage> callback) {
			CGSize screenSize = new CGSize (0, 0); // screenSize.width == 0 true only if we're getting background
			if (resourceName.Contains ("background")) {
				screenSize = UIScreen.MainScreen.Bounds.Size; //from now on, can check if this value is null rather than doing .Contains("background")
				callback (Media_iOS_Extension.Media_UIImage_Extension.ScaleImage (UIImage.FromFile ("backgrounds/bgGray@3x.jpg"), (nint)screenSize.Width));
			}
			EMTask.DispatchBackground (() => {
				Semaphore colorSem = null;
				lock (mapLock) {
					if (!currentlyExecuting.TryGetValue (color.ToHexString (), out colorSem)) {
						colorSem = new Semaphore (1, 1);
						currentlyExecuting.Add (color.ToHexString (), colorSem);
					}
				}
				colorSem.WaitOne ();
				string targetPath = FormatResourceFileName (color, resourceName, ori);
				if (!ApplicationModel.SharedPlatform.GetFileSystemManager ().FileExistsAtPath (targetPath)) {
					if (screenSize.Width > 0) {
						InitializeBackground (color, screenSize);
					} else {
						InitializeIcon (color, resourceName);
					}
				}
				lock (mapLock) {
					Semaphore colorSem2 = null;
					currentlyExecuting.TryGetValue (color.ToHexString (), out colorSem2);
					colorSem2.Release ();
				}
				if (screenSize.Width > 0 && color == BackgroundColor.Gray) return;
				EMTask.DispatchMain (() => {
					callback (UIImage.FromFile (targetPath));
				});
			});
		}
			
		public static void GetChatAddContactButtonResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "iconAdd2", UIInterfaceOrientation.Unknown, callback);
		}

		public static string GetChatRecordingIndicatorResource (this BackgroundColor color) {
			return "chat/RecordingIndicator.png";
		}

		public static void GetChatAttachmentsResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "Attach", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetChatSendButtonResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "Send", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetChatVoiceRecordingButtonResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "iconRecord", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetSoundRecordingControlPlayInlineResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "RecordingPlay", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetSoundRecordingControlStopInlineResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "RecordingStop", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetRoundedRectangleResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "Rounded-Rectangle-", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetPhotoFrameRightResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "photoFrameRight", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetPhotoFrameLeftResource (this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "photoFrameLeft", UIInterfaceOrientation.Unknown, callback);
		}

		public static string GetColorSelectionSquareResource (this BackgroundColor color) {
			string hexString = color.HexString;
			if (hexString.Equals(BackgroundColor.Blue.HexString))
				return "onboarding/squareBlue.png";
			else if (hexString.Equals(BackgroundColor.Orange.HexString))
				return "onboarding/squareGold.png";
			else if (hexString.Equals(BackgroundColor.Pink.HexString))
				return "onboarding/squarePink.png";
			else if (hexString.Equals(BackgroundColor.Green.HexString))
				return "onboarding/squareGreen.png";
			else if (hexString.Equals(BackgroundColor.Purple.HexString))
				return "onboarding/squarePurple.png";
			else 
				return "onboarding/squareGray.png";
		}

		public static void GetBlankPhotoAccountResource(this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "userBlank", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetColorThemeSelectionImageResource(this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "changeTheme", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetShareImageResource(this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "share", UIInterfaceOrientation.Unknown, callback);
		}

		public static void GetAddImageResource(this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "iconAdd2", UIInterfaceOrientation.Unknown, callback);
		}
		public static void GetAKAMaskResource(this BackgroundColor color, Action<UIImage> callback) {
			GetResource (color, "aka", UIInterfaceOrientation.Unknown, callback);
		}
	}
}