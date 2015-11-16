using System;
using System.Threading;
using System.Diagnostics;
using em;
using Foundation;
using UIKit;

namespace iOS {
	public class KeepAliveManager {

		private static KeepAliveManager _shared = null;
		public static KeepAliveManager Shared {
			get {
				if (_shared == null) {
					_shared = new KeepAliveManager ();
				}

				return _shared;
			}
		}

		private const int START_COUNT = 1;

		private CountdownEvent _countDown = null;
		public CountdownEvent CountDown {
			get {
				if (this._countDown == null) {
					this._countDown = new CountdownEvent (START_COUNT);
				}

				return _countDown;
			}
		}

		public KeepAliveManager () {
			NSNotificationCenter.DefaultCenter.AddObserver ((NSString)em.Constants.DID_ENTER_BACKGROUND, HandleDidEnterBackgroundNotification);
		}

		public void Add () {
			this.CountDown.AddCount ();
		}

		public void Remove () {
			this.CountDown.Signal ();
		}

		#region keep app alive while in background
		private nint TaskId { get; set; }

		private void HandleDidEnterBackgroundNotification (NSNotification notification) {
			KeepRequestsAlive ();

			EMTask.Dispatch (() => {

				// We started with a number, so when we hit this event, we need to signal it down.
				// The reason is because we can't start our CountDownEvent with a number of 0.
				for (int i = 0; i < START_COUNT; i++) {
					this.CountDown.Signal ();
				}

				// CountDown waits until it reaches 0 before it unblocks.
				this.CountDown.Wait ();
				this.CountDown.Reset ();
				FinishKeepingRequestsAlive ();
			}, EMTask.KEEP_BACKGROUND_TASK_QUEUE); // put in its own queue as to not block other potential tasks running (that could potentially increment the countdown while it's waiting).

		}

		private void KeepRequestsAlive () {
			this.TaskId = UIApplication.SharedApplication.BeginBackgroundTask (() => {
				// App is going to be terminated, kill the task.
				FinishKeepingRequestsAlive ();
			});
		}

		private void FinishKeepingRequestsAlive () {
			if (this.TaskId != UIApplication.BackgroundTaskInvalid) {
				if (UIApplication.SharedApplication != null) {
					UIApplication.SharedApplication.EndBackgroundTask (this.TaskId);
					this.TaskId = UIApplication.BackgroundTaskInvalid;
				}
			}
		}
		#endregion
	}
}

