using System;

namespace em {
	public enum OtherApp {
		WhosHere,
		Badoo,
		Skout,
		Zap, /* found only on iOS */
		Grindr,
		GrindXtra, /* unable to find */
		Blendr,
		Tinder,
		Match,
		POF,
		OKCupid,
		JDate,
		ChristianMingle,
		Zoosk,
		EHarmony,
		Whatsapp,
		FacebookMessenger,
		Line,
		Circle, /* unable to find */
		Voxer,
		Tango,
		KakaoTalk,
		Hipchat,
		GroupMe,
		Skype,
		Twitch,
		Kik,
		Keek,
		TextNow,
		Viber,
		Telegram,
		Nimbuzz,
		WeChat,
		Paltalk,
		ChatOn, /* found only on android */
		SnapChat,
		OoVoo,
		LocalChatWhatsGood, /* found only on iOS */
		Qurki /* found only on iOS */
	}

	public class AppDescriptionOutbound {
		public static readonly string WHOSHERE_APP = "WhosHere";

		public static readonly string BADOO_APP = "Badoo";
		public static readonly string SKOUT_APP = "Skout";
		public static readonly string ZAP_APP = "ZAP";
		public static readonly string GRINDR_APP = "Grindr";
		public static readonly string GRINDRXTRA_APP = "GrindXtra"; 
		public static readonly string BLENDR_APP = "Blendr";
		public static readonly string TINDER_APP = "Tinder";
		public static readonly string MATCH_APP = "Match";
		public static readonly string POF_APP = "POF";
		public static readonly string OKCUPID_APP = "OKCupid";
		public static readonly string JDATE_APP = "JDate";
		public static readonly string CHRISTIANMINGLE_APP = "ChristianMingle";
		public static readonly string ZOOSK_APP = "Zoosk";
		public static readonly string EHARMONY_APP = "eHarmony";

		public static readonly string WHATSAPP_APP = "Whatsapp";
		public static readonly string FB_APP = "FacebookMessenger";
		public static readonly string LINE_APP = "Line";
		public static readonly string CIRCLE_APP = "Circle"; 
		public static readonly string VOXER_APP = "Voxer";
		public static readonly string TANGO_APP = "tango";
		public static readonly string KAKAO_APP = "Kakaotalk";
		public static readonly string HIPCHAT_APP = "Hipchat";
		public static readonly string GROUPME_APP = "GroupMe";
		public static readonly string SKYPE_APP = "Skype";
		public static readonly string TWITCH_APP = "Twitch";
		public static readonly string KIK_APP = "kik";
		public static readonly string KEEK_APP = "KEEK";
		public static readonly string TEXTNOW_APP = "TextNow";
		public static readonly string VIBER_APP = "Viber";
		public static readonly string TELEGRAM_APP = "Telegram";
		public static readonly string NIBNUZZ_APP = "Nimbuzz";
		public static readonly string WECHAT_APP = "WeChat";
		public static readonly string PALTALK_APP = "Paltalk";
		public static readonly string CHATON_APP = "ChatON";
		public static readonly string SNAPCHAT_APP = "Snapchat";
		public static readonly string OOVOO_APP = "OoVoo";
		public static readonly string LOCAL_CHAT_WHATS_GOOD_APP = "LocalChatWhatsGood";
		public static readonly string QURKI_APP = "Qurki"; 

		public string app { get; set; }
		public bool installed { get; set; }

		public static string DescriptionOf (OtherApp app) {
			switch (app) {
			default:
			case OtherApp.WhosHere:
				return WHOSHERE_APP;
			case OtherApp.Badoo:
				return BADOO_APP;
			case OtherApp.Skout:
				return SKOUT_APP;
			case OtherApp.Zap:
				return ZAP_APP;
			case OtherApp.Grindr:
				return GRINDR_APP;
			case OtherApp.GrindXtra:
				return GRINDRXTRA_APP;
			case OtherApp.Blendr:
				return BLENDR_APP;
			case OtherApp.Tinder:
				return TINDER_APP;
			case OtherApp.Match:
				return MATCH_APP;
			case OtherApp.POF:
				return POF_APP;
			case OtherApp.OKCupid:
				return OKCUPID_APP;
			case OtherApp.JDate:
				return JDATE_APP;
			case OtherApp.ChristianMingle:
				return CHRISTIANMINGLE_APP;
			case OtherApp.Zoosk:
				return ZOOSK_APP;
			case OtherApp.EHarmony:
				return EHARMONY_APP;
			case OtherApp.Whatsapp:
				return WHATSAPP_APP;
			case OtherApp.FacebookMessenger:
				return FB_APP;
			case OtherApp.Line:
				return LINE_APP;
			case OtherApp.Circle:
				return CIRCLE_APP;
			case OtherApp.Voxer:
				return VOXER_APP;
			case OtherApp.Tango:
				return TANGO_APP;
			case OtherApp.KakaoTalk:
				return KAKAO_APP;
			case OtherApp.Hipchat:
				return HIPCHAT_APP;
			case OtherApp.GroupMe:
				return GROUPME_APP;
			case OtherApp.Skype:
				return SKYPE_APP;
			case OtherApp.Twitch:
				return TWITCH_APP;
			case OtherApp.Kik:
				return KIK_APP;
			case OtherApp.Keek:
				return KEEK_APP;
			case OtherApp.TextNow:
				return TEXTNOW_APP;
			case OtherApp.Viber:
				return VIBER_APP;
			case OtherApp.Telegram:
				return TELEGRAM_APP;
			case OtherApp.Nimbuzz:
				return NIBNUZZ_APP;
			case OtherApp.WeChat:
				return WECHAT_APP;
			case OtherApp.Paltalk:
				return PALTALK_APP;
			case OtherApp.ChatOn:
				return CHATON_APP;
			case OtherApp.SnapChat:
				return SNAPCHAT_APP;
			case OtherApp.OoVoo:
				return OOVOO_APP;
			case OtherApp.LocalChatWhatsGood:
				return LOCAL_CHAT_WHATS_GOOD_APP;
			case OtherApp.Qurki:
				return QURKI_APP;
			}
		}

	}
}

