using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;

namespace em {
	public class DefaultOutgoingQueueStrategy : OutgoingQueueStrategy {

		private const string Tag = "DefaultOutgoingQueueStrategy: ";

		private OutgoingQueueState state;

		private OutgoingQueueRestRetryLooper retryLooper;

		private QueueEntryInstruction messageWaitingAck = null;

		private OutgoingQueueState stoppedState;
		private OutgoingQueueState sendingState;

		private OutgoingQueue outgoingQueue;
		protected OutgoingQueue OutgoingQueue {
			get { return outgoingQueue; }
			set { outgoingQueue = value; }
		}

		public DefaultOutgoingQueueStrategy (OutgoingQueue queue, OutgoingQueueDao queueDao) {
			this.OutgoingQueue = queue;
			this.retryLooper = new OutgoingQueueRestRetryLooper (queue, queueDao);
			this.stoppedState = new StoppedQueueState (this, queueDao);
			this.sendingState = new SendQueueState (this, queueDao);

			this.state = stoppedState;
		}

		/**
		 * field getters/setters
		 */

		public OutgoingQueueRestRetryLooper RetryLooper {
			get {
				return retryLooper;
			}
		}

		public QueueEntryInstruction MessageWaitingAck {
			get {
				return messageWaitingAck;
			}
			set {
				messageWaitingAck = value;
			}
		}
			
		/**
		 * OutgoingQueueStrategy Implementations
		 */

		public void Enqueue (QueueInstruction instruction) {
			ProcessQueueInstructionAsync (instruction);
		}

		public void Send (QueueEntryInstruction instruction) {
			QueueEntry entry = instruction.QueueEntry;
			Action<EMHttpResponse> callback = instruction.Callback;
			this.OutgoingQueue.Send (entry, callback);
		}

		public void Start () {
			Debug.WriteLine (Tag + "Start ()");
			Enqueue (new StateUpdateQueueInstruction (this.sendingState));
		}

		public void Stop () {
			Debug.WriteLine (Tag + "Stop ()");
			Enqueue (new StateUpdateQueueInstruction (this.stoppedState));
		}

		/**
		 * these methods are thread-safe if run from the queueConsumer handler
		 */

		private void ProcessQueueEntryInstruction (QueueEntryInstruction instruction) {
			this.state.ProcessQueueEntryInstruction (instruction);
		}

		private void UpdateState (OutgoingQueueState newState) {
			if (this.state != null) {
				this.state.End ();
			}

			if (newState == null) {
				throw new Exception ("new state cannot be null");
			}

			this.state = newState;
			this.state.Begin ();
		}

		private void Event (QueueEntryEvent queueEntryEvent) {
			this.state.Event (queueEntryEvent);
		}

		private void ProcessQueueInstructionAsync (QueueInstruction instruction) {
			EMTask.Dispatch (() => {
				ProcessQueueInstruction (instruction);
			}, EMTask.OUTGOING_QUEUE_QUEUE);
		}

		private void ProcessQueueInstruction (QueueInstruction instruction) {
			if (instruction is StateUpdateQueueInstruction) {
				StateUpdateQueueInstruction update = (StateUpdateQueueInstruction) instruction;
				UpdateState (update.NewState);
			} else if (instruction is ActionQueueInstruction) {
				ActionQueueInstruction action = (ActionQueueInstruction) instruction;
				action.Execute ();
			} else if (instruction is QueueEntryEvent) {
				QueueEntryEvent entryEvent = (QueueEntryEvent) instruction;
				Event (entryEvent);
			} else if (instruction is QueueEntryInstruction) {
				QueueEntryInstruction entryInstruction = (QueueEntryInstruction) instruction;
				ProcessQueueEntryInstruction (entryInstruction);
			} else {
				Debug.WriteLine (String.Format ("{0} Processing: unknown instruction type " + instruction.GetType () + ". Dropping.", state));
			}
		}
	}
}