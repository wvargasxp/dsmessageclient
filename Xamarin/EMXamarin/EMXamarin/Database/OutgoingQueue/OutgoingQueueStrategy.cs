using System;
using System.Threading.Tasks;

namespace em {
	public interface OutgoingQueueStrategy {
		void Enqueue (QueueInstruction instruction);
		void Send (QueueEntryInstruction instruction);
		void Start ();
		void Stop ();
	}
}