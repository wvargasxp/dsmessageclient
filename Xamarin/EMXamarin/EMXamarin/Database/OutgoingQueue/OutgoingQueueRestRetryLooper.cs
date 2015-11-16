using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace em {
	public class OutgoingQueueRestRetryLooper {
		private const string Tag = "OutgoingQueueRetryLooper: ";
		private Boolean enabled = false;

		private RetryLooperStrategy strategy = null;
		private OutgoingQueue outgoingQueue = null;
		private OutgoingQueueDao queueDao = null;

		private Timer retryTimer = null;

		public OutgoingQueueRestRetryLooper (OutgoingQueue outgoingQueue, OutgoingQueueDao queueDao) {
			Debug.Assert (outgoingQueue != null, Tag + "Expected outgoingQueue to be non-null");
			Debug.Assert (queueDao != null, Tag + "Expected queueDdao to be non-null");
			this.outgoingQueue = outgoingQueue;
			this.queueDao = queueDao;
		}

		public void Fire () {
			EMTask.Dispatch (() => {
				TimeTask ();
			}, EMTask.OUTGOING_RETRY_QUEUE);
		}

		public void Enable () {
			EMTask.Dispatch (() => {
				EnableTask ();
			}, EMTask.OUTGOING_RETRY_QUEUE);
		}

		public void Disable () {
			EMTask.Dispatch (() => {
				DisableTask ();
			}, EMTask.OUTGOING_RETRY_QUEUE);
		}

		/**
		 * these methods are thread-safe if run from the retryConsumer handler
		 */
		private void TimeTask () {
			if (!this.enabled) {
				return;
			}

			if (this.retryTimer != null) {
				// do nothing if a timer is already in effect
				return;
			}

			if (this.strategy == null) {
				this.strategy = new FixedIntervalsRetryLooperStrategy (Constants.FIXED_DURATIONS_FOR_CHAT_MESSAGE_RETRY);
			}

			int interval = this.strategy.NextInterval;

			Debug.WriteLine (String.Format ("{0}waiting {1}ms", Tag, interval));

			this.retryTimer = new Timer (o => {
				EMTask.Dispatch (() => {
					try {
						RetryTask ();
					} catch (Exception e) {
						Debug.WriteLine("{0}Failed trying to send messages requiring re-upload {1}", Tag, e.ToString());
					}
				}, EMTask.OUTGOING_RETRY_QUEUE);
			}, null, interval, Timeout.Infinite);
		}

		private void RetryTask () {
			if (!this.enabled) {
				return;
			}

			if (this.retryTimer != null) {
				this.retryTimer.Dispose ();
				this.retryTimer = null;
			}

			IList<QueueEntry> reuploads = this.queueDao.FindUploadPendingQueueEntries ();

			if (reuploads.Count == 0) {
				// When we don't have any entries we need to re-upload, it is safe to assume that the interval should be the lowest possible.
				this.strategy.Reset ();
				return;
			} else {
				Debug.WriteLine (String.Format ("{0}there are {1} entries requiring re-upload", Tag, reuploads.Count));
			}

			foreach (QueueEntry queueEntry in reuploads) {
				this.outgoingQueue.RetrySend (queueEntry, RetryResponse);
			}
		}

		private void EnableTask () {
			this.enabled = true;
		}

		private void DisableTask () {
			this.enabled = false;

			if (this.retryTimer != null) {
				this.retryTimer.Dispose ();
				this.retryTimer = null;
			}

			this.queueDao.MarkReuploadingRestQueueEntriesAsUploadPending ();
		}

		private void RetryResponse (EMHttpResponse response) {
			EMTask.Dispatch (() => {
				if (response != null) {
					bool success = response.IsSuccess;
					if (success) {
						// When we get a successful response back from the server, we reset the interval back to the beginning.
						this.strategy.Reset ();
					}
				}
			}, EMTask.OUTGOING_RETRY_QUEUE);	
		}
	}

	public interface RetryLooperStrategy {
		int NextInterval { get; }
		void Reset ();
	}

	public class FixedIntervalsRetryLooperStrategy : RetryLooperStrategy {
		private int index = 0;
		private int[] intervals;

		public FixedIntervalsRetryLooperStrategy (int [] intervals) {
			this.intervals = intervals;
		}

		public int NextInterval {
			get {
				int next = this.intervals [index];

				if (index < this.intervals.Length - 1) {
					index++;
				}

				return next;
			}
		}

		public void Reset () {
			//Debug.WriteLine ("FixedIntervalsRetryLooperStrategy: Resetting Interval.");
			index = 0;
		}
	}
}