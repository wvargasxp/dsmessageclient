using System;
using em;
using UIKit;
using Foundation;

namespace iOS {
	public class iOSAppInstallResolver : IInstalledAppResolver {

		private static iOSAppInstallResolver _shared;
		public static iOSAppInstallResolver Shared {
			get {
				if (_shared == null) {
					_shared = new iOSAppInstallResolver ();
				}

				return _shared;
			}
		}

		public iOSAppInstallResolver () {}
			
		#region IInstalledAppResolver implementation
		public bool AppInstalled (OtherApp app) {
			string urlForApp = app.UrlString ();

			bool appInstalled = UIApplication.SharedApplication.CanOpenUrl (NSUrl.FromString (urlForApp));
			return appInstalled;
		}
		#endregion
	}

	public static class IOSOtherAppExtension {
		public static string UrlString (this OtherApp app) {
			switch (app) {
			default:
			case OtherApp.WhosHere:
				return "whoshere://";
			case OtherApp.Skout:
				return string.Empty; // couldn't find url
			case OtherApp.Badoo:
				return "bds://";
			case OtherApp.Zap:
				return string.Empty; // couldn't find url
			case OtherApp.Grindr:
				return "grindr://";
			case OtherApp.GrindXtra:
				return string.Empty;
			case OtherApp.Blendr:
				return "bdb://";
			case OtherApp.Tinder:
				return "tinder://";
			case OtherApp.Match:
				return string.Empty; // cound't find url
			case OtherApp.POF:
				return string.Empty; // couldn't find url
			case OtherApp.OKCupid:
				return "okcupid://";
			case OtherApp.JDate:
				return string.Empty; // couldn't find url
			case OtherApp.ChristianMingle:
				return string.Empty; // couldn't find url
			case OtherApp.Zoosk:
				return "zoosk://";
			case OtherApp.EHarmony:
				return string.Empty; // couldn't find url
			case OtherApp.Whatsapp:
				return "whatsapp://";
			case OtherApp.FacebookMessenger:
				return "fb-messenger://";
			case OtherApp.Line:
				return "line://";
			case OtherApp.Circle:
				return string.Empty;
			case OtherApp.Voxer:
				return "voxer://";
			case OtherApp.Tango:
				return "tango://";
			case OtherApp.KakaoTalk:
				return "kakaotalk://";
			case OtherApp.Hipchat:
				return string.Empty; // couldn't find url
			case OtherApp.GroupMe:
				return "groupme://";
			case OtherApp.Skype:
				return "skype://";
			case OtherApp.Twitch:
				return "twitch://";
			case OtherApp.Kik:
				return "kik-share://";
			case OtherApp.Keek:
				return "keek://";
			case OtherApp.TextNow:
				return "textnow://";
			case OtherApp.Viber:
				return "viber://";
			case OtherApp.Telegram:
				return string.Empty; // couldn't find url
			case OtherApp.Nimbuzz:
				return string.Empty; // couldn't find url
			case OtherApp.WeChat:
				return "wechat://";
			case OtherApp.Paltalk:
				return string.Empty; // couldn't find url
			case OtherApp.ChatOn:
				return string.Empty; // couldn't find url
			case OtherApp.SnapChat:
				return "snapchat://";
			case OtherApp.OoVoo:
				return "oovoo.special.scheme://";
			case OtherApp.LocalChatWhatsGood:
				return string.Empty; // couldn't find url
			case OtherApp.Qurki:
				return string.Empty; // couldn't find url
			}
		}
	}
}

