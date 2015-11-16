using System;
using System.Collections.Generic;
using System.Threading;

namespace em {
	public static class EMTask {
		public static readonly string MAIN_QUEUE = "em_main_queue";
		public static readonly string BACKGROUND_QUEUE = "em_global_background_queue";
		public static readonly string HTTP_UPLOAD_QUEUE = "em_http.upload.queue";

		public static readonly string DOWNLOAD_QUEUE = "em_download_queue";

		public static readonly string HANDLE_MESSAGE_QUEUE = "handle.message.queue";
		public static readonly string LIVE_SERVER_QUEUE = "live.server.queue";
		public static readonly string OUTGOING_QUEUE_QUEUE = "outgoing.queue.queue";
		public static readonly string OUTGOING_RETRY_QUEUE = "outgoing.retry.queue";
		public static readonly string LOGIN_QUEUE = "app.login.queue";
		public static readonly string WEBSOCKET_CLIENT_QUEUE = "web.socket.client.queue";
		public static readonly string WEBSOCKET_RECEIVE_MESSAGE_QUEUE = "web.socket.receive.message.queue";
		public static readonly string WEBSOCKET_PINGPONG_QUEUE ="web.socket.ping.pong.timeout.queue";
		public static readonly string HTTP_REQUEST_QUEUE = "em.http.request.queue";
		public static readonly string KEEP_BACKGROUND_TASK_QUEUE = "em.keep.alive.manager.queue";
		public static readonly string VIDEO_ENCODING = "em.encoding.queue";

		public static void DispatchBackground (Action action) {
			Dispatch (action, BACKGROUND_QUEUE);
		}

		public static void PerformOnMain (Action action) {
			if (ApplicationModel.SharedPlatform.OnMainThread )
				action ();
			else
				DispatchMain (action, null);
		}

		public static void DispatchMain (Action action) {
			DispatchMain (action, null);
		}

		static Dictionary<Thread,int> tasksForkedToMain = new Dictionary<Thread,int>();
		public static void PrepareToWaitForMainThreadCallbacks() {
			lock (tasksForkedToMain) {
				tasksForkedToMain [Thread.CurrentThread] = 0;
			}
		}

		public static void WaitForMainThreadCallbacks() {
			lock (tasksForkedToMain) {
				Thread t = Thread.CurrentThread;
				int? tasksInProgress = null;
				if (tasksForkedToMain.ContainsKey (t))
					tasksInProgress = tasksForkedToMain [t];
				while (tasksInProgress != null && tasksInProgress > 0) {
					Monitor.Wait (tasksForkedToMain, 3000);
					if (tasksForkedToMain.ContainsKey (t))
						tasksInProgress = tasksForkedToMain [t];
					else
						tasksInProgress = null;
				}

				tasksForkedToMain.Remove (t);
			}
		}

		/** Method that blocks on the main thread if already on the main
		 * thread otherwise it dispatches to the background
		 */
		public static void ExecuteNowIfMainOrDispatchMain(Action action) {
			if (ApplicationModel.SharedPlatform.OnMainThread)
				action ();
			else
				DispatchMain (action);
		}

		public static void DispatchMain (Action action, Func<bool> okayToContinue) {
			lock (tasksForkedToMain) {
				Thread t = Thread.CurrentThread;
				bool trackingCompletion = tasksForkedToMain.ContainsKey (t);
				if (trackingCompletion) {
					int tasksInProgress = tasksForkedToMain [t];
					tasksForkedToMain [t] = tasksInProgress + 1;
					Action underlying = action;
					action = () => {
						try {
							underlying();
						}
						finally {
							lock (tasksForkedToMain) {
								if ( tasksForkedToMain.ContainsKey (t)) {
									int tasks = tasksForkedToMain [t];
									tasksForkedToMain [t] = tasks - 1;

									Monitor.PulseAll(tasksForkedToMain);
								}
							}
						}
					};
				}
				ApplicationModel.SharedPlatform.RunOnMainThread (action, okayToContinue);
			}
		}

		public static void Dispatch (Action action, string queue = null) {
			ApplicationModel.SharedPlatform.RunOnBackgroundQueue (action, queue);
		}
	}
}