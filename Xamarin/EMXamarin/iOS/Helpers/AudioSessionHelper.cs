using System;
using AVFoundation;
using Foundation;

namespace iOS {
	public class AudioSessionHelper {

		public int CurrentSessionId { get; set; }

		private static AudioSessionHelper sharedInstance = null;

		public static AudioSessionHelper SharedInstance () {
			if (AudioSessionHelper.sharedInstance == null) {
				AudioSessionHelper.sharedInstance = new AudioSessionHelper ();
			}

			return AudioSessionHelper.sharedInstance;
		}
		
		public void PrepareAudioSessionForPlayback (int sessionId) {
			this.CurrentSessionId = sessionId;

			AVAudioSession audioSession = AVAudioSession.SharedInstance ();
			NSError err = null;
			err = audioSession.SetCategory (AVAudioSessionCategory.Playback);
			if (err != null) {
				throw new Exception ("cannot setup audio session " + err);

			}
			err = audioSession.SetActive (true);
			if (err != null ){
				throw new Exception ("cannot setup audio session " + err);
			}
		}

		/**
		 * the last actor to prepare the audio session should be the one allowed to reset the session
		 */
		public void ReturnAudioSessionToDefault (int sessionId) {
			if (this.CurrentSessionId == sessionId) {
				AppDelegate.Instance.SetAudioSessionToRespectSilence ();
			}
		}
	}
}

