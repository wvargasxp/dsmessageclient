using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace em {
	public class TimedQueueExecutor {

		private Queue<TimedTask> taskQueue = new Queue<TimedTask> ();
		private Timer currentTaskTimer;
		private bool isRunning = false;

		private Object executorLock = new Object ();


		public TimedQueueExecutor () {

		}

		public void Schedule (int delay, Action task) {
			EMTask.DispatchBackground (() => {
				lock (executorLock) {
					taskQueue.Enqueue (new TimedTask (delay, task));
				}

				RunTasksAsync ();
			});
		}

		private void RunTasksAsync () {
			EMTask.DispatchBackground (() => {
				TimedTask task = null;

				lock (executorLock) {
					if (isRunning) {
						return;
					}

					if (taskQueue.Peek () == null) {
						return;
					}

					task = taskQueue.Dequeue ();

					isRunning = true;
				}

				if (currentTaskTimer != null) {
					currentTaskTimer.Dispose ();
				}

				currentTaskTimer = new Timer (o => {
					try {
						Debug.WriteLine ("START TimedQueueExecutor:RunTasksAsync " + task.Action);
						task.Action ();
						Debug.WriteLine ("END TimedQueueExecutor:RunTasksAsync" + task.Action);
					} catch (Exception e) {
						Debug.WriteLine ("TimedQueueExecutor:RunTasksAsync failed attempting to run timed task " + task.Action + " : " + e);
					} finally {
						isRunning = false;

						RunTasksAsync ();
					}
				}, null, task.Delay, Timeout.Infinite);
			});
		}

		private class TimedTask {
			private int delay;
			private Action action;

			public int Delay {
				get {
					return delay;
				}
			}
			public Action Action {
				get {
					return action;
				}
			}

			public TimedTask (int delay, Action action) {
				this.delay = delay;
				this.action = action;
			}
		}
	}
}

