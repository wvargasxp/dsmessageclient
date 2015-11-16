using System;
using em;
using AdjustBindingsiOS;
using System.Collections.Generic;
using System.Diagnostics;

namespace iOS {
	public class iOSAdjustHelper : IAdjustHelper {

		private static iOSAdjustHelper _shared = null;
		public static iOSAdjustHelper Shared {
			get {
				if (_shared == null) {
					_shared = new iOSAdjustHelper ();
				}

				return _shared;
			}
		}

		private string AppToken {
			get {
				return "g83dkl9m59g9";
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
				return "auve5f";
			default:
				return "auve5f";
			}
		}

		public iOSAdjustHelper () {}

		#region IAdjustHelper implementation

		public void Init () {
			ADJConfig config = new ADJConfig (this.AppToken, this.Environment);

			if (AppEnv.EnvType == EnvType.Release) {
				config.LogLevel = ADJLogLevel.Error;
			} else {
				config.LogLevel = ADJLogLevel.Error;
			}
				
			config.WeakDelegate = this.AdjustDelegate;
			Adjust.AppDidLaunch (config);
		}

		public void SendEvent (EmAdjustEvent adjustEvent) {
			string token = TokenFromAdjustEvent (adjustEvent);
			ADJEvent adjEvent = new ADJEvent (token);
			Adjust.TrackEvent (adjEvent);
		}

		public void SendEvent (EmAdjustEvent adjustEvent, Dictionary<string, string> parameters) {
			string token = TokenFromAdjustEvent (adjustEvent);
			ADJEvent adjEvent = new ADJEvent (token);
			foreach (KeyValuePair<string, string> pair in parameters) {
				adjEvent.AddCallbackParameter (pair.Key, pair.Value);
			}
			Adjust.TrackEvent (adjEvent);
		}
			
		#endregion

		private void SendEventWithToken (string token) {
			ADJEvent adjEvent = new ADJEvent (token);
			Adjust.TrackEvent (adjEvent);
		}

		public void AdjustAttributionChanged (ADJAttribution attribution) {
			Debug.WriteLine ("Attribution changed! New attribution: {0}", attribution.ToString ());
		}
	}
		
	public class AdjustDelegateXamarin : AdjustDelegate {
		private WeakReference _r = null;
		private iOSAdjustHelper Helper {
			get { return this._r != null ? this._r.Target as iOSAdjustHelper : null; }
			set { this._r = new WeakReference (value); }
		}

		public AdjustDelegateXamarin (iOSAdjustHelper self) {
			this.Helper = self;
		}

		public override void AdjustAttributionChanged (ADJAttribution attribution) {
			iOSAdjustHelper self = this.Helper;
			if (self == null) return;
			self.AdjustAttributionChanged (attribution);
		}
	}

}

