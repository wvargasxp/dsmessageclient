using System;

namespace em {
	public class StateUpdateQueueInstruction : QueueInstruction {
		private OutgoingQueueState newState;
		public OutgoingQueueState NewState {
			get {
				return newState;
			}
		}

		public StateUpdateQueueInstruction (OutgoingQueueState newState) {
			this.newState = newState;
		}
	}
}

