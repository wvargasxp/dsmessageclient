using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace em {
	public class StoppedQueueState : OutgoingQueueState {

		private const string Tag = "StoppedQueueState: ";
		private DefaultOutgoingQueueStrategy strategy;
		private OutgoingQueueDao queueDao;

		public StoppedQueueState (DefaultOutgoingQueueStrategy strategy, OutgoingQueueDao queueDao) {
			this.strategy = strategy;
			this.queueDao = queueDao;
		}

		/**
		 * OutgoingQueueState implementations
		 */

		public void Begin () {
			Debug.WriteLine (Tag + "Begin ()");
		}

		public void End () {
			Debug.WriteLine (Tag + "End ()");
		}

		public void ProcessQueueEntryInstruction (QueueEntryInstruction instruction) {

			QueueEntry entryToSend = instruction.QueueEntry;

			QueueEntryInstruction instructionToSend = null;
			QueueRoute route = instruction.QueueEntry.route;

			if (route == QueueRoute.VideoEncoding) {
				instructionToSend = instruction;
			} else {
				// Immediately send out messages that have callbacks, since callbacks cannot be persisted, even if the live server connection is down.
				// Or if the entry is for Encoding, send it out anyway since we don't need the live server connection to be up to do encoding.
				if (instruction.Callback != null) {
					instructionToSend = instruction;
				}
			}

			if (instructionToSend == null) {
				// We have nothing to send, check if we need to update the queueEntry's entry state.
				switch (entryToSend.route) {
				case QueueRoute.Rest: 
					{
						// In this case, mark it as UploadPending.
						entryToSend.entryState = QueueEntryState.UploadPending;
						this.queueDao.UpdateStatus (entryToSend);
						break;
					}
				default:
					{
						break;
					}
				}
			} else {
				switch (entryToSend.route) {
				case QueueRoute.Websocket:
					break;
				case QueueRoute.Rest:
					entryToSend.entryState = QueueEntryState.Reuploading;
					break;
				case QueueRoute.VideoEncoding:
					entryToSend.entryState = QueueEntryState.Encoding;
					break;
				}

				this.queueDao.UpdateStatus (entryToSend);
				this.strategy.Send (instructionToSend);
			}
		}
			
		public void Event (QueueEntryEvent queueEntryEvent) {
			//Debug.WriteLine (String.Format ("received event {0} for message with id {1}", queueEntryEvent.EventType, queueEntryEvent.QueueEntry.messageID));
			QueueEntry queueEntry = queueEntryEvent.QueueEntry;
			switch (queueEntry.route) {
			case QueueRoute.VideoEncoding:
				{
					switch (queueEntryEvent.EventType) {
					case QueueEntryEventType.EndEncoding:
						{
							// After we finish encoding a video, set the queue entry's route to Rest so that it can be sent out through the Rest channel.
							queueEntry.route = QueueRoute.Rest;
							queueEntry.entryState = QueueEntryState.UploadPending;
							this.queueDao.UpdateRouteAndStatus (queueEntry);

							// Now that are queue entry's contents have been encoded and set to Rest route, fire the retry looper so that our message is sent out.
							this.strategy.RetryLooper.Fire (); 

							break;
						}
					}

					break;
				}
			}
		}
	}
}