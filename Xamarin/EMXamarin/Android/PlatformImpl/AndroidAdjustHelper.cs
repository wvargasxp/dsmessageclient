using System;
using em;
using Com.Adjust.Sdk;
using System.Collections.Generic;

namespace Emdroid {
	public class AndroidAdjustHelper : IAdjustHelper {

		private static AndroidAdjustHelper _shared = null;
		public static AndroidAdjustHelper Shared {
			get {
				if (_shared == null) {
					_shared = new AndroidAdjustHelper ();
				}

				return _shared;
			}
		}

		private string AppToken {
			get {
				return "b4ybq5fc4bbj";
			}
		}

		private string Environment {
			get {
				if (AppEnv.EnvType == EnvType.Release) {
					return AdjustConfig.EnvironmentProduction;
				} else {
					return AdjustConfig.EnvironmentSandbox;
				}
			}
		}

		private AdjustDelegateXamarin _delegate = null;
		private AdjustDelegateXamarin AdjustDelegate {
			get {
				if (this._delegate == null) {
					this._delegate = new AdjustDelegateXamarin (this);
				}

				return this._delegate;
			}
		}

		public string TokenFromAdjustEvent (EmAdjustEvent adjustEvent) {
			switch (adjustEvent) {
			case EmAdjustEvent.Verified:
				return "2sfq4j";
			}

			return "2sfq4j";
		}

		public AndroidAdjustHelper () {}

		#region IAdjustHelper implementation

		public void Init () {
			AdjustConfig config = new AdjustConfig (EMApplication.Instance.BaseContext, this.AppToken, this.Environment);

			if (AppEnv.EnvType == EnvType.Release) {
				config.SetLogLevel (LogLevel.Error);
			} else {
				config.SetLogLevel (LogLevel.Error);
			}

			config.SetOnAttributionChangedListener (this.AdjustDelegate);
			Adjust.OnCreate (config);
		}

		public void SendEvent (EmAdjustEvent adjustEvent) {
			string token = TokenFromAdjustEvent (adjustEvent);
			AdjustEvent adjEvent = new AdjustEvent (token);
			Adjust.TrackEvent (adjEvent);
		}

		public void SendEvent (EmAdjustEvent adjustEvent, Dictionary<string, string> parameters) {
			string token = TokenFromAdjustEvent (adjustEvent);
			AdjustEvent adjEvent = new AdjustEvent (token);
			foreach (KeyValuePair<string, string> pair in parameters) {
				adjEvent.AddCallbackParameter (pair.Key, pair.Value);
			}


			
			
			Adjust.TrackEvent (adjEvent);
		}

		#endregion

		public void AdjustAttributionChanged (AdjustAttribution attribution) {
			System.Diagnostics.Debug.WriteLine ("Attribution changed! New attribution: {0}", attribution.ToString ());
		}
			
		public void Resume () {
			Adjust.OnResume ();
		}

		public void Pause () {
			Adjust.OnPause ();
		}
	}

	public class AdjustDelegateXamarin : Java.Lang.Object, IOnAttributionChangedListener {
		private WeakReference _r = null;
		private AndroidAdjustHelper Self {
			get { return this._r != null ? this._r.Target as AndroidAdjustHelper : null; }
			set { this._r = new WeakReference (value); }
		}

		public AdjustDelegateXamarin (AndroidAdjustHelper self) {
			this.Self = self;
		}
			
		#region IOnAttributionChangedListener implementation
		public void OnAttributionChanged (AdjustAttribution p0) {
			AndroidAdjustHelper self = this.Self;
			if (self == null) return;
			self.AdjustAttributionChanged (p0);
		}
		#endregion
	}
}

