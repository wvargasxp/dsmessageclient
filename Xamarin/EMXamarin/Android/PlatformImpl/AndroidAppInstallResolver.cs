using System;
using em;
using Android.Content;

namespace Emdroid {
	public class AndroidAppInstallResolver : IInstalledAppResolver {

		private static AndroidAppInstallResolver _shared;
		public static AndroidAppInstallResolver Shared {
			get {
				if (_shared == null) {
					_shared = new AndroidAppInstallResolver ();
				}

				return _shared;
			}
		}

		public AndroidAppInstallResolver () {}

		#region IInstalledAppResolver implementation
		public bool AppInstalled (OtherApp app) {
			Context ctx = EMApplication.Context;
			string packageName = app.PackageName ();
			bool appInstalled = AndroidDeviceInfo.IsPackageInstalled (packageName, ctx);
			return appInstalled;
		}
		#endregion
	}

	public static class AndroidOtherAppExtension {
		public static string PackageName (this OtherApp app) {
			switch (app) {
			default:
			case OtherApp.WhosHere:
				return "com.whoshere.whoshere";
			case OtherApp.Badoo:
				return "com.badoo.mobile";
			case OtherApp.Skout:
				return "com.skout.android";
			case OtherApp.Zap:
				return string.Empty;
			case OtherApp.Grindr:
				return "com.grindrapp.android";
			case OtherApp.GrindXtra:
				return string.Empty;
			case OtherApp.Blendr:
				return "com.blendr.mobile";
			case OtherApp.Tinder:
				return "com.tinder";
			case OtherApp.Match:
				return "com.match.android.matchmobile";
			case OtherApp.POF:
				return "com.pof.android";
			case OtherApp.OKCupid:
				return "com.okcupid.okcupid";
			case OtherApp.JDate:
				return "com.spark.jdate";
			case OtherApp.ChristianMingle:
				return "com.spark.christianmingle";
			case OtherApp.Zoosk:
				return "com.zoosk.zoosk";
			case OtherApp.EHarmony:
				return "com.eharmony";
			case OtherApp.Whatsapp:
				return "com.whatsapp";
			case OtherApp.FacebookMessenger:
				return "com.facebook.orca";
			case OtherApp.Line:
				return "jp.naver.line.android";
			case OtherApp.Circle:
				return string.Empty;
			case OtherApp.Voxer:
				return "com.rebelvox.voxer";
			case OtherApp.Tango:
				return "com.sgiggle.production";
			case OtherApp.KakaoTalk:
				return "com.kakao.talk";
			case OtherApp.Hipchat:
				return "com.hipchat";
			case OtherApp.GroupMe:
				return "com.groupme.android";
			case OtherApp.Skype:
				return "com.skype.raider";
			case OtherApp.Twitch:
				return "tv.twitch.android.app";
			case OtherApp.Kik:
				return "kik.android";
			case OtherApp.Keek:
				return "com.keek";
			case OtherApp.TextNow:
				return "com.enflick.android.TextNow";
			case OtherApp.Viber:
				return "com.viber.voip";
			case OtherApp.Telegram:
				return "org.telegram.messenger";
			case OtherApp.Nimbuzz:
				return "com.nimbuzz";
			case OtherApp.WeChat:
				return "com.tencent.mm";
			case OtherApp.Paltalk:
				return "com.paltalk.chat.android";
			case OtherApp.ChatOn:
				return "com.sec.chaton";
			case OtherApp.SnapChat:
				return "com.snapchat.android";
			case OtherApp.OoVoo:
				return "com.oovoo";
			case OtherApp.LocalChatWhatsGood:
				return string.Empty;
			case OtherApp.Qurki:
				return string.Empty;
			}
		}
	}
}

