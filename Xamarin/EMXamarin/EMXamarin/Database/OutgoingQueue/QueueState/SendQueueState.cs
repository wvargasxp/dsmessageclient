using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace em {
	public class SendQueueState : OutgoingQueueState {
		
		private const string Tag = "SendQueueState: ";
		private DefaultOutgoingQueueStrategy strategy;
		private OutgoingQueueDao queueDao;

		private QueueEntryInstruction dummyInstruction = null;

		public SendQueueState (DefaultOutgoingQueueStrategy strategy, OutgoingQueueDao queueDao) {
			this.strategy = strategy;
			this.queueDao = queueDao;

			QueueEntry dummyEntry = new QueueEntry ();
			dummyEntry.messageID = -1;
			dummyEntry.entryState = QueueEntryState.Sending;
			dummyEntry.destination = "/fakedestination";
			this.dummyInstruction = new QueueEntryInstruction (dummyEntry, null);
		}

		/**
		 * OutgoingQueueState implementations
		 */

		public void Begin () {
			Debug.WriteLine (Tag + "Begin ()");
			this.strategy.RetryLooper.Enable ();
			this.strategy.RetryLooper.Fire (); // re-send any pending re-uploads now

			this.strategy.Enqueue (dummyInstruction); // enqueue dummy instruction to kickstart sending
		}

		public void End () {
			Debug.WriteLine (Tag + "End ()");
			this.strategy.MessageWaitingAck = null;

			this.strategy.RetryLooper.Disable ();
		}

		public void ProcessQueueEntryInstruction (QueueEntryInstruction instruction) {
			QueueEntryInstruction instructionToSend = null;
			QueueRoute route = instruction.QueueEntry.route;

			if (route == QueueRoute.VideoEncoding) {
				instructionToSend = instruction;
			} else {
				if (instruction.Callback != null) {
					// immediately send out messages that have callbacks, since callbacks cannot be persisted
					instructionToSend = instruction;
				} else if (instruction.QueueEntry.entryState == QueueEntryState.Reuploading) {
					// immediately send message if it is a re-upload
					instructionToSend = instruction;
				} else if (instruction.QueueEntry.destination.Equals (StompPath.kSendMessageUpdate)) {
					// immediately send message status updates
					instructionToSend = instruction;
				} else if (strategy.MessageWaitingAck == null) {
					// dequeue next message if we're not waiting on an ack
					//Debug.WriteLine (String.Format ("ProcessQueueEntryInstruction : {0} not waiting for ack, so we poll the dao for the next pending entry", instruction.QueueEntry.messageID));
					QueueEntry queueEntry = this.queueDao.FindOldestPendingNonEncodingQueueEntry ();
					if (queueEntry == null) {
						//Debug.WriteLine (String.Format ("ProcessQueueEntryInstruction : {0} dao has zero (0) pending entries. Aborting", instruction.QueueEntry.messageID));
						return;
					}

					instructionToSend = new QueueEntryInstruction (queueEntry, null);

					switch (instructionToSend.QueueEntry.route) {
					case QueueRoute.Websocket:
						//Debug.WriteLine (String.Format ("ProcessQueueEntryInstruction : {0} sending entry {1} and waiting for its ACK", instruction.QueueEntry.messageID, queueEntry.messageID));
						this.strategy.MessageWaitingAck = instructionToSend;
						break;
					case QueueRoute.Rest:
						//Debug.WriteLine (String.Format ("ProcessQueueEntryInstruction : {0} sending entry {1} BUT not waiting for its ACK", instruction.QueueEntry.messageID, queueEntry.messageID));
						break;
					case QueueRoute.VideoEncoding:
						instructionToSend = null; // shouldn't reach here / shouldn't handle 
						break;
					}
				} else if (EqualsMessageWaitingAck (instruction.QueueEntry)) {
					// retry send
					instructionToSend = this.strategy.MessageWaitingAck;
				}
			}

			if (instructionToSend != null) {
				QueueEntry entryToSend = instructionToSend.QueueEntry;

				switch (entryToSend.route) {
				case QueueRoute.Websocket:
					entryToSend.entryState = QueueEntryState.Sending;
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
			case QueueRoute.Websocket:
				switch (queueEntryEvent.EventType) {
				case QueueEntryEventType.Removed:
				case QueueEntryEventType.Acked:
					if (EqualsMessageWaitingAck (queueEntry)) {
						//Debug.WriteLine (String.Format ("clearing message requiring ACK for received event {0} for message with id {1}", queueEntryEvent.EventType, queueEntryEvent.QueueEntry.messageID));
						this.strategy.MessageWaitingAck = null;
						this.strategy.Enqueue (dummyInstruction);
					}
					break;
				case QueueEntryEventType.Failed:
					if (EqualsMessageWaitingAck (queueEntry)) {
						//Debug.WriteLine (String.Format ("queueing up retry for message requiring ACK for received event {0} for message with id {1}", queueEntryEvent.EventType, queueEntryEvent.QueueEntry.messageID));
						this.strategy.Enqueue (new QueueEntryInstruction (queueEntryEvent.QueueEntry, null));
					}
					break;
				}
				break;
			case QueueRoute.Rest:
				switch (queueEntryEvent.EventType) {
				case QueueEntryEventType.Removed:
				case QueueEntryEventType.Acked:
				case QueueEntryEventType.Failed:
					this.strategy.RetryLooper.Fire ();
					break;
				}
				break;
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
			
		private bool EqualsMessageWaitingAck (QueueEntry queueEntry) {
			QueueEntryInstruction messageAwaitingAck = this.strategy.MessageWaitingAck;
			if (messageAwaitingAck == null) {
				return false;
			}

			// Can QueueEntry ever be null?
			int messageAwaitingAckMessageId = messageAwaitingAck.QueueEntry.messageID;
			if (!messageAwaitingAckMessageId.Equals (queueEntry.messageID)) {
				return false;
			}

			return true;
		}
	}
}

