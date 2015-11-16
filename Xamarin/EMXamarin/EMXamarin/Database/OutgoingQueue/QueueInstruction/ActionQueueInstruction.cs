using System;

namespace em {
	public class ActionQueueInstruction :QueueInstruction {

		private Action action;

		public ActionQueueInstruction (Action action) {
			this.action = action;
		}

		public void Execute () {
			action ();
		}
	}
}