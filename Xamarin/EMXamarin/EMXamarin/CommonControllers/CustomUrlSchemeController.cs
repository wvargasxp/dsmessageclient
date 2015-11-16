using System;
using System.Collections.Generic;

namespace em {
	public class CustomUrlSchemeController {

		readonly HttpQueryStringParser parser = new HttpQueryStringParser ();
		readonly ApplicationModel appModel;

		public CustomUrlSchemeController (ApplicationModel appModel) {
			this.appModel = appModel;
		}

		/**
		 * the em:// protocol 
		 * 		//login 	takes query parameters:
		* 				vc	verification code
		*
		*              ex. em://v?c=11111
		*/
		public void Handle (Uri url) {
			string queryString = url.Query;
			if (queryString == null)
				return;

			//this method URL decodes the query string values
			Dictionary<string, string> parameterMap = parser.Parse (queryString);

			try {
				// to keep the url short, we use the HOST part as PATH semantically
				switch (url.Host) {
				//verification format: em://v?c=verification_code
				case "v" :
					string verificationCode = parameterMap ["c"];
					System.Diagnostics.Debug.WriteLine ("verificationCode is " + verificationCode);

					ISecurityManager securityManager = appModel.platformFactory.GetSecurityManager ();
					securityManager.SaveSecureKeyValue (Constants.URL_QUERY_VERIFICATION_CODE_KEY, verificationCode);

					break;
				//new message format: em://nm?t=toAka&f=fromAka
				//currently, this is only hit for iOS. Android hits the MainActivity as it has it's intent registered
				case "nm" :
					string toAka = parameterMap ["t"];
					string fromAka = parameterMap ["f"];

					if(!string.IsNullOrEmpty(toAka)) {
						var prepopulatedInfo = new PrepopulatedChatEntryInfo(toAka, fromAka);

						ChatEntry chatEntry = ChatEntry.NewUnderConstructionChatEntry (appModel, DateTime.Now.ToEMStandardTime(appModel));
						chatEntry.prePopulatedInfo = prepopulatedInfo;

						INavigationManager navigationManager = appModel.platformFactory.GetNavigationManager ();
						navigationManager.StartNewChat(chatEntry);

						System.Diagnostics.Debug.WriteLine ("Send new message. To AKA: " + toAka + " From AKA: " + fromAka);
					}
					break;
				case "whoshereregistration":
					//do nothing
					break;
				default :
					System.Diagnostics.Debug.WriteLine ("Unknown em:// URL to handle! Host: " + url.Host + " QueryString: " + queryString);
					break;
				}

			} catch (KeyNotFoundException) {
				// do nothing
			}
		}
	}
}