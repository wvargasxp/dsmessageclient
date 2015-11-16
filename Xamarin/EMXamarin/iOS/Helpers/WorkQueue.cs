using System;
using System.Collections.Generic;
using em;
using EMXamarin;

namespace iOS {
	public class WorkQueue {
		Queue<Work> works = new Queue<Work> ();
		Work currentWork = null;

		public void Add (Work work) {
			EMTask.DispatchMain (() => {
				works.Enqueue (work);
				DoNextWork ();
			});
		}

		private void DoNextWork () {
			if (currentWork == null && works.Count > 0) {
				this.currentWork = works.Dequeue ();
				currentWork.Do ();
			}
		}

		public void Done () {
			EMTask.DispatchMain (() => {
				this.currentWork.Done ();

				this.currentWork = null;

				DoNextWork ();
			});
		}
	}

	public interface Work {
		string Id { get; set; }
		void Do ();
		void Done ();
	}
}

