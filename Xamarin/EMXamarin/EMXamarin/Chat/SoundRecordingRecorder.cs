using System;

namespace em {
	public interface SoundRecordingRecorder : IDisposable {

		Action<string> DelegateDidSavedRecording { get; set; }
		Action DelegateDidBeginFileExport { get; set; }
		Action DelegateDidFailFileExport { get; set; }

		void Record (string savePath);
		void Stop ();
		void Cancel ();
	}
}

