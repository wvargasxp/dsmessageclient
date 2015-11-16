using System;

namespace em {
	public interface SoundRecordingPlayer : IDisposable {
		SoundRecordingPlayerState State { get; set; }

		Action DelegateDidFinishedPlaying { get; set; }
		void Play (String pathToSoundFile);
		void Stop ();
		
	}
}

