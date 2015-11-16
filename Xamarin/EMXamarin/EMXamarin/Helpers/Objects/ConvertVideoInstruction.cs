using System;
using System.Diagnostics;

namespace em {
	public class ConvertVideoInstruction {
		public string InputPath { get; set; }
		public string OutputPath { get; set; }
		public Action<bool, QueueEntry> QueueEntryCallback { get; set; }
		public Action<bool> RegularCallback { get; set; }
		public QueueEntry QueueEntry { get; set; }

		public ConvertVideoInstruction (string path, Action<bool> callback) {
			this.InputPath = ApplicationModel.SharedPlatform.GetFileSystemManager ().ResolveSystemPathForUri (path);
			this.RegularCallback = callback;
			this.OutputPath = this.InputPath.Insert (this.InputPath.LastIndexOf ("."), "1");
		}	

		public ConvertVideoInstruction (string path, Action<bool, QueueEntry> callback, QueueEntry queueEntry) {
			this.InputPath = ApplicationModel.SharedPlatform.GetFileSystemManager ().ResolveSystemPathForUri (path);
			this.QueueEntryCallback = callback;
			this.QueueEntry = queueEntry;
			this.OutputPath = this.InputPath.Insert (this.InputPath.LastIndexOf ("."), "1");
		}	

		public void DidFinishInstruction (bool success) {
			if (this.QueueEntryCallback != null) {
				this.QueueEntryCallback (success, this.QueueEntry);
			} else {
				Debug.Assert (this.QueueEntry == null, "ConvertVideoInstruction - Using regular callback but queue entry is not null.");
				this.RegularCallback (success);
			}
		}

		public bool FromOutgoingQueue {
			get {
				return this.QueueEntry != null;
			}
		}
	}
}

