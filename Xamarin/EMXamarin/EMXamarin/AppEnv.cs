namespace em {
	public static class AppEnv {

		public static EnvType EnvType { get; set; }			//switch to indicate which environment defaults to configure

		static ConnectionType ConnectionType { get; set; }	//switch to indicate which server you want to point to

		static AppEnv () {
			// ------- for production server  ----------
			AppEnv.ConfigureEnvironmentDefaults (EnvType.Release);
			// ------ for dev server (please comment out before commiting) ---------
			//AppEnv.ConfigureEnvironmentDefaults (EnvType.Dev);

			// ------ override default connection -------
			//AppEnv.ConnectionType = ConnectionType.Dev;		//example

			// ------ override default debug mode -------
			//AppEnv.DEBUG_MODE_ENABLED = true;					//example

			// ------ override skipping of onboarding ---
			//AppEnv.SKIP_ONBOARDING = false;					//example
		}

		private static string _domain = null;
		public static string DOMAIN {
			get {
				if (_domain == null) {
					switch (ConnectionType) {
					case ConnectionType.Dev:
//						_domain = "192.168.11.10:8080"; // JAMES
						_domain = "192.168.11.18:8080"; // BRYANT
//						_domain = "192.168.1.104:8080"; // DUC
//						_domain = "192.168.1.102:8080"; // NICK
//						_domain = "192.168.1.117:8080"; // NICK @ HOME
						break;
					case ConnectionType.Staging:
						_domain = "stagingapi.emwith.me";
						break;
					default:
						_domain = "api.emwith.me";
						break;
					}
				}

				return _domain;

			}
		}

		public static void SetDomainTo (string domain) {
			_domain = domain;
			_websocketDomain = _domain;
		}


		private static string _websocketDomain = null;
		public static string WEBSOCKET_DOMAIN {
			get {
				if (_websocketDomain == null) {
					switch (ConnectionType) {
					case ConnectionType.Dev:
						_websocketDomain = DOMAIN;
						break;
					case ConnectionType.Staging:
						_websocketDomain = "stagingws.emwith.me";
						break;
					default:
						_websocketDomain = "ws.emwith.me";
						break;
					}
				}

				return _websocketDomain;
			}
		}
			
		private static string _httpProtocol = null;
		static string HTTP_PROTOCOL {
			get {
				if (_httpProtocol == null) {
					switch(ConnectionType) {
					case ConnectionType.Dev:
						_httpProtocol = "http://";
						break;
					default:
						_httpProtocol = "https://";
						break;
					}
				}

				return _httpProtocol;
			}
		}

		public static void SwitchHttpProtocolToHTTP () {
			_httpProtocol = "http://";
		}

		private static string _websocketProtocol = null;
		static string WEBSOCKET_PROTOCOL {
			get {
				if (_websocketProtocol == null) {
					switch(ConnectionType) {
					case ConnectionType.Dev:
						_websocketProtocol = "ws://";
						break;
					default:
						_websocketProtocol = "wss://";
						break;
					}
				}

				return _websocketProtocol;
			}
		}

		public static void SwitchSecureWebsocketsToUnsecured () {
			_websocketProtocol = "ws://";
		}

		public static string HTTP_BASE_ADDRESS {
			get { return HTTP_PROTOCOL + DOMAIN; }
		}

		public static string UPLOAD_HTTP_BASE_ADDRESS {
			get {
				switch(ConnectionType) {
				case ConnectionType.Staging:
					return HTTP_PROTOCOL + "staginguploads.emwith.me";
				case ConnectionType.Dev:
					return HTTP_BASE_ADDRESS;
				default:
					return HTTP_PROTOCOL + "uploads.emwith.me";
				}
			}
		}

		public static string WEBSOCKET_BASE_ADDRESS {
			get { return WEBSOCKET_PROTOCOL + WEBSOCKET_DOMAIN; }
		}

		public static bool DEBUG_MODE_ENABLED { get; set; }

		public static bool SKIP_ONBOARDING { get; set; }

		static void ConfigureEnvironmentDefaults (EnvType envType) {
			AppEnv.EnvType = envType;

			switch (envType) {
			case EnvType.Release:
				AppEnv.ConnectionType = ConnectionType.Release;
				AppEnv.DEBUG_MODE_ENABLED = false;
				AppEnv.SKIP_ONBOARDING = false;
				break;
			case EnvType.Dev:
				AppEnv.ConnectionType = ConnectionType.Dev;
				AppEnv.DEBUG_MODE_ENABLED = true;
				AppEnv.SKIP_ONBOARDING = false;
				break;
			case EnvType.Staging:
				AppEnv.ConnectionType = ConnectionType.Staging;
				AppEnv.DEBUG_MODE_ENABLED = true;
				AppEnv.SKIP_ONBOARDING = false;
				break;
			}
		}

		public static string SkipOnboardingEmailToRegisterWith = "james@whoshere.net";
		public static string SkipOnboardingMobileToRegisterWith = "3108480993";
		public static string SkipOnboardingVerificationCode = "dfxuvi";
	}

	public enum EnvType {
		Dev,
		Release,
		Staging
	}

	public enum ConnectionType {
		Dev,
		Release,
		Staging
	}
}