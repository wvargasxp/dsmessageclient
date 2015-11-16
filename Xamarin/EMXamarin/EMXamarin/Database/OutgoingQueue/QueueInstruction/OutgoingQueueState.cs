using System;
using System.Threading.Tasks;

namespace em {
	public interface OutgoingQueueState : QueueInstruction {
		void Begin ();
		void End ();
		void ProcessQueueEntryInstruction (QueueEntryInstruction instruction);
		void Event (QueueEntryEvent queueEntryEvent);
	}
}